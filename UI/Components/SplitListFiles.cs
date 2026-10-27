using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes SplitList\ and CompletedSplits\. Both lists write the same files, for different ranges of splits,
	/// except that CompletedSplits\ leaves out the files only the current and upcoming splits fill.
	/// </summary>
	class SplitListFiles : OutputWriter {

		const string HIGHLIGHT_FILL = "███████████████████████████████████████████";

		// Both split lists write the same files, each under its own folder, except the upcoming ones in CompletedSplits\.
		// The FILE_LIST_* templates are relative to that folder: prefix one of the FOLDER_* constants.
		const string FOLDER_SPLITLIST = @"SplitList\";
		const string FOLDER_COMPLETEDSPLITS = @"CompletedSplits\";

		const string FILE_LIST_NAMES = @"{1}\Names.txt";
		const string FILE_LIST_NAMES_RAW = @"{1}\Names_Raw.txt";
		const string FILE_LIST_WHITESPACE = @"{1}\Names_Indent_{2}.txt";
		const string FILE_LIST_FINISH_TIME_LIVE = @"{1}\{0}\Time_Run_Current.txt";
		const string FILE_LIST_GOLD_DELTA = @"{1}\{0}\Delta_Gold.txt";
		const string FILE_LIST_GOLD_TIME_UPCOMING = @"{1}\{0}\Time_Gold_Upcoming.txt";
		const string FILE_LIST_CURRENT_SPLIT_HIGHLIGHT = @"{1}\Highlight_CurrentSplit.txt";
		const string FILE_LIST_SCROLLED_TO_SPLIT_HIGHLIGHT = @"{1}\Highlight_ScrolledToSplit.txt";
		const string FILE_LIST_GOLD_HIGHLIGHT = @"{1}\{0}\Highlight_Gold.txt";
		// {3} is the comparison's folder: these files are written once for each of Levels.Comparisons
		const string FILE_LIST_FINISH_TIME_UPCOMING = @"{1}\{0}\{3}\Time_Run_Upcoming.txt";
		const string FILE_LIST_SEGMENT_TIME_UPCOMING = @"{1}\{0}\{3}\Time_Segment_Upcoming.txt";
		const string FILE_LIST_RUN_DELTA = @"{1}\{0}\{3}\Delta_Run.txt";
		const string FILE_LIST_SEGMENT_DELTA = @"{1}\{0}\{3}\Delta_Segment.txt";
		const string FILE_LIST_AHEAD_GAINED_HIGHLIGHT = @"{1}\{0}\{3}\Highlight_AheadGained.txt";
		const string FILE_LIST_AHEAD_LOST_HIGHLIGHT = @"{1}\{0}\{3}\Highlight_AheadLost.txt";
		const string FILE_LIST_BEHIND_GAINED_HIGHLIGHT = @"{1}\{0}\{3}\Highlight_BehindGained.txt";
		const string FILE_LIST_BEHIND_LOST_HIGHLIGHT = @"{1}\{0}\{3}\Highlight_BehindLost.txt";

		// How far LiveSplit's scrolling has moved the split that every level's SplitList window is laid out around, in splits of the run,
		// down positive. Scroll() keeps that split within the run, so scrolling back moves it at once.
		int _scrollOffset;

		public SplitListFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		/// <summary>
		/// Follows LiveSplit's scrolling: each notch moves the split the windows are laid out around by one split of the run.
		/// Call once per rewrite, after the levels are calculated.
		/// </summary>
		/// <param name="notches">The notches scrolled since the last call, down positive</param>
		/// <param name="backToCurrent">Whether to go back to the current split first</param>
		public void Scroll(int notches, bool backToCurrent) {
			if (backToCurrent) _scrollOffset = 0;
			// Notches past either end of the run are dropped, as LiveSplit's Splits component drops its own
			int split = Math.Min(Math.Max(UnscrolledSplit() + _scrollOffset + notches, 0), State.Run.Count - 1);
			_scrollOffset = split - UnscrolledSplit();
		}

		// The split the windows are laid out around, by index in the run
		int ScrolledToSplit() => UnscrolledSplit() + _scrollOffset;

		// The local index of the level's member that holds the scrolled-to split, or -1 while not scrolled
		int ScrolledToMember(int lvl) => _scrollOffset == 0 ? -1 : MemberAtOrAfter(lvl, ScrolledToSplit());

		// The split the windows are laid out around without scrolling, by index in the run: the current split,
		// the first split before the start, and the last after the finish
		int UnscrolledSplit() {
			if (State.CurrentPhase == TimerPhase.NotRunning) return 0;
			if (State.CurrentPhase == TimerPhase.Ended) return State.Run.Count - 1;
			return State.CurrentSplitIndex;
		}

		public void WriteSplitLists(TimingMethod method) {
			int startingLevel = 0;
			if (!Settings.OutputSubsplits) startingLevel = Levels.Count - 1;

			for (int lvl = startingLevel; lvl < Levels.Count; lvl++) {
				if (Settings.OutputSplitList) {
					var (first, last) = SplitListRange(lvl);
					WriteSplitListLevel(FOLDER_SPLITLIST, method, lvl, first, last, upcoming: true);
				}

				// Every completed split: none before the start, all after the finish. No upcoming lines, so no upcoming files.
				if (Settings.OutputCompletedSplits) {
					WriteSplitListLevel(FOLDER_COMPLETEDSPLITS, method, lvl, 0, GetPreviousSubsplitIndex(lvl), upcoming: false);
				}
			}
		}

		// The SplitList window at one level, as local indices: the member holding the scrolled-to split, with SplitsBefore members
		// before it and SplitsAfter after it, shifted to stay within the level. Without scrolling, that member is the current one,
		// the first before the start and the last after the finish.
		(int First, int Last) SplitListRange(int lvl) {
			int count = Levels[lvl].Indices.Count;
			int range = 1 + Settings.SplitsBefore + Settings.SplitsAfter;
			if (range > count) {
				return (0, count - 1);
			}
			int member = MemberAtOrAfter(lvl, ScrolledToSplit());
			int first = Math.Min(Math.Max(member - Settings.SplitsBefore, 0), count - range);
			return (first, first + range - 1);
		}

		// Writes the files of one split list (folder) at one level, for the splits with local indices first to last.
		// Without upcoming, the files of the current and upcoming splits are left out, so last must be at most the previous split.
		void WriteSplitListLevel(string folder, TimingMethod method, int lvl, int first, int last, bool upcoming) {
			// -1 for none: no current split before the start or after the finish, nothing completed before the start
			int currentSubsplitIndex = GetCurrentSubsplitIndex(lvl);
			int previousSubsplitIndex = GetPreviousSubsplitIndex(lvl);
			int scrolledToIndex = ScrolledToMember(lvl);
			var lines = new ListLines(Levels.Comparisons, upcoming, lvl + 1);

			for (int spl = first; spl <= last; spl++) {
				AppendNames(lines, lvl, spl);
				if (spl == currentSubsplitIndex) lines.CurrentSplitHighlight.Append(HIGHLIGHT_FILL);
				// CompletedSplits doesn't have it, though a completed split can be scrolled to
				if (spl == scrolledToIndex) lines.ScrolledToSplitHighlight?.Append(HIGHLIGHT_FILL);
				if (spl <= previousSubsplitIndex) {
					AppendCompletedLine(lines, method, lvl, spl);
				}
				else {
					AppendUpcomingLine(lines, method, lvl, spl);
				}
				lines.EndLine();
			}

			foreach (var file in lines.Files) {
				MakeFile(folder + file.Template, file.Lines.ToString(), method, lvl, file.ComparisonFolder);
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
			lines.RunTimeCurrent.Append(DurationString(split.SplitTime[method]));
			lines.GoldDelta.Append(DeltaString(GoldDelta(method, lvl, spl)));
			// A gold is filled in Highlight_Gold, and in none of a comparison's highlights. A skipped split has no time, and no highlight.
			bool isGold = IsGold(method, lvl, spl);
			bool highlighted = !isGold && LiveRunTime(method, lvl, spl) != null;
			if (isGold) lines.GoldHighlight.Append(HIGHLIGHT_FILL);
			foreach (var comparison in lines.Comparisons) {
				TimeSpan? runDelta = RunDelta(method, comparison.Name, lvl, spl);
				TimeSpan? segmentDelta = SegmentDelta(method, comparison.Name, lvl, spl);
				comparison.RunDelta.Append(DeltaString(runDelta));
				comparison.SegmentDelta.Append(DeltaString(segmentDelta));
				if (highlighted) HighlightFile(comparison, method, lvl, spl, runDelta, segmentDelta)?.Append(HIGHLIGHT_FILL);
			}
		}

		// The current split and those after it: their best segments, and each comparison's times
		void AppendUpcomingLine(ListLines lines, TimingMethod method, int lvl, int spl) {
			lines.GoldTimeUpcoming.Append(DurationString(Levels[lvl].BestSegmentTimes[method][spl]));
			foreach (var comparison in lines.Comparisons) {
				comparison.RunTimeUpcoming.Append(DurationString(ComparisonTime(method, comparison.Name, lvl, spl)));
				comparison.SegmentTimeUpcoming.Append(DurationString(Levels[lvl].ComparisonSegmentTimes[comparison.Name][method][spl]));
			}
		}

		// The comparison's highlight file that a completed split that isn't a gold is filled in, or null for none. The first that applies wins.
		StringBuilder HighlightFile(ComparisonLines lines, TimingMethod method, int lvl, int spl, TimeSpan? runDelta, TimeSpan? segmentDelta) {
			if (runDelta < TimeSpan.Zero && segmentDelta < TimeSpan.Zero) return lines.AheadGainedHighlight;
			if (runDelta < TimeSpan.Zero) return lines.AheadLostHighlight;
			if (segmentDelta < TimeSpan.Zero) return lines.BehindGainedHighlight;
			if (ComparisonTime(method, lines.Name, lvl, spl) != null) return lines.BehindLostHighlight;
			return null;
		}

		/// <summary>
		/// The files of one split list, built a line at a time: every file gets a line per split, left blank where it has nothing for that split.
		/// One StringBuilder per file, appended to directly. StringBuilders, not +=: CompletedSplits rebuilds every completed line on every split.
		/// </summary>
		class ListLines {

			public readonly StringBuilder Names, NamesRaw;
			public readonly StringBuilder RunTimeCurrent, GoldDelta, GoldHighlight;
			// Only for the current split and those after it, which CompletedSplits never has, and for scrolling, which only
			// moves SplitList. Null there.
			public readonly StringBuilder GoldTimeUpcoming, CurrentSplitHighlight, ScrolledToSplitHighlight;
			// Names_Indent_{depth}, one per depth up to the level's
			public readonly StringBuilder[] Indents;
			// The files of each comparison, in the order of SplitLevels.Comparisons
			public readonly ComparisonLines[] Comparisons;

			// Every file above with its FILE_LIST_* template, and the comparison's folder for a comparison's file
			readonly List<(string Template, string ComparisonFolder, StringBuilder Lines)> _files = new List<(string, string, StringBuilder)>();

			public ListLines(List<(string Name, string Folder)> comparisons, bool upcoming, int depths) {
				Names = Add(FILE_LIST_NAMES);
				NamesRaw = Add(FILE_LIST_NAMES_RAW);
				RunTimeCurrent = Add(FILE_LIST_FINISH_TIME_LIVE);
				GoldDelta = Add(FILE_LIST_GOLD_DELTA);
				GoldHighlight = Add(FILE_LIST_GOLD_HIGHLIGHT);
				if (upcoming) {
					GoldTimeUpcoming = Add(FILE_LIST_GOLD_TIME_UPCOMING);
					CurrentSplitHighlight = Add(FILE_LIST_CURRENT_SPLIT_HIGHLIGHT);
					ScrolledToSplitHighlight = Add(FILE_LIST_SCROLLED_TO_SPLIT_HIGHLIGHT);
				}
				Indents = new StringBuilder[depths];
				for (int depth = 0; depth < depths; depth++) {
					Indents[depth] = Add(IndentFile(depth));
				}
				Comparisons = comparisons.Select(c => new ComparisonLines(c.Name, upcoming, template => Add(template, c.Folder))).ToArray();
			}

			StringBuilder Add(string template, string comparisonFolder = null) {
				var lines = new StringBuilder();
				_files.Add((template, comparisonFolder, lines));
				return lines;
			}

			public IEnumerable<(string Template, string ComparisonFolder, StringBuilder Lines)> Files => _files;

			public void EndLine() {
				// '\n', not AppendLine, which would write "\r\n"
				foreach (var file in _files) {
					file.Lines.Append('\n');
				}
			}
		}

		/// <summary>
		/// The files of one list that compare with one comparison, in that comparison's folder
		/// </summary>
		class ComparisonLines {

			public readonly string Name;
			public readonly StringBuilder RunDelta, SegmentDelta;
			public readonly StringBuilder AheadGainedHighlight, AheadLostHighlight, BehindGainedHighlight, BehindLostHighlight;
			// Only for the current split and those after it, which CompletedSplits never has. Null there.
			public readonly StringBuilder RunTimeUpcoming, SegmentTimeUpcoming;

			// add makes the StringBuilder of a file, and adds it to the list's files
			public ComparisonLines(string name, bool upcoming, Func<string, StringBuilder> add) {
				Name = name;
				RunDelta = add(FILE_LIST_RUN_DELTA);
				SegmentDelta = add(FILE_LIST_SEGMENT_DELTA);
				AheadGainedHighlight = add(FILE_LIST_AHEAD_GAINED_HIGHLIGHT);
				AheadLostHighlight = add(FILE_LIST_AHEAD_LOST_HIGHLIGHT);
				BehindGainedHighlight = add(FILE_LIST_BEHIND_GAINED_HIGHLIGHT);
				BehindLostHighlight = add(FILE_LIST_BEHIND_LOST_HIGHLIGHT);
				if (upcoming) {
					RunTimeUpcoming = add(FILE_LIST_FINISH_TIME_UPCOMING);
					SegmentTimeUpcoming = add(FILE_LIST_SEGMENT_TIME_UPCOMING);
				}
			}
		}
	}
}
