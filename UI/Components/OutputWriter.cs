using LiveSplit.Model;
using System.Collections.Generic;
using System.IO;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Base for the classes that each write one group of output files.
	/// Holds what they share: the timer state, the settings, the subsplit levels and file writing.
	/// </summary>
	abstract class OutputWriter {

		protected LiveSplitState State { get; }
		protected OutputToFileSettings Settings { get; }
		protected SplitLevels Levels { get; }

		// Full path → the contents this writer last wrote there. Files whose contents haven't changed are skipped.
		readonly Dictionary<string, string> _written = new Dictionary<string, string>();

		protected OutputWriter(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels) {
			State = state;
			Settings = settings;
			Levels = levels;
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
			string path = Path.Combine(settingsPath, fileName);
			if (_written.TryGetValue(path, out string written) && written == contents) return;
			try {
				File.WriteAllText(path, contents);
			}
			catch (DirectoryNotFoundException) {
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllText(path, contents);
			}
			_written[path] = contents;
		}

		/// <summary>
		/// Makes the next write of every file go to disk, even if its contents haven't changed.
		/// Brings back files deleted or edited outside LiveSplit.
		/// </summary>
		public void ForgetWrittenFiles() {
			_written.Clear();
		}

		#endregion
	}
}
