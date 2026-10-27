using LiveSplit.Model;
using LiveSplit.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// The LiveSplit side of the component: it reacts to timer events and to changes seen in Update(),
	/// and decides when each group of files gets written. The writers do the writing:
	/// RunInfoFiles (root), TimerFiles (Timers\), SplitFiles (CurrentSplit\, PreviousSplit\)
	/// and SplitListFiles (SplitList\, CompletedSplits\).
	/// </summary>
	public class OutputToFileComponent : IComponent {

		public OutputToFileSettings Settings { get; set; }
		protected LiveSplitState State { get; set; }

		public GraphicsCache Cache { get; set; }

		public string ComponentName => "Output to File";

		public float HorizontalWidth { get { return 0f; } }
		public float MinimumWidth => 0f;
		public float VerticalHeight { get; set; }
		public float MinimumHeight { get; set; }

		public float PaddingTop => 0f;
		public float PaddingLeft => 0f;
		public float PaddingBottom => 0f;
		public float PaddingRight => 0f;

		public IDictionary<string, Action> ContextMenuControls => null;

		// One instance, shared by the writers. CalculateSegments() regenerates it in place.
		SplitLevels _levels;
		// Writes the files on a background thread. Shared by the writers.
		FileQueue _files;
		// Timer events can arrive on another thread: LiveSplit's server raises them on its connection's thread. So the handlers
		// only count them, and Update() does the work on the UI thread. Two threads using the levels at once would break them.
		// Counts starts, splits, skips, undos and resets. A cache key in Update().
		int _timerEvents;
		// Counts resets, and the count Update() last acted on
		int _resets;
		int _resetsSeen;
		// Counts RunManuallyModified, which LiveSplit raises after each edit in Edit Splits and when splits are opened. A cache key in Update().
		int _runEdits;
		// A cache key for each comparison written, with its name: the comparison's time at the last split. LiveSplit regenerates
		// its generated comparisons (Average Segments, …) on reset, after edits and from its Settings dialog. A reset sent through
		// the server, or the Settings dialog, can change them after the rewrite, with no event; their last split time changes then.
		List<(string Key, string Name)> _comparisonChecks = new List<(string, string)>();
		// The run's split names as of the last rewrite. LiveSplit's server renames a split in place (setsplitname,
		// setcurrentsplitname), with no event, so Update() compares them every frame.
		string[] _splitNames = new string[0];

		RunInfoFiles _runInfoFiles;
		TimerFiles _timerFiles;
		SplitFiles _splitFiles;
		SplitListFiles _splitListFiles;

		// The folders the writers write to, in the output folder. A full rewrite empties every file in them that it doesn't write,
		// and every file in the output folder itself that matches OUTPUT_ROOT_FILES. The other root files are always written.
		static readonly string[] OUTPUT_FOLDERS = { "CurrentSplit", "PreviousSplit", "SplitList", "CompletedSplits", "Timers", "Variables" };
		// TotalSplits_{level}.txt, and TotalSplits.txt from before subsplits
		const string OUTPUT_ROOT_FILES = "TotalSplits*.txt";

		#region livesplitoverhead

		public OutputToFileComponent(LiveSplitState state) {
			Settings = new OutputToFileSettings();
			Cache = new GraphicsCache();

			state.OnStart += state_OnStart;
			state.OnSplit += state_OnSplitChange;
			state.OnSkipSplit += state_OnSplitChange;
			state.OnUndoSplit += state_OnSplitChange;
			state.OnReset += state_OnReset;
			state.RunManuallyModified += state_RunManuallyModified;

			State = state;
			_levels = new SplitLevels();
			_files = new FileQueue();
			_runInfoFiles = new RunInfoFiles(State, Settings, _levels, _files);
			_timerFiles = new TimerFiles(State, Settings, _levels, _files);
			_splitFiles = new SplitFiles(State, Settings, _levels, _files);
			_splitListFiles = new SplitListFiles(State, Settings, _levels, _files);
			CalculateSegments(State.CurrentComparison);
		}

		// Draws nothing: the component only writes files
		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }

		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }

		public Control GetSettingsControl(LayoutMode mode) {
			// LiveSplit asks for the control each time it opens the settings, on the UI thread
			Settings.ShowRunComparisons(State.Run);
			return Settings;
		}

		public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document) {
			return Settings.GetSettings(document);
		}

		public void SetSettings(System.Xml.XmlNode settings) {
			Settings.SetSettings(settings);
		}

		// The handlers only count, so they are safe on any thread. Update() rewrites everything on the next frame.

		void state_OnStart(object sender, EventArgs e) {
			Interlocked.Increment(ref _timerEvents);
		}

		void state_OnSplitChange(object sender, EventArgs e) {
			Interlocked.Increment(ref _timerEvents);
		}

		void state_OnReset(object sender, TimerPhase e) {
			Interlocked.Increment(ref _resets);
			Interlocked.Increment(ref _timerEvents);
		}

		void state_RunManuallyModified(object sender, EventArgs e) {
			// The run was edited in place, or replaced
			Interlocked.Increment(ref _runEdits);
		}

		#endregion

		// comparison: the one selected in LiveSplit, read once per rewrite, so that every file uses the same one
		void CalculateSegments(string comparison) {
			IRun run = State.Run;
			// Before the levels and the writers read the names: a rename after this is seen next frame
			_splitNames = run.Select(s => s.Name).ToArray();
			_levels.GenerateSubsplitLevels(run, Settings.OutputSubsplits);
			// Outside LiveSplit, no comparison may be selected
			_levels.ChooseComparisons(run, comparison ?? Run.PersonalBestComparisonName, Settings.IsComparisonTicked, Settings.AllCustomComparisons);
			_levels.CalculateComparisonSegments(run);
			_levels.CalculateBestSegments(run, TimingMethod.RealTime);
			_levels.CalculateBestSegments(run, TimingMethod.GameTime);
			_comparisonChecks = _levels.Comparisons.Select(c => c.Name).Distinct().Select(name => ("LastSplit " + name, name)).ToList();
			// The levels, or the phase, may have changed what the segment timers count from
			_timerFiles.SegmentStartsChanged();
		}

		void WriteSplits() {
			_splitFiles.WriteSplitInformation();
			_splitFiles.WriteSplitTimes(TimingMethod.RealTime);
			_splitFiles.WriteSplitTimes(TimingMethod.GameTime);
			_splitListFiles.WriteSplitLists(TimingMethod.RealTime);
			_splitListFiles.WriteSplitLists(TimingMethod.GameTime);
		}

		// Called once per LiveSplit frame, on the UI thread. Rewrites every file when a cache key below has changed;
		// the timer files have their own check.
		public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
			// Before any state: an event comes after its change to the state, so a change that this frame reads half made is counted, and rewritten, next frame
			int timerEvents = Volatile.Read(ref _timerEvents);
			int resets = Volatile.Read(ref _resets);
			int runEdits = Volatile.Read(ref _runEdits);
			// Write errors happen on the background thread; the settings control may only be touched here, on the UI thread
			if (_files.TakeErrorChange(out string writeError)) {
				Settings.ShowWriteError(writeError);
			}
			if (resets != _resetsSeen) {
				_resetsSeen = resets;
				// Write every file in full once per attempt. A reset is also a timer event, so the rewrite below follows.
				_files.ForgetWrittenFiles();
			}
			Cache.Restart();
			// Check for basic 'user loaded or changed splits' etc
			Cache["AttemptHistoryCount"] = state.Run.AttemptCount;
			Cache["GameName"] = state.Run.GameName;
			Cache["CategoryName"] = state.Run.CategoryName;
			// LiveSplit downloads the game from speedrun.com in the background, then clears the platform, region and variables
			// the game doesn't have, with no event
			Cache["PlatformName"] = state.Run.Metadata.PlatformName;
			Cache["RegionName"] = state.Run.Metadata.RegionName;
			Cache["UsesEmulator"] = state.Run.Metadata.UsesEmulator;
			// Compared by reference. That clean-up replaces the dictionary; Edit Splits changes it in place, and raises RunManuallyModified.
			Cache["Variables"] = state.Run.Metadata.VariableValueNames;
			Cache["RunEdits"] = runEdits;
			Cache["TimerEvents"] = timerEvents;
			// Compared by reference. Close Splits replaces the run without raising RunManuallyModified.
			Cache["Run"] = state.Run;
			Cache["FolderPath"] = Settings.FolderPath;
			Cache["SplitsBefore"] = Settings.SplitsBefore;
			Cache["SplitsAfter"] = Settings.SplitsAfter;
			Cache["OutputTimer"] = Settings.OutputTimer;
			Cache["OutputSplitList"] = Settings.OutputSplitList;
			Cache["OutputSubsplits"] = Settings.OutputSubsplits;
			Cache["OutputCompletedSplits"] = Settings.OutputCompletedSplits;
			Cache["TimeAccuracy"] = Settings.TimeAccuracy;
			Cache["DeltaAccuracy"] = Settings.DeltaAccuracy;
			Cache["TimerPhase"] = state.CurrentPhase;
			// Selecting a comparison raises no event
			string comparison = state.CurrentComparison;
			Cache["CurrentComparison"] = comparison;
			Cache["AllCustomComparisons"] = Settings.AllCustomComparisons;
			Cache["TickedComparisons"] = Settings.TickedComparisonsVersion;
			ISegment lastSplit = state.Run[state.Run.Count - 1];
			foreach (var check in _comparisonChecks) {
				Cache[check.Key] = lastSplit.Comparisons[check.Name];
			}
			// A full rewrite queues every file the settings and levels call for. Those it doesn't are emptied.
			bool rewrite = Cache.HasChanged || SplitNamesChanged(state.Run);
			if (rewrite) {
				_files.BeginSweep();
				CalculateSegments(comparison);
				_runInfoFiles.Write();
				WriteSplits();
			}
			// After a rewrite, this queues every timer file: CalculateSegments() has called SegmentStartsChanged()
			if (Settings.OutputTimer) {
				_timerFiles.WriteTimer(TimingMethod.RealTime);
				_timerFiles.WriteTimer(TimingMethod.GameTime);
			}
			if (rewrite) {
				_files.EndSweep(Settings.FolderPath, OUTPUT_FOLDERS, OUTPUT_ROOT_FILES);
			}
		}

		// Whether a split's name changed since the last rewrite. A string comparison checks the reference first,
		// so an unchanged run costs one reference comparison per split.
		bool SplitNamesChanged(IRun run) {
			if (run.Count != _splitNames.Length) return true;
			for (int i = 0; i < run.Count; i++) {
				if (run[i].Name != _splitNames[i]) return true;
			}
			return false;
		}

		// Called when LiveSplit closes, on Close Splits (which switches to a layout with only a timer), when another layout
		// is opened, and when the component is removed from the layout. Can be called twice.
		public void Dispose() {
			State.OnStart -= state_OnStart;
			State.OnSplit -= state_OnSplitChange;
			State.OnSkipSplit -= state_OnSplitChange;
			State.OnUndoSplit -= state_OnSplitChange;
			State.OnReset -= state_OnReset;
			State.RunManuallyModified -= state_RunManuallyModified;
			// LiveSplit resets the timer when it closes, and on Close Splits, then disposes the component with no frame in between.
			// One last frame writes that reset. When nothing has changed since the last frame, it writes nothing.
			try {
				Update(null, State, 0, 0, LayoutMode.Vertical);
			}
			catch (Exception ex) {
				// Thrown out of LiveSplit's closing, it would stop LiveSplit disposing the other components and stopping its server
				Log.Error(ex);
			}
			// Let queued files finish, but don't hang LiveSplit on a stuck drive
			_files.WaitForWrites(2000);
		}

		/// <summary>
		/// Blocks until every file queued so far is written. For tests: files are written on a background thread.
		/// </summary>
		public void WaitForFileWrites() {
			_files.WaitForWrites(Timeout.Infinite);
		}

		// LiveSplit only uses this to decide when to redraw the layout, so it has no visible effect: the component draws nothing
		public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
	}
}
