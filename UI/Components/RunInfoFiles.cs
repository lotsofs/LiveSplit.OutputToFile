using LiveSplit.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// Writes the files in the root of the output folder: run information, split counts and the selected comparison.
	/// Also writes Variables\, a file per variable of the run.
	/// </summary>
	class RunInfoFiles : OutputWriter {

		const string FILE_INFO_GAMENAME = @"GameName.txt";
		const string FILE_INFO_CATEGORYNAME = @"CategoryName.txt";
		const string FILE_INFO_PLATFORMNAME = @"PlatformName.txt";
		const string FILE_INFO_REGIONNAME = @"RegionName.txt";
		const string FILE_INFO_USESEMULATOR = @"UsesEmulator.txt";
		const string FILE_INFO_VARIABLES = @"Variables.txt";
		const string FILE_INFO_SPLITCOUNT = @"TotalSplits_{1}.txt";
		const string FILE_INFO_ATTEMPTCOUNT = @"AttemptCount.txt";
		const string FILE_INFO_FINISHEDRUNSCOUNT = @"FinishedRunsCount.txt";
		const string FILE_INFO_CURRENTCOMPARISON = @"CurrentComparison.txt";
		// Not a template: its files are named after the variables, whose names may hold braces
		const string FOLDER_VARIABLES = @"Variables";

		public RunInfoFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels, FileQueue files) : base(state, settings, levels, files) { }

		public void Write() {
			RunMetadata metadata = State.Run.Metadata;
			MakeFile(FILE_INFO_GAMENAME, State.Run.GameName);
			MakeFile(FILE_INFO_CATEGORYNAME, State.Run.CategoryName);
			MakeFile(FILE_INFO_PLATFORMNAME, metadata.PlatformName);
			MakeFile(FILE_INFO_REGIONNAME, metadata.RegionName);
			MakeFile(FILE_INFO_USESEMULATOR, metadata.UsesEmulator ? "Emulator" : "");
			WriteVariables(metadata.VariableValueNames);
			MakeFile(FILE_INFO_ATTEMPTCOUNT, State.Run.AttemptCount.ToString());
			int finishedRunsInHistory = State.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
			MakeFile(FILE_INFO_FINISHEDRUNSCOUNT, (finishedRunsInHistory + (State.CurrentPhase == TimerPhase.Ended ? 1 : 0)).ToString());
			MakeFile(FILE_INFO_CURRENTCOMPARISON, Levels.CurrentComparison);
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				MakeFile(FILE_INFO_SPLITCOUNT, Levels[lvl].Indices.Count.ToString(), null, lvl);
			}
		}

		// The variables (speedrun.com's subcategories) as stored in the splits: name → value. Taken once, as the caller does:
		// LiveSplit's clean-up after downloading the game replaces the dictionary from another thread, but never changes the one it replaces.
		void WriteVariables(IDictionary<string, string> variables) {
			var labels = new List<string>();
			foreach (KeyValuePair<string, string> variable in variables) {
				// The MakeFile that doesn't format, so braces in a name are kept
				MakeFile(Path.Combine(FOLDER_VARIABLES, VariableFileName(variable.Key)), variable.Value);
				if (!string.IsNullOrEmpty(variable.Value)) {
					labels.Add(VariableLabel(variable.Key, variable.Value));
				}
			}
			MakeFile(FILE_INFO_VARIABLES, string.Join(", ", labels));
		}

		// A variable's file: its name without the '?' that yes/no variables end with, characters a file name can't hold replaced,
		// and the trailing dots and spaces that Windows would drop trimmed
		static string VariableFileName(string name) {
			char[] invalid = Path.GetInvalidFileNameChars();
			string fileName = new string(name.TrimEnd('?').Select(c => invalid.Contains(c) ? '_' : c).ToArray());
			return fileName.TrimEnd('.', ' ') + ".txt";
		}

		// A variable as LiveSplit adds it to the category in its title: Glitches? = Yes is "Glitches", No is "No Glitches", other values as they are
		static string VariableLabel(string name, string value) {
			string valueLower = value.ToLowerInvariant();
			if (valueLower == "yes") return name.TrimEnd('?');
			if (valueLower == "no") return "No " + name.TrimEnd('?');
			return value;
		}
	}
}
