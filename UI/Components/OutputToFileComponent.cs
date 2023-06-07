using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace LiveSplit.UI.Components {
	public class OutputToFileComponent : IComponent {
		// This internal component does the actual heavy lifting. Whenever we want to do something
		// like display text, we will call the appropriate function on the internal component.
		protected InfoTextComponent InternalComponent { get; set; }
		// This is how we will access all the settings that the user has set.
		public OutputToFileSettings Settings { get; set; }
		// This object contains all of the current information about the splits, the timer, etc.
		protected LiveSplitState CurrentState { get; set; }

		public GraphicsCache Cache { get; set; }
		protected ShortTimeFormatter Formatter = new ShortTimeFormatter();

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

		enum Signs {
			Undetermined,
			NotApplicable,
			Ahead,
			Behind,
			Gained,
			Lost,
			Gold,
			GainedAhead,
			GainedBehind,
			LostAhead,
			LostBehind,
			GoldAhead,
			GoldBehind,
			PB,
			NoPB,
			PBGained,
			PBLost,
			PBGold,
			NoPBGained,
			NoPBLost,
			NoPBGold,
		}

		bool _writeSplits = false;
		bool _calculateSegs = false;

		public class SplitLevel {
			public Dictionary<TimingMethod, TimeSpan?[]> PbSegmentTimes;
			public Dictionary<TimingMethod, TimeSpan?[]> LiveSegmentTimes;
			public Dictionary<TimingMethod, TimeSpan?[]> RunDeltae;
			public Dictionary<TimingMethod, TimeSpan?[]> SegmentDeltae;
			public Dictionary<TimingMethod, TimeSpan?[]> GoldDeltae;

			public SplitLevel(int count) {
				PbSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
				LiveSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
				RunDeltae = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
				SegmentDeltae = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
				GoldDeltae = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
			}
		}

		SplitLevel[] _splitLevels = new SplitLevel[1];
		List<List<int>> _splitLevelIndices = new List<List<int>>();
		int[] _splitDepths;
		List<List<string>> _splitNamesByLevel;

		//Dictionary<TimingMethod, TimeSpan?[]> pbSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>();
		//Dictionary<TimingMethod, TimeSpan?[]> liveSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>();
		//Dictionary<TimingMethod, TimeSpan?[]> runDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();
		//Dictionary<TimingMethod, TimeSpan?[]> segmentDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();
		//Dictionary<TimingMethod, TimeSpan?[]> goldDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();

		#region filenames

		const string FILE_CURRENT_SEGMENT_TIME = @"CurrentSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_CURRENT_GOLD_TIME = @"CurrentSplit\{1}\{0}\GoldTime.txt";
		const string FILE_CURRENT_RUN_TIME = @"CurrentSplit\{1}\{0}\RunTime.txt";
		const string FILE_CURRENT_NAME = @"CurrentSplit\{1}\Name.txt";
		const string FILE_CURRENT_NAME_RAW = @"CurrentSplit\{1}\Name_Raw.txt";
		const string FILE_CURRENT_INDEX = @"CurrentSplit\{1}\Index.txt";
		const string FILE_CURRENT_REVERSEINDEX = @"CurrentSplit\{1}\ReverseIndex.txt";

		const string FILE_INFO_GAMENAME = @"GameName.txt";
		const string FILE_INFO_CATEGORYNAME = @"CategoryName.txt";
		const string FILE_INFO_SPLITCOUNT = @"TotalSplits_{1}.txt";
		const string FILE_INFO_ATTEMPTCOUNT = @"AttemptCount.txt";
		const string FILE_INFO_FINISHEDRUNSCOUNT = @"FinishedRunsCount.txt";
		const string FILE_TIMER_RUN = @"Timers\{0}\RunTimer.txt";
		const string FILE_TIMER_SPLIT = @"Timers\{0}\SegmentTimer_{1}.txt";

		const string FILE_PREVIOUS_SIGN = @"PreviousSplit\{1}\{0}\Sign.txt";
		const string FILE_PREVIOUS_SEGMENT_TIME = @"PreviousSplit\{1}\{0}\SegmentTime.txt";
		const string FILE_PREVIOUS_RUN_TIME = @"PreviousSplit\{1}\{0}\RunTime.txt";
		const string FILE_PREVIOUS_SEGMENT_DIFFERENCE = @"PreviousSplit\{1}\{0}\SegmentDifference.txt";
		const string FILE_PREVIOUS_GOLD_DIFFERENCE = @"PreviousSplit\{1}\{0}\GoldDifference.txt";
		const string FILE_PREVIOUS_RUN_DIFFERENCE = @"PreviousSplit\{1}\{0}\RunDifference.txt";
		const string FILE_PREVIOUS_NAME = @"PreviousSplit\{1}\Name.txt";
		const string FILE_PREVIOUS_NAME_RAW = @"PreviousSplit\{1}\Name_Raw.txt";

		const string FILE_SPLITLIST_NAMES = @"SplitList\{1}\Names.txt";
		const string FILE_SPLITLIST_NAMES_RAW = @"SplitList\{1}\Names_Raw.txt";
		const string FILE_SPLITLIST_WHITESPACE = @"SplitList\{1}\Names_Indent_{2}.txt";
		const string FILE_SPLITLIST_FINISH_TIME_LIVE = @"SplitList\{1}\{0}\Time_Run_Current.txt";
		const string FILE_SPLITLIST_FINISH_TIME_UPCOMING = @"SplitList\{1}\{0}\Time_Run_Upcoming.txt";
		const string FILE_SPLITLIST_RUN_DELTA = @"SplitList\{1}\{0}\Delta_Run.txt";
		const string FILE_SPLITLIST_GOLD_DELTA = @"SplitList\{1}\{0}\Delta_Gold.txt";
		const string FILE_SPLITLIST_GOLD_TIME_UPCOMING = @"SplitList\{1}\{0}\Time_Gold_Upcoming.txt";
		const string FILE_SPLITLIST_SEGMENT_TIME_UPCOMING = @"SplitList\{1}\{0}\Time_Segment_Upcoming.txt";
		const string FILE_SPLITLIST_SEGMENT_DELTA = @"SplitList\{1}\{0}\Delta_Segment.txt";
		const string FILE_SPLITLIST_CURRENT_SPLIT_HIGHLIGHT = @"SplitList\{1}\Highlight_CurrentSplit.txt";
		const string FILE_SPLITLIST_GOLD_HIGHLIGHT = @"SplitList\{1}\{0}\Highlight_Gold.txt";
		const string FILE_SPLITLIST_AHEAD_GAINED_HIGHLIGHT = @"SplitList\{1}\{0}\Highlight_AheadGained.txt";
		const string FILE_SPLITLIST_AHEAD_LOST_HIGHLIGHT = @"SplitList\{1}\{0}\Highlight_AheadLost.txt";
		const string FILE_SPLITLIST_BEHIND_GAINED_HIGHLIGHT = @"SplitList\{1}\{0}\Highlight_BehindGained.txt";
		const string FILE_SPLITLIST_BEHIND_LOST_HIGHLIGHT = @"SplitList\{1}\{0}\Highlight_BehindLost.txt";

		#endregion

		#region livesplitoverhead

		// This function is called when LiveSplit creates your component. This happens when the
		// component is added to the layout, or when LiveSplit opens a layout with this component
		// already added.
		public OutputToFileComponent(LiveSplitState state) {
			Settings = new OutputToFileSettings();
			Cache = new GraphicsCache();

			state.OnStart += state_OnStart;
			state.OnSplit += state_OnSplitChange;
			state.OnSkipSplit += state_OnSplitChange;
			state.OnUndoSplit += state_OnSplitChange;
			state.OnReset += state_OnReset;

			_calculateSegs = true;

			CurrentState = state;
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
			_writeSplits = true;
			_calculateSegs = true;
		}

		void state_OnSplitChange(object sender, EventArgs e) {
			_writeSplits = true;
		}

		void state_OnReset(object sender, TimerPhase e) {
			_writeSplits = true;
			_calculateSegs = true;
		}

		#endregion

		#region calculatesplittimes

		void GenerateSubsplitLevels(LiveSplitState state, TimingMethod method) {
			// Count how many levels of subsplits there are (including the main top level). Default subsplits component only supports 2 levels (Main + subsplits)
			_splitDepths = new int[state.Run.Count];
			for (int i = 0; i < state.Run.Count; i++) {
				if (!Settings.OutputSubsplits) {
					_splitDepths[i] = 0;
					continue;
				}
				_splitDepths[i] = Array.FindIndex<char>(state.Run[i].Name.ToCharArray(), c => c != '-');
				if (_splitDepths[i] == -1) {
					_splitDepths[i] = state.Run[i].Name.Length;
				}
			}

			// Count what the highest consecutive present level is
			int numberOfLevels = 1;
			for (int i = 1; i <= numberOfLevels; i++) {
				if (_splitDepths.Contains(i)) {
					numberOfLevels++;
				}
			}

			// Squish any splits that skip a non-existent level (eg super long headers "-----------------") down to the deepest possible level
			for (int i = 1; i < _splitDepths.Length; i++) {
				if (_splitDepths[i] >= numberOfLevels) {
					_splitDepths[i] = numberOfLevels - 1;
				}
			}
			_splitDepths[_splitDepths.Length - 1] = 0; // Last split is always top level

			_splitLevelIndices = new List<List<int>>();

			// Mark which splits belong to which level. Level 0 contains only top level splits, and then working down
			for (int lvl = 0; lvl < numberOfLevels; lvl++) {
				_splitLevelIndices.Add(new List<int>());
				for (int splitIndex = 0; splitIndex < state.Run.Count; splitIndex++) {
					if (_splitDepths[splitIndex] <= lvl) {
						_splitLevelIndices[lvl].Add(splitIndex);
					}
				}
			}
			// Move the last level to the start so that the output for 'all splits' is reliable to the user
			//var allSplits = _splitLevelIndices[_splitLevelIndices.Count - 1];
			//_splitLevelIndices.RemoveAt(_splitLevelIndices.Count - 1);
			//_splitLevelIndices.Insert(0, allSplits);

			// Make a list of times and deltae for each level
			_splitLevels = new SplitLevel[numberOfLevels];
			for (int i = 0; i < numberOfLevels; i++) {
				_splitLevels[i] = new SplitLevel(_splitLevelIndices[i].Count);
			}

			// Make a list of all the split names by level, taking into consideration removing dashes and {} parts
			_splitNamesByLevel = new List<List<string>>();
			for (int split = 0; split < state.Run.Count; split++) {
				_splitNamesByLevel.Add(new List<string>());
				string name = state.Run[split].Name;
				for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
					string sectionName = GetSectionHeader(name);
					if (sectionName != null) {
						_splitNamesByLevel[split].Add(sectionName);
						name = name.Substring(sectionName.Length + 2);
						continue;
					}
					_splitNamesByLevel[split].Add(name);
					if (name[0] == '-') {
						name = name.Substring(1);
					}
				}
				//var allSplits2 = _splitNamesByLevel[split][_splitNamesByLevel[split].Count - 1];
				//_splitNamesByLevel[split].RemoveAt(_splitNamesByLevel[split].Count - 1);
				//_splitNamesByLevel[split].Insert(0, allSplits2);
			}
		}

		string GetSectionHeader(string name) {
			if (name[0] != '{') return null;
			int closing = name.IndexOf('}');
			if (closing == -1) return null;
			return name.Substring(1, closing - 1);
		}

		//string GetSubsplitName(string name, int level) {
		//	int levels = _splitLevels.Length;
		//	if (level != 0) {
		//		levels = level;
		//	}
		//	for (int i = 0; i < levels; i++) {
		//		if (name[0] != '{') return name;
		//		int closing = name.IndexOf('}');
		//		if (closing == -1) return name;
		//		name = name.Substring(closing + 1);
		//	}
		//	return name;
		//}

		//string GetSubsplitAdjustedSplitName(LiveSplitState state, int globalSplitIndex, int level) {
		//	string name = state.Run[globalSplitIndex].Name;
		//	int depth = _splitDepths[globalSplitIndex];
		//	if (depth < level) {
		//		name = GetSectionHeader(name.Substring(Math.Min(level, depth)));
		//	}
		//	else {
		//		name = GetSubsplitName(name.Substring(depth), level);
		//	}
		//	return name + level.ToString() + depth.ToString();
		//}



		/// <summary>
		/// Calculate a list of segment times for all splits
		/// </summary>
		/// <param name="state"></param>
		/// <param name="method"></param>
		void CalculatePBSegments(LiveSplitState state, TimingMethod method) {
			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];
				List<int> indices = _splitLevelIndices[lvl];
				level.PbSegmentTimes[method] = new TimeSpan?[indices.Count];
				level.LiveSegmentTimes[method] = new TimeSpan?[indices.Count];
				level.SegmentDeltae[method] = new TimeSpan?[indices.Count];
				level.RunDeltae[method] = new TimeSpan?[indices.Count];
				level.GoldDeltae[method] = new TimeSpan?[indices.Count];

				for (int localIndex = 0; localIndex < _splitLevelIndices[lvl].Count; localIndex++) {
					int globalIndex = indices[localIndex];
					TimeSpan? finishTime = state.Run[globalIndex].PersonalBestSplitTime[method];
					if (globalIndex == 0 || localIndex == 0) {
						// This is the first (Sub)split so segment time = run time
						level.PbSegmentTimes[method][localIndex] = finishTime;
						continue;
					}
					int prevGlobalIndex = indices[localIndex - 1];
					TimeSpan? prevFinishTime = state.Run[prevGlobalIndex].PersonalBestSplitTime[method];
					if (finishTime == null || prevFinishTime == null) {
						// This or the previous split time is missing so we can't calculate a segment time
						level.PbSegmentTimes[method][localIndex] = null;
					}
					else {
						// segment time can be calculated
						level.PbSegmentTimes[method][localIndex] = finishTime - prevFinishTime;
					}
				}
			}
		}

		/// <summary>
		/// Calculate the segment time of the previous split
		/// </summary>
		/// <param name="state"></param>
		/// <param name="method"></param>
		void CalculateLiveSegment(LiveSplitState state, TimingMethod method) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { return; }
			else if (state.CurrentSplitIndex == 0) { return; } // Run just started, no previous splits to calculate

			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];
				List<int> indices = _splitLevelIndices[lvl];
				int prevLocalIndex = indices.IndexOf(state.CurrentSplitIndex - 1);
				if (prevLocalIndex <= 0) { continue; } // This isn't a subsplit or its the first subsplit for this level, no previous split to calculate
				if (state.CurrentPhase == TimerPhase.Ended) {
					// Run finished, there is only one split. Segment time == run time
					TimeSpan? ultimateFinishTime = state.Run[state.Run.Count - 1].SplitTime[method];
					if (indices.Count == 1) {
						level.LiveSegmentTimes[method][0] = ultimateFinishTime;
						continue;
					}
					// More than 1 split
					int penultimateSubsplitGlobalIndex = indices[indices.Count - 2];
					TimeSpan? penultimateFinishTime = state.Run[penultimateSubsplitGlobalIndex].SplitTime[method];
					if (penultimateFinishTime == null) {
						level.LiveSegmentTimes[method][level.LiveSegmentTimes.Count - 1] = null;
					}
					else {
						level.LiveSegmentTimes[method][level.LiveSegmentTimes.Count - 1] = ultimateFinishTime - penultimateFinishTime;
					}
				}
				else {
					// Mid-run split
					int prevGlobalIndex = indices[prevLocalIndex];
					TimeSpan? prevSegmentTime;
					if (prevLocalIndex == 0) {
						// First split
						prevSegmentTime = state.Run[prevGlobalIndex].SplitTime[method];
					}
					else {
						// Any other split
						int antePrevGlobalIndex = indices[prevLocalIndex - 1];
						TimeSpan? prevCompletionTime = state.Run[prevGlobalIndex].SplitTime[method];
						TimeSpan? antePrevCompletionTime = state.Run[antePrevGlobalIndex].SplitTime[method];
						if (prevCompletionTime == null || antePrevCompletionTime == null) {
							prevSegmentTime = null;
						}
						else {
							prevSegmentTime = state.Run[prevGlobalIndex].SplitTime[method] - state.Run[antePrevGlobalIndex].SplitTime[method];
						}
					}
					level.LiveSegmentTimes[method][prevLocalIndex] = prevSegmentTime;
				}
			}
		}

		TimeSpan? CalculatePreviousGoldDelta(LiveSplitState state, TimingMethod method, int lvl) {
			if (lvl != 0) { return null; } // No gold splits are being kept (reliably) for multisplit segments. Abandon ship.
			if (state.CurrentPhase == TimerPhase.NotRunning) { return null; }
			if (state.CurrentSplitIndex == 0) { return null; } // Run just started, no previous splits to calculate

			int prevIndex = state.CurrentPhase == TimerPhase.Ended ? state.Run.Count - 1 : state.CurrentSplitIndex - 1;
			TimeSpan? liveSegmentTime = _splitLevels[0].LiveSegmentTimes[method][prevIndex];
			TimeSpan? goldTime = state.Run[prevIndex].BestSegmentTime[method];
			if (liveSegmentTime == null || goldTime == null) { return null; }
			return liveSegmentTime - goldTime;
		}

		TimeSpan? CalculatePreviousSegmentDelta(LiveSplitState state, TimingMethod method, int lvl, int prevLocalIndex, out TimeSpan? liveSegmentTime) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { liveSegmentTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { liveSegmentTime = null; return null; }
			SplitLevel level = _splitLevels[lvl];

			if (prevLocalIndex == -1) { liveSegmentTime = null; return null; }

			liveSegmentTime = level.LiveSegmentTimes[method][prevLocalIndex];
			if (liveSegmentTime == null) { return null; }

			TimeSpan? pbSegmentTime = level.PbSegmentTimes[method][prevLocalIndex];
			if (pbSegmentTime == null) { return null; }
			return liveSegmentTime - pbSegmentTime;
		}

		TimeSpan? CalculatePreviousRunDelta(LiveSplitState state, TimingMethod method, int lvl, int prevLocalIndex, out TimeSpan? liveRunTime) {
			if (state.CurrentPhase == TimerPhase.NotRunning) { liveRunTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { liveRunTime = null; return null; }
			SplitLevel level = _splitLevels[lvl];

			if (prevLocalIndex == -1) { liveRunTime = null; return null; }

			int prevGlobalIndex = _splitLevelIndices[lvl][prevLocalIndex];

			liveRunTime = state.Run[prevGlobalIndex].SplitTime[method];
			if (liveRunTime == null) { return null; }

			TimeSpan? pbRunTime = state.Run[prevGlobalIndex].PersonalBestSplitTime[method];
			if (pbRunTime == null) { return null; }
			return liveRunTime - pbRunTime;
		}

		#endregion

		void WriteTimer(LiveSplitState state, TimingMethod method) {
			Cache.Restart();

			TimeSpan time = (method == TimingMethod.RealTime ? state.CurrentTime.RealTime : state.CurrentTime.GameTime) ?? TimeSpan.Zero;

			string cacheString = method == TimingMethod.RealTime ? "RealTimeSeconds" : "GameTimeSeconds";
			Cache[cacheString] = time.Seconds;
			if (!Cache.HasChanged) { return; }

			if (state.CurrentPhase == TimerPhase.NotRunning) {
				MakeFile(FILE_TIMER_RUN, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), method, false, false);
				for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
					MakeFile(FILE_TIMER_SPLIT, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), method, lvl, false, false);
				}
				return;
			}

			// Run Timer
			MakeFile(FILE_TIMER_RUN, time.ToString(@"hh\:mm\:ss") ?? "-", method, false, false);

			// Split Timer
			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];
				List<int> indices = _splitLevelIndices[lvl];
				int prevLocalIndex = GetLocalSplitIndex(state, lvl, true);

				if (prevLocalIndex == -1) {
					MakeFile(FILE_TIMER_SPLIT, time.ToString(@"hh\:mm\:ss") ?? "-", method, lvl);
					continue;
				}


				if (state.CurrentPhase == TimerPhase.Ended) {
					if (indices.Count > 1) {
						int prevGlobalIndex = indices[prevLocalIndex];
						TimeSpan? prevTime = method == TimingMethod.RealTime ? state.Run[prevGlobalIndex].SplitTime.RealTime : state.Run[prevGlobalIndex].SplitTime.GameTime;
						TimeSpan? currTime = method == TimingMethod.RealTime ? state.CurrentTime.RealTime : state.CurrentTime.GameTime;
						if (prevTime == null) {
							MakeFile(FILE_TIMER_SPLIT, "-", method, lvl, false, false);
						}
						else {
							if (currTime.HasValue) {
								TimeSpan t = currTime.Value - prevTime.Value;
								MakeFile(FILE_TIMER_SPLIT, t.ToString(@"hh\:mm\:ss"), method, lvl, false, false);
							}
							else {
								MakeFile(FILE_TIMER_SPLIT, "-", method, lvl, false, false);
							}
						}
					}
					else {
						MakeFile(FILE_TIMER_SPLIT, time.ToString(@"hh\:mm\:ss"), method, lvl, false, false);
					}
					return;
				}

				int currLocalIndex = GetLocalSplitIndex(state, lvl, false);
				int currGlobalIndex = state.CurrentSplitIndex;
				if (currLocalIndex == 0) {
					MakeFile(FILE_TIMER_SPLIT, time.ToString(@"hh\:mm\:ss"), method, lvl, false, false);
				}
				else {
					int prevGlobalIndex = indices[prevLocalIndex];
					TimeSpan? prevTime = method == TimingMethod.RealTime ? state.Run[prevGlobalIndex].SplitTime.RealTime : state.Run[prevGlobalIndex].SplitTime.GameTime;
					TimeSpan? currTime = method == TimingMethod.RealTime ? state.CurrentTime.RealTime : state.CurrentTime.GameTime;
					if (prevTime == null) {
						MakeFile(FILE_TIMER_SPLIT, "-", method, lvl, false, false);
					}
					else {
						if (currTime.HasValue) {
							TimeSpan t = currTime.Value - prevTime.Value;
							MakeFile(FILE_TIMER_SPLIT, t.ToString(@"hh\:mm\:ss"), method, lvl, false, false);
						}
						else {
							MakeFile(FILE_TIMER_SPLIT, "-", method, lvl, false, false);
						}
					}
				}
			}
		}

		// This is the function where we decide what needs to be displayed at this moment in time,
		// and tell the internal component to display it. This function is called hundreds to
		// thousands of times per second.
		public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
			if (Settings.OutputTimer) {
				WriteTimer(state, TimingMethod.RealTime);
				WriteTimer(state, TimingMethod.GameTime);
			}
			if (_calculateSegs) {
				_calculateSegs = false;
				GenerateSubsplitLevels(state, TimingMethod.RealTime);
				GenerateSubsplitLevels(state, TimingMethod.GameTime);
				CalculatePBSegments(state, TimingMethod.RealTime);
				CalculatePBSegments(state, TimingMethod.GameTime);
			}
			Cache.Restart();
			// Check for basic 'user loaded or changed splits' etc
			Cache["AttemptHistoryCount"] = state.Run.AttemptCount;
			Cache["GameName"] = state.Run.GameName;
			Cache["CategoryName"] = state.Run.CategoryName;
			Cache["FolderPath"] = Settings.FolderPath;
			Cache["SplitsBefore"] = Settings.SplitsBefore;
			Cache["SplitsAfter"] = Settings.SplitsAfter;
			Cache["TimerPhase"] = state.CurrentPhase;
			if (Cache.HasChanged) {
				MakeFile(FILE_INFO_GAMENAME, state.Run.GameName);
				MakeFile(FILE_INFO_CATEGORYNAME, state.Run.CategoryName);
				MakeFile(FILE_INFO_ATTEMPTCOUNT, state.Run.AttemptCount.ToString());
				int finishedRunsInHistory = state.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
				MakeFile(FILE_INFO_FINISHEDRUNSCOUNT, (finishedRunsInHistory + (state.CurrentPhase == TimerPhase.Ended ? 1 : 0)).ToString());
				for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
					MakeFile(FILE_INFO_SPLITCOUNT, _splitLevelIndices[lvl].Count.ToString(), null, lvl);
				}
				_writeSplits = true;
			}
			if (_writeSplits) {
				_writeSplits = false;
				WriteSplitInformation(state);
				WriteSplitTimes(state, TimingMethod.RealTime);
				WriteSplitTimes(state, TimingMethod.GameTime);
				WriteSplitList(state, TimingMethod.RealTime);
				WriteSplitList(state, TimingMethod.GameTime);
			}
		}

		void WriteSplitList(LiveSplitState state, TimingMethod method) {
			if (!Settings.OutputSplitList) return;
			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];
				List<int> indices = _splitLevelIndices[lvl];
				int localCurrentSplitIndex = GetLocalSplitIndex(state, lvl, false, true);

				int range = 1 + Settings.SplitsBefore + Settings.SplitsAfter;
				int first;
				int last;
				if (range > indices.Count) {
					range = indices.Count;
					first = 0;
					last = indices.Count - 1;
				}
				else if (state.CurrentPhase == TimerPhase.NotRunning || localCurrentSplitIndex < Settings.SplitsBefore) {
					first = 0;
					last = range - 1;
				}
				else if (state.CurrentPhase == TimerPhase.Ended || localCurrentSplitIndex > indices.Count - 1 - Settings.SplitsAfter) {
					first = indices.Count - range;
					last = indices.Count - 1;
				}
				else {
					first = localCurrentSplitIndex - Settings.SplitsBefore;
					last = localCurrentSplitIndex + Settings.SplitsAfter;
				}
				string splitNames = "";
				string splitNamesRaw = "";
				string finishTimeLive = "";
				string finishTimeUpcoming = "";
				string runDelta = "";
				string goldDelta = "";
				string goldTimeUpcoming = "";
				string segmentTimeUpcoming = "";
				string segmentDelta = "";
				string currentSplitHighlight = "";
				string goldHighlights = "";
				string aheadGainedHighlights = "";
				string aheadLostHighlights = "";
				string behindGainedHighlights = "";
				string behindLostHighlights = "";

				for (int j = first; j <= last; j++) {
					int globalIndex = indices[j];
					var split = state.Run[globalIndex];

					splitNamesRaw += split.Name;
					splitNames += _splitNamesByLevel[globalIndex][lvl]; 

					if (j == localCurrentSplitIndex) currentSplitHighlight += "███████████████████████████████████████████";

					if (j < localCurrentSplitIndex) {
						finishTimeLive += CustomTimeFormat(split.SplitTime[method]?.ToString() ?? "-", false);
						runDelta += CustomTimeFormat(level.RunDeltae[method][j]?.ToString() ?? "-", true);
						if (lvl == _splitLevels.Length - 1) goldDelta += CustomTimeFormat(level.GoldDeltae[method][j]?.ToString() ?? "-", true);
						segmentDelta += CustomTimeFormat(level.SegmentDeltae[method][j]?.ToString() ?? "-", true);
						if (lvl == _splitLevels.Length - 1 && level.GoldDeltae[method][j] < TimeSpan.Zero) goldHighlights += "███████████████████████████████████████████";
						else if (level.RunDeltae[method][j] < TimeSpan.Zero && level.SegmentDeltae[method][j] < TimeSpan.Zero) aheadGainedHighlights += "███████████████████████████████████████████";
						else if (level.RunDeltae[method][j] < TimeSpan.Zero) aheadLostHighlights += "███████████████████████████████████████████";
						else if (level.SegmentDeltae[method][j] < TimeSpan.Zero) behindGainedHighlights += "███████████████████████████████████████████";
						else if (split.PersonalBestSplitTime[method] != null) behindLostHighlights += "███████████████████████████████████████████";
					}
					else {
						finishTimeUpcoming += CustomTimeFormat(split.PersonalBestSplitTime[method]?.ToString() ?? "-", false);
						if (lvl == _splitLevels.Length - 1) goldTimeUpcoming += CustomTimeFormat(split.BestSegmentTime[method]?.ToString() ?? "-", false);
						segmentTimeUpcoming += CustomTimeFormat(level.PbSegmentTimes[method][j]?.ToString() ?? "-", false);
					}

					splitNames += "\n";
					splitNamesRaw += "\n";
					finishTimeLive += "\n";
					finishTimeUpcoming += "\n";
					runDelta += "\n";
					goldDelta += "\n";
					goldTimeUpcoming += "\n";
					segmentTimeUpcoming += "\n";
					segmentDelta += "\n";
					currentSplitHighlight += "\n";
					goldHighlights += "\n";
					aheadGainedHighlights += "\n";
					behindGainedHighlights += "\n";
					aheadLostHighlights += "\n";
					behindLostHighlights += "\n";
				}

				MakeFile(FILE_SPLITLIST_NAMES, splitNames, null, lvl);
				MakeFile(FILE_SPLITLIST_NAMES_RAW, splitNamesRaw, null, lvl);
				MakeFile(FILE_SPLITLIST_FINISH_TIME_LIVE, finishTimeLive, method, lvl);
				MakeFile(FILE_SPLITLIST_FINISH_TIME_UPCOMING, finishTimeUpcoming, method, lvl);
				MakeFile(FILE_SPLITLIST_RUN_DELTA, runDelta, method, lvl);
				MakeFile(FILE_SPLITLIST_SEGMENT_TIME_UPCOMING, segmentTimeUpcoming, method, lvl);
				MakeFile(FILE_SPLITLIST_SEGMENT_DELTA, segmentDelta, method, lvl);
				MakeFile(FILE_SPLITLIST_CURRENT_SPLIT_HIGHLIGHT, currentSplitHighlight, method, lvl);
				MakeFile(FILE_SPLITLIST_AHEAD_GAINED_HIGHLIGHT, aheadGainedHighlights, method, lvl);
				MakeFile(FILE_SPLITLIST_AHEAD_LOST_HIGHLIGHT, aheadLostHighlights, method, lvl);
				MakeFile(FILE_SPLITLIST_BEHIND_GAINED_HIGHLIGHT, behindGainedHighlights, method, lvl);
				MakeFile(FILE_SPLITLIST_BEHIND_LOST_HIGHLIGHT, behindLostHighlights, method, lvl);
				if (lvl == _splitLevels.Length - 1) MakeFile(FILE_SPLITLIST_GOLD_DELTA, goldDelta, method, lvl);
				if (lvl == _splitLevels.Length - 1) MakeFile(FILE_SPLITLIST_GOLD_TIME_UPCOMING, goldTimeUpcoming, method, lvl);
				if (lvl == _splitLevels.Length - 1) MakeFile(FILE_SPLITLIST_GOLD_HIGHLIGHT, goldHighlights, method, lvl);
				
				if (!Settings.OutputSubsplits) continue;
				string[] whitespacedSplitListNames = new string[_splitLevels.Length];
				for (int i = lvl; i < whitespacedSplitListNames.Length; i++) {
					whitespacedSplitListNames[i] = "";
				}
				for (int split = 0; split < _splitLevelIndices[lvl].Count; split++) {
					int globalIndex = _splitLevelIndices[lvl][split];
					for (int list = 0; list <= lvl; list++) {
						if (list == _splitDepths[globalIndex]) whitespacedSplitListNames[list] += _splitNamesByLevel[globalIndex][lvl];
						whitespacedSplitListNames[list] += "\n";
					}
				}
				for (int list = 0; list <= lvl; list++) {
					MakeFile(string.Format(FILE_SPLITLIST_WHITESPACE, "", "{1}", list), whitespacedSplitListNames[list], method, lvl);
				}
			}
		}

		int GetLocalSplitIndex(LiveSplitState state, int level, bool getPrevious = false, bool getNextIfNotPresent = false) {
			List<int> globalIndices = _splitLevelIndices[level];
			if (state.CurrentPhase == TimerPhase.Ended) {
				if (getPrevious) return globalIndices.Count - 1;
				else return -1;
			}

			if (!getPrevious && !getNextIfNotPresent) {
				return globalIndices.IndexOf(state.CurrentSplitIndex);
			}
			int localIndex = -1;
			for (int i = 0; i < globalIndices.Count; i++) {
				if (globalIndices[i] >= state.CurrentSplitIndex) {
					localIndex = i;
					break;
				}
			}
			if (getPrevious) return localIndex - 1;
			return localIndex;
		}

		void WriteSplitTimes(LiveSplitState state, TimingMethod method) {
			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];
				int prevLocalSplitIndex = GetLocalSplitIndex(state, lvl, true);
				if (prevLocalSplitIndex != -1) {
					CalculateLiveSegment(state, method);
					TimeSpan? goldDelta = CalculatePreviousGoldDelta(state, method, lvl);
					TimeSpan? segmentDelta = CalculatePreviousSegmentDelta(state, method, lvl, prevLocalSplitIndex, out TimeSpan? segmentTime);
					TimeSpan? runDelta = CalculatePreviousRunDelta(state, method, lvl, prevLocalSplitIndex, out TimeSpan? runTime);

					if ((state.CurrentPhase != TimerPhase.NotRunning) && state.CurrentSplitIndex > 0) {
						level.GoldDeltae[method][prevLocalSplitIndex] = goldDelta;
						level.SegmentDeltae[method][prevLocalSplitIndex] = segmentDelta;
						level.RunDeltae[method][prevLocalSplitIndex] = runDelta;
					}

					MakeFile(FILE_PREVIOUS_SEGMENT_TIME, segmentTime?.ToString() ?? "-", method, lvl, true, false);
					MakeFile(FILE_PREVIOUS_RUN_TIME, runTime?.ToString() ?? "-", method, lvl, true, false);
					MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, segmentDelta?.ToString() ?? "-", method, lvl, true, true);
					MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, runDelta?.ToString() ?? "-", method, lvl, true, true);
					if (lvl == _splitLevels.Length - 1) MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, goldDelta?.ToString() ?? "-", method, lvl, true, true);
					
					// Sign
					if (state.CurrentPhase == TimerPhase.NotRunning) {
						MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl, false);
					}
					else if (state.CurrentPhase == TimerPhase.Ended) {
						if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGold : Signs.NoPBGold).ToString(), method, lvl, false); }
						else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PB : Signs.NoPB).ToString(), method, lvl, false); }
						else if (segmentDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGained : Signs.NoPBGained).ToString(), method, lvl, false); }
						else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBLost : Signs.NoPBLost).ToString(), method, lvl, false); }
					}
					else if (runDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl, false); }
					else if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.GoldAhead : Signs.GoldBehind).ToString(), method, lvl, false); }
					else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.Ahead : Signs.Behind).ToString(), method, lvl, false); }
					else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.LostAhead : Signs.LostBehind).ToString(), method, lvl, false); }
				}
				else {
					MakeFile(FILE_PREVIOUS_SEGMENT_TIME, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_RUN_TIME, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, "-", method, lvl);
					MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, lvl);
				}


				// Current
				if (state.CurrentPhase == TimerPhase.NotRunning || state.CurrentPhase == TimerPhase.Ended) {
					MakeFile(FILE_CURRENT_SEGMENT_TIME, "-", method, lvl, true, false);
					MakeFile(FILE_CURRENT_RUN_TIME, "-", method, lvl, true, false);
					if (lvl == _splitLevels.Length - 1) MakeFile(FILE_CURRENT_GOLD_TIME, "-", method, lvl, true, false);
				}
				else {
					int currLocalSplitIndex = GetLocalSplitIndex(state, lvl, false, true);
					int currGlobalSplitIndex = _splitLevelIndices[lvl][currLocalSplitIndex];
					MakeFile(FILE_CURRENT_SEGMENT_TIME, level.PbSegmentTimes[method][currLocalSplitIndex]?.ToString() ?? "-", method, lvl, true, false);
					MakeFile(FILE_CURRENT_RUN_TIME, state.Run[currGlobalSplitIndex].PersonalBestSplitTime[method]?.ToString() ?? "-", method, lvl, true, false);
					if (lvl == _splitLevels.Length - 1) MakeFile(FILE_CURRENT_GOLD_TIME, state.CurrentSplit.BestSegmentTime[method]?.ToString() ?? "-", method, lvl, true, false);
				}
			}
		}

		/// <summary>
		/// Write non-timing related information about a split
		/// </summary>
		/// <param name="state"></param>
		void WriteSplitInformation(LiveSplitState state) {
			for (int lvl = 0; lvl < _splitLevels.Length; lvl++) {
				SplitLevel level = _splitLevels[lvl];

				if (state.CurrentPhase == TimerPhase.NotRunning) {
					MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
					MakeFile(FILE_CURRENT_INDEX, "-1", null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, (_splitLevelIndices[lvl].Count + 1).ToString(), null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
					return;
				}
				if (state.CurrentPhase == TimerPhase.Ended) {
					MakeFile(FILE_CURRENT_NAME, "-", null, lvl);
					MakeFile(FILE_CURRENT_INDEX, _splitLevelIndices[lvl].Count.ToString(), null, lvl);
					MakeFile(FILE_CURRENT_REVERSEINDEX, "0", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, state.Run[state.Run.Count - 1].Name, null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, _splitNamesByLevel[state.Run.Count - 1][lvl], null, lvl);
					return;
				}
				int prevLocalSplitIndex = GetLocalSplitIndex(state, lvl, true);
				if (prevLocalSplitIndex == -1) {
					MakeFile(FILE_PREVIOUS_NAME, "-", null, lvl);
					MakeFile(FILE_PREVIOUS_NAME_RAW, "-", null, lvl);
				}
				else {
					int prevGlobalSplitIndex = _splitLevelIndices[lvl][prevLocalSplitIndex];
					MakeFile(FILE_PREVIOUS_NAME_RAW, state.Run[prevGlobalSplitIndex].Name, null, lvl);
					MakeFile(FILE_PREVIOUS_NAME, _splitNamesByLevel[prevGlobalSplitIndex][lvl], null, lvl);
				}
				int currLocalSplitIndex = GetLocalSplitIndex(state, lvl, false, true);
				int currGlobalSplitIndex = _splitLevelIndices[lvl][currLocalSplitIndex];
				MakeFile(FILE_CURRENT_NAME_RAW, state.Run[currGlobalSplitIndex].Name, null, lvl);
				MakeFile(FILE_CURRENT_NAME, _splitNamesByLevel[currGlobalSplitIndex][lvl], null, lvl);
				MakeFile(FILE_CURRENT_INDEX, currLocalSplitIndex.ToString(), null, lvl);
				MakeFile(FILE_CURRENT_REVERSEINDEX, (_splitLevelIndices[lvl].Count - currLocalSplitIndex).ToString(), null, lvl);
			}
		}

		#region file writing

		void MakeFile(string fileName, string contents, TimingMethod? method, int subsplitLevel, bool formatTime = false, bool showPlus = false) {
			string subsplitLevelTag;
			if (subsplitLevel == _splitLevels.Length - 1) {
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
			if (formatTime) {
				c = CustomTimeFormat(contents, showPlus);
			}
			MakeFile(file, c);
		}

		/// <summary>
		/// Write to file
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="contents"></param>
		void MakeFile(string fileName, string contents, TimingMethod method, bool formatTime, bool showPlus = false) {
			string file = string.Format(fileName, method.ToString());
			string c = contents;
			if (formatTime) {
				c = CustomTimeFormat(contents, showPlus);
			}
			MakeFile(file, c);
		}

		void MakeFile(string fileName, string contents) {
			string settingsPath = Settings.FolderPath;
			if (string.IsNullOrEmpty(settingsPath)) return;
			string path = Path.Combine(settingsPath, fileName);
			if (!Directory.Exists(Path.GetDirectoryName(path))) {
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			}
			File.WriteAllText(path, contents);
		}

		string CustomTimeFormat(string time, bool showPlus) {
			// time is null, return '-'
			if (time == "-") return time;

			bool negative = time[0] == '-';
			if (negative) time = time.Substring(1);

			if (time.Length < 9) time += ".0000000"; // hh:mm:ss.ddddddd

			if (time.Substring(0, 7) == "00:00:0") {
				time = time.Substring(7, 4);
				// s.dd
			}
			else if (time.Substring(0, 6) == "00:00:") {
				time = time.Substring(6, 4);
				// ss.d
			}
			else if (time.Substring(0, 4) == "00:0") {
				time = time.Substring(4, 4);
				// m:ss
			}
			else if (time.Substring(0, 3) == "00:") {
				time = time.Substring(3, 5);
				// mm:ss
			}
			else if (time.Substring(0, 1) == "0") {
				time = time.Substring(1, 7);
				// h:mm:ss
			}
			else if (time.Length == 16) {
				time = time.Substring(0, 8);
				// hh:mm:ss
			}
			else {
				time = time.Substring(0, time.Length - 8);
				// d.hh:mm:ss
			}
			string prefix = negative ? "-" : (showPlus ? "+" : "");
			time = prefix + time;

			return time;
		}

		#endregion

		// This function is called when the component is removed from the layout, or when LiveSplit
		// closes a layout with this component in it.
		public void Dispose() {
			CurrentState.OnStart -= state_OnStart;
			CurrentState.OnSplit -= state_OnSplitChange;
			CurrentState.OnSkipSplit -= state_OnSplitChange;
			CurrentState.OnUndoSplit -= state_OnSplitChange;
			CurrentState.OnReset -= state_OnReset;
		}

		// I do not know what this is for.
		public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
	}
}
