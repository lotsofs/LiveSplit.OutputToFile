using LiveSplit.Model;
using LiveSplit.OutputToFile.UI.Components;
using LiveSplit.OutputToFile.UI.Components.OutputToFile;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// The LiveSplit side of the component: it reacts to timer events and to changes seen in Update(),
	/// and decides when each group of files gets written. The writers do the writing:
	/// RunInfoFiles (root), TimerFiles (Timers\), SplitFiles (CurrentSplit\, PreviousSplit\)
	/// and SplitListFiles (SplitList\, CompletedSplits\).
	/// </summary>
	public class OutputToFileComponent : IComponent {

		protected InfoTextComponent InternalComponent { get; set; }
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

		RunInfoFiles _runInfoFiles;
		TimerFiles _timerFiles;
		SplitFiles _splitFiles;
		SplitListFiles _splitListFiles;

		#region livesplitoverhead

		public OutputToFileComponent(LiveSplitState state) {
			Settings = new OutputToFileSettings();
			Cache = new GraphicsCache();

			state.OnStart += state_OnStart;
			state.OnSplit += state_OnSplitChange;
			state.OnSkipSplit += state_OnSplitChange;
			state.OnUndoSplit += state_OnSplitChange;
			state.OnReset += state_OnReset;

			State = state;
			_levels = new SplitLevels();
			_runInfoFiles = new RunInfoFiles(State, Settings, _levels);
			_timerFiles = new TimerFiles(State, Settings, _levels);
			_splitFiles = new SplitFiles(State, Settings, _levels);
			_splitListFiles = new SplitListFiles(State, Settings, _levels);
			CalculateSegments();
		}

		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }

		// We will be adding the ability to display the component across two rows in our settings menu.
		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }

		public Control GetSettingsControl(LayoutMode mode) {
			Settings.Mode = mode;
			return Settings;
		}

		public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document) {
			return Settings.GetSettings(document);
		}

		public void SetSettings(System.Xml.XmlNode settings) {
			Settings.SetSettings(settings);
		}

		void state_OnStart(object sender, EventArgs e) {
			CalculateSegments();
			WriteSplits();
		}

		void state_OnSplitChange(object sender, EventArgs e) {
			WriteSplits();
		}

		void state_OnReset(object sender, TimerPhase e) {
			// Write every file in full once per attempt
			_runInfoFiles.ForgetWrittenFiles();
			_timerFiles.ForgetWrittenFiles();
			_splitFiles.ForgetWrittenFiles();
			_splitListFiles.ForgetWrittenFiles();
			WriteSplits();
			CalculateSegments();
		}

		#endregion

		void CalculateSegments() {
			_levels.GenerateSubsplitLevels(State.Run, Settings.OutputSubsplits, TimingMethod.RealTime);
			_levels.GenerateSubsplitLevels(State.Run, Settings.OutputSubsplits, TimingMethod.GameTime);
			_levels.CalculatePBSegments(State.Run, TimingMethod.RealTime);
			_levels.CalculatePBSegments(State.Run, TimingMethod.GameTime);
		}

		void WriteSplits() {
			_splitFiles.WriteSplitInformation();
			_splitFiles.WriteSplitTimes(TimingMethod.RealTime);
			_splitFiles.WriteSplitTimes(TimingMethod.GameTime);
			_splitListFiles.WriteSplitLists(TimingMethod.RealTime);
			_splitListFiles.WriteSplitLists(TimingMethod.GameTime);
		}

		// This is the function where we decide what needs to be displayed at this moment in time,
		// and tell the internal component to display it. This function is called hundreds to
		// thousands of times per second.
		public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
			if (Settings.OutputTimer) {
				_timerFiles.WriteTimer(TimingMethod.RealTime);
				_timerFiles.WriteTimer(TimingMethod.GameTime);
			}
			Cache.Restart();
			// Check for basic 'user loaded or changed splits' etc
			Cache["AttemptHistoryCount"] = state.Run.AttemptCount;
			Cache["GameName"] = state.Run.GameName;
			Cache["CategoryName"] = state.Run.CategoryName;
			Cache["PlatformName"] = state.Run.Metadata.PlatformName;
			Cache["FolderPath"] = Settings.FolderPath;
			Cache["SplitsBefore"] = Settings.SplitsBefore;
			Cache["SplitsAfter"] = Settings.SplitsAfter;
			Cache["TimerPhase"] = state.CurrentPhase;
			if (Cache.HasChanged) {
				CalculateSegments();
				_runInfoFiles.Write();
				WriteSplits();
			}
		}

		// This function is called when the component is removed from the layout, or when LiveSplit
		// closes a layout with this component in it.
		public void Dispose() {
			State.OnStart -= state_OnStart;
			State.OnSplit -= state_OnSplitChange;
			State.OnSkipSplit -= state_OnSplitChange;
			State.OnUndoSplit -= state_OnSplitChange;
			State.OnReset -= state_OnReset;
		}

		// I do not know what this is for.
		public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
	}
}
