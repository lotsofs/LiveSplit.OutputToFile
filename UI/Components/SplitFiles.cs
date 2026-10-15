using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;
using System.Collections.Generic;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes CurrentSplit\ and PreviousSplit\: names, indexes, times, differences and Sign.txt.
	/// Also stores the previous split's deltas in the level arrays, for the split lists to read.
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

		public SplitFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels) : base(state, settings, levels) { }

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
					MakeFile(FILE_CURRENT_INDEX, "-1", null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, Levels[lvl].Indices.Count.ToString(), null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
					return;
				}
				if (State.CurrentPhase == TimerPhase.Ended) {
					MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
					MakeFile(FILE_CURRENT_INDEX, Levels[lvl].Indices.Count.ToString(), null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, "-1", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, run[run.Count - 1].Name, null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, Levels.Names[run.Count - 1][lvl], null, lvl);
					return;
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
					CalculateLiveSegment(State, method);
					TimeSpan? goldDelta = CalculatePreviousGoldDelta(State, method, lvl);
					TimeSpan? segmentDelta = CalculatePreviousSegmentDelta(State, method, lvl, previousSplitIndex, out TimeSpan? segmentTime);
					TimeSpan? runDelta = CalculatePreviousRunDelta(State, method, lvl, previousSplitIndex, out TimeSpan? runTime);

					if ((State.CurrentPhase != TimerPhase.NotRunning) && State.CurrentSplitIndex > 0) {
						level.GoldDeltae[method][previousSplitIndex] = goldDelta;
						level.SegmentDeltae[method][previousSplitIndex] = segmentDelta;
						level.RunDeltae[method][previousSplitIndex] = runDelta;
					}

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
						if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGold : Signs.NoPBGold).ToString(), method, lvl); }
						else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PB : Signs.NoPB).ToString(), method, lvl); }
						else if (segmentDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGained : Signs.NoPBGained).ToString(), method, lvl); }
						else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBLost : Signs.NoPBLost).ToString(), method, lvl); }
					}
					else if (runDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl); }
					else if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.GoldAhead : Signs.GoldBehind).ToString(), method, lvl); }
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

		/// <summary>
		/// Calculate the segment time of the previous split
		/// </summary>
		/// <param name="state"></param>
		/// <param name="method"></param>
		void CalculateLiveSegment(LiveSplitState state, TimingMethod method) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { return; }
			else if (state.CurrentSplitIndex == 0) { return; } // Run just started, no previous splits to calculate

			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				SplitLevel level = Levels[lvl];
				List<int> indices = level.Indices;
				int prevLocalIndex = indices.IndexOf(state.CurrentSplitIndex - 1);
				if (prevLocalIndex < 0) { continue; } // This isn't a subsplit or its the first subsplit for this level, no previous split to calculate
				if (state.CurrentPhase == TimerPhase.Ended) {
					// Run finished, there is only one split. Segment time == run time
					TimeSpan? ultimateFinishTime = state.Run[state.Run.Count - 1].SplitTime[method];
					if (indices.Count == 1) {
						level.LiveSegmentTimes[method][0] = ultimateFinishTime;
						continue;
					}
					// More than 1 split
					int penultimateSubsplitGlobalIndex = indices[indices.Count - 2];
					TimeSpan? penultimateFinishTime = state.Run[penultimateSubsplitGlobalIndex].SplitTime[method];
					if (penultimateFinishTime == null) {
						level.LiveSegmentTimes[method][level.LiveSegmentTimes[method].Length - 1] = null;
					}
					else {
						level.LiveSegmentTimes[method][level.LiveSegmentTimes[method].Length - 1] = ultimateFinishTime - penultimateFinishTime;
					}
				}
				else {
					// Mid-run split
					int prevGlobalIndex = indices[prevLocalIndex];
					TimeSpan? prevSegmentTime;
					if (prevLocalIndex == 0) {
						// First split
						prevSegmentTime = state.Run[prevGlobalIndex].SplitTime[method];
					}
					else {
						// Any other split
						int antePrevGlobalIndex = indices[prevLocalIndex - 1];
						TimeSpan? prevCompletionTime = state.Run[prevGlobalIndex].SplitTime[method];
						TimeSpan? antePrevCompletionTime = state.Run[antePrevGlobalIndex].SplitTime[method];
						if (prevCompletionTime == null || antePrevCompletionTime == null) {
							prevSegmentTime = null;
						}
						else {
							prevSegmentTime = state.Run[prevGlobalIndex].SplitTime[method] - state.Run[antePrevGlobalIndex].SplitTime[method];
						}
					}
					level.LiveSegmentTimes[method][prevLocalIndex] = prevSegmentTime;
				}
			}
		}

		TimeSpan? CalculatePreviousGoldDelta(LiveSplitState state, TimingMethod method, int lvl) {
			if (lvl != Levels.Count - 1) { return null; } // No gold splits are being kept (reliably) for multisplit segments. Abandon ship.
			if (state.CurrentPhase == TimerPhase.NotRunning) { return null; }
			if (state.CurrentSplitIndex == 0) { return null; } // Run just started, no previous splits to calculate

			int prevIndex = state.CurrentPhase == TimerPhase.Ended ? state.Run.Count - 1 : state.CurrentSplitIndex - 1;
			TimeSpan? liveSegmentTime = Levels[lvl].LiveSegmentTimes[method][prevIndex];
			TimeSpan? goldTime = state.Run[prevIndex].BestSegmentTime[method];
			if (liveSegmentTime == null || goldTime == null) { return null; }
			return liveSegmentTime - goldTime;
		}

		TimeSpan? CalculatePreviousSegmentDelta(LiveSplitState state, TimingMethod method, int lvl, int prevLocalIndex, out TimeSpan? liveSegmentTime) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { liveSegmentTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { liveSegmentTime = null; return null; }
			SplitLevel level = Levels[lvl];

			if (prevLocalIndex == -1) { liveSegmentTime = null; return null; }

			liveSegmentTime = level.LiveSegmentTimes[method][prevLocalIndex];
			if (liveSegmentTime == null) { return null; }

			TimeSpan? pbSegmentTime = level.PbSegmentTimes[method][prevLocalIndex];
			if (pbSegmentTime == null) { return null; }
			return liveSegmentTime - pbSegmentTime;
		}

		TimeSpan? CalculatePreviousRunDelta(LiveSplitState state, TimingMethod method, int lvl, int prevLocalIndex, out TimeSpan? liveRunTime) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { liveRunTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { liveRunTime = null; return null; }
			SplitLevel level = Levels[lvl];

			if (prevLocalIndex == -1) { liveRunTime = null; return null; }

			int prevGlobalIndex = Levels[lvl].Indices[prevLocalIndex];

			liveRunTime = state.Run[prevGlobalIndex].SplitTime[method];
			if (liveRunTime == null) { return null; }

			TimeSpan? pbRunTime = state.Run[prevGlobalIndex].PersonalBestSplitTime[method];
			if (pbRunTime == null) { return null; }
			return liveRunTime - pbRunTime;
		}
	}
}
