using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;
using System.Collections.Generic;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes SplitList\ and CompletedSplits\. Both lists write the same files, for different ranges of splits.
	/// </summary>
	class SplitListFiles : OutputWriter {

		const string HIGHLIGHT_FILL = "███████████████████████████████████████████";

		// Both split lists write the same files, each under its own folder.
		// The FILE_LIST_* templates are relative to that folder: prefix one of the FOLDER_* constants.
		const string FOLDER_SPLITLIST = @"SplitList\";
		const string FOLDER_COMPLETEDSPLITS = @"CompletedSplits\";

		const string FILE_LIST_NAMES = @"{1}\Names.txt";
		const string FILE_LIST_NAMES_RAW = @"{1}\Names_Raw.txt";
		const string FILE_LIST_WHITESPACE = @"{1}\Names_Indent_{2}.txt";
		const string FILE_LIST_FINISH_TIME_LIVE = @"{1}\{0}\Time_Run_Current.txt";
		const string FILE_LIST_FINISH_TIME_UPCOMING = @"{1}\{0}\Time_Run_Upcoming.txt";
		const string FILE_LIST_RUN_DELTA = @"{1}\{0}\Delta_Run.txt";
		const string FILE_LIST_GOLD_DELTA = @"{1}\{0}\Delta_Gold.txt";
		const string FILE_LIST_GOLD_TIME_UPCOMING = @"{1}\{0}\Time_Gold_Upcoming.txt";
		const string FILE_LIST_SEGMENT_TIME_UPCOMING = @"{1}\{0}\Time_Segment_Upcoming.txt";
		const string FILE_LIST_SEGMENT_DELTA = @"{1}\{0}\Delta_Segment.txt";
		const string FILE_LIST_CURRENT_SPLIT_HIGHLIGHT = @"{1}\Highlight_CurrentSplit.txt";
		const string FILE_LIST_GOLD_HIGHLIGHT = @"{1}\{0}\Highlight_Gold.txt";
		const string FILE_LIST_AHEAD_GAINED_HIGHLIGHT = @"{1}\{0}\Highlight_AheadGained.txt";
		const string FILE_LIST_AHEAD_LOST_HIGHLIGHT = @"{1}\{0}\Highlight_AheadLost.txt";
		const string FILE_LIST_BEHIND_GAINED_HIGHLIGHT = @"{1}\{0}\Highlight_BehindGained.txt";
		const string FILE_LIST_BEHIND_LOST_HIGHLIGHT = @"{1}\{0}\Highlight_BehindLost.txt";

		public SplitListFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels) : base(state, settings, levels) { }

		public void WriteSplitLists(TimingMethod method) {
			int startingLevel = 0;
			if (!Settings.OutputSubsplits) startingLevel = Levels.Count - 1;

			for (int lvl = startingLevel; lvl < Levels.Count; lvl++) {
				List<int> subsplitIndices = Levels[lvl].Indices;
				int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);

				if (Settings.OutputSplitList) {
					// We must first determine the range of the splits we need to write
					int range = 1 + Settings.SplitsBefore + Settings.SplitsAfter;
					int first;
					int last;
					if (range > subsplitIndices.Count) {
						range = subsplitIndices.Count;
						first = 0;
						last = subsplitIndices.Count - 1;
					}
					else if (State.CurrentPhase == TimerPhase.NotRunning || currentSubsplitIndex < Settings.SplitsBefore) {
						first = 0;
						last = range - 1;
					}
					else if (State.CurrentPhase == TimerPhase.Ended || currentSubsplitIndex > subsplitIndices.Count - 1 - Settings.SplitsAfter) {
						first = subsplitIndices.Count - range;
						last = subsplitIndices.Count - 1;
					}
					else {
						first = currentSubsplitIndex - Settings.SplitsBefore;
						last = currentSubsplitIndex + Settings.SplitsAfter;
					}
					WriteSplitListLevel(FOLDER_SPLITLIST, method, lvl, first, last);
				}

				// Every split before the current one
				if (Settings.OutputCompletedSplits) {
					WriteSplitListLevel(FOLDER_COMPLETEDSPLITS, method, lvl, 0, currentSubsplitIndex - 1);
				}
			}
		}

		// Writes the files of one split list (folder) at one level, for the splits with local indices first to last.
		void WriteSplitListLevel(string folder, TimingMethod method, int lvl, int first, int last) {
			SplitLevel level = Levels[lvl];
			List<int> subsplitIndices = level.Indices;
			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);

			string splitNames = "";
			string splitNamesRaw = "";
			string finishTimeLive = "";
			string finishTimeUpcoming = "";
			string runDelta = "";
			string goldDelta = "";
			string goldTimeUpcoming = "";
			string segmentTimeUpcoming = "";
			string segmentDelta = "";
			string currentSplitHighlight = "";
			string goldHighlights = "";
			string aheadGainedHighlights = "";
			string aheadLostHighlights = "";
			string behindGainedHighlights = "";
			string behindLostHighlights = "";

			// Build the strings
			for (int spl = first; spl <= last; spl++) {
				int allSplitsIndex = subsplitIndices[spl];
				var split = State.Run[allSplitsIndex];

				splitNamesRaw += split.Name;
				splitNames += Levels.Names[allSplitsIndex][lvl];

				if (spl == currentSubsplitIndex) currentSplitHighlight += HIGHLIGHT_FILL;

				if (spl < currentSubsplitIndex) {
					finishTimeLive += TimeFormatter.DurationString(split.SplitTime[method]);
					runDelta += TimeFormatter.DeltaString(level.RunDeltae[method][spl]);
					goldDelta += lvl == Levels.Count - 1 ? TimeFormatter.DeltaString(level.GoldDeltae[method][spl]) : "";
					segmentDelta += TimeFormatter.DeltaString(level.SegmentDeltae[method][spl]);
					// highlight fills
					if (lvl == Levels.Count - 1 && level.GoldDeltae[method][spl] < TimeSpan.Zero)
						goldHighlights += HIGHLIGHT_FILL;
					else if (level.RunDeltae[method][spl] < TimeSpan.Zero && level.SegmentDeltae[method][spl] < TimeSpan.Zero)
						aheadGainedHighlights += HIGHLIGHT_FILL;
					else if (level.RunDeltae[method][spl] < TimeSpan.Zero)
						aheadLostHighlights += HIGHLIGHT_FILL;
					else if (level.SegmentDeltae[method][spl] < TimeSpan.Zero)
						behindGainedHighlights += HIGHLIGHT_FILL;
					else if (split.PersonalBestSplitTime[method] != null)
						behindLostHighlights += HIGHLIGHT_FILL;
				}
				else {
					finishTimeUpcoming += TimeFormatter.DurationString(split.PersonalBestSplitTime[method]);
					goldTimeUpcoming += lvl == Levels.Count - 1 ? TimeFormatter.DurationString(split.BestSegmentTime[method]) : "";
					segmentTimeUpcoming += TimeFormatter.DurationString(level.PbSegmentTimes[method][spl]);
				}

				splitNames += "\n";
				splitNamesRaw += "\n";
				finishTimeLive += "\n";
				finishTimeUpcoming += "\n";
				runDelta += "\n";
				goldDelta += "\n";
				goldTimeUpcoming += "\n";
				segmentTimeUpcoming += "\n";
				segmentDelta += "\n";
				currentSplitHighlight += "\n";
				goldHighlights += "\n";
				aheadGainedHighlights += "\n";
				behindGainedHighlights += "\n";
				aheadLostHighlights += "\n";
				behindLostHighlights += "\n";
			}

			// Output
			MakeFile(folder + FILE_LIST_NAMES, splitNames, null, lvl);
			MakeFile(folder + FILE_LIST_NAMES_RAW, splitNamesRaw, null, lvl);
			MakeFile(folder + FILE_LIST_FINISH_TIME_LIVE, finishTimeLive, method, lvl);
			MakeFile(folder + FILE_LIST_FINISH_TIME_UPCOMING, finishTimeUpcoming, method, lvl);
			MakeFile(folder + FILE_LIST_RUN_DELTA, runDelta, method, lvl);
			MakeFile(folder + FILE_LIST_SEGMENT_TIME_UPCOMING, segmentTimeUpcoming, method, lvl);
			MakeFile(folder + FILE_LIST_SEGMENT_DELTA, segmentDelta, method, lvl);
			MakeFile(folder + FILE_LIST_CURRENT_SPLIT_HIGHLIGHT, currentSplitHighlight, method, lvl);
			MakeFile(folder + FILE_LIST_AHEAD_GAINED_HIGHLIGHT, aheadGainedHighlights, method, lvl);
			MakeFile(folder + FILE_LIST_AHEAD_LOST_HIGHLIGHT, aheadLostHighlights, method, lvl);
			MakeFile(folder + FILE_LIST_BEHIND_GAINED_HIGHLIGHT, behindGainedHighlights, method, lvl);
			MakeFile(folder + FILE_LIST_BEHIND_LOST_HIGHLIGHT, behindLostHighlights, method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_DELTA, goldDelta, method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_TIME_UPCOMING, goldTimeUpcoming, method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_HIGHLIGHT, goldHighlights, method, lvl);

			// Write differnet levels of subsplits to different files, allowing the user to indent them in their OBS etc.
			string[] whitespacedSplitListNames = new string[Levels.Count];
			for (int i = lvl; i < whitespacedSplitListNames.Length; i++) {
				whitespacedSplitListNames[i] = "";
			}
			for (int split = first; split <= last; split++) {
				int globalIndex = Levels[lvl].Indices[split];
				for (int list = 0; list <= lvl; list++) {
					if (list == Levels.Depths[globalIndex]) whitespacedSplitListNames[list] += Levels.Names[globalIndex][lvl];
					whitespacedSplitListNames[list] += "\n";
				}
			}
			for (int list = 0; list <= lvl; list++) {
				MakeFile(folder + string.Format(FILE_LIST_WHITESPACE, "", "{1}", list), whitespacedSplitListNames[list], method, lvl);
			}
		}
	}
}
