using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.Options.SettingsFactories;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// A run driven headless through LiveSplit's TimerModel, with the component writing every output into a fresh folder.
	/// Each action is followed by Tick(), one LiveSplit frame: the timer events only count, and Update() writes.
	/// Game time is exact: it changes only through At() and the actions that take a time. Real time drifts, so check GameTime files.
	/// Expected strings use LiveSplit's minus sign − (U+2212) for negatives.
	/// </summary>
	sealed class TestRun : IDisposable {

		/// <summary>
		/// The regression base run: every best segment is 9 s, and it has no segment history, so the sections in SansSubsplits
		/// (Area A and Area B) have no best segment, and completing them counts as a gold. Play it with PlayBaseRun().
		/// </summary>
		public static readonly (string Name, double? Pb, double? Best)[] BaseSplits = {
			("-A1", 10, 9), ("-A2", 20, 9), ("{Area A}A3", 30, 9), ("-B1", 40, 9), ("{Area B}B2", 50, 9),
		};

		/// <summary>
		/// The base run's split times, in seconds of game time: A1 gains time, A2 loses it, A3 is a gold, and the run beats the PB.
		/// </summary>
		public static readonly double[] BaseTimes = { 9.5, 21, 28, 39, 48 };

		public LiveSplitState State { get; }
		public TimerModel Model { get; }
		public OutputToFileComponent Component { get; }
		public OutputToFileSettings Settings => Component.Settings;
		// The output folder: a new one, unless setup sets another
		public string Folder => Settings.FolderPath;

		readonly TempFolder _folder = new TempFolder();

		static TestRun() {
			// LiveSplit.Core saves speedrun.com's game list in the current directory once a run has a game name (see MakeRun): keep it in bin\
			Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
		}

		public static TestRun Base(Action<TestRun> setup = null) => new TestRun(setup, BaseSplits);

		public TestRun(params (string Name, double? Pb, double? Best)[] splits) : this(null, splits) { }

		/// <param name="setup">Changes the run or the settings before the first frame</param>
		/// <param name="splits">Each split's name, PB split time and best segment, in seconds; null for none</param>
		public TestRun(Action<TestRun> setup, params (string Name, double? Pb, double? Best)[] splits) {
			// StandardSettingsFactory throws outside LiveSplit; construct Settings directly
			State = new LiveSplitState(MakeRun(splits), null, new Layout(), new StandardLayoutSettingsFactory().Create(), new LiveSplit.Options.Settings());
			Model = new TimerModel { CurrentState = State };
			// Null until set, outside LiveSplit
			State.CurrentComparison = Run.PersonalBestComparisonName;

			Component = new OutputToFileComponent(State);
			Settings.FolderPath = _folder.Path;
			Settings.OutputTimer = true;
			Settings.OutputCompletedSplits = true;
			Settings.OutputCustomVariables = true;
			setup?.Invoke(this);
			Tick();
		}

		/// <summary>
		/// A run with no game name: with one, LiveSplit.Core downloads speedrun.com's game list, about 2 MB, and the test run waits
		/// about a minute for it to finish, unless the current directory holds the list from an earlier run
		/// </summary>
		public static Run MakeRun(params (string Name, double? Pb, double? Best)[] splits) {
			var run = new Run(new StandardComparisonGeneratorsFactory()) { CategoryName = "Any%" };
			foreach (var (name, pb, best) in splits) {
				run.Add(new Segment(name, T(pb), T(best), null, new Time()));
			}
			return run;
		}

		/// <summary>
		/// A time with both timing methods, or no time for null
		/// </summary>
		public static Time T(double? s) => s == null ? new Time() : new Time(TimeSpan.FromSeconds(s.Value), TimeSpan.FromSeconds(s.Value));

		/// <summary>
		/// Adds a custom comparison with these split times, as Edit Splits does
		/// </summary>
		public void AddComparison(string name, params double?[] splitTimes) {
			State.Run.CustomComparisons.Add(name);
			for (int i = 0; i < splitTimes.Length; i++) {
				State.Run[i].Comparisons[name] = T(splitTimes[i]);
			}
		}

		#region actions

		/// <summary>
		/// One LiveSplit frame, then wait for the files it queued
		/// </summary>
		public void Tick() {
			Component.Update(null, State, 0, 0, LayoutMode.Vertical);
			Component.WaitForFileWrites();
		}

		public void Start() {
			Model.Start();
			Model.InitializeGameTime();
			// From here on, game time changes only through SetGameTime
			State.IsGameTimePaused = true;
			SetClocks(0);
			Tick();
		}

		public void At(double s) {
			SetClocks(s);
			Tick();
		}

		public void Split(double s) {
			SetClocks(s);
			Model.Split();
			Tick();
		}

		public void Skip() {
			Model.SkipSplit();
			Tick();
		}

		public void Undo() {
			Model.UndoSplit();
			Tick();
		}

		/// <summary>
		/// Pauses, or resumes when paused. Real time stays frozen while paused.
		/// </summary>
		public void Pause() {
			Model.Pause();
			Tick();
		}

		/// <summary>
		/// Resets as LiveSplit does: the attempt goes into the history, and the best segments and the PB are updated
		/// </summary>
		public void Reset() {
			Model.Reset();
			Tick();
		}

		/// <summary>
		/// Plays the base run's splits at BaseTimes, which finishes the run
		/// </summary>
		public void PlayBaseRun() {
			Start();
			foreach (double s in BaseTimes) {
				Split(s);
			}
		}

		/// <summary>
		/// Plays an attempt and resets, so that it's in the segment history. Null skips a split; fewer times than splits resets mid-run.
		/// </summary>
		public void PlayAttempt(params double?[] splitTimes) {
			Start();
			foreach (double? s in splitTimes) {
				if (s == null) Skip();
				else Split(s.Value);
			}
			Reset();
		}

		/// <summary>
		/// One notch of the mouse wheel over LiveSplit's window
		/// </summary>
		public void ScrollUp() {
			Model.ScrollUp();
			Tick();
		}

		public void ScrollDown() {
			Model.ScrollDown();
			Tick();
		}

		/// <summary>
		/// Selects a comparison, as the right-click menu or the server does: no event
		/// </summary>
		public void SelectComparison(string name) {
			State.CurrentComparison = name;
			Tick();
		}

		/// <summary>
		/// Edits the run in place, as Edit Splits does
		/// </summary>
		public void Edit(Action<IRun> edit) {
			edit(State.Run);
			State.CallRunManuallyModified();
			Tick();
		}

		/// <summary>
		/// Renames a custom comparison, as Edit Splits does: in the run, then ComparisonRenamed and RunManuallyModified
		/// </summary>
		public void RenameComparison(string oldName, string newName) {
			IRun run = State.Run;
			run.CustomComparisons[run.CustomComparisons.IndexOf(oldName)] = newName;
			foreach (ISegment segment in run) {
				segment.Comparisons[newName] = segment.Comparisons[oldName];
				segment.Comparisons.Remove(oldName);
			}
			State.CallComparisonRenamed(new RenameEventArgs { OldName = oldName, NewName = newName });
			State.CallRunManuallyModified();
			Tick();
		}

		/// <summary>
		/// Removes a custom comparison, as Edit Splits does, which reports it as renamed to "Current Comparison"
		/// </summary>
		public void RemoveComparison(string name) {
			State.Run.CustomComparisons.Remove(name);
			State.CallComparisonRenamed(new RenameEventArgs { OldName = name, NewName = "Current Comparison" });
			foreach (ISegment segment in State.Run) {
				segment.Comparisons.Remove(name);
			}
			State.CallRunManuallyModified();
			Tick();
		}

		/// <summary>
		/// Replaces the run, as LiveSplit does when splits are opened, and when Edit Splits is cancelled, with a copy of the run
		/// taken when it opened (State.Run.Clone()): no event but RunManuallyModified
		/// </summary>
		public void SetRun(IRun run) {
			State.Run = run;
			State.CallRunManuallyModified();
			Tick();
		}

		// Set both clocks. Game time is exact; real time is only close.
		void SetClocks(double s) {
			State.AdjustedStartTime = TimeStamp.Now - TimeSpan.FromSeconds(s);
			State.SetGameTime(TimeSpan.FromSeconds(s));
		}

		#endregion

		#region reading the files

		public string Read(string file) => File.ReadAllText(Path.Combine(Folder, file));

		/// <summary>
		/// A list file's lines. Each line ends with '\n', the last one included.
		/// </summary>
		public string[] Lines(string file) {
			string contents = Read(file);
			if (contents.EndsWith("\n")) contents = contents.Substring(0, contents.Length - 1);
			return contents.Split('\n');
		}

		public bool Exists(string file) => File.Exists(Path.Combine(Folder, file));

		/// <summary>
		/// Every file in the folder, by path relative to it, in ordinal order
		/// </summary>
		public SortedDictionary<string, string> AllFiles() {
			var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
			foreach (string path in Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)) {
				files[path.Substring(Folder.Length + 1)] = File.ReadAllText(path);
			}
			return files;
		}

		#endregion

		public void Dispose() {
			Component.Dispose();
			_folder.Dispose();
		}
	}

	/// <summary>
	/// A new empty folder in %TEMP%, deleted on Dispose
	/// </summary>
	sealed class TempFolder : IDisposable {

		public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LiveSplit.OutputToFile.Tests", Guid.NewGuid().ToString("N"));

		public TempFolder() {
			Directory.CreateDirectory(Path);
		}

		public void Dispose() {
			try {
				Directory.Delete(Path, true);
			}
			catch (IOException) {
				// A file still open; %TEMP% gets cleaned eventually
			}
		}
	}
}
