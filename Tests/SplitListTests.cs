using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// SplitList\ and CompletedSplits\
	/// </summary>
	public class SplitListTests {

		static readonly string[] HIGHLIGHTS = {
			@"GameTime\Highlight_Gold.txt",
			@"GameTime\Current Comparison\Highlight_AheadGained.txt",
			@"GameTime\Current Comparison\Highlight_AheadLost.txt",
			@"GameTime\Current Comparison\Highlight_BehindGained.txt",
			@"GameTime\Current Comparison\Highlight_BehindLost.txt",
		};

		[Fact]
		public void PausingKeepsTheDifferences() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Split(21);
				run.Split(28);
				run.Pause();
				Assert.Equal(new[] { "−0.5", "+1.0", "−2.0", "", "" }, run.Lines(@"SplitList\AllSplits\GameTime\Current Comparison\Delta_Run.txt"));
				Assert.Equal(new[] { "−0.5", "+1.0", "−2.0" }, run.Lines(@"CompletedSplits\AllSplits\GameTime\Current Comparison\Delta_Run.txt"));
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
					@"GameTime\Current Comparison\Time_Run_Upcoming.txt",
					@"GameTime\Current Comparison\Time_Segment_Upcoming.txt",
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
	}
}
