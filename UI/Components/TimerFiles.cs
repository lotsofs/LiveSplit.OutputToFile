using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes Timers\: the run timer and each level's segment timer.
	/// </summary>
	class TimerFiles : OutputWriter {

		const string FILE_TIMER_RUN = @"Timers\{0}\RunTimer.txt";
		const string FILE_TIMER_SPLIT = @"Timers\{0}\SegmentTimer_{1}.txt";

		// Per timing method (indexed by it), per level: the time the segment timer counts from, so it shows the run timer minus this.
		// Null if the previous member was skipped. Finding that member takes a search, so these are only rebuilt after SegmentStartsChanged().
		readonly TimeSpan?[][] _segmentStarts = new TimeSpan?[2][];
		bool _segmentStartsStale = true;

		// Per timing method (indexed by it): what the files were last written showing, in the terms that decide their contents.
		// [0] is the run timer and [1 + lvl] each level's segment timer. A null phase means the files must be written.
		readonly (long Seconds, bool Negative)?[][] _shown = new (long, bool)?[2][];
		readonly TimerPhase?[] _shownPhase = new TimerPhase?[2];

		public TimerFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		/// <summary>
		/// Call when a segment timer may count from somewhere else: after a split, skip or undo, and when the levels are rebuilt.
		/// </summary>
		public void SegmentStartsChanged() {
			_segmentStartsStale = true;
		}

		public void WriteTimer(TimingMethod method) {
			if (_segmentStartsStale) UpdateSegmentStarts();

			// Before the start, the splits' "Start timer at" time, as LiveSplit's Timer component shows it
			TimeSpan? runTime = State.CurrentPhase == TimerPhase.NotRunning ? State.Run.Offset : State.CurrentTime[method];
			if (!HasTimerChanged(method, runTime)) { return; }

			// Run Timer. "-" when there is none.
			MakeFile(FILE_TIMER_RUN, TimeFormatter.DurationString(runTime, FormatAccuracy.Seconds), method);

			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				MakeFile(FILE_TIMER_SPLIT, TimeFormatter.DurationString(SegmentTime(method, lvl, runTime), FormatAccuracy.Seconds), method, lvl);
			}
		}

		// Whether any of the files would change: a timer's whole seconds or sign as shown, or the phase.
		// Runs every frame, so it compares value tuples in place rather than building the strings.
		bool HasTimerChanged(TimingMethod method, TimeSpan? runTime) {
			int m = (int)method;
			bool changed = _shownPhase[m] != State.CurrentPhase;
			_shownPhase[m] = State.CurrentPhase;
			changed |= Remember(_shown[m], 0, Shown(runTime));
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				changed |= Remember(_shown[m], 1 + lvl, Shown(SegmentTime(method, lvl, runTime)));
			}
			return changed;
		}

		// Stores a value, and says whether it differs from the one stored before
		static bool Remember((long, bool)?[] shown, int i, (long, bool)? value) {
			if (shown[i] == value) return false;
			shown[i] = value;
			return true;
		}

		// A time as DurationString shows it in whole seconds: truncated toward zero, and the sign, since -0.5 s shows as −0:00. Null shows as "-".
		static (long Seconds, bool Negative)? Shown(TimeSpan? time) {
			if (time == null) return null;
			return (time.Value.Ticks / TimeSpan.TicksPerSecond, time.Value < TimeSpan.Zero);
		}

		// Rebuilds the segment starts for the current levels and phase, and has every file written next time
		void UpdateSegmentStarts() {
			for (int m = 0; m < _segmentStarts.Length; m++) {
				var starts = new TimeSpan?[Levels.Count];
				for (int lvl = 0; lvl < Levels.Count; lvl++) {
					starts[lvl] = SegmentStart((TimingMethod)m, lvl);
				}
				_segmentStarts[m] = starts;
				_shown[m] = new (long, bool)?[1 + Levels.Count];
				_shownPhase[m] = null;
			}
			_segmentStartsStale = false;
		}

		// What a level's segment timer counts from before the finish. Null if the previous member was skipped.
		TimeSpan? SegmentStart(TimingMethod method, int lvl) {
			if (State.CurrentPhase == TimerPhase.NotRunning) {
				// The segment timer equals the run timer. That isn't always 0:00, due to the "Start timer at" split setting.
				return TimeSpan.Zero;
			}

			// Due to subsplits, the previous member at this level isn't simply the previous split
			int previousSplitIndex = GetPreviousSubsplitIndex(lvl);

			if (previousSplitIndex == -1) {
				// We are on the first split. The segment timer equals the run timer.
				return TimeSpan.Zero;
			}

			return LiveRunTime(method, lvl, previousSplitIndex);
		}

		// The time a level's segment timer shows. Null, shown as "-", if there is no run timer (game time not initialized)
		// or the previous member was skipped.
		TimeSpan? SegmentTime(TimingMethod method, int lvl, TimeSpan? runTime) {
			if (runTime == null) return null;
			if (State.CurrentPhase == TimerPhase.Ended) {
				// The last member's segment, frozen
				return LiveSegmentTime(method, lvl, Levels[lvl].Indices.Count - 1);
			}
			return runTime - _segmentStarts[(int)method][lvl];
		}
	}
}
