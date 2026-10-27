using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes CustomVariables\: a file per custom variable of the run, with its value. Auto splitters and LiveSplit's server set them
	/// with no event, maybe every frame, and the server from its own thread. So Write() compares them every frame, and writes only
	/// when one changed. A variable added asks for a full rewrite instead (FilesChanged).
	/// </summary>
	class CustomVariableFiles : OutputWriter {

		// Not a template: its files are named after the variables, whose names may hold braces
		const string FOLDER_CUSTOMVARIABLES = @"CustomVariables";

		// The variables as last written, name and value, in the dictionary's order. Null until the first write.
		string[] _names;
		string[] _values;

		public CustomVariableFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		/// <summary>
		/// Whether a variable was added since the last write, or none was written yet. Its file must then be written by a full rewrite:
		/// a rewrite empties the files it doesn't write only when they differ from the last one's, so it would miss a file written between.
		/// </summary>
		public bool FilesChanged() => _names == null || State.Run.Metadata.CustomVariables.Count != _names.Length;

		/// <summary>
		/// Writes the files if a value changed since the last write
		/// </summary>
		/// <param name="all">Write every file even so, as a full rewrite must: the files it doesn't write are emptied</param>
		public void Write(bool all) {
			Dictionary<string, CustomVariable> variables = State.Run.Metadata.CustomVariables;
			if (!all && !HaveChanged(variables)) return;
			if (TryCopy(variables, out string[] names, out string[] values) && (all || names.Length == _names?.Length)) {
				_names = names;
				_values = values;
			}
			// Changed during the copy, or a variable added since FilesChanged(): the next frame copies again, in a full rewrite
			// for a new variable. A rewrite keeps the files meanwhile.
			else if (!all || _names == null) {
				return;
			}
			for (int i = 0; i < _names.Length; i++) {
				// The MakeFile that doesn't format, so braces in a name are kept. No value is "-", as LiveSplit's Text component shows it.
				MakeFile(Path.Combine(FOLDER_CUSTOMVARIABLES, RunInfoFiles.VariableFileName(_names[i])), _values[i] ?? "-");
			}
		}

		// Whether a variable was added or its value changed since the last write. Runs every frame, so it allocates nothing,
		// and a string comparison checks the reference first: an unchanged variable costs two reference comparisons.
		bool HaveChanged(Dictionary<string, CustomVariable> variables) {
			if (_names == null || variables.Count != _names.Length) return true;
			try {
				int i = 0;
				foreach (KeyValuePair<string, CustomVariable> variable in variables) {
					// One added during the loop
					if (i == _names.Length) return true;
					if (variable.Key != _names[i] || variable.Value?.Value != _values[i]) return true;
					i++;
				}
				return false;
			}
			catch (InvalidOperationException) {
				// Added to from the server's thread during the loop
				return true;
			}
		}

		// Copies the variables' names and values. False if the server's thread changed them meanwhile.
		static bool TryCopy(Dictionary<string, CustomVariable> variables, out string[] names, out string[] values) {
			names = values = null;
			var nameList = new List<string>(variables.Count);
			var valueList = new List<string>(variables.Count);
			try {
				// A foreach, which notices a change: ToArray() and ToList() copy without checking
				foreach (KeyValuePair<string, CustomVariable> variable in variables) {
					// An entry still being added: Dictionary counts it before filling it in
					if (variable.Key == null || variable.Value == null) return false;
					nameList.Add(variable.Key);
					valueList.Add(variable.Value.Value);
				}
			}
			catch (InvalidOperationException) {
				return false;
			}
			names = nameList.ToArray();
			values = valueList.ToArray();
			return true;
		}
	}
}
