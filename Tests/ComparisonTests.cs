using LiveSplit.Model;
using System;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// The CurrentComparison folder, and a folder per ticked or custom comparison
	/// </summary>
	public class ComparisonTests {

		const string PREVIOUS = @"PreviousSplit\AllSplits\GameTime\";

		// The base run with a custom comparison Target, a second faster than the PB at every split
		static TestRun WithTarget(Action<TestRun> setup = null) {
			return TestRun.Base(r => {
				r.AddComparison("Target", 9, 19, 29, 39, 49);
				setup?.Invoke(r);
			});
		}

		[Fact]
		public void CurrentComparisonFollowsTheSelection() {
			using (var run = WithTarget()) {
				Assert.Equal(Run.PersonalBestComparisonName, run.Read("CurrentComparison.txt"));
				run.Start();
				run.Split(9.5);
				Assert.Equal("−0.5", run.Read(PREVIOUS + @"CurrentComparison\RunDifference.txt"));
				run.SelectComparison("Target");
				Assert.Equal("Target", run.Read("CurrentComparison.txt"));
				Assert.Equal("+0.5", run.Read(PREVIOUS + @"CurrentComparison\RunDifference.txt"));
			}
		}

		[Fact]
		public void UnknownComparisonHasNoDifferences() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				// The server can select any name
				run.SelectComparison("Nothing");
				Assert.Equal("Nothing", run.Read("CurrentComparison.txt"));
				Assert.Equal("-", run.Read(PREVIOUS + @"CurrentComparison\RunDifference.txt"));
				Assert.Equal("NotApplicable", run.Read(PREVIOUS + @"CurrentComparison\Sign.txt"));
			}
		}

		[Fact]
		public void TickedComparisonGetsAFolder() {
			using (var run = WithTarget(r => r.Settings.SetComparisonTicked("Target", true))) {
				run.Start();
				run.Split(9.5);
				Assert.Equal("+0.5", run.Read(PREVIOUS + @"Target\RunDifference.txt"));
				Assert.Equal("−0.5", run.Read(PREVIOUS + @"CurrentComparison\RunDifference.txt"));
				run.Settings.SetComparisonTicked("Target", false);
				run.Tick();
				Assert.Equal("", run.Read(PREVIOUS + @"Target\RunDifference.txt"));
			}
		}

		[Fact]
		public void TickedComparisonsFolderHasNoSpaces() {
			using (var run = TestRun.Base(r => r.Settings.SetComparisonTicked(Run.PersonalBestComparisonName, true))) {
				run.Start();
				run.Split(9.5);
				Assert.Equal("−0.5", run.Read(PREVIOUS + @"PersonalBest\RunDifference.txt"));
				Assert.False(run.Exists(PREVIOUS + @"Personal Best\RunDifference.txt"));
			}
		}

		[Fact]
		public void AllCustomComparisonsGetAFolder() {
			using (var run = WithTarget(r => {
				r.AddComparison("Other", 11, 21, 31, 41, 51);
				r.Settings.AllCustomComparisons = true;
			})) {
				run.Start();
				run.Split(9.5);
				Assert.Equal("+0.5", run.Read(PREVIOUS + @"Target\RunDifference.txt"));
				Assert.Equal("−1.5", run.Read(PREVIOUS + @"Other\RunDifference.txt"));
				// Personal Best isn't a custom comparison
				Assert.False(run.Exists(PREVIOUS + @"PersonalBest\RunDifference.txt"));
			}
		}

		[Theory]
		[InlineData("Personal Best", "PersonalBest")]
		[InlineData(" My  Rival ", "MyRival")]
		[InlineData("A/B", "A_B")]
		[InlineData("a:b*c?", "a_b_c_")]
		[InlineData("Run.", "Run_")]
		[InlineData("a. ", "a_")]
		[InlineData("", "_")]
		[InlineData(" ", "_")]
		public void ComparisonFolderDropsSpacesAndReplacesWhatAFolderNameCantHave(string comparison, string folder) {
			Assert.Equal(folder, SplitLevels.ComparisonFolder(comparison));
		}
	}
}
