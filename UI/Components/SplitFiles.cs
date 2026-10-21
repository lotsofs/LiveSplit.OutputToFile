using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes CurrentSplit\ and PreviousSplit\: names, indexes, times, differences and Sign.txt.
	/// </summary>
	class SplitFiles : OutputWriter {

		const string FILE_CURRENT_SEGMENT_TIME = @"CurrentSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_CURRENT_GOLD_TIME = @"CurrentSplit\{1}\{0}\GoldTime.txt";
		const string FILE_CURRENT_RUN_TIME = @"CurrentSplit\{1}\{0}\RunTime.txt";
		const string FILE_CURRENT_NAME = @"CurrentSplit\{1}\Name.txt";
		const string FILE_CURRENT_NAME_RAW = @"CurrentSplit\{1}\Name_Raw.txt";
		const string FILE_CURRENT_NUMBER = @"CurrentSplit\{1}\Number.txt";
		const string FILE_CURRENT_COMPLETED = @"CurrentSplit\{1}\Completed.txt";
		const string FILE_CURRENT_REMAINING = @"CurrentSplit\{1}\Remaining.txt";
		const string FILE_CURRENT_AFTER = @"CurrentSplit\{1}\After.txt";

		const string FILE_PREVIOUS_SIGN = @"PreviousSplit\{1}\{0}\Sign.txt";
		const string FILE_PREVIOUS_SEGMENT_TIME = @"PreviousSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_PREVIOUS_RUN_TIME = @"PreviousSplit\{1}\{0}\RunTime.txt";
		const string FILE_PREVIOUS_SEGMENT_DIFFERENCE = @"PreviousSplit\{1}\{0}\SegmentDifference.txt";
		const string FILE_PREVIOUS_GOLD_DIFFERENCE = @"PreviousSplit\{1}\{0}\GoldDifference.txt";
		const string FILE_PREVIOUS_RUN_DIFFERENCE = @"PreviousSplit\{1}\{0}\RunDifference.txt";
		const string FILE_PREVIOUS_NAME = @"PreviousSplit\{1}\Name.txt";
		const string FILE_PREVIOUS_NAME_RAW = @"PreviousSplit\{1}\Name_Raw.txt";

		public SplitFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		/// <summary>
		/// Write non-timing related information about a split
		/// </summary>
		/// <param name="state"></param>
		public void WriteSplitInformation() {
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				SplitLevel level = Levels[lvl];
				WriteCurrentSplitInformation(lvl);
				WritePreviousSplitInformation(lvl);
			}
		}

		void WriteCurrentSplitInformation(int lvl) {
			IRun run = State.Run;
			int count = Levels[lvl].Indices.Count;

			// Members completed, skipped ones included: none before the start, all after the finish
			int completed = GetPreviousSubsplitIndex(lvl) + 1;
			MakeFile(FILE_CURRENT_COMPLETED, completed.ToString(), null, lvl);
			MakeFile(FILE_CURRENT_REMAINING, (count - completed).ToString(), null, lvl);

			int currLocalSplitIndex = GetCurrentSubsplitIndex(lvl);
			if (currLocalSplitIndex == -1) {
				// No current split: the timer isn't running, or the run is finished
				MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
				MakeFile(FILE_CURRENT_NAME_RAW, "-", null, lvl);
				MakeFile(FILE_CURRENT_NUMBER, "-", null, lvl);
				MakeFile(FILE_CURRENT_AFTER, "-", null, lvl);
				return;
			}

			int currGlobalSplitIndex = Levels[lvl].Indices[currLocalSplitIndex];
			MakeFile(FILE_CURRENT_NAME_RAW, run[currGlobalSplitIndex].Name, null, lvl);
			MakeFile(FILE_CURRENT_NAME, Levels.Names[currGlobalSplitIndex][lvl], null, lvl);
			MakeFile(FILE_CURRENT_NUMBER, (currLocalSplitIndex + 1).ToString(), null, lvl);
			MakeFile(FILE_CURRENT_AFTER, (count - currLocalSplitIndex - 1).ToString(), null, lvl);
		}

		void WritePreviousSplitInformation(int lvl) {
			IRun run = State.Run;

			if (State.CurrentPhase == TimerPhase.NotRunning) {
				MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
				MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
				return;
			}
			if (State.CurrentPhase == TimerPhase.Ended) {
				MakeFile(FILE_PREVIOUS_NAME_RAW, run[run.Count - 1].Name, null, lvl);
				MakeFile(FILE_PREVIOUS_NAME, Levels.Names[run.Count - 1][lvl], null, lvl);
				return;
			}

			int prevSplitIndex = GetPreviousSubsplitIndex(lvl);
			if (prevSplitIndex == -1) {
				MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
				MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
				return;
			}
			int prevGlobalSplitIndex = Levels[lvl].Indices[prevSplitIndex];
			MakeFile(FILE_PREVIOUS_NAME_RAW, run[prevGlobalSplitIndex].Name, null, lvl);
			MakeFile(FILE_PREVIOUS_NAME, Levels.Names[prevGlobalSplitIndex][lvl], null, lvl);
		}

		public void WriteSplitTimes(TimingMethod method) {
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				WritePreviousSplitTimes(method, lvl);
				WriteCurrentSplitTimes(method, lvl);
			}
		}

		void WritePreviousSplitTimes(TimingMethod method, int lvl) {
			int previousSplitIndex = GetPreviousSubsplitIndex(lvl);
			if (previousSplitIndex == -1) {
				// No previous member: the timer isn't running, or the level's first member is current
				MakeFile(FILE_PREVIOUS_SEGMENT_TIME, "-", method, lvl);
				MakeFile(FILE_PREVIOUS_RUN_TIME, "-", method, lvl);
				MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, "-", method, lvl);
				MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, "-", method, lvl);
				if (lvl == Levels.Count - 1) MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, "-", method, lvl);
				MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl);
				return;
			}

			TimeSpan? segmentTime = LiveSegmentTime(method, lvl, previousSplitIndex);
			TimeSpan? runTime = LiveRunTime(method, lvl, previousSplitIndex);
			TimeSpan? goldDelta = GoldDelta(method, lvl, previousSplitIndex);
			TimeSpan? segmentDelta = SegmentDelta(method, lvl, previousSplitIndex);
			TimeSpan? runDelta = RunDelta(method, lvl, previousSplitIndex);
			bool isGold = IsGold(method, lvl, previousSplitIndex);

			MakeFile(FILE_PREVIOUS_SEGMENT_TIME, TimeFormatter.DurationString(segmentTime), method, lvl);
			MakeFile(FILE_PREVIOUS_RUN_TIME, TimeFormatter.DurationString(runTime), method, lvl);
			MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, TimeFormatter.DeltaString(segmentDelta), method, lvl);
			MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, TimeFormatter.DeltaString(runDelta), method, lvl);
			// Golds are not supported for subsplits, so do not even bother writing a '-' for those levels
			if (lvl == Levels.Count - 1) {
				MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, TimeFormatter.DeltaString(goldDelta), method, lvl);
			}
			MakeFile(FILE_PREVIOUS_SIGN, PreviousSign(isGold, runDelta, segmentDelta).ToString(), method, lvl);
		}

		/// <summary>
		/// Sign.txt for the previous split: the first rule that applies wins. "Ahead" and "PB" mean the run delta is negative.
		/// </summary>
		Signs PreviousSign(bool isGold, TimeSpan? runDelta, TimeSpan? segmentDelta) {
			bool ahead = runDelta < TimeSpan.Zero;
			if (State.CurrentPhase == TimerPhase.Ended) {
				if (isGold) return ahead ? Signs.PBGold : Signs.NoPBGold;
				if (segmentDelta == null) return ahead ? Signs.PB : Signs.NoPB;
				if (segmentDelta < TimeSpan.Zero) return ahead ? Signs.PBGained : Signs.NoPBGained;
				return ahead ? Signs.PBLost : Signs.NoPBLost;
			}
			if (runDelta == null) return Signs.NotApplicable;
			if (isGold) return ahead ? Signs.GoldAhead : Signs.GoldBehind;
			if (segmentDelta == null) return ahead ? Signs.Ahead : Signs.Behind;
			if (segmentDelta < TimeSpan.Zero) return ahead ? Signs.GainedAhead : Signs.GainedBehind;
			return ahead ? Signs.LostAhead : Signs.LostBehind;
		}

		void WriteCurrentSplitTimes(TimingMethod method, int lvl) {
			SplitLevel level = Levels[lvl];

			if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended) {
				MakeFile(FILE_CURRENT_SEGMENT_TIME, "-", method, lvl);
				MakeFile(FILE_CURRENT_RUN_TIME, "-", method, lvl);
				if (lvl == Levels.Count - 1) MakeFile(FILE_CURRENT_GOLD_TIME, "-", method, lvl);
				return;
			}

			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
			int currentAllSplitsIndex = Levels[lvl].Indices[currentSubsplitIndex];
			TimeSpan? curSegTime = level.PbSegmentTimes[method][currentSubsplitIndex];
			TimeSpan? curRunTime = State.Run[currentAllSplitsIndex].PersonalBestSplitTime[method];
			MakeFile(FILE_CURRENT_SEGMENT_TIME, TimeFormatter.DurationString(curSegTime), method, lvl);
			MakeFile(FILE_CURRENT_RUN_TIME, TimeFormatter.DurationString(curRunTime), method, lvl);
			if (lvl == Levels.Count - 1) {
				TimeSpan? curGoldTime = State.CurrentSplit.BestSegmentTime[method];
				MakeFile(FILE_CURRENT_GOLD_TIME, TimeFormatter.DurationString(curGoldTime), method, lvl);
			}
		}
	}
}
