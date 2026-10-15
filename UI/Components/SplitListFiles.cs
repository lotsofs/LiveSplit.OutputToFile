using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;
using System.Collections.Generic;
using System.Text;

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

		public SplitListFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

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

			// StringBuilders, not +=: CompletedSplits rebuilds every completed line on every split
			var splitNames = new StringBuilder();
			var splitNamesRaw = new StringBuilder();
			var finishTimeLive = new StringBuilder();
			var finishTimeUpcoming = new StringBuilder();
			var runDelta = new StringBuilder();
			var goldDelta = new StringBuilder();
			var goldTimeUpcoming = new StringBuilder();
			var segmentTimeUpcoming = new StringBuilder();
			var segmentDelta = new StringBuilder();
			var currentSplitHighlight = new StringBuilder();
			var goldHighlights = new StringBuilder();
			var aheadGainedHighlights = new StringBuilder();
			var aheadLostHighlights = new StringBuilder();
			var behindGainedHighlights = new StringBuilder();
			var behindLostHighlights = new StringBuilder();

			// Build the strings
			for (int spl = first; spl <= last; spl++) {
				int allSplitsIndex = subsplitIndices[spl];
				var split = State.Run[allSplitsIndex];

				splitNamesRaw.Append(split.Name);
				splitNames.Append(Levels.Names[allSplitsIndex][lvl]);

				if (spl == currentSubsplitIndex) currentSplitHighlight.Append(HIGHLIGHT_FILL);

				if (spl < currentSubsplitIndex) {
					TimeSpan? lineRunDelta = RunDelta(method, lvl, spl);
					TimeSpan? lineSegmentDelta = SegmentDelta(method, lvl, spl);
					finishTimeLive.Append(TimeFormatter.DurationString(split.SplitTime[method]));
					runDelta.Append(TimeFormatter.DeltaString(lineRunDelta));
					goldDelta.Append(lvl == Levels.Count - 1 ? TimeFormatter.DeltaString(GoldDelta(method, lvl, spl)) : "");
					segmentDelta.Append(TimeFormatter.DeltaString(lineSegmentDelta));
					// highlight fills
					if (IsGold(method, lvl, spl))
						goldHighlights.Append(HIGHLIGHT_FILL);
					else if (lineRunDelta < TimeSpan.Zero && lineSegmentDelta < TimeSpan.Zero)
						aheadGainedHighlights.Append(HIGHLIGHT_FILL);
					else if (lineRunDelta < TimeSpan.Zero)
						aheadLostHighlights.Append(HIGHLIGHT_FILL);
					else if (lineSegmentDelta < TimeSpan.Zero)
						behindGainedHighlights.Append(HIGHLIGHT_FILL);
					else if (split.PersonalBestSplitTime[method] != null)
						behindLostHighlights.Append(HIGHLIGHT_FILL);
				}
				else {
					finishTimeUpcoming.Append(TimeFormatter.DurationString(split.PersonalBestSplitTime[method]));
					goldTimeUpcoming.Append(lvl == Levels.Count - 1 ? TimeFormatter.DurationString(split.BestSegmentTime[method]) : "");
					segmentTimeUpcoming.Append(TimeFormatter.DurationString(level.PbSegmentTimes[method][spl]));
				}

				// '\n', not AppendLine, which would write "\r\n"
				splitNames.Append('\n');
				splitNamesRaw.Append('\n');
				finishTimeLive.Append('\n');
				finishTimeUpcoming.Append('\n');
				runDelta.Append('\n');
				goldDelta.Append('\n');
				goldTimeUpcoming.Append('\n');
				segmentTimeUpcoming.Append('\n');
				segmentDelta.Append('\n');
				currentSplitHighlight.Append('\n');
				goldHighlights.Append('\n');
				aheadGainedHighlights.Append('\n');
				behindGainedHighlights.Append('\n');
				aheadLostHighlights.Append('\n');
				behindLostHighlights.Append('\n');
			}

			// Output
			MakeFile(folder + FILE_LIST_NAMES, splitNames.ToString(), null, lvl);
			MakeFile(folder + FILE_LIST_NAMES_RAW, splitNamesRaw.ToString(), null, lvl);
			MakeFile(folder + FILE_LIST_FINISH_TIME_LIVE, finishTimeLive.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_FINISH_TIME_UPCOMING, finishTimeUpcoming.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_RUN_DELTA, runDelta.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_SEGMENT_TIME_UPCOMING, segmentTimeUpcoming.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_SEGMENT_DELTA, segmentDelta.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_CURRENT_SPLIT_HIGHLIGHT, currentSplitHighlight.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_AHEAD_GAINED_HIGHLIGHT, aheadGainedHighlights.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_AHEAD_LOST_HIGHLIGHT, aheadLostHighlights.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_BEHIND_GAINED_HIGHLIGHT, behindGainedHighlights.ToString(), method, lvl);
			MakeFile(folder + FILE_LIST_BEHIND_LOST_HIGHLIGHT, behindLostHighlights.ToString(), method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_DELTA, goldDelta.ToString(), method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_TIME_UPCOMING, goldTimeUpcoming.ToString(), method, lvl);
			if (lvl == Levels.Count - 1) MakeFile(folder + FILE_LIST_GOLD_HIGHLIGHT, goldHighlights.ToString(), method, lvl);

			// Write differnet levels of subsplits to different files, allowing the user to indent them in their OBS etc.
			// One file per depth 0..lvl
			StringBuilder[] whitespacedSplitListNames = new StringBuilder[lvl + 1];
			for (int i = 0; i < whitespacedSplitListNames.Length; i++) {
				whitespacedSplitListNames[i] = new StringBuilder();
			}
			for (int split = first; split <= last; split++) {
				int globalIndex = Levels[lvl].Indices[split];
				for (int list = 0; list <= lvl; list++) {
					if (list == Levels.Depths[globalIndex]) whitespacedSplitListNames[list].Append(Levels.Names[globalIndex][lvl]);
					whitespacedSplitListNames[list].Append('\n');
				}
			}
			for (int list = 0; list <= lvl; list++) {
				MakeFile(folder + string.Format(FILE_LIST_WHITESPACE, "", "{1}", list), whitespacedSplitListNames[list].ToString(), method, lvl);
			}
		}
	}
}
