using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Whole runs through every timer phase, compared file by file with the snapshots in Snapshots\. An intended change to the output
	/// updates them in the same change (see Snapshot).
	/// </summary>
	public class SnapshotTests {

		[Fact]
		public void BaseRun() {
			PlayEveryPhase("BaseRun", TestRun.Base());
		}

		[Fact]
		public void BaseRunNoSubsplits() {
			PlayEveryPhase("BaseRunNoSubsplits", TestRun.Base(r => r.Settings.OutputSubsplits = false));
		}

		// Before the start, a gain, a loss, a gold, a skip and its undo, a pause, the finish and the reset
		static void PlayEveryPhase(string name, TestRun run) {
			using (run) {
				var snapshot = new Snapshot(name);
				snapshot.Record("Before the start", run);
				run.Start();
				snapshot.Record("Start", run);
				run.At(5);
				snapshot.Record("At 5", run);
				run.Split(9.5);
				snapshot.Record("A1 at 9.5, gaining time", run);
				run.Split(21);
				snapshot.Record("A2 at 21, losing time", run);
				run.Split(28);
				snapshot.Record("A3 at 28, a gold", run);
				run.At(33.2);
				snapshot.Record("At 33.2", run);
				run.Skip();
				snapshot.Record("Skip B1", run);
				run.Undo();
				snapshot.Record("Undo the skip", run);
				run.Split(39);
				snapshot.Record("B1 at 39", run);
				run.At(42);
				run.Pause();
				snapshot.Record("Pause at 42", run);
				run.Pause();
				snapshot.Record("Resume", run);
				run.Split(48);
				snapshot.Record("B2 at 48, the finish", run);
				run.Reset();
				snapshot.Record("Reset", run);
				snapshot.Verify();
			}
		}

		[Fact]
		public void HistoryAndComparisons() {
			using (var run = TestRun.Base(r => {
				r.AddComparison("Target", 9.2, 19.5, 29, 38.5, 49);
				r.Settings.SetComparisonTicked("Target", true);
			})) {
				// Area A's best is 26.5, from an attempt reset after A3. Area B's is 22, from a finished attempt slower than the PB.
				run.PlayAttempt(9, 18, 26.5);
				run.PlayAttempt(10, 20, 30, 41, 52);
				var snapshot = new Snapshot("HistoryAndComparisons");
				snapshot.Record("Two attempts in the history, Target ticked", run);
				run.Start();
				snapshot.Record("Start", run);
				run.Split(9.5);
				snapshot.Record("A1 at 9.5", run);
				run.Split(21);
				snapshot.Record("A2 at 21", run);
				run.Split(28);
				snapshot.Record("A3 at 28, not a section gold", run);
				run.SelectComparison("Target");
				snapshot.Record("Select Target", run);
				run.Split(39);
				snapshot.Record("B1 at 39", run);
				run.Split(48);
				snapshot.Record("B2 at 48, the finish", run);
				run.Reset();
				snapshot.Record("Reset", run);
				run.Settings.SetComparisonTicked("Target", false);
				run.Tick();
				snapshot.Record("Untick Target", run);
				snapshot.Verify();
			}
		}
	}
}
