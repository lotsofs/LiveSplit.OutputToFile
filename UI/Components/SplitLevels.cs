using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.UI.Components {
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

		public void GenerateSubsplitLevels(IRun run, bool outputSubsplits, TimingMethod method) {
			// Count how many levels of subsplits there are (including the main top level). Default subsplits component only supports 2 levels (Main + subsplits)
			Depths = new int[run.Count];
			for (int i = 0; i < run.Count; i++) {
				if (!outputSubsplits) {
					Depths[i] = 0;
					continue;
				}
				Depths[i] = Array.FindIndex<char>(run[i].Name.ToCharArray(), c => c != '-');
				if (Depths[i] == -1) {
					Depths[i] = run[i].Name.Length;
				}
			}

			// Count what the highest consecutive present level is
			int numberOfLevels = 1;
			for (int i = 1; i <= numberOfLevels; i++) {
				if (Depths.Contains(i)) {
					numberOfLevels++;
				}
			}

			// Squish any splits that skip a non-existent level (eg super long headers "-----------------") down to the deepest possible level
			for (int i = 1; i < Depths.Length; i++) {
				if (Depths[i] >= numberOfLevels) {
					Depths[i] = numberOfLevels - 1;
				}
			}
			Depths[Depths.Length - 1] = 0; // Last split is always top level

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
			// Move the last level to the start so that the output for 'all splits' is reliable to the user
			//var allSplits = _splitLevelIndices[_splitLevelIndices.Count - 1];
			//_splitLevelIndices.RemoveAt(_splitLevelIndices.Count - 1);
			//_splitLevelIndices.Insert(0, allSplits);

			// Make a list of times and deltae for each level
			_levels = new SplitLevel[numberOfLevels];
			for (int i = 0; i < numberOfLevels; i++) {
				_levels[i] = new SplitLevel(splitLevelIndices[i]);
			}

			// Make a list of all the split names by level, taking into consideration removing dashes and {} parts
			Names = new List<List<string>>();
			for (int split = 0; split < run.Count; split++) {
				Names.Add(new List<string>());
				string name = run[split].Name;
				for (int lvl = 0; lvl < _levels.Length; lvl++) {
					string sectionName = GetSectionHeader(name);
					if (sectionName != null) {
						Names[split].Add(sectionName);
						name = name.Substring(sectionName.Length + 2);
						continue;
					}
					Names[split].Add(name);
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
	}

	/// <summary>
	/// One subsplit level: which splits it contains, and its PB segment times.
	/// Arrays are indexed by local index, i.e. position in Indices.
	/// Nothing here may describe the current attempt: CalculateSegments() rebuilds every level on any cache key change, including timer phase changes.
	/// </summary>
	class SplitLevel {

		// Local index → global index into the run
		public List<int> Indices;

		public Dictionary<TimingMethod, TimeSpan?[]> PbSegmentTimes;

		public SplitLevel(List<int> indices) {
			Indices = indices;
			int count = indices.Count;
			PbSegmentTimes = new Dictionary<TimingMethod, TimeSpan?[]>() { { TimingMethod.RealTime, new TimeSpan?[count] }, { TimingMethod.GameTime, new TimeSpan?[count] } };
		}
	}
}
