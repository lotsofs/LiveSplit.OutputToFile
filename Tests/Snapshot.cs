using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Sdk;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// The whole output of a run, step by step, compared with Snapshots\{name}.txt in the source tree.
	/// Each step lists the files whose contents changed since the step before; the first lists every file.
	/// A file is its path, then a line "  |" + each line of its contents, so "x" is one line and "x\n" two.
	/// RealTime files only show whether they have contents, since real time drifts.
	/// Run the tests with the environment variable UPDATE_SNAPSHOTS=1 to write the snapshots instead, then review their diff.
	/// </summary>
	sealed class Snapshot {

		const string STEP = "=== ";
		const string LINE = "  |";
		const string UPDATE_VARIABLE = "UPDATE_SNAPSHOTS";

		readonly string _name;
		readonly string _folder;
		readonly List<string> _lines = new List<string>();
		// Each file's contents as shown at the last step
		readonly Dictionary<string, string> _shown = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <param name="source">Leave out: the folder of the calling source file, which holds Snapshots\</param>
		public Snapshot(string name, [CallerFilePath] string source = "") {
			_name = name;
			_folder = Path.Combine(Path.GetDirectoryName(source), "Snapshots");
		}

		public void Record(string step, TestRun run) {
			_lines.Add(STEP + step);
			foreach (var file in run.AllFiles()) {
				string shown = Shown(file.Key, file.Value);
				if (_shown.TryGetValue(file.Key, out string before) && before == shown) continue;
				_shown[file.Key] = shown;
				_lines.Add(file.Key);
				_lines.AddRange(shown.Split('\n').Select(line => LINE + line));
			}
		}

		static string Shown(string file, string contents) {
			if (contents.Length == 0) return "(empty)";
			if (file.Contains(@"\RealTime\")) return "(real time)";
			return contents.Replace("\r", @"\r").Replace("\t", @"\t");
		}

		public void Verify() {
			string expectedPath = Path.Combine(_folder, _name + ".txt");
			string receivedPath = Path.Combine(_folder, _name + ".received.txt");
			if (File.Exists(receivedPath)) File.Delete(receivedPath);
			if (Environment.GetEnvironmentVariable(UPDATE_VARIABLE) == "1") {
				Write(expectedPath);
				return;
			}
			if (!File.Exists(expectedPath)) {
				Write(receivedPath);
				throw new XunitException($"There is no snapshot {_name}.txt yet. The output is in {receivedPath}. Run the tests with {UPDATE_VARIABLE}=1 to write it.");
			}

			// Line by line, so that git's line endings don't matter
			string[] expected = File.ReadAllLines(expectedPath);
			int count = Math.Min(expected.Length, _lines.Count);
			int line = 0;
			while (line < count && expected[line] == _lines[line]) line++;
			if (line == count && expected.Length == _lines.Count) return;

			Write(receivedPath);
			throw new XunitException(
				$"The output differs from snapshot {_name}.txt at line {line + 1}, {Where(line)}.\n" +
				$"Expected: {(line < expected.Length ? expected[line] : "(end of snapshot)")}\n" +
				$"Actual:   {(line < _lines.Count ? _lines[line] : "(end of output)")}\n" +
				$"The whole output is in {receivedPath}. If the change is intended, run the tests with {UPDATE_VARIABLE}=1 and review the snapshot's diff.");
		}

		// The step and file that the output's line belongs to
		string Where(int line) {
			string step = null, file = null;
			for (int i = Math.Min(line, _lines.Count - 1); i >= 0 && step == null; i--) {
				if (_lines[i].StartsWith(STEP)) step = _lines[i].Substring(STEP.Length);
				else if (file == null && !_lines[i].StartsWith(LINE)) file = _lines[i];
			}
			return file == null ? $"step \"{step}\"" : $"step \"{step}\", file {file}";
		}

		void Write(string path) {
			Directory.CreateDirectory(_folder);
			File.WriteAllText(path, string.Join("\n", _lines) + "\n", new UTF8Encoding(false));
		}
	}
}
