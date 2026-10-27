using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// SplitList\ and CompletedSplits\
	/// </summary>
	public class SplitListTests {

		static readonly string[] HIGHLIGHTS = {
			@"GameTime\Highlight_Gold.txt",
			@"GameTime\CurrentComparison\Highlight_AheadGained.txt",
			@"GameTime\CurrentComparison\Highlight_AheadLost.txt",
			@"GameTime\CurrentComparison\Highlight_BehindGained.txt",
			@"GameTime\CurrentComparison\Highlight_BehindLost.txt",
		};

		[Fact]
		public void PausingKeepsTheDifferences() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Split(21);
				run.Split(28);
				run.Pause();
				Assert.Equal(new[] { "−0.5", "+1.0", "−2.0", "", "" }, run.Lines(@"SplitList\AllSplits\GameTime\CurrentComparison\Delta_Run.txt"));
				Assert.Equal(new[] { "−0.5", "+1.0", "−2.0" }, run.Lines(@"CompletedSplits\AllSplits\GameTime\CurrentComparison\Delta_Run.txt"));
			}
		}

		[Fact]
		public void SkippedSplitHasNoHighlight() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Skip();
				run.Split(21);
				foreach (string list in new[] { "SplitList", "CompletedSplits" }) {
					foreach (string highlight in HIGHLIGHTS) {
						Assert.Equal("", run.Lines($@"{list}\AllSplits\{highlight}")[0]);
					}
				}
			}
		}

		[Fact]
		public void CompletedSplitsHasNoUpcomingFiles() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				foreach (string file in new[] {
					@"Highlight_CurrentSplit.txt",
					@"GameTime\Time_Gold_Upcoming.txt",
					@"GameTime\CurrentComparison\Time_Run_Upcoming.txt",
					@"GameTime\CurrentComparison\Time_Segment_Upcoming.txt",
				}) {
					Assert.True(run.Exists(@"SplitList\AllSplits\" + file), file);
					Assert.False(run.Exists(@"CompletedSplits\AllSplits\" + file), file);
				}
			}
		}

		[Fact]
		public void WindowFollowsTheCurrentSplit() {
			using (var run = TestRun.Base(r => {
				r.Settings.SplitsBefore = 1;
				r.Settings.SplitsAfter = 1;
			})) {
				const string NAMES = @"SplitList\AllSplits\Names.txt";
				Assert.Equal(new[] { "A1", "A2", "A3" }, run.Lines(NAMES));
				run.Start();
				run.Split(9.5);
				run.Split(21);
				Assert.Equal(new[] { "A2", "A3", "B1" }, run.Lines(NAMES));
				run.Split(28);
				run.Split(39);
				Assert.Equal(new[] { "A3", "B1", "B2" }, run.Lines(NAMES));
				run.Split(48);
				Assert.Equal(new[] { "A3", "B1", "B2" }, run.Lines(NAMES));
			}
		}

		const string ALL_NAMES = @"SplitList\AllSplits\Names.txt";

		// Shaped like a user's splits: subsplits two deep in the section that 0009 ends, then top-level splits up to 0020.
		// Two splits before the current one and two after.
		static TestRun NestedRun() {
			var splits = new List<(string, double?, double?)> {
				("0001", 10, 9), ("-0002", 20, 9), ("--0003", 30, 9), ("--0004", 40, 9), ("-0005", 50, 9), ("-0006", 60, 9), ("--0007", 70, 9), ("-0008", 80, 9),
			};
			for (int i = 9; i <= 20; i++) splits.Add((i.ToString("0000"), 10 * i, 9));
			return new TestRun(r => {
				r.Settings.SplitsBefore = 2;
				r.Settings.SplitsAfter = 2;
			}, splits.ToArray());
		}

		static string[] Names(params int[] splits) => splits.Select(s => s.ToString("0000")).ToArray();

		const string SCROLLED_TO = @"Highlight_ScrolledToSplit.txt";

		// A highlight's five lines, with the fill on the given line from 1, or on none for 0
		static string[] FilledLine(int line) => Enumerable.Range(1, 5).Select(l => l == line ? new string('█', 43) : "").ToArray();

		[Fact]
		public void ScrollingMovesTheSplitTheWindowIsAround() {
			using (var run = NestedRun()) {
				run.Start();
				Assert.Equal(Names(1, 2, 3, 4, 5), run.Lines(ALL_NAMES));
				Assert.Equal(FilledLine(0), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
				run.ScrollDown();
				run.ScrollDown();
				// Scrolled to 0003, which has the window's two splits before it
				Assert.Equal(Names(1, 2, 3, 4, 5), run.Lines(ALL_NAMES));
				Assert.Equal(FilledLine(3), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
				run.ScrollDown();
				Assert.Equal(Names(2, 3, 4, 5, 6), run.Lines(ALL_NAMES));
				Assert.Equal(FilledLine(3), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
				Assert.False(run.Exists(@"CompletedSplits\AllSplits\" + SCROLLED_TO));
				// Back where it started, so no longer scrolled
				for (int i = 0; i < 3; i++) run.ScrollUp();
				Assert.Equal(FilledLine(0), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
			}
		}

		[Fact]
		public void EachLevelIsAroundTheMemberHoldingTheScrolledToSplit() {
			using (var run = NestedRun()) {
				run.Start();
				for (int i = 1; i <= 13; i++) run.Split(10 * i);
				// From 0014 to 0005
				for (int i = 0; i < 9; i++) run.ScrollUp();
				Assert.Equal(Names(3, 4, 5, 6, 7), run.Lines(ALL_NAMES));
				Assert.Equal(FilledLine(3), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
				Assert.Equal(Names(1, 2, 5, 6, 8), run.Lines(@"SplitList\SansSubsplits_Level2\Names.txt"));
				Assert.Equal(FilledLine(3), run.Lines(@"SplitList\SansSubsplits_Level2\" + SCROLLED_TO));
				// 0005 is in the section that 0009 ends
				Assert.Equal(Names(1, 9, 10, 11, 12), run.Lines(@"SplitList\SansSubsplits\Names.txt"));
				Assert.Equal(FilledLine(2), run.Lines(@"SplitList\SansSubsplits\" + SCROLLED_TO));
			}
		}

		[Fact]
		public void ScrollingStopsAtTheEndsOfTheRun() {
			using (var run = NestedRun()) {
				// Before the start, from the first split: 19 notches to the last split, and 6 past it
				for (int i = 0; i < 25; i++) run.ScrollDown();
				Assert.Equal(Names(16, 17, 18, 19, 20), run.Lines(ALL_NAMES));
				// The notches past the end were dropped: three back is 0017
				for (int i = 0; i < 3; i++) run.ScrollUp();
				Assert.Equal(Names(15, 16, 17, 18, 19), run.Lines(ALL_NAMES));
				for (int i = 0; i < 30; i++) run.ScrollUp();
				for (int i = 0; i < 3; i++) run.ScrollDown();
				Assert.Equal(Names(2, 3, 4, 5, 6), run.Lines(ALL_NAMES));
			}
		}

		[Fact]
		public void SplittingOrResettingGoesBackToTheCurrentSplit() {
			using (var run = NestedRun()) {
				run.Start();
				for (int i = 0; i < 6; i++) run.ScrollDown();
				Assert.Equal(Names(5, 6, 7, 8, 9), run.Lines(ALL_NAMES));
				// The current split, 0001, is out of the window
				Assert.Equal(new[] { "", "", "", "", "" }, run.Lines(@"SplitList\AllSplits\Highlight_CurrentSplit.txt"));
				run.Split(10);
				Assert.Equal(Names(1, 2, 3, 4, 5), run.Lines(ALL_NAMES));
				Assert.Equal(FilledLine(0), run.Lines(@"SplitList\AllSplits\" + SCROLLED_TO));
				for (int i = 0; i < 6; i++) run.ScrollDown();
				run.Reset();
				Assert.Equal(Names(1, 2, 3, 4, 5), run.Lines(ALL_NAMES));
			}
		}

		[Fact]
		public void ScrollingAfterTheFinish() {
			using (var run = NestedRun()) {
				run.Start();
				for (int i = 1; i <= 20; i++) run.Split(10 * i);
				Assert.Equal(Names(16, 17, 18, 19, 20), run.Lines(ALL_NAMES));
				// From the last split to 0017
				for (int i = 0; i < 3; i++) run.ScrollUp();
				Assert.Equal(Names(15, 16, 17, 18, 19), run.Lines(ALL_NAMES));
				Assert.Equal(new[] { "2:30", "2:40", "2:50", "3:00", "3:10" }, run.Lines(@"SplitList\AllSplits\GameTime\Time_Run_Current.txt"));
			}
		}
	}
}
