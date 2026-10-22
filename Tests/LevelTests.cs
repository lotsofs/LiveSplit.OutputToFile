using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Subsplit levels and the names shown at each
	/// </summary>
	public class LevelTests {

		[Fact]
		public void EachDepthGetsALevel() {
			using (var run = new TestRun(("---a", 10, 9), ("--b", 20, 9), ("-c", 30, 9), ("d", 40, 9))) {
				Assert.Equal("1", run.Read("TotalSplits_SansSubsplits.txt"));
				Assert.Equal("2", run.Read("TotalSplits_SansSubsplits_Level2.txt"));
				Assert.Equal("3", run.Read("TotalSplits_SansSubsplits_Level3.txt"));
				Assert.Equal("4", run.Read("TotalSplits_AllSplits.txt"));
				Assert.Equal(new[] { "c", "d" }, run.Lines(@"SplitList\SansSubsplits_Level2\Names.txt"));
			}
		}

		[Fact]
		public void LastSplitsDashesAddNoLevel() {
			using (var run = new TestRun(("A", 10, 9), ("B", 20, 9), ("-C", 30, 9))) {
				Assert.Equal("3", run.Read("TotalSplits_AllSplits.txt"));
				Assert.False(run.Exists("TotalSplits_SansSubsplits.txt"));
				Assert.Equal(new[] { "A", "B", "C" }, run.Lines(@"SplitList\AllSplits\Names.txt"));
			}
		}

		[Fact]
		public void FirstSplitDeeperThanAnyLevelJoinsTheDeepest() {
			using (var run = new TestRun(("--Intro", 10, 9), ("A", 20, 9), ("B", 30, 9))) {
				Assert.Equal(new[] { "Intro", "A", "B" }, run.Lines(@"SplitList\AllSplits\Names.txt"));
				run.Start();
				run.Split(9.5);
				run.Split(21);
				Assert.Equal("+2.5", run.Read(@"PreviousSplit\AllSplits\GameTime\GoldDifference.txt"));
				run.Split(30);
				Assert.Equal("Intro", run.Lines(@"CompletedSplits\AllSplits\Names.txt")[0]);
			}
		}

		[Fact]
		public void EmptyNamesShowAsBlankLines() {
			using (var run = new TestRun(("", 10, 9), ("-", 20, 9), ("{}", 30, 9), ("End", 40, 9))) {
				Assert.Equal(new[] { "", "", "", "End" }, run.Lines(@"SplitList\AllSplits\Names.txt"));
				Assert.Equal(new[] { "", "", "End" }, run.Lines(@"SplitList\SansSubsplits\Names.txt"));
				run.Start();
				run.Split(9);
				run.Split(19);
				run.Split(29);
				run.Split(39);
				run.Reset();
			}
		}

		[Fact]
		public void NamesLoseTheirDashesAndOneSpace() {
			using (var run = new TestRun(("- -40", 10, 9), ("End", 20, 9))) {
				Assert.Equal(new[] { "-40", "End" }, run.Lines(@"SplitList\AllSplits\Names.txt"));
				Assert.Equal(new[] { "- -40", "End" }, run.Lines(@"SplitList\AllSplits\Names_Raw.txt"));
			}
		}

		[Fact]
		public void NamesAreTheSameWithSubsplitsOff() {
			using (var on = TestRun.Base())
			using (var off = TestRun.Base(r => r.Settings.OutputSubsplits = false)) {
				Assert.Equal(new[] { "A1", "A2", "A3", "B1", "B2" }, on.Lines(@"SplitList\AllSplits\Names.txt"));
				Assert.Equal(on.Read(@"SplitList\AllSplits\Names.txt"), off.Read(@"SplitList\AllSplits\Names.txt"));
				Assert.False(off.Exists(@"SplitList\SansSubsplits\Names.txt"));
			}
		}
	}
}
