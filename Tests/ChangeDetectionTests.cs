using System.IO;
using System.Linq;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// What makes Update() rewrite the files, and the files it empties when they're no longer written
	/// </summary>
	public class ChangeDetectionTests {

		const string NAMES = @"SplitList\AllSplits\Names.txt";

		[Fact]
		public void EditingTheSplitsRewritesTheFiles() {
			using (var run = TestRun.Base()) {
				run.Edit(r => r[0].Name = "-X1");
				Assert.Equal("X1", run.Lines(NAMES)[0]);
				run.Edit(r => r[0].PersonalBestSplitTime = TestRun.T(11));
				Assert.Equal("0:11", run.Lines(@"SplitList\AllSplits\GameTime\CurrentComparison\Time_Run_Upcoming.txt")[0]);
			}
		}

		[Fact]
		public void ChangingTheRegionRewritesTheFiles() {
			using (var run = TestRun.Base()) {
				Assert.Equal("", run.Read("RegionName.txt"));

				run.State.Run.Metadata.RegionName = "USA / NTSC";
				run.Tick();

				Assert.Equal("USA / NTSC", run.Read("RegionName.txt"));

				run.Settings.RegionFormat = RegionNameFormat.Abbreviation;
				run.Tick();
				Assert.Equal("USA / NTSC", run.Read("RegionName.txt"));
			}
		}

		[Fact(Skip = "SSN: a split renamed without an event, as LiveSplit's server does, shows only after the next timer event")]
		public void RenamingASplitWithoutAnEventRewritesTheFiles() {
			using (var run = TestRun.Base()) {
				run.Start();
				// LiveSplit's server command setcurrentsplitname renames the split in place, and raises no event
				run.State.Run[0].Name = "-Renamed";
				run.Tick();
				Assert.Equal("Renamed", run.Read(@"CurrentSplit\AllSplits\Name.txt"));
				Assert.Equal("Renamed", run.Lines(NAMES)[0]);
			}
		}

		[Fact]
		public void ReplacingTheRunRewritesTheFiles() {
			using (var run = TestRun.Base()) {
				// Close Splits replaces the run without an event
				run.State.Run = TestRun.MakeRun(("One", 10, 9), ("Two", 20, 9));
				run.Tick();
				Assert.Equal(new[] { "One", "Two" }, run.Lines(NAMES));
			}
		}

		[Fact]
		public void TurningAnOutputOffEmptiesItsFiles() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Settings.OutputTimer = false;
				run.Settings.OutputSplitList = false;
				run.Settings.OutputCompletedSplits = false;
				run.Tick();
				Assert.Equal("", run.Read(@"Timers\GameTime\RunTimer.txt"));
				Assert.Equal("", run.Read(NAMES));
				Assert.Equal("", run.Read(@"CompletedSplits\AllSplits\Names.txt"));
				Assert.Equal("A2", run.Read(@"CurrentSplit\AllSplits\Name.txt"));

				run.Settings.OutputSplitList = true;
				run.Tick();
				Assert.Equal("A1", run.Lines(NAMES)[0]);
			}
		}

		[Fact]
		public void TurningSubsplitsOffEmptiesTheOtherLevels() {
			using (var run = TestRun.Base()) {
				run.Settings.OutputSubsplits = false;
				run.Tick();
				Assert.Equal("", run.Read(@"CurrentSplit\SansSubsplits\Name.txt"));
				Assert.Equal("", run.Read("TotalSplits_SansSubsplits.txt"));
				Assert.Equal("5", run.Read("TotalSplits_AllSplits.txt"));
			}
		}

		[Fact]
		public void NewComponentEmptiesLeftoversAndKeepsOtherFiles() {
			using (var before = TestRun.Base()) {
				File.WriteAllText(Path.Combine(before.Folder, "Notes.txt"), "keep");
				File.WriteAllText(Path.Combine(before.Folder, @"CurrentSplit\Old.txt"), "old");
				using (var after = new TestRun(r => r.Settings.FolderPath = before.Folder, ("A", 10, 9), ("B", 20, 9))) {
					// SansSubsplits is a level the new run doesn't have
					Assert.Equal("", after.Read(@"CurrentSplit\SansSubsplits\Name.txt"));
					Assert.Equal("", after.Read("TotalSplits_SansSubsplits.txt"));
					Assert.Equal("", after.Read(@"CurrentSplit\Old.txt"));
					Assert.Equal("keep", after.Read("Notes.txt"));
					Assert.Equal("2", after.Read("TotalSplits_AllSplits.txt"));
				}
			}
		}

		[Fact]
		public void ResetWritesEveryFileAgain() {
			using (var run = TestRun.Base()) {
				string category = Path.Combine(run.Folder, "CategoryName.txt");
				run.Start();
				File.WriteAllText(category, "edited");
				// Unchanged contents aren't written again
				run.Split(9.5);
				Assert.Equal("edited", File.ReadAllText(category));
				run.Reset();
				Assert.Equal("Any%", File.ReadAllText(category));
			}
		}

		[Fact]
		public void ClosingLiveSplitWritesItsReset() {
			using (var closed = TestRun.Base())
			using (var reset = TestRun.Base()) {
				foreach (var run in new[] { closed, reset }) {
					run.Start();
					run.Split(9.5);
				}
				// LiveSplit resets the timer as it closes, then disposes the component with no frame in between
				closed.Model.Reset();
				closed.Component.Dispose();
				reset.Reset();
				Assert.Equal("-", closed.Read(@"CurrentSplit\AllSplits\Name.txt"));
				Assert.Equal(reset.AllFiles().Where(f => !f.Key.Contains("RealTime")), closed.AllFiles().Where(f => !f.Key.Contains("RealTime")));
			}
		}

		[Fact]
		public void DisposingWithNothingChangedWritesNothing() {
			using (var run = TestRun.Base()) {
				string category = Path.Combine(run.Folder, "CategoryName.txt");
				run.Start();
				File.WriteAllText(category, "edited");
				// As when another layout is opened
				run.Component.Dispose();
				Assert.Equal("edited", File.ReadAllText(category));
			}
		}
	}
}
