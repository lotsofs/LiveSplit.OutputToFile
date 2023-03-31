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

namespace LiveSplit.UI.Components
{
	public class OutputToFileComponent : IComponent
	{
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

		// I'm going to be honest, I don't know what this is for, but I know we don't need it.
		public IDictionary<string, Action> ContextMenuControls => null;

		enum Signs
		{
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

		Dictionary<TimingMethod, TimeSpan?[]> pbSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>();
		Dictionary<TimingMethod, TimeSpan?[]> liveSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>();
		Dictionary<TimingMethod, TimeSpan?[]> runDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();
		Dictionary<TimingMethod, TimeSpan?[]> segmentDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();
		Dictionary<TimingMethod, TimeSpan?[]> goldDeltae = new Dictionary<TimingMethod, TimeSpan?[]>();



		const string FILE_CURRENT_SEGMENT_TIME =	@"CurrentSplit\{0}\SegmentTime.txt";
		const string FILE_CURRENT_GOLD_TIME =		@"CurrentSplit\{0}\GoldTime.txt";
		const string FILE_CURRENT_RUN_TIME =		@"CurrentSplit\{0}\RunTime.txt";
		const string FILE_CURRENT_NAME =			@"CurrentSplit\Name.txt";
		const string FILE_CURRENT_INDEX =			@"CurrentSplit\Index.txt";
		const string FILE_CURRENT_REVERSEINDEX =	@"CurrentSplit\ReverseIndex.txt";

		const string FILE_INFO_GAMENAME =			@"GameName.txt";
		const string FILE_INFO_CATEGORYNAME =		@"CategoryName.txt";
		const string FILE_INFO_SPLITCOUNT =			@"TotalSplits.txt";
		const string FILE_INFO_ATTEMPTCOUNT =		@"AttemptCount.txt";
		const string FILE_INFO_FINISHEDRUNSCOUNT =	@"FinishedRunsCount.txt";
		const string FILE_TIMER_RUN =				@"Timers\{0}\RunTimer.txt";
		const string FILE_TIMER_SEGMENT =			@"Timers\{0}\SegmentTimer.txt";

		const string FILE_PREVIOUS_SIGN =				@"PreviousSplit\{0}\Sign.txt";
		const string FILE_PREVIOUS_SEGMENT_TIME =		@"PreviousSplit\{0}\SegmentTime.txt";
		const string FILE_PREVIOUS_RUN_TIME =			@"PreviousSplit\{0}\RunTime.txt";
		const string FILE_PREVIOUS_SEGMENT_DIFFERENCE = @"PreviousSplit\{0}\SegmentDifference.txt";
		const string FILE_PREVIOUS_GOLD_DIFFERENCE =	@"PreviousSplit\{0}\GoldDifference.txt";
		const string FILE_PREVIOUS_RUN_DIFFERENCE =		@"PreviousSplit\{0}\RunDifference.txt";
		const string FILE_PREVIOUS_NAME =				@"PreviousSplit\Name.txt";

		const string FILE_SPLITLIST_NAMES =						@"SplitList\Names.txt";
		const string FILE_SPLITLIST_FINISH_TIME_LIVE =			@"SplitList\{0}\Time_Run_Current.txt";
		const string FILE_SPLITLIST_FINISH_TIME_UPCOMING =		@"SplitList\{0}\Time_Run_Upcoming.txt";
		const string FILE_SPLITLIST_RUN_DELTA =					@"SplitList\{0}\Delta_Run.txt";
		const string FILE_SPLITLIST_GOLD_DELTA =				@"SplitList\{0}\Delta_Gold.txt";
		const string FILE_SPLITLIST_GOLD_TIME_UPCOMING =		@"SplitList\{0}\Time_Gold_Upcoming.txt";
		const string FILE_SPLITLIST_SEGMENT_TIME_UPCOMING =		@"SplitList\{0}\Time_Segment_Upcoming.txt";
		const string FILE_SPLITLIST_SEGMENT_DELTA =				@"SplitList\{0}\Delta_Segment.txt";
		const string FILE_SPLITLIST_CURRENT_SPLIT_HIGHLIGHT =	@"SplitList\Highlight_CurrentSplit.txt";
		const string FILE_SPLITLIST_GOLD_HIGHLIGHT =			@"SplitList\{0}\Highlight_Gold.txt";
		const string FILE_SPLITLIST_AHEAD_GAINED_HIGHLIGHT =	@"SplitList\{0}\Highlight_AheadGained.txt";
		const string FILE_SPLITLIST_AHEAD_LOST_HIGHLIGHT =		@"SplitList\{0}\Highlight_AheadLost.txt";
		const string FILE_SPLITLIST_BEHIND_GAINED_HIGHLIGHT =	@"SplitList\{0}\Highlight_BehindGained.txt";
		const string FILE_SPLITLIST_BEHIND_LOST_HIGHLIGHT =		@"SplitList\{0}\Highlight_BehindLost.txt";


