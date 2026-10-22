using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Timers\
	/// </summary>
	public class TimerTests {

		const string TIMERS = @"Timers\GameTime\";

		[Fact]
		public void SegmentTimerCountsFromTheLevelsPreviousMember() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.5);
				run.Split(21);
				run.Split(28);
				run.At(33.2);
				Assert.Equal("0:33", run.Read(TIMERS + "RunTimer.txt"));
				Assert.Equal("0:05", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
				Assert.Equal("0:05", run.Read(TIMERS + "SegmentTimer_SansSubsplits.txt"));
			}
		}

		[Fact]
		public void ResetAfterAMinuteShowsZero() {
			using (var run = TestRun.Base()) {
				run.Start();
				for (double s = 0.5; s <= 60.5; s += 0.5) {
					run.At(s);
				}
				Assert.Equal("1:00", run.Read(TIMERS + "RunTimer.txt"));
				run.Reset();
				Assert.Equal("0:00", run.Read(TIMERS + "RunTimer.txt"));
			}
		}

		[Fact]
		public void SegmentTimerRestartsOnASplitAndTicksOnItsOwnSecond() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Split(9.7);
				Assert.Equal("0:00", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
				run.At(10.5);
				Assert.Equal("0:00", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
				run.At(10.7);
				Assert.Equal("0:01", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
			}
		}

		[Fact]
		public void SegmentTimerAfterASkipShowsNothing() {
			using (var run = TestRun.Base()) {
				run.Start();
				run.Skip();
				run.At(12);
				Assert.Equal("-", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
				Assert.Equal("0:12", run.Read(TIMERS + "SegmentTimer_SansSubsplits.txt"));
			}
		}

		[Fact]
		public void SegmentTimerShowsTheLastSegmentAfterTheFinish() {
			using (var run = TestRun.Base()) {
				run.PlayBaseRun();
				Assert.Equal("0:48", run.Read(TIMERS + "RunTimer.txt"));
				Assert.Equal("0:09", run.Read(TIMERS + "SegmentTimer_AllSplits.txt"));
				Assert.Equal("0:20", run.Read(TIMERS + "SegmentTimer_SansSubsplits.txt"));
			}
		}
	}
}
