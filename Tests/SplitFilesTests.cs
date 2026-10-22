using System.Linq;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// CurrentSplit\ and PreviousSplit\, including Sign.txt
	/// </summary>
	public class SplitFilesTests {

		const string PREVIOUS = @"PreviousSplit\AllSplits\GameTime\";

		[Fact]
		public void SegmentThatBeatsTheComparisonsSegmentIsGained() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				Assert.Equal("GainedAhead", run.Read(PREVIOUS + @"Current Comparison\Sign.txt"));
			}
		}

		[Fact]
		public void SplitAfterASkipHasNoSegmentDifference() {
			using (var run = new TestRun(("A", 10, 9), ("B", 20, 9), ("C", 30, 9))) {
				run.Start();
				run.Skip();
				run.Split(19);
				Assert.Equal("-", run.Read(PREVIOUS + @"Current Comparison\SegmentDifference.txt"));
				Assert.Equal("Ahead", run.Read(PREVIOUS + @"Current Comparison\Sign.txt"));
			}
		}

		[Fact]
		public void FinishShowsTheLastSplit() {
			using (var run = TestRun.Base()) {
				run.PlayBaseRun();
				Assert.Equal("B2", run.Read(@"PreviousSplit\AllSplits\Name.txt"));
				Assert.Equal("Area B", run.Read(@"PreviousSplit\SansSubsplits\Name.txt"));
				Assert.Equal("0:09", run.Read(PREVIOUS + "SegmentTime.txt"));
				Assert.Equal("0:48", run.Read(PREVIOUS + "RunTime.txt"));
				Assert.Equal("−1.0", run.Read(PREVIOUS + @"Current Comparison\SegmentDifference.txt"));
				Assert.Equal("−2.0", run.Read(PREVIOUS + @"Current Comparison\RunDifference.txt"));
				Assert.Equal("0.00", run.Read(PREVIOUS + "GoldDifference.txt"));
				Assert.Equal("FinishedGainedAhead", run.Read(PREVIOUS + @"Current Comparison\Sign.txt"));
			}
		}

		[Fact]
		public void ResetClearsTheCurrentSplitAtEveryLevel() {
			using (var run = TestRun.Base()) {
				run.PlayBaseRun();
				run.Reset();
				foreach (string level in new[] { "AllSplits", "SansSubsplits" }) {
					Assert.Equal("-", run.Read($@"CurrentSplit\{level}\Name.txt"));
					Assert.Equal("-", run.Read($@"CurrentSplit\{level}\Name_Raw.txt"));
					Assert.Equal("-", run.Read($@"CurrentSplit\{level}\Number.txt"));
				}
			}
		}

		[Fact]
		public void UndoingTheFirstSplitClearsThePreviousSplit() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Undo();
				Assert.Equal("-", run.Read(@"PreviousSplit\AllSplits\Name.txt"));
				Assert.Equal("-", run.Read(PREVIOUS + "GoldDifference.txt"));
				Assert.Equal("NotApplicable", run.Read(PREVIOUS + @"Current Comparison\Sign.txt"));
			}
		}

		[Fact]
		public void CountsFollowThePhases() {
			using (var run = TestRun.Base()) {
				// Number, Completed, Remaining, After
				Assert.Equal("- 0 5 -", Counts(run, "AllSplits"));
				run.Start();
				run.Split(9.5);
				Assert.Equal("2 1 4 3", Counts(run, "AllSplits"));
				// On A2, the current member is Area A
				Assert.Equal("1 0 2 1", Counts(run, "SansSubsplits"));
				foreach (double s in TestRun.BaseTimes.Skip(1)) {
					run.Split(s);
				}
				Assert.Equal("- 5 0 -", Counts(run, "AllSplits"));
				Assert.Equal("- 2 0 -", Counts(run, "SansSubsplits"));
			}
		}

		static string Counts(TestRun run, string level) {
			return string.Join(" ", new[] { "Number", "Completed", "Remaining", "After" }.Select(file => run.Read($@"CurrentSplit\{level}\{file}.txt")));
		}
	}
}
