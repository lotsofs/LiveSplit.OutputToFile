using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiveSplit.UI.Components {
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

		protected int GetPreviousSubsplitIndex(int level) {
			List<int> indices = Levels[level].Indices;
			if (State.CurrentPhase == TimerPhase.Ended) {
				return indices.Count - 1;
			}
			if (State.CurrentPhase == TimerPhase.NotRunning) {
				return -2;
			}
			for (int i = 0; i < indices.Count; i++) {
				if (indices[i] >= State.CurrentSplitIndex) {
					return i - 1;
				}
			}
			return -3; // Shouldn't happen
		}

		protected int GetCurrentSubsplitIndex(int level) {
			List<int> indices = Levels[level].Indices;
			if (State.CurrentPhase == TimerPhase.Ended) {
				return indices.Count;
			}
			if (State.CurrentPhase == TimerPhase.NotRunning) {
				return -1;
			}
			int currentSplitIndex = -1;
			for (int i = 0; i < indices.Count; i++) {
				if (indices[i] >= State.CurrentSplitIndex) {
					return i;
				}
			}
			return -3; // Shouldn't happen
		}

		#endregion

		#region live times and deltas

		// This attempt's times and deltas for a member of a level, by local index. They are computed from the run
		// whenever files are written, never stored: PB times and best segments don't change during a run, and a
		// completed split's SplitTime only changes when it is undone.
		// A negative local index (the sentinels of the subsplit index functions) gives null.
		// Subtracting TimeSpan? values gives null when either is null.

		/// <summary>
		/// This attempt's split time
		/// </summary>
		protected TimeSpan? LiveRunTime(TimingMethod method, int lvl, int localIndex) {
			if (localIndex < 0) return null;
			return State.Run[Levels[lvl].Indices[localIndex]].SplitTime[method];
		}

		/// <summary>
		/// This attempt's segment time at this level: the split time minus the previous member's.
		/// The first member's segment time is its split time.
		/// </summary>
		protected TimeSpan? LiveSegmentTime(TimingMethod method, int lvl, int localIndex) {
			if (localIndex <= 0) return LiveRunTime(method, lvl, localIndex);
			return LiveRunTime(method, lvl, localIndex) - LiveRunTime(method, lvl, localIndex - 1);
		}

		protected TimeSpan? RunDelta(TimingMethod method, int lvl, int localIndex) {
			if (localIndex < 0) return null;
			return LiveRunTime(method, lvl, localIndex) - State.Run[Levels[lvl].Indices[localIndex]].PersonalBestSplitTime[method];
		}

		protected TimeSpan? SegmentDelta(TimingMethod method, int lvl, int localIndex) {
			if (localIndex < 0) return null;
			return LiveSegmentTime(method, lvl, localIndex) - Levels[lvl].PbSegmentTimes[method][localIndex];
		}

		/// <summary>
		/// Segment time minus best segment. Golds only exist at the last level.
		/// </summary>
		protected TimeSpan? GoldDelta(TimingMethod method, int lvl, int localIndex) {
			if (lvl != Levels.Count - 1 || localIndex < 0) return null;
			return LiveSegmentTime(method, lvl, localIndex) - State.Run[Levels[lvl].Indices[localIndex]].BestSegmentTime[method];
		}

		/// <summary>
		/// Whether a completed member's segment is a gold: it beat the best segment, or the split has none yet.
		/// Golds only exist at the last level.
		/// </summary>
		protected bool IsGold(TimingMethod method, int lvl, int localIndex) {
			if (lvl != Levels.Count - 1 || localIndex < 0) return false;
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
				subsplitLevelTag = "SansSubsplits_Level" + subsplitLevel;
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