		#region livesplitoverhead

		// This function is called when LiveSplit creates your component. This happens when the
		// component is added to the layout, or when LiveSplit opens a layout with this component
		// already added.
		public OutputToFileComponent(LiveSplitState state)
		{
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

		public Control GetSettingsControl(LayoutMode mode)
		{
			Settings.Mode = mode;
			return Settings;
		}

		public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
		{
			return Settings.GetSettings(document);
		}

		public void SetSettings(System.Xml.XmlNode settings)
		{
			Settings.SetSettings(settings);
		}

		void state_OnStart(object sender, EventArgs e)
		{
			_writeSplits = true;
			_calculateSegs = true;
		}

		void state_OnSplitChange(object sender, EventArgs e)
		{
			_writeSplits = true;
		}

		void state_OnReset(object sender, TimerPhase e)
		{
			_writeSplits = true;
			_calculateSegs = true;
		}

		#endregion

		#region calculatesplittimes

		/// <summary>
		/// Calculate a list of segment times for all splits
		/// </summary>
		/// <param name="state"></param>
		/// <param name="method"></param>
		void CalculatePBSegments(LiveSplitState state, TimingMethod method)
		{
			pbSegmentTimes[method] = new TimeSpan?[state.Run.Count];
			liveSegmentTimes[method] = new TimeSpan?[state.Run.Count];
			goldDeltae[method] = new TimeSpan?[state.Run.Count];
			segmentDeltae[method] = new TimeSpan?[state.Run.Count];
			runDeltae[method] = new TimeSpan?[state.Run.Count];

			for (int i = 0; i < state.Run.Count; i++)
			{
				TimeSpan? t = state.Run[i].PersonalBestSplitTime[method];
				if (i == 0)
				{
					pbSegmentTimes[method][i] = t;
				}
				else if (state.Run[i].PersonalBestSplitTime[method] == null || state.Run[i - 1].PersonalBestSplitTime[method] == null)
				{
					pbSegmentTimes[method][i] = null;
				}
				else
				{
					pbSegmentTimes[method][i] = state.Run[i].PersonalBestSplitTime[method] - state.Run[i - 1].PersonalBestSplitTime[method];
				}
			}
		}

		/// <summary>
		/// Calculate the segment time of our current split
		/// </summary>
		/// <param name="state"></param>
		/// <param name="method"></param>
		void CalculateLiveSegment(LiveSplitState state, TimingMethod method)
		{
			if (state.CurrentPhase == TimerPhase.NotRunning) { return; }
			else if (state.CurrentSplitIndex == 0) { return; }
			else if (state.CurrentSplitIndex == 1)
			{
				TimeSpan? time = state.Run[0].SplitTime[method];
				liveSegmentTimes[method][0] = time != null ? time : null;
			}
			else if (state.CurrentPhase == TimerPhase.Ended)
			{
				if (state.Run.Count == 1)
				{
					liveSegmentTimes[method][0] = state.Run[0].SplitTime[method];
					return;
				}
				TimeSpan? ult = state.Run[state.Run.Count - 1].SplitTime[method];
				TimeSpan? penult = state.Run[state.Run.Count - 2].SplitTime[method];
				if (penult == null)
				{
					liveSegmentTimes[method][state.Run.Count - 1] = null;
					return;
				}
				liveSegmentTimes[method][state.Run.Count - 1] = ult - penult;
			}
			else
			{
				TimeSpan? prev = state.Run[state.CurrentSplitIndex - 1].SplitTime[method];
				TimeSpan? anteprev = state.Run[state.CurrentSplitIndex - 2].SplitTime[method];
				if (prev == null || anteprev == null)
				{
					liveSegmentTimes[method][state.CurrentSplitIndex - 1] = null;
					return;
				}
				liveSegmentTimes[method][state.CurrentSplitIndex - 1] = prev - anteprev;
			}
		}

		TimeSpan? CalculateGoldDelta(LiveSplitState state, TimingMethod method)
		{
			if (state.CurrentPhase == TimerPhase.NotRunning) { return null; }
			if (state.CurrentSplitIndex == 0) { return null; }
			int index = state.CurrentPhase == TimerPhase.Ended ? state.Run.Count - 1 : state.CurrentSplitIndex - 1;
			if (liveSegmentTimes[method][index] == null) { return null; }

			if (state.Run[index].BestSegmentTime[method] == null) { return null; }
			return liveSegmentTimes[method][index] - state.Run[index].BestSegmentTime[method];
		}

		TimeSpan? CalculateSegmentDelta(LiveSplitState state, TimingMethod method, out TimeSpan? segmentTime)
		{
			if (state.CurrentPhase == TimerPhase.NotRunning) { segmentTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { segmentTime = null; return null; }
			int index = state.CurrentPhase == TimerPhase.Ended ? state.Run.Count - 1 : state.CurrentSplitIndex - 1;
			if (liveSegmentTimes[method][index] == null) { segmentTime = null; return null; }
			segmentTime = liveSegmentTimes[method][index];
			if (pbSegmentTimes[method][index] == null) { return null; }
			return liveSegmentTimes[method][index] - pbSegmentTimes[method][index];
		}

		TimeSpan? CalculateRunDelta(LiveSplitState state, TimingMethod method, out TimeSpan? runTime)
		{
			if (state.CurrentPhase == TimerPhase.NotRunning) { runTime = null; return null; }
			if (state.CurrentSplitIndex == 0) { runTime = null; return null; }
			int index = state.CurrentPhase == TimerPhase.Ended ? state.Run.Count - 1 : state.CurrentSplitIndex - 1;
			if (state.Run[index].SplitTime[method] == null) { runTime = null; return null; };
			runTime = state.Run[index].SplitTime[method];
			if (state.Run[index].PersonalBestSplitTime[method] == null) { return null; }
			return state.Run[index].SplitTime[method] - state.Run[index].PersonalBestSplitTime[method];
		}

		#endregion

		void WriteTimer(LiveSplitState state)
		{
			Cache.Restart();
			TimeSpan realTime = state.CurrentTime.RealTime ?? TimeSpan.Zero;
			TimeSpan gameTime = state.CurrentTime.GameTime ?? TimeSpan.Zero;

			Cache["RealTimeSeconds"] = realTime.Seconds;
			Cache["GameTimeSeconds"] = gameTime.Seconds;
			if (Cache.HasChanged)
			{
				if (state.CurrentPhase == TimerPhase.NotRunning)
				{
					MakeFile(FILE_TIMER_RUN, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), TimingMethod.RealTime, false, false);
					MakeFile(FILE_TIMER_SEGMENT, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), TimingMethod.RealTime, false, false);
					MakeFile(FILE_TIMER_RUN, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), TimingMethod.GameTime, false, false);
					MakeFile(FILE_TIMER_SEGMENT, TimeSpan.Zero.ToString(@"hh\:mm\:ss"), TimingMethod.GameTime, false, false);
					return;
				}
				else if (state.CurrentPhase == TimerPhase.Ended)
				{
					if (state.Run.Count > 1)
					{
						if (state.Run[state.Run.Count - 2].SplitTime.RealTime == null)
						{
							MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.RealTime, false, false);
							MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.GameTime, false, false);
						}
						else
						{
							if (state.CurrentTime.RealTime.HasValue)
							{
								MakeFile(FILE_TIMER_SEGMENT, (state.CurrentTime.RealTime.Value - state.Run[state.Run.Count - 2].SplitTime.RealTime.Value).ToString(@"hh\:mm\:ss"), TimingMethod.RealTime, false, false);
							}
							else
							{
								MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.RealTime, false, false);
							}
							if (state.CurrentTime.GameTime.HasValue)
							{
								MakeFile(FILE_TIMER_SEGMENT, (state.CurrentTime.GameTime.Value - state.Run[state.Run.Count - 2].SplitTime.GameTime.Value).ToString(@"hh\:mm\:ss"), TimingMethod.GameTime, false, false);
							}
							else
							{
								MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.GameTime, false, false);
							}
						}
					}
					else { 
						MakeFile(FILE_TIMER_SEGMENT, state.CurrentTime.RealTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.RealTime, false, false);
						MakeFile(FILE_TIMER_SEGMENT, state.CurrentTime.GameTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.GameTime, false, false);
					}
				}
				else
				{
					if (state.CurrentSplitIndex > 0)
					{
						if (state.Run[state.CurrentSplitIndex - 1].SplitTime.RealTime == null)
						{
							MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.RealTime, false, false);
							MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.GameTime, false, false);
						}
						else
						{
							if (state.CurrentTime.RealTime.HasValue)
							{
								MakeFile(FILE_TIMER_SEGMENT, (state.CurrentTime.RealTime.Value - state.Run[state.CurrentSplitIndex - 1].SplitTime.RealTime.Value).ToString(@"hh\:mm\:ss"), TimingMethod.RealTime, false, false);
							}
							else
							{
								MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.RealTime, false, false);
							}
							if (state.CurrentTime.GameTime.HasValue)
							{
								MakeFile(FILE_TIMER_SEGMENT, (state.CurrentTime.GameTime.Value - state.Run[state.CurrentSplitIndex - 1].SplitTime.GameTime.Value).ToString(@"hh\:mm\:ss"), TimingMethod.GameTime, false, false);
							}
							else
							{
								MakeFile(FILE_TIMER_SEGMENT, "-", TimingMethod.GameTime, false, false);
							}
						}
					}
					else
					{
						MakeFile(FILE_TIMER_SEGMENT, state.CurrentTime.RealTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.RealTime, false, false);
						MakeFile(FILE_TIMER_SEGMENT, state.CurrentTime.GameTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.GameTime, false, false);
					}
				}
				MakeFile(FILE_TIMER_RUN, state.CurrentTime.RealTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.RealTime, false, false);
				MakeFile(FILE_TIMER_RUN, state.CurrentTime.GameTime?.ToString(@"hh\:mm\:ss") ?? "-", TimingMethod.GameTime, false, false);

			}
		}

		// This is the function where we decide what needs to be displayed at this moment in time,
		// and tell the internal component to display it. This function is called hundreds to
		// thousands of times per second.
		public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
		{
			if (Settings.OutputTimer)
			{
				WriteTimer(state);
			}
			if (_calculateSegs)
			{
				_calculateSegs = false;
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
			if (Cache.HasChanged)
			{
				MakeFile(FILE_INFO_GAMENAME, state.Run.GameName);
				MakeFile(FILE_INFO_CATEGORYNAME, state.Run.CategoryName);
				MakeFile(FILE_INFO_SPLITCOUNT, state.Run.Count.ToString());
				MakeFile(FILE_INFO_ATTEMPTCOUNT, state.Run.AttemptCount.ToString());
				int finishedRunsInHistory = state.Run.AttemptHistory.Where(x => x.Time.RealTime != null).Count();
				MakeFile(FILE_INFO_FINISHEDRUNSCOUNT, (finishedRunsInHistory + (state.CurrentPhase == TimerPhase.Ended ? 1 : 0)).ToString());
				_writeSplits = true;
			}
			if (_writeSplits)
			{
				_writeSplits = false;
				WriteSplitInformation(state);
				WriteSplitTimes(state, TimingMethod.RealTime);
				WriteSplitTimes(state, TimingMethod.GameTime);
				WriteSplitList(state, TimingMethod.RealTime);
				WriteSplitList(state, TimingMethod.GameTime);
			}
		}

		void WriteSplitList(LiveSplitState state, TimingMethod method)
		{
			int range = 1 + Settings.SplitsBefore + Settings.SplitsAfter;
			int first;
			int last;
			if (range > state.Run.Count) { 
				range = state.Run.Count;
				first = 0;
				last = state.Run.Count - 1;
			}
			else if (state.CurrentPhase == TimerPhase.NotRunning || state.CurrentSplitIndex < Settings.SplitsBefore)
			{
				first = 0;
				last = range - 1;
			}
			else if (state.CurrentPhase == TimerPhase.Ended || state.CurrentSplitIndex > state.Run.Count - 1 - Settings.SplitsAfter)
			{
				first = state.Run.Count - range;
				last = state.Run.Count - 1;
			}
			else
			{
				first = state.CurrentSplitIndex - Settings.SplitsBefore;
				last = state.CurrentSplitIndex + Settings.SplitsAfter;
			}

			string splitNames = "";
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

			for (int i = first; i <= last; i++)
			{
				splitNames += state.Run[i].Name; // only write to this once
				
				if (i == state.CurrentSplitIndex) currentSplitHighlight += "███████████████████████████████████████████";

				if (i < state.CurrentSplitIndex)
				{
					finishTimeLive += CustomTimeFormat(state.Run[i].SplitTime[method]?.ToString() ?? "-", false);
					runDelta += CustomTimeFormat(runDeltae[method][i]?.ToString() ?? "-", true);
					goldDelta += CustomTimeFormat(goldDeltae[method][i]?.ToString() ?? "-", true);
					segmentDelta += CustomTimeFormat(segmentDeltae[method][i]?.ToString() ?? "-", true);
					if (goldDeltae[method][i] < TimeSpan.Zero) goldHighlights += "███████████████████████████████████████████";
					else if (runDeltae[method][i] < TimeSpan.Zero && segmentDeltae[method][i] < TimeSpan.Zero) aheadGainedHighlights += "███████████████████████████████████████████";
					else if (runDeltae[method][i] < TimeSpan.Zero) aheadLostHighlights += "███████████████████████████████████████████";
					else if (segmentDeltae[method][i] < TimeSpan.Zero) behindGainedHighlights += "███████████████████████████████████████████";
					else if (state.Run[i].PersonalBestSplitTime[method] != null) behindLostHighlights += "███████████████████████████████████████████";
				}
				else
				{
					finishTimeUpcoming += CustomTimeFormat(state.Run[i].PersonalBestSplitTime[method]?.ToString() ?? "-", false);
					goldTimeUpcoming += CustomTimeFormat(state.Run[i].BestSegmentTime[method]?.ToString() ?? "-", false);
					segmentTimeUpcoming += CustomTimeFormat(pbSegmentTimes[method][i]?.ToString() ?? "-", false);
				}

				splitNames += "\n";
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

			MakeFile(string.Format(FILE_SPLITLIST_NAMES, ""), splitNames);
			MakeFile(string.Format(FILE_SPLITLIST_FINISH_TIME_LIVE, method.ToString()), finishTimeLive);
			MakeFile(string.Format(FILE_SPLITLIST_FINISH_TIME_UPCOMING, method.ToString()), finishTimeUpcoming);
			MakeFile(string.Format(FILE_SPLITLIST_RUN_DELTA, method.ToString()), runDelta);
			MakeFile(string.Format(FILE_SPLITLIST_GOLD_DELTA, method.ToString()), goldDelta);
			MakeFile(string.Format(FILE_SPLITLIST_GOLD_TIME_UPCOMING, method.ToString()), goldTimeUpcoming);
			MakeFile(string.Format(FILE_SPLITLIST_SEGMENT_TIME_UPCOMING, method.ToString()), segmentTimeUpcoming);
			MakeFile(string.Format(FILE_SPLITLIST_SEGMENT_DELTA, method.ToString()), segmentDelta);
			MakeFile(string.Format(FILE_SPLITLIST_CURRENT_SPLIT_HIGHLIGHT, method.ToString()), currentSplitHighlight);
			MakeFile(string.Format(FILE_SPLITLIST_GOLD_HIGHLIGHT, method.ToString()), goldHighlights);
			MakeFile(string.Format(FILE_SPLITLIST_AHEAD_GAINED_HIGHLIGHT, method.ToString()), aheadGainedHighlights);
			MakeFile(string.Format(FILE_SPLITLIST_AHEAD_LOST_HIGHLIGHT, method.ToString()), aheadLostHighlights);
			MakeFile(string.Format(FILE_SPLITLIST_BEHIND_GAINED_HIGHLIGHT, method.ToString()), behindGainedHighlights);
			MakeFile(string.Format(FILE_SPLITLIST_BEHIND_LOST_HIGHLIGHT, method.ToString()), behindLostHighlights);
		}

		void WriteSplitTimes(LiveSplitState state, TimingMethod method)
		{
			CalculateLiveSegment(state, method);
			TimeSpan? goldDelta = CalculateGoldDelta(state, method);
			TimeSpan? segmentDelta = CalculateSegmentDelta(state, method, out TimeSpan? segmentTime);
			TimeSpan? runDelta = CalculateRunDelta(state, method, out TimeSpan? runTime);
			if ((state.CurrentPhase != TimerPhase.NotRunning) && state.CurrentSplitIndex > 0)
			{
				goldDeltae[method][state.CurrentSplitIndex - 1] = goldDelta;
				segmentDeltae[method][state.CurrentSplitIndex - 1] = segmentDelta;
				runDeltae[method][state.CurrentSplitIndex - 1] = runDelta;
			}

			MakeFile(FILE_PREVIOUS_SEGMENT_TIME, segmentTime?.ToString() ?? "-", method, true, false);
			MakeFile(FILE_PREVIOUS_RUN_TIME, runTime?.ToString() ?? "-", method, true, false);
			MakeFile(FILE_PREVIOUS_SEGMENT_DIFFERENCE, segmentDelta?.ToString() ?? "-", method, true, true);
			MakeFile(FILE_PREVIOUS_RUN_DIFFERENCE, runDelta?.ToString() ?? "-", method, true, true);
			MakeFile(FILE_PREVIOUS_GOLD_DIFFERENCE, goldDelta?.ToString() ?? "-", method, true, true);

			if (state.CurrentPhase == TimerPhase.NotRunning)
			{
				MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, false);
			}
			else if (state.CurrentPhase == TimerPhase.Ended)
			{
				if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGold : Signs.NoPBGold).ToString(), method, false); }
				else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PB : Signs.NoPB).ToString(), method, false); }
				else if (segmentDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBGained : Signs.NoPBGained).ToString(), method, false); }
				else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.PBLost : Signs.NoPBLost).ToString(), method, false); }
			}
			else if (runDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, Signs.NotApplicable.ToString(), method, false); }
			else if (goldDelta == null || goldDelta < TimeSpan.Zero) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.GoldAhead : Signs.GoldBehind).ToString(), method, false); }
			else if (segmentDelta == null) { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.Ahead : Signs.Behind).ToString(), method, false); }
			else { MakeFile(FILE_PREVIOUS_SIGN, (runDelta < TimeSpan.Zero ? Signs.LostAhead : Signs.LostBehind).ToString(), method, false); }

			if (state.CurrentPhase == TimerPhase.NotRunning || state.CurrentPhase == TimerPhase.Ended)
			{
				MakeFile(FILE_CURRENT_SEGMENT_TIME, "-", method, true, false);
				MakeFile(FILE_CURRENT_GOLD_TIME, "-", method, true, false);
				MakeFile(FILE_CURRENT_RUN_TIME, "-", method, true, false);
			}
			else
			{
				MakeFile(FILE_CURRENT_SEGMENT_TIME, pbSegmentTimes[method][state.CurrentSplitIndex]?.ToString() ?? "-", method, true, false);
				MakeFile(FILE_CURRENT_GOLD_TIME, state.CurrentSplit.BestSegmentTime[method]?.ToString() ?? "-", method, true, false);
				MakeFile(FILE_CURRENT_RUN_TIME, state.CurrentSplit.PersonalBestSplitTime[method]?.ToString() ?? "-", method, true, false);
			}
		}

		/// <summary>
		/// Write non-timing related information about a split
		/// </summary>
		/// <param name="state"></param>
		void WriteSplitInformation(LiveSplitState state)
		{
			Cache.Restart();
			var currentSplit = state.CurrentSplit;
			if (state.CurrentPhase == TimerPhase.NotRunning)
			{
				MakeFile(FILE_CURRENT_NAME, "-");
				MakeFile(FILE_CURRENT_INDEX, "-1");
				MakeFile(FILE_CURRENT_REVERSEINDEX, (state.Run.Count + 1).ToString());
				MakeFile(FILE_PREVIOUS_NAME, "-");
			}
			else if (state.CurrentPhase == TimerPhase.Ended)
			{
				MakeFile(FILE_CURRENT_NAME, "-");
				MakeFile(FILE_CURRENT_INDEX, state.Run.Count.ToString());
				MakeFile(FILE_CURRENT_REVERSEINDEX, "0");
				MakeFile(FILE_PREVIOUS_NAME, state.Run[state.Run.Count - 1].Name);
			}
			else
			{
				MakeFile(FILE_CURRENT_NAME, currentSplit.Name);
				MakeFile(FILE_CURRENT_INDEX, state.CurrentSplitIndex.ToString());
				MakeFile(FILE_CURRENT_REVERSEINDEX, (state.Run.Count - state.CurrentSplitIndex).ToString());
				MakeFile(FILE_PREVIOUS_NAME, state.CurrentSplitIndex >= 1 ? state.Run[state.CurrentSplitIndex - 1].Name : "-");
			}
		}

		/// <summary>
		/// Write to file
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="contents"></param>
		void MakeFile(string fileName, string contents, TimingMethod method, bool formatTime, bool showPlus = false)
		{
			string file = string.Format(fileName, method.ToString());
			string c = contents;
			if (formatTime)
			{
				c = CustomTimeFormat(contents, showPlus);
			}
			MakeFile(file, c);
		}

		void MakeFile(string fileName, string contents)
		{
			string settingsPath = Settings.FolderPath;
			if (string.IsNullOrEmpty(settingsPath)) return;
			string path = Path.Combine(settingsPath, fileName);
			if (!Directory.Exists(Path.GetDirectoryName(path)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			}
			File.WriteAllText(path, contents);
		}

		string CustomTimeFormat(string time, bool showPlus)
		{
			// time is null, return '-'
			if (time == "-") return time;
			
			bool negative = time[0] == '-';
			if (negative) time = time.Substring(1);
			
			if (time.Length < 9) time += ".0000000"; // hh:mm:ss.ddddddd

			if (time.Substring(0, 7) == "00:00:0")
			{
				time = time.Substring(7, 4);
				// s.dd
			}
			else if (time.Substring(0, 6) == "00:00:")
			{
				time = time.Substring(6, 4);
				// ss.d
			}
			else if (time.Substring(0, 4) == "00:0")
			{
				time = time.Substring(4, 4);
				// m:ss
			}
			else if (time.Substring(0, 3) == "00:")
			{
				time = time.Substring(3, 5);
				// mm:ss
			}
			else if (time.Substring(0, 1) == "0")
			{
				time = time.Substring(1, 7);
				// h:mm:ss
			}
			else if (time.Length == 16)
			{
				time = time.Substring(0, 8);
				// hh:mm:ss
			}
			else
			{
				time = time.Substring(0, time.Length - 8);
				// d.hh:mm:ss
			}
			time = (negative ? "-" : (showPlus ? "+" : "") + time);
			
			return time;
		}

		// This function is called when the component is removed from the layout, or when LiveSplit
		// closes a layout with this component in it.
		public void Dispose()
		{
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
