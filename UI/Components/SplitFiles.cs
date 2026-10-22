using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes CurrentSplit\ and PreviousSplit\: names, indexes, times, differences and Sign.txt.
	/// </summary>
	class SplitFiles : OutputWriter {

		// {3} is the comparison's folder: these files are written once for each of Levels.Comparisons
		const string FILE_CURRENT_SEGMENT_TIME = @"CurrentSplit\{1}\{0}\{3}\SegmentTime.txt";
		const string FILE_CURRENT_GOLD_TIME = @"CurrentSplit\{1}\{0}\GoldTime.txt";
		const string FILE_CURRENT_RUN_TIME = @"CurrentSplit\{1}\{0}\{3}\RunTime.txt";
		const string FILE_CURRENT_NAME = @"CurrentSplit\{1}\Name.txt";
		const string FILE_CURRENT_NAME_RAW = @"CurrentSplit\{1}\Name_Raw.txt";
		const string FILE_CURRENT_NUMBER = @"CurrentSplit\{1}\Number.txt";
		const string FILE_CURRENT_COMPLETED = @"CurrentSplit\{1}\Completed.txt";
		const string FILE_CURRENT_REMAINING = @"CurrentSplit\{1}\Remaining.txt";
		const string FILE_CURRENT_AFTER = @"CurrentSplit\{1}\After.txt";

		const string FILE_PREVIOUS_SIGN = @"PreviousSplit\{1}\{0}\{3}\Sign.txt";
		const string FILE_PREVIOUS_SEGMENT_TIME = @"PreviousSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_PREVIOUS_RUN_TIME = @"PreviousSplit\{1}\{0}\RunTime.txt";
		const string FILE_PREVIOUS_SEGMENT_DIFFERENCE = @"PreviousSplit\{1}\{0}\{3}\SegmentDifference.txt";
		const string FILE_PREVIOUS_GOLD_DIFFERENCE = @"PreviousSplit\{1}\{0}\GoldDifference.txt";
		const string FILE_PREVIOUS_RUN_DIFFERENCE = @"PreviousSplit\{1}\{0}\{3}\RunDifference.txt";
		const string FILE_PREVIOUS_NAME = @"PreviousSplit\{1}\Name.txt";
		const string FILE_PREVIOUS_NAME_RAW = @"PreviousSplit\{1}\Name_Raw.txt";

		public SplitFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		/// <summary>
		/// Write non-timing related information about a split
		/// </summary>
		public void WriteSplitInformation() {
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
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
				MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, "-", method, lvl);
				foreach (var comparison in Levels.Comparisons) {
					MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, "-", method, lvl, comparison.Folder);
					MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, "-", method, lvl, comparison.Folder);
					MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl, comparison.Folder);
				}
				return;
			}

			TimeSpan? segmentTime = LiveSegmentTime(method, lvl, previousSplitIndex);
			TimeSpan? runTime = LiveRunTime(method, lvl, previousSplitIndex);
			TimeSpan? goldDelta = GoldDelta(method, lvl, previousSplitIndex);
			bool isGold = IsGold(method, lvl, previousSplitIndex);

			MakeFile(FILE_PREVIOUS_SEGMENT_TIME, DurationString(segmentTime), method, lvl);
			MakeFile(FILE_PREVIOUS_RUN_TIME, DurationString(runTime), method, lvl);
			MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, DeltaString(goldDelta), method, lvl);
			foreach (var comparison in Levels.Comparisons) {
				TimeSpan? segmentDelta = SegmentDelta(method, comparison.Name, lvl, previousSplitIndex);
				TimeSpan? runDelta = RunDelta(method, comparison.Name, lvl, previousSplitIndex);
				MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, DeltaString(segmentDelta), method, lvl, comparison.Folder);
				MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, DeltaString(runDelta), method, lvl, comparison.Folder);
				MakeFile(FILE_PREVIOUS_SIGN, PreviousSign(isGold, runDelta, segmentDelta).ToString(), method, lvl, comparison.Folder);
			}
		}

		/// <summary>
		/// Sign.txt for the previous split: the first rule that applies wins. "Ahead" means the run delta is negative.
		/// After the finish, the same value with Finished in front.
		/// </summary>
		Signs PreviousSign(bool isGold, TimeSpan? runDelta, TimeSpan? segmentDelta) {
			bool ahead = runDelta < TimeSpan.Zero;
			bool finished = State.CurrentPhase == TimerPhase.Ended;
			if (runDelta == null) return finished ? Signs.FinishedNotApplicable : Signs.NotApplicable;
			if (isGold) {
				if (finished) return ahead ? Signs.FinishedGoldAhead : Signs.FinishedGoldBehind;
				return ahead ? Signs.GoldAhead : Signs.GoldBehind;
			}
			if (segmentDelta == null) {
				if (finished) return ahead ? Signs.FinishedAhead : Signs.FinishedBehind;
				return ahead ? Signs.Ahead : Signs.Behind;
			}
			if (segmentDelta < TimeSpan.Zero) {
				if (finished) return ahead ? Signs.FinishedGainedAhead : Signs.FinishedGainedBehind;
				return ahead ? Signs.GainedAhead : Signs.GainedBehind;
			}
			if (finished) return ahead ? Signs.FinishedLostAhead : Signs.FinishedLostBehind;
			return ahead ? Signs.LostAhead : Signs.LostBehind;
		}

		void WriteCurrentSplitTimes(TimingMethod method, int lvl) {
			SplitLevel level = Levels[lvl];

			if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended) {
				MakeFile(FILE_CURRENT_GOLD_TIME, "-", method, lvl);
				foreach (var comparison in Levels.Comparisons) {
					MakeFile(FILE_CURRENT_SEGMENT_TIME, "-", method, lvl, comparison.Folder);
					MakeFile(FILE_CURRENT_RUN_TIME, "-", method, lvl, comparison.Folder);
				}
				return;
			}

			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
			TimeSpan? curGoldTime = level.BestSegmentTimes[method][currentSubsplitIndex];
			MakeFile(FILE_CURRENT_GOLD_TIME, DurationString(curGoldTime), method, lvl);
			foreach (var comparison in Levels.Comparisons) {
				TimeSpan? curSegTime = level.ComparisonSegmentTimes[comparison.Name][method][currentSubsplitIndex];
				TimeSpan? curRunTime = ComparisonTime(method, comparison.Name, lvl, currentSubsplitIndex);
				MakeFile(FILE_CURRENT_SEGMENT_TIME, DurationString(curSegTime), method, lvl, comparison.Folder);
				MakeFile(FILE_CURRENT_RUN_TIME, DurationString(curRunTime), method, lvl, comparison.Folder);
			}
		}
	}
}
