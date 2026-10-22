using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Golds at every level: a section's best segment comes from the segment history
	/// </summary>
	public class GoldTests {

		const string SECTIONS = @"PreviousSplit\SansSubsplits\GameTime\";

		[Fact]
		public void SectionWithNoHistoryIsAGoldOnItsFirstCompletion() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Split(21);
				run.Split(28);
				Assert.Equal("GoldAhead", run.Read(SECTIONS + @"CurrentComparison\Sign.txt"));
				Assert.Equal("-", run.Read(SECTIONS + "GoldDifference.txt"));
			}
		}

		[Fact]
		public void SectionsBestComesFromTheHistory() {
			using (var run = TestRun.Base()) {
				// Reset after A3: Area A was done in 26.5 within one attempt
				run.PlayAttempt(9, 18, 26.5);
				run.Start();
				Assert.Equal("0:26", run.Read(@"CurrentSplit\SansSubsplits\GameTime\GoldTime.txt"));
				run.Split(9.5);
				run.Split(21);
				run.Split(28);
				Assert.Equal("GainedAhead", run.Read(SECTIONS + @"CurrentComparison\Sign.txt"));
				Assert.Equal("+1.5", run.Read(SECTIONS + "GoldDifference.txt"));
			}
		}

		[Fact]
		public void SplitWithNoBestSegmentIsAGold() {
			using (var run = new TestRun(("A", 10, null), ("B", 20, 9))) {
				run.Start();
				run.Split(9.5);
				Assert.Equal("GoldAhead", run.Read(@"PreviousSplit\AllSplits\GameTime\CurrentComparison\Sign.txt"));
				Assert.NotEqual("", run.Lines(@"SplitList\AllSplits\GameTime\Highlight_Gold.txt")[0]);
				// A gold is highlighted only in Highlight_Gold
				Assert.Equal("", run.Lines(@"SplitList\AllSplits\GameTime\CurrentComparison\Highlight_AheadGained.txt")[0]);
			}
		}
	}
}
