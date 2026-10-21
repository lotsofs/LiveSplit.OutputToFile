using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Base for the classes that each write one group of output files.
	/// Holds what they share: the timer state, the settings, the subsplit levels and the file queue.
	/// </summary>
	abstract class OutputWriter {

		protected LiveSplitState State { get; }
		protected OutputToFileSettings Settings { get; }
		protected SplitLevels Levels { get; }
		protected FileQueue Files { get; }

		protected OutputWriter(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) {
			State = state;
			Settings = settings;
			Levels = levels;
			Files = files;
		}

		#region subsplit indices

		/// <summary>
		/// The local index of the level's last completed member, or -1 if there is none:
		/// -1 before the start and on the level's first member, the last member after the finish.
		/// </summary>
		protected int GetPreviousSubsplitIndex(int level) {
			if (State.CurrentPhase == TimerPhase.NotRunning) {
				return -1;
			}
			if (State.CurrentPhase == TimerPhase.Ended) {
				return Levels[level].Indices.Count - 1;
			}
			return GetCurrentSubsplitIndex(level) - 1;
		}

		/// <summary>
		/// The local index of the level's current member while the timer is running or paused, or -1 when there is no current split:
		/// before the start and after the finish.
		/// </summary>
		protected int GetCurrentSubsplitIndex(int level) {
			if (State.CurrentPhase != TimerPhase.Running && State.CurrentPhase != TimerPhase.Paused) {
				return -1;
			}
			// The first member at or after the current split. The last split is a member of every level, so there is one.
			List<int> indices = Levels[level].Indices;
			for (int i = 0; i < indices.Count; i++) {
				if (indices[i] >= State.CurrentSplitIndex) {
					return i;
				}
			}
			throw new InvalidOperationException($"Level {level} has no member at or after split {State.CurrentSplitIndex}: the levels don't match the run.");
		}

		#endregion

		#region live times and deltas

		// This attempt's times and deltas for a member of a level, by local index. They are computed from the run
		// whenever files are written, never stored: PB times and best segments don't change during a run, and a
		// completed split's SplitTime only changes when it is undone.
		// The local index must be a member's: callers handle -1 (no member) from the subsplit index functions themselves.
		// Subtracting TimeSpan? values gives null when either is null.

		/// <summary>
		/// This attempt's split time
		/// </summary>
		protected TimeSpan? LiveRunTime(TimingMethod method, int lvl, int localIndex) {
			return State.Run[Levels[lvl].Indices[localIndex]].SplitTime[method];
		}

		/// <summary>
		/// This attempt's segment time at this level: the split time minus the previous member's.
		/// The first member's segment time is its split time.
		/// </summary>
		protected TimeSpan? LiveSegmentTime(TimingMethod method, int lvl, int localIndex) {
			if (localIndex == 0) return LiveRunTime(method, lvl, localIndex);
			return LiveRunTime(method, lvl, localIndex) - LiveRunTime(method, lvl, localIndex - 1);
		}

		protected TimeSpan? RunDelta(TimingMethod method, int lvl, int localIndex) {
			return LiveRunTime(method, lvl, localIndex) - State.Run[Levels[lvl].Indices[localIndex]].PersonalBestSplitTime[method];
		}

		protected TimeSpan? SegmentDelta(TimingMethod method, int lvl, int localIndex) {
			return LiveSegmentTime(method, lvl, localIndex) - Levels[lvl].PbSegmentTimes[method][localIndex];
		}

		/// <summary>
		/// Segment time minus best segment. Golds only exist at the last level.
		/// </summary>
		protected TimeSpan? GoldDelta(TimingMethod method, int lvl, int localIndex) {
			if (lvl != Levels.Count - 1) return null;
			return LiveSegmentTime(method, lvl, localIndex) - State.Run[Levels[lvl].Indices[localIndex]].BestSegmentTime[method];
		}

		/// <summary>
		/// Whether a completed member's segment is a gold: it beat the best segment, or the split has none yet.
		/// Golds only exist at the last level.
		/// </summary>
		protected bool IsGold(TimingMethod method, int lvl, int localIndex) {
			if (lvl != Levels.Count - 1) return false;
			TimeSpan? liveSegmentTime = LiveSegmentTime(method, lvl, localIndex);
			if (liveSegmentTime == null) return false;
			TimeSpan? goldTime = State.Run[Levels[lvl].Indices[localIndex]].BestSegmentTime[method];
			return goldTime == null || liveSegmentTime < goldTime;
		}

		#endregion

		#region file writing

		protected void MakeFile(string fileName, string contents, TimingMethod? method, int subsplitLevel) {
			string subsplitLevelTag;
			if (subsplitLevel == Levels.Count - 1) {
				subsplitLevelTag = "AllSplits";
			}
			else if (subsplitLevel == 0) {
				subsplitLevelTag = "SansSubsplits";
			}
			else {
				// Named after the depth it leaves out: level 1 drops the splits with 2+ dashes
				subsplitLevelTag = "SansSubsplits_Level" + (subsplitLevel + 1);
			}
			string file = string.Format(fileName, method.ToString(), subsplitLevelTag);
			string c = contents;
			//if (formatTime) {
			//	c = CustomTimeFormat(contents, showPlus);
			//}
			MakeFile(file, c);
		}

		/// <summary>
		/// Write to file
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="contents"></param>
		protected void MakeFile(string fileName, string contents, TimingMethod method) {
			string file = string.Format(fileName, method.ToString());
			string c = contents;
			//if (formatTime) {
			//	c = CustomTimeFormat(contents, showPlus);
			//}
			MakeFile(file, c);
		}

		protected void MakeFile(string fileName, string contents) {
			string settingsPath = Settings.FolderPath;
			if (string.IsNullOrEmpty(settingsPath)) return;
			string path;
			try {
				path = Path.Combine(settingsPath, fileName);
			}
			catch (ArgumentException ex) {
				// Invalid characters in the folder setting
				Files.ReportError(fileName, ex);
				return;
			}
			Files.Write(path, fileName, contents);
		}

		#endregion
	}
}
