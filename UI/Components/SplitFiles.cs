using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes CurrentSplit\ and PreviousSplit\: names, indexes, times, differences and Sign.txt.
	/// </summary>
	class SplitFiles : OutputWriter {

		const string FILE_CURRENT_SEGMENT_TIME = @"CurrentSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_CURRENT_GOLD_TIME = @"CurrentSplit\{1}\{0}\GoldTime.txt";
		const string FILE_CURRENT_RUN_TIME = @"CurrentSplit\{1}\{0}\RunTime.txt";
		const string FILE_CURRENT_NAME = @"CurrentSplit\{1}\Name.txt";
		const string FILE_CURRENT_NAME_RAW = @"CurrentSplit\{1}\Name_Raw.txt";
		const string FILE_CURRENT_INDEX = @"CurrentSplit\{1}\Index.txt";
		const string FILE_CURRENT_REVERSEINDEX = @"CurrentSplit\{1}\ReverseIndex.txt";

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
				IRun run = State.Run;

				if (State.CurrentPhase == TimerPhase.NotRunning) {
					MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
					MakeFile(FILE_CURRENT_NAME_RAW, "-", null, lvl);
					MakeFile(FILE_CURRENT_INDEX, "-1", null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, Levels[lvl].Indices.Count.ToString(), null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
					continue;
				}
				if (State.CurrentPhase == TimerPhase.Ended) {
					MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
					MakeFile(FILE_CURRENT_NAME_RAW, "-", null, lvl);
					MakeFile(FILE_CURRENT_INDEX, Levels[lvl].Indices.Count.ToString(), null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, "-1", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, run[run.Count - 1].Name, null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, Levels.Names[run.Count - 1][lvl], null, lvl);
					continue;
				}

				int prevSplitIndex = GetPreviousSubsplitIndex(lvl);
				if (prevSplitIndex == -1) {
					MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
				}
				else {
					int prevGlobalSplitIndex = Levels[lvl].Indices[prevSplitIndex];
					MakeFile(FILE_PREVIOUS_NAME_RAW, run[prevGlobalSplitIndex].Name, null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, Levels.Names[prevGlobalSplitIndex][lvl], null, lvl);
				}
				int currLocalSplitIndex = GetCurrentSubsplitIndex(lvl);
				int currGlobalSplitIndex = Levels[lvl].Indices[currLocalSplitIndex];
				MakeFile(FILE_CURRENT_NAME_RAW, run[currGlobalSplitIndex].Name, null, lvl);
				MakeFile(FILE_CURRENT_NAME, Levels.Names[currGlobalSplitIndex][lvl], null, lvl);
				MakeFile(FILE_CURRENT_INDEX, currLocalSplitIndex.ToString(), null, lvl);
				MakeFile(FILE_CURRENT_REVERSEINDEX, (Levels[lvl].Indices.Count - currLocalSplitIndex - 1).ToString(), null, lvl);
			}
		}

		public void WriteSplitTimes(TimingMethod method) {
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				SplitLevel level = Levels[lvl];

				// Previous Split
				int previousSplitIndex = GetPreviousSubsplitIndex(lvl);
				if (previousSplitIndex != -1) {
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

					// Sign
					if (State.CurrentPhase == TimerPhase.NotRunning) {
						MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl);
					}
					else if (State.CurrentPhase == TimerPhase.Ended) {
						if (isGold) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGold : Signs.NoPBGold).ToString(), method, lvl); }
						else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PB : Signs.NoPB).ToString(), method, lvl); }
						else if (segmentDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGained : Signs.NoPBGained).ToString(), method, lvl); }
						else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBLost : Signs.NoPBLost).ToString(), method, lvl); }
					}
					else if (runDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl); }
					else if (isGold) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.GoldAhead : Signs.GoldBehind).ToString(), method, lvl); }
					else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.Ahead : Signs.Behind).ToString(), method, lvl); }
					else if (segmentDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.GainedAhead : Signs.GainedBehind).ToString(), method, lvl); }
					else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.LostAhead : Signs.LostBehind).ToString(), method, lvl); }
				}
				else {
					MakeFile(FILE_PREVIOUS_SEGMENT_TIME, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_RUN_TIME, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl);
				}


				// Current Split
				if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended) {
					MakeFile(FILE_CURRENT_SEGMENT_TIME, "-", method, lvl);
					MakeFile(FILE_CURRENT_RUN_TIME, "-", method, lvl);
					if (lvl == Levels.Count - 1) MakeFile(FILE_CURRENT_GOLD_TIME, "-", method, lvl);
				}
				else {
					int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
					int currentAllSplitsIndex = Levels[lvl].Indices[currentSubsplitIndex];
					TimeSpan? curSegTime = level.PbSegmentTimes[method][currentSubsplitIndex];
					TimeSpan? curRunTime = State.Run[currentAllSplitsIndex].PersonalBestSplitTime[method];
					MakeFile(FILE_CURRENT_SEGMENT_TIME, TimeFormatter.DurationString(curSegTime), method, lvl);
					MakeFile(FILE_CURRENT_RUN_TIME, TimeFormatter.DurationString(curRunTime), method, lvl);
					if (lvl == Levels.Count - 1) {
						TimeSpan? curGoldTime = State.CurrentSplit.BestSegmentTime[method];
						string curGoldTimeS = curGoldTime.HasValue ? TimeFormatter.DurationString(curGoldTime) : "-";
						MakeFile(FILE_CURRENT_GOLD_TIME, curGoldTimeS, method, lvl);
					}
				}
			}
		}
	}
}
