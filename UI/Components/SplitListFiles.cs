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
				if (Settings.OutputSplitList) {
					var (first, last) = SplitListRange(lvl);
					WriteSplitListLevel(FOLDER_SPLITLIST, method, lvl, first, last);
				}

				// Every completed split: none before the start, all after the finish
				if (Settings.OutputCompletedSplits) {
					WriteSplitListLevel(FOLDER_COMPLETEDSPLITS, method, lvl, 0, GetPreviousSubsplitIndex(lvl));
				}
			}
		}

		// The SplitList window at one level, as local indices: the current split with SplitsBefore splits before it
		// and SplitsAfter after it, shifted to stay within the level
		(int First, int Last) SplitListRange(int lvl) {
			int count = Levels[lvl].Indices.Count;
			int range = 1 + Settings.SplitsBefore + Settings.SplitsAfter;
			if (range > count) {
				return (0, count - 1);
			}
			// No current split before the start or after the finish: the first members, or the last
			if (State.CurrentPhase == TimerPhase.NotRunning) {
				return (0, range - 1);
			}
			if (State.CurrentPhase == TimerPhase.Ended) {
				return (count - range, count - 1);
			}
			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
			if (currentSubsplitIndex < Settings.SplitsBefore) {
				return (0, range - 1);
			}
			if (currentSubsplitIndex > count - 1 - Settings.SplitsAfter) {
				return (count - range, count - 1);
			}
			return (currentSubsplitIndex - Settings.SplitsBefore, currentSubsplitIndex + Settings.SplitsAfter);
		}

		// Writes the files of one split list (folder) at one level, for the splits with local indices first to last.
		void WriteSplitListLevel(string folder, TimingMethod method, int lvl, int first, int last) {
			// -1 for none: no current split before the start or after the finish, nothing completed before the start
			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
			int previousSubsplitIndex = GetPreviousSubsplitIndex(lvl);
			var lines = new ListLines(lvl == Levels.Count - 1, lvl + 1);

			for (int spl = first; spl <= last; spl++) {
				AppendNames(lines, lvl, spl);
				if (spl == currentSubsplitIndex) lines.CurrentSplitHighlight.Append(HIGHLIGHT_FILL);
				if (spl <= previousSubsplitIndex) {
					AppendCompletedLine(lines, method, lvl, spl);
				}
				else {
					AppendUpcomingLine(lines, method, lvl, spl);
				}
				lines.EndLine();
			}

			foreach (var file in lines.Files) {
				MakeFile(folder + file.Template, file.Lines.ToString(), method, lvl);
			}
		}

		// Names_Indent_{depth}.txt, with {1} left in for MakeFile
		static string IndentFile(int depth) => string.Format(FILE_LIST_WHITESPACE, "", "{1}", depth);

		void AppendNames(ListLines lines, int lvl, int spl) {
			int allSplitsIndex = Levels[lvl].Indices[spl];
			string name = Levels.Names[allSplitsIndex][lvl];
			lines.NamesRaw.Append(State.Run[allSplitsIndex].Name);
			lines.Names.Append(name);
			// Write different levels of subsplits to different files, allowing the user to indent them in their OBS etc.
			lines.Indents[Levels.Depths[allSplitsIndex]].Append(name);
		}

		// A completed split: this attempt's time and deltas, and its highlight
		void AppendCompletedLine(ListLines lines, TimingMethod method, int lvl, int spl) {
			var split = State.Run[Levels[lvl].Indices[spl]];
			TimeSpan? runDelta = RunDelta(method, lvl, spl);
			TimeSpan? segmentDelta = SegmentDelta(method, lvl, spl);
			lines.RunTimeCurrent.Append(TimeFormatter.DurationString(split.SplitTime[method]));
			lines.RunDelta.Append(TimeFormatter.DeltaString(runDelta));
			if (lvl == Levels.Count - 1) lines.GoldDelta.Append(TimeFormatter.DeltaString(GoldDelta(method, lvl, spl)));
			lines.SegmentDelta.Append(TimeFormatter.DeltaString(segmentDelta));
			HighlightFile(lines, method, lvl, spl, runDelta, segmentDelta)?.Append(HIGHLIGHT_FILL);
		}

		// The current split and those after it: their PB times
		void AppendUpcomingLine(ListLines lines, TimingMethod method, int lvl, int spl) {
			var split = State.Run[Levels[lvl].Indices[spl]];
			lines.RunTimeUpcoming.Append(TimeFormatter.DurationString(split.PersonalBestSplitTime[method]));
			if (lvl == Levels.Count - 1) lines.GoldTimeUpcoming.Append(TimeFormatter.DurationString(split.BestSegmentTime[method]));
			lines.SegmentTimeUpcoming.Append(TimeFormatter.DurationString(Levels[lvl].PbSegmentTimes[method][spl]));
		}

		// The highlight file that a completed split is filled in, or null for none. The first that applies wins.
		// A skipped split has no time, and no highlight.
		StringBuilder HighlightFile(ListLines lines, TimingMethod method, int lvl, int spl, TimeSpan? runDelta, TimeSpan? segmentDelta) {
			if (LiveRunTime(method, lvl, spl) == null) return null;
			if (IsGold(method, lvl, spl)) return lines.GoldHighlight;
			if (runDelta < TimeSpan.Zero && segmentDelta < TimeSpan.Zero) return lines.AheadGainedHighlight;
			if (runDelta < TimeSpan.Zero) return lines.AheadLostHighlight;
			if (segmentDelta < TimeSpan.Zero) return lines.BehindGainedHighlight;
			if (State.Run[Levels[lvl].Indices[spl]].PersonalBestSplitTime[method] != null) return lines.BehindLostHighlight;
			return null;
		}

		/// <summary>
		/// The files of one split list, built a line at a time: every file gets a line per split, left blank where it has nothing for that split.
		/// One StringBuilder per file, appended to directly. StringBuilders, not +=: CompletedSplits rebuilds every completed line on every split.
		/// </summary>
		class ListLines {

			public readonly StringBuilder Names, NamesRaw;
			public readonly StringBuilder RunTimeCurrent, RunTimeUpcoming, RunDelta;
			public readonly StringBuilder SegmentTimeUpcoming, SegmentDelta;
			public readonly StringBuilder CurrentSplitHighlight;
			public readonly StringBuilder AheadGainedHighlight, AheadLostHighlight, BehindGainedHighlight, BehindLostHighlight;
			// Golds are only supported at the last level. Null at the other levels.
			public readonly StringBuilder GoldDelta, GoldTimeUpcoming, GoldHighlight;
			// Names_Indent_{depth}, one per depth up to the level's
			public readonly StringBuilder[] Indents;

			// Every file above with its FILE_LIST_* template
			readonly List<(string Template, StringBuilder Lines)> _files = new List<(string, StringBuilder)>();

			public ListLines(bool golds, int depths) {
				Names = Add(FILE_LIST_NAMES);
				NamesRaw = Add(FILE_LIST_NAMES_RAW);
				RunTimeCurrent = Add(FILE_LIST_FINISH_TIME_LIVE);
				RunTimeUpcoming = Add(FILE_LIST_FINISH_TIME_UPCOMING);
				RunDelta = Add(FILE_LIST_RUN_DELTA);
				SegmentTimeUpcoming = Add(FILE_LIST_SEGMENT_TIME_UPCOMING);
				SegmentDelta = Add(FILE_LIST_SEGMENT_DELTA);
				CurrentSplitHighlight = Add(FILE_LIST_CURRENT_SPLIT_HIGHLIGHT);
				AheadGainedHighlight = Add(FILE_LIST_AHEAD_GAINED_HIGHLIGHT);
				AheadLostHighlight = Add(FILE_LIST_AHEAD_LOST_HIGHLIGHT);
				BehindGainedHighlight = Add(FILE_LIST_BEHIND_GAINED_HIGHLIGHT);
				BehindLostHighlight = Add(FILE_LIST_BEHIND_LOST_HIGHLIGHT);
				if (golds) {
					GoldDelta = Add(FILE_LIST_GOLD_DELTA);
					GoldTimeUpcoming = Add(FILE_LIST_GOLD_TIME_UPCOMING);
					GoldHighlight = Add(FILE_LIST_GOLD_HIGHLIGHT);
				}
				Indents = new StringBuilder[depths];
				for (int depth = 0; depth < depths; depth++) {
					Indents[depth] = Add(IndentFile(depth));
				}
			}

			StringBuilder Add(string template) {
				var lines = new StringBuilder();
				_files.Add((template, lines));
				return lines;
			}

			public IEnumerable<(string Template, StringBuilder Lines)> Files => _files;

			public void EndLine() {
				// '\n', not AppendLine, which would write "\r\n"
				foreach (var file in _files) {
					file.Lines.Append('\n');
				}
			}
		}
	}
}
