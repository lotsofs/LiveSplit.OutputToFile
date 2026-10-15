using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using System;
using System.Collections.Generic;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes Timers\: the run timer and each level's segment timer.
	/// </summary>
	class TimerFiles : OutputWriter {

		const string FILE_TIMER_RUN = @"Timers\{0}\RunTimer.txt";
		const string FILE_TIMER_SPLIT = @"Timers\{0}\SegmentTimer_{1}.txt";

		GraphicsCache Cache { get; } = new GraphicsCache();

		public TimerFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		bool HasTimerChanged(TimingMethod method) {
			Cache.Restart();
			TimeSpan time = (method == TimingMethod.RealTime ? State.CurrentTime.RealTime : State.CurrentTime.GameTime) ?? TimeSpan.Zero;
			string cacheString = method == TimingMethod.RealTime ? "RealTimeSeconds" : "GameTimeSeconds";
			Cache[cacheString] = time.Seconds;
			return Cache.HasChanged;
		}

		public void WriteTimer(TimingMethod method) {
			if (!HasTimerChanged(method)) { return; }

			TimeSpan? currentTime = (method == TimingMethod.RealTime ? State.CurrentTime.RealTime : State.CurrentTime.GameTime);
			if (!currentTime.HasValue) {
				// There is no current timer. Probably the game timer if it isn't in use.
				MakeFile(FILE_TIMER_RUN, "-", method);
				for (int lvl = 0; lvl < Levels.Count; lvl++) {
					MakeFile(FILE_TIMER_SPLIT, "-", method, lvl);
				}
				return;
			}

			TimeSpan runTimer = currentTime.Value;
			string timeString = TimeFormatter.DurationString(runTimer);

			if (State.CurrentPhase == TimerPhase.NotRunning) {
				// Timer isn't running. This does not mean the time is at 0:00, due to the "Start timer at" split setting.
				MakeFile(FILE_TIMER_RUN, timeString, method);
				for (int lvl = 0; lvl < Levels.Count; lvl++) {
					MakeFile(FILE_TIMER_SPLIT, timeString, method, lvl);
				}
				return;
			}

			// Run Timer
			MakeFile(FILE_TIMER_RUN, timeString, method);

			// Split Timer
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				//SplitLevel level = _splitLevels[lvl];
				List<int> indices = Levels[lvl].Indices;
				// The timer for the current split is the current full run timer, minus the finish time of the previous split
				// However, due to subsplits, it is not immediately obvious what 'the previous split' is. So we must figure that out.
				int previousSplitIndex = GetPreviousSubsplitIndex(lvl);

				if (previousSplitIndex == -1) {
					// We are on the first split. The split timer equals the run timer.
					MakeFile(FILE_TIMER_SPLIT, timeString, method, lvl);
					continue;
				}

				// We know now what the previous split we need to compare against is. Get its time.
				ISegment previousSplit = State.Run[previousSplitIndex];
				Time previousTime = previousSplit.SplitTime;
				TimeSpan? previousSplitFinishTime = method == TimingMethod.RealTime ? previousTime.RealTime : previousTime.GameTime;

				if (!previousSplitFinishTime.HasValue) {
					// We skipped the previous split. No timer can be calculated.
					MakeFile(FILE_TIMER_SPLIT, "-", method, lvl);
					continue;
				}

				// Do the necessary subtraction and output.
				TimeSpan splitTimer = runTimer - previousSplitFinishTime.Value;
				timeString = TimeFormatter.DurationString(splitTimer);
				MakeFile(FILE_TIMER_SPLIT, timeString, method, lvl);
			}
		}
	}
}
