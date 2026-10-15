using LiveSplit.Model;
using System.Linq;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes the files in the root of the output folder: run information and split counts.
	/// </summary>
	class RunInfoFiles : OutputWriter {

		const string FILE_INFO_GAMENAME = @"GameName.txt";
		const string FILE_INFO_CATEGORYNAME = @"CategoryName.txt";
		const string FILE_INFO_PLATFORMNAME = @"PlatformName.txt";
		const string FILE_INFO_SPLITCOUNT = @"TotalSplits_{1}.txt";
		const string FILE_INFO_ATTEMPTCOUNT = @"AttemptCount.txt";
		const string FILE_INFO_FINISHEDRUNSCOUNT = @"FinishedRunsCount.txt";

		public RunInfoFiles(LiveSplitState state, OutputToFileSettings settings, SplitLevels levels) : base(state, settings, levels) { }

		public void Write() {
			MakeFile(FILE_INFO_GAMENAME, State.Run.GameName);
			MakeFile(FILE_INFO_CATEGORYNAME, State.Run.CategoryName);
			MakeFile(FILE_INFO_PLATFORMNAME, State.Run.Metadata.PlatformName);
			MakeFile(FILE_INFO_ATTEMPTCOUNT, State.Run.AttemptCount.ToString());
			int finishedRunsInHistory = State.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
			MakeFile(FILE_INFO_FINISHEDRUNSCOUNT, (finishedRunsInHistory + (State.CurrentPhase == TimerPhase.Ended ? 1 : 0)).ToString());
			for (int lvl = 0; lvl < Levels.Count; lvl++) {
				MakeFile(FILE_INFO_SPLITCOUNT, Levels[lvl].Indices.Count.ToString(), null, lvl);
			}
		}
	}
}
