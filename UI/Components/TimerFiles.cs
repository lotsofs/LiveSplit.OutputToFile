using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes Timers\: the run timer and each level's segment timer.
	/// </summary>
	class TimerFiles : OutputWriter {

		const string FILE_TIMER_RUN = @"Timers\{0}\RunTimer.txt";
		const string FILE_TIMER_SPLIT = @"Timers\{0}\SegmentTimer_{1}.txt";

		// What the files were last written for, per timing method (indexed by it). Null before the first write.
		readonly (long? Seconds, bool Negative, TimerPhase Phase)?[] _writtenFor = new (long?, bool, TimerPhase)?[2];

		public TimerFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		// Whether the files would change: the whole seconds as DurationString shows them, or the phase, which the segment timers depend on.
		// Runs every frame, so it compares a value tuple rather than boxing values into a GraphicsCache.
		bool HasTimerChanged(TimingMethod method) {
			TimeSpan? time = State.CurrentTime[method];
			// Truncated toward zero, like DurationString, so -0.5 s and 0.5 s differ only in sign. Null without a time.
			var key = (time?.Ticks / TimeSpan.TicksPerSecond, time < TimeSpan.Zero, State.CurrentPhase);
			if (_writtenFor[(int)method] == key) return false;
			_writtenFor[(int)method] = key;
			return true;
		}

		public void WriteTimer(TimingMethod method) {
			if (!HasTimerChanged(method)) { return; }

			TimeSpan? currentTime = (method == TimingMethod.RealTime ? State.CurrentTime.RealTime : State.CurrentTime.GameTime);

			// Run Timer. "-" when there is none.
			MakeFile(FILE_TIMER_RUN, TimeFormatter.DurationString(currentTime), method);

			// Split Timer
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				//SplitLevel level = _splitLevels[lvl];
				MakeFile(FILE_TIMER_SPLIT, SegmentTimer(method, lvl, currentTime), method, lvl);
			}
		}

		string SegmentTimer(TimingMethod method, int lvl, TimeSpan? currentTime) {
			if (!currentTime.HasValue) {
				// There is no current timer. Probably the game timer if it isn't in use.
				return "-";
			}

			TimeSpan runTimer = currentTime.Value;

			if (State.CurrentPhase == TimerPhase.NotRunning) {
				// Timer isn't running. This does not mean the time is at 0:00, due to the "Start timer at" split setting.
				return TimeFormatter.DurationString(runTimer);
			}

			// The timer for the current split is the current full run timer, minus the finish time of the previous split
			// However, due to subsplits, it is not immediately obvious what 'the previous split' is. So we must figure that out.
			int previousSplitIndex = GetPreviousSubsplitIndex(lvl);

			if (previousSplitIndex == -1) {
				// We are on the first split. The split timer equals the run timer.
				return TimeFormatter.DurationString(runTimer);
			}

			// We know now what the previous split we need to compare against is. Get its time.
			TimeSpan? previousSplitFinishTime = LiveRunTime(method, lvl, previousSplitIndex);

			if (!previousSplitFinishTime.HasValue) {
				// We skipped the previous split. No timer can be calculated.
				return "-";
			}

			// Do the necessary subtraction.
			TimeSpan splitTimer = runTimer - previousSplitFinishTime.Value;
			return TimeFormatter.DurationString(splitTimer);
		}
	}
}
