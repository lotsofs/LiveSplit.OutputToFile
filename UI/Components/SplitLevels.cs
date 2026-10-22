using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// The subsplit levels of the run. Level lvl contains the splits whose depth (number of leading dashes) is at most lvl.
	/// The last level contains every split.
	/// </summary>
	class SplitLevels {

		SplitLevel[] _levels = new SplitLevel[1];

		// Depth of each split, by global index
		public int[] Depths { get; private set; }
		// Display name of each split at each level: Names[global][lvl]
		public List<List<string>> Names { get; private set; }

		public int Count => _levels.Length;
		public SplitLevel this[int lvl] => _levels[lvl];

		public void GenerateSubsplitLevels(IRun run, bool outputSubsplits) {
			int numberOfLevels = CalculateDepths(run);
			// Names come from the subsplit levels even with Subsplits off, so AllSplits shows the same names either way
			GenerateNames(run, numberOfLevels);
			if (!outputSubsplits) {
				Depths = new int[run.Count];
				numberOfLevels = 1;
				Names = Names.Select(n => new List<string> { n[n.Count - 1] }).ToList();
			}
			GenerateLevels(run, numberOfLevels);
		}

		/// <summary>
		/// Set each split's depth. Returns the number of levels.
		/// </summary>
		int CalculateDepths(IRun run) {
			// Count how many levels of subsplits there are (including the main top level). Default subsplits component only supports 2 levels (Main + subsplits)
			Depths = new int[run.Count];
			for (int i = 0; i < run.Count; i++) {
				Depths[i] = Array.FindIndex<char>(run[i].Name.ToCharArray(), c => c != '-');
				if (Depths[i] == -1) {
					Depths[i] = run[i].Name.Length;
				}
			}
			// Last split is always top level. Set before counting, so that its dashes don't add a level
			Depths[Depths.Length - 1] = 0;

			// Count what the highest consecutive present level is
			int numberOfLevels = 1;
			for (int i = 1; i <= numberOfLevels; i++) {
				if (Depths.Contains(i)) {
					numberOfLevels++;
				}
			}

			// Squish any splits that skip a non-existent level (eg super long headers "-----------------") down to the deepest possible level
			for (int i = 0; i < Depths.Length; i++) {
				if (Depths[i] >= numberOfLevels) {
					Depths[i] = numberOfLevels - 1;
				}
			}
			return numberOfLevels;
		}

		/// <summary>
		/// Make the levels: level lvl contains the splits with depth ≤ lvl.
		/// </summary>
		void GenerateLevels(IRun run, int numberOfLevels) {
			var splitLevelIndices = new List<List<int>>();

			// Mark which splits belong to which level. Level 0 contains only top level splits, and then working down
			for (int lvl = 0; lvl < numberOfLevels; lvl++) {
				splitLevelIndices.Add(new List<int>());
				for (int splitIndex = 0; splitIndex < run.Count; splitIndex++) {
					if (Depths[splitIndex] <= lvl) {
						splitLevelIndices[lvl].Add(splitIndex);
					}
				}
			}

			// Make a list of times and deltae for each level
			_levels = new SplitLevel[numberOfLevels];
			for (int i = 0; i < numberOfLevels; i++) {
				_levels[i] = new SplitLevel(splitLevelIndices[i]);
			}
		}

		void GenerateNames(IRun run, int numberOfLevels) {
			// Make a list of all the split names by level, taking into consideration removing dashes and {} parts
			Names = new List<List<string>>();
			for (int split = 0; split < run.Count; split++) {
				Names.Add(new List<string>());
				string name = run[split].Name;
				for (int lvl = 0; lvl < numberOfLevels; lvl++) {
					string sectionName = GetSectionHeader(name);
					if (sectionName != null) {
						Names[split].Add(sectionName);
						name = name.Substring(sectionName.Length + 2);
						continue;
					}
					Names[split].Add(WithoutDashes(name));
					if (name.Length > 0 && name[0] == '-') {
						name = name.Substring(1);
					}
				}
			}
		}

		string GetSectionHeader(string name) {
			if (name.Length == 0 || name[0] != '{') return null;
			int closing = name.IndexOf('}');
			if (closing == -1) return null;
			return name.Substring(1, closing - 1);
		}

		// A name as displayed: without its leading dashes, and without one space after them, so "- -40" shows as "-40"
		static string WithoutDashes(string name) {
			string rest = name.TrimStart('-');
			return rest.StartsWith(" ") ? rest.Substring(1) : rest;
		}

		/// <summary>
		/// Calculate a list of segment times for all splits
		/// </summary>
		/// <param name="run"></param>
		/// <param name="method"></param>
		public void CalculatePBSegments(IRun run, TimingMethod method) {
			for (int lvl = 0; lvl < _levels.Length; lvl++) {
				SplitLevel level = _levels[lvl];
				List<int> indices = level.Indices;
				level.PbSegmentTimes[method] = new TimeSpan?[indices.Count];

				for (int localIndex = 0; localIndex < indices.Count; localIndex++) {
					int globalIndex = indices[localIndex];
					TimeSpan? finishTime = run[globalIndex].PersonalBestSplitTime[method];
					if (globalIndex == 0 || localIndex == 0) {
						// This is the first (Sub)split so segment time = run time
						level.PbSegmentTimes[method][localIndex] = finishTime;
						continue;
					}
					int prevGlobalIndex = indices[localIndex - 1];
					TimeSpan? prevFinishTime = run[prevGlobalIndex].PersonalBestSplitTime[method];
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
		/// Each member's best segment: the fastest it was done within one attempt, or null if it never was.
		/// A member of one split has that split's best segment. A member of several takes it from the segment history,
		/// because the sum of its splits' best segments is a time nobody ran.
		/// </summary>
		public void CalculateBestSegments(IRun run, TimingMethod method) {
			// The members of several splits, with the global indices of their first split's predecessor (-1 for the start) and their last split
			var sections = new List<(TimeSpan?[] Bests, int LocalIndex, int Start, int End)>();
			foreach (SplitLevel level in _levels) {
				List<int> indices = level.Indices;
				var bests = new TimeSpan?[indices.Count];
				level.BestSegmentTimes[method] = bests;
				for (int localIndex = 0; localIndex < indices.Count; localIndex++) {
					int globalIndex = indices[localIndex];
					int prevGlobalIndex = localIndex == 0 ? -1 : indices[localIndex - 1];
					if (globalIndex == prevGlobalIndex + 1) {
						bests[localIndex] = run[globalIndex].BestSegmentTime[method];
					}
					else {
						sections.Add((bests, localIndex, prevGlobalIndex, globalIndex));
					}
				}
			}
			if (sections.Count == 0) return;

			// Every attempt in the history, with the last split it has an entry for. Edits can remove an attempt's entry on any split,
			// the first included, so look at them all.
			var lastSplits = new Dictionary<int, int>();
			for (int i = 0; i < run.Count; i++) {
				foreach (int attempt in run[i].SegmentHistory.Keys) {
					lastSplits[attempt] = i;
				}
			}

			var times = new TimeSpan?[run.Count];
			var anchors = new int[run.Count];
			foreach (var attempt in lastSplits) {
				AttemptSplitTimes(run, method, attempt.Key, attempt.Value, times, anchors);
				foreach (var section in sections) {
					TimeSpan? endTime = times[section.End];
					if (endTime == null) continue;
					TimeSpan? startTime = section.Start == -1 ? TimeSpan.Zero : times[section.Start];
					int startAnchor = section.Start == -1 ? -1 : anchors[section.Start];
					// Both ends must count from the same split, with no gap in the history between them
					if (startTime == null || anchors[section.End] != startAnchor) continue;
					TimeSpan? segmentTime = endTime - startTime;
					TimeSpan? best = section.Bests[section.LocalIndex];
					if (best == null || segmentTime < best) {
						section.Bests[section.LocalIndex] = segmentTime;
					}
				}
			}
		}

		/// <summary>
		/// One attempt's split times, from its segment history: times[i] is the time from split anchors[i] to split i, or null if
		/// the attempt has no time there. lastSplit is the last split the attempt has an entry for.
		/// LiveSplit records each split's time since the attempt's last recorded split, so adding them up gives the split times.
		/// They count from the start (anchor -1) until a split has no entry, where an edit removed it (clearing a best segment,
		/// Clean Sum of Best). The next split with a time then counts from itself, since its entry includes time from before the gap.
		/// </summary>
		static void AttemptSplitTimes(IRun run, TimingMethod method, int attempt, int lastSplit, TimeSpan?[] times, int[] anchors) {
			Array.Clear(times, lastSplit + 1, run.Count - lastSplit - 1);
			TimeSpan time = TimeSpan.Zero;
			int anchor = -1;
			bool afterGap = false;
			for (int i = 0; i <= lastSplit; i++) {
				times[i] = null;
				if (!run[i].SegmentHistory.TryGetValue(attempt, out Time entry)) {
					afterGap = true;
					continue;
				}
				// Null for a skipped split: its time is in the next entry
				TimeSpan? segmentTime = entry[method];
				if (segmentTime == null) continue;
				if (afterGap) {
					afterGap = false;
					anchor = i;
					time = TimeSpan.Zero;
				}
				else {
					time += segmentTime.Value;
				}
				times[i] = time;
				anchors[i] = anchor;
			}
		}
	}

	/// <summary>
	/// One subsplit level: which splits it contains, and its PB segment times and best segments.
	/// Arrays are indexed by local index, i.e. position in Indices.
	/// Nothing here may describe the current attempt: CalculateSegments() rebuilds every level on any cache key change, including timer phase changes.
	/// </summary>
	class SplitLevel {

		// Local index → global index into the run
		public List<int> Indices;

		public Dictionary<TimingMethod, TimeSpan?[]> PbSegmentTimes;
		// The fastest each member was done within one attempt, from SplitLevels.CalculateBestSegments
		public Dictionary<TimingMethod, TimeSpan?[]> BestSegmentTimes;

		public SplitLevel(List<int> indices) {
			Indices = indices;
			int count = indices.Count;
			PbSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
			BestSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
		}
	}
}
