# Architecture

How the component works internally, in enough detail to change it safely. For the list of output files see [output-files.md](output-files.md). For the LiveSplit behavior this code depends on see [livesplit-notes.md](livesplit-notes.md). Known bugs are in [TODO.md](TODO.md); this document describes what the code *does*, and flags the places where that differs from what it is meant to do.

Line references are to `UI/Components/OutputToFileComponent.cs` as of commit `30d30fa`, unless another file is named.

## Files

| File | Role |
|---|---|
| `Properties/AssemblyInfo.cs` | `[assembly: ComponentFactory(typeof(OutputToFileFactory))]`. This attribute is how LiveSplit finds the component in the DLL. |
| `UI/Components/OutputToFileFactory.cs` | Name, description and category (Other) shown in the Layout Editor. `Create(state)` builds the component. |
| `UI/Components/OutputToFileComponent.cs` | Everything else: event handling, all calculations, all file writing. |
| `UI/Components/OutputToFileSettings.cs` (+ `.Designer.cs`, `.resx`) | The settings `UserControl` and its XML save/load. |
| `UI/Components/TimeFormatter.cs` | Converts `TimeSpan?` to display strings. |

## Lifecycle

1. LiveSplit loads the layout and calls `OutputToFileFactory.Create(state)`. The constructor ([:152-164](../UI/Components/OutputToFileComponent.cs#L152-L164)) creates the settings control with default values, subscribes to the timer events, and runs `CalculateSegments()`.
2. LiveSplit then calls `SetSettings(xml)` with the saved settings. This does **not** trigger a recalculation by itself. In practice the first `Update()` does it, because every cache key is new at that point.
3. `Update()` is called repeatedly for as long as the layout is open. The component has zero size, and `DrawVertical`/`DrawHorizontal` are empty.
4. `GetSettings()` runs when the layout is saved. LiveSplit compares `GetSettingsHashCode()` values to detect unsaved changes.
5. `Dispose()` unsubscribes from the events.

## When files get written

| Trigger | What runs |
|---|---|
| `OnStart` | `CalculateSegments()`, then `WriteSplits()` |
| `OnSplit`, `OnSkipSplit`, `OnUndoSplit` | `WriteSplits()` |
| `OnReset` | `WriteSplits()`, then `CalculateSegments()` |
| `Update()` (every frame) | 1. If `OutputTimer` is on: `WriteTimer(RealTime)` and `WriteTimer(GameTime)`. Each returns immediately unless that method's whole-second value changed. <br>2. If any cache key changed: `CalculateSegments()`, the info files (game, category, platform, attempts, finished runs, total splits), then `WriteSplits()`. |

The cache keys ([:518-525](../UI/Components/OutputToFileComponent.cs#L518-L525)) are attempt count, game name, category name, platform name, `FolderPath`, `SplitsBefore`, `SplitsAfter` and `CurrentPhase`.

Because the phase is a cache key, every start, pause, resume, finish and reset goes through row 2 of `Update()` one frame after the event handler has run. Because `CalculateSegments()` recreates all the arrays (see below), this wipes the deltas stored during the run. See [PAU](Defects.md#pau--pausing-or-finishing-wipes-stored-deltas).

Not cache keys: the `Output*` checkboxes, split names, split count and PB times. Changing any of those does nothing until something else triggers a rewrite.

LiveSplit fires every event *after* it has updated the timer state. A handler therefore sees the new state: on `OnSplit` the index has already moved on, and on the last split the phase is already `Ended`. Details are in [livesplit-notes.md](livesplit-notes.md).

`WriteSplits()` ([:498-506](../UI/Components/OutputToFileComponent.cs#L498-L506)) runs these in order:

1. `WriteSplitInformation()`: names and indexes.
2. `WriteSplitTimes(RealTime)`, `WriteSplitTimes(GameTime)`: the CurrentSplit and PreviousSplit time files. These also **store** the run, segment and gold deltas of the previous split.
3. `WriteSplitList(RealTime)`, `WriteSplitList(GameTime)`.
4. `WriteCompletedSplitList(RealTime)`, `WriteCompletedSplitList(GameTime)`.

Steps 3 and 4 read the deltas stored in step 2, so the order matters.

## Subsplit levels

This is the central idea. Every output except the root info files and the run timer is written once per **level**.

### Depth

A split's depth is its number of leading `-` characters (`_splitDepths`). When the Subsplits setting is off, every depth is 0. `GenerateSubsplitLevels` ([:209-282](../UI/Components/OutputToFileComponent.cs#L209-L282)) then does the following:

1. Counts levels: start at 1, and add a level for each consecutive depth 1, 2, … that some split actually has.
2. Clamps any depth ≥ the level count to `levels - 1`. (The loop skips split 0; see [CLM](Defects.md#clm--a-first-split-with-two-or-more-dashes-is-dropped).)
3. Forces the last split to depth 0. The last split is always a top-level split.

### Levels

Level `lvl` contains every split whose depth is ≤ `lvl`, in order. `_splitLevelIndices[lvl]` lists their **global** indices, meaning indices into `State.Run`. Position within that list is the **local** index.

- Level 0 contains top-level splits only.
- The last level (`_splitLevels.Length - 1`) contains every split. Local index equals global index there. Gold code and `State.CurrentSplit` lookups rely on this.

Folder names come from `MakeFile` ([:943-960](../UI/Components/OutputToFileComponent.cs#L943-L960)):

| Level | Folder tag |
|---|---|
| last | `AllSplits` |
| 0 (when not also the last) | `SansSubsplits` |
| 1 … last−1 | `SansSubsplits_Level{n}` |

With a single level (no subsplits, or the setting off), level 0 *is* the last level, so it's tagged `AllSplits` and there is no `SansSubsplits` folder.

### Display names

`_splitNamesByLevel[global][lvl]` holds a split's name as displayed at each level. Building it walks the levels from 0 upwards:

- If the name starts with `{Header}` (the convention from LiveSplit's own Subsplits component), the level shows `Header`, and the `{Header}` prefix is removed for the deeper levels.
- Otherwise the level shows the current name, and one leading `-` is removed for the deeper levels.

A split only appears at levels ≥ its depth, and by then all its dashes have been removed. The raw `Segment.Name` goes to the `*_Raw.txt` files.

### Worked example

These values were checked against the component's actual output.

| Global | Raw name | Depth | Level 0 name (`SansSubsplits`) | Level 1 name (`AllSplits`) |
|---|---|---|---|---|
| 0 | `-A1` | 1 | (not shown) | `A1` |
| 1 | `-A2` | 1 | (not shown) | `A2` |
| 2 | `{Area A}A3` | 0 | `Area A` | `A3` |
| 3 | `-B1` | 1 | (not shown) | `B1` |
| 4 | `{Area B}B2` | 0 | `Area B` | `B2` |

- `_splitLevelIndices` = `[[2, 4], [0, 1, 2, 3, 4]]`.
- At level 0, "Area A" is a single segment covering A1 + A2 + A3. Its PB segment time is the PB split time of split 2.
- While the runner is on A1 (global 0), level 0's *current* split is "Area A" (local 0) and it has no previous split.
- `SplitList\AllSplits\Names_Indent_0.txt` contains only `A3` and `B2`. `Names_Indent_1.txt` contains only `A1`, `A2` and `B1`. Every other line is blank, so each file can be a separately indented OBS text source.

With depths 0/1/2 (splits `A`, `-B`, `--C`, `D`) there are three levels: `SansSubsplits` = A, D; `SansSubsplits_Level1` = A, B, D; `AllSplits` = all four.

## Per-level data: `SplitLevel`

Each level holds five `Dictionary<TimingMethod, TimeSpan?[]>`, one array per timing method, indexed by **local** index ([:62-77](../UI/Components/OutputToFileComponent.cs#L62-L77)):

| Field | Meaning | Filled by |
|---|---|---|
| `PbSegmentTimes` | PB split time of this member minus PB split time of the previous member. The first member gets its PB split time. Null if either value is missing. | `CalculatePBSegments`, during `CalculateSegments()` |
| `LiveSegmentTimes` | The same calculation using this attempt's `SplitTime`s | `CalculateLiveSegment`, for the previous member, each time `WriteSplitTimes` runs |
| `RunDeltae` | `SplitTime − PersonalBestSplitTime` | stored by `WriteSplitTimes` at the previous member's index |
| `SegmentDeltae` | `LiveSegmentTimes − PbSegmentTimes` | same |
| `GoldDeltae` | `LiveSegmentTimes − BestSegmentTime`, last level only | same |

`CalculateSegments()` throws all of these away and rebuilds them. It also rebuilds the level structure (and builds it twice; see [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice)).

A dictionary's `.Count` is the number of timing methods (2), not the array length. That mistake is in `CalculateLiveSegment` ([FIN](Defects.md#fin--final-splits-segment-time-and-deltas-are-lost)).

## Phase-dependent indexes

Most write functions start from these two helpers, which return **local** indexes:

| Timer phase | `GetCurrentSubsplitIndex(lvl)` | `GetPreviousSubsplitIndex(lvl)` |
|---|---|---|
| NotRunning | −1 | −2 |
| Running / Paused, before the level's first member is completed | 0 | −1 |
| Running / Paused, otherwise | local index of the first member whose global index ≥ `CurrentSplitIndex` | that − 1 |
| Ended | `Count` | `Count − 1` |

The functions branch on the phase separately and inconsistently. For example, `WriteSplitTimes` uses `!= -1`, which lets NotRunning (−2) through to a code path that happens to write `-`. Check every phase when changing one of these functions.

## How the values are calculated

- **Comparison:** always Personal Best (`PersonalBestSplitTime`). `state.CurrentComparison` is ignored.
- **Timing methods:** every timed file is written once for `RealTime` and once for `GameTime`. Game time is `null`, written as `-`, until the game time is initialized.
- **Run delta:** live split time minus PB split time, for the same split.
- **Segment delta:** live segment minus PB segment, at this level.
- **Gold delta:** live segment minus `BestSegmentTime`, at the last level only. Best segments and PB times don't change during a run; LiveSplit updates them on reset. Comparisons made during the run therefore use the values from before the run.
- **`Sign.txt`** ([:850-871](../UI/Components/OutputToFileComponent.cs#L850-L871)): a decision chain over the run, segment and gold deltas. "Ahead" or "PB" means run delta < 0. The values are listed in [output-files.md](output-files.md#sign-values). Known defects: the "gained" branch is missing mid-run ([GAI](Defects.md#gai--sign-never-says-gainedahead-or-gainedbehind)), and a null gold delta counts as a gold ([NUL](Defects.md#nul--sign-reports-a-gold-whenever-the-gold-delta-is-null)).
- **Highlights** ([:586-596](../UI/Components/OutputToFileComponent.cs#L586-L596)): each completed line gets exactly one highlight, checked in this order:
  1. Gold: gold delta < 0, last level only.
  2. AheadGained: run delta < 0 and segment delta < 0.
  3. AheadLost: run delta < 0.
  4. BehindGained: segment delta < 0.
  5. BehindLost: the split has a PB time.

  `HIGHLIGHT_FILL` is 43 × `█`.
- **Index / ReverseIndex:** the local index of the current member, i.e. the number of members completed at this level, and `Count − Index − 1`. When the timer isn't running these are −1 / `Count`; after the finish they are `Count` / −1.
- **Run timer:** `State.CurrentTime[method]`. **Segment timer:** the run timer minus the `SplitTime` of the level's previous member. It equals the run timer on the first member and when the timer isn't running.
- **FinishedRunsCount:** the number of attempts in history with a real time, plus 1 while the timer is `Ended`, because the current attempt joins the history only on reset.

## Writing files

- Every path is a `const string FILE_*` template ([:86-148](../UI/Components/OutputToFileComponent.cs#L86-L148)) with these placeholders:
  - `{0}` the timing method (`RealTime` / `GameTime`);
  - `{1}` the level tag;
  - `{2}` the indent index, `Names_Indent_*` only. Those files are pre-formatted with `{2}` filled in and `{1}` re-inserted as a literal ([:651](../UI/Components/OutputToFileComponent.cs#L651)).
- There are three `MakeFile` overloads ([:943-984](../UI/Components/OutputToFileComponent.cs#L943-L984)):
  - `(template, contents, method?, level)` fills `{0}` and `{1}`;
  - `(template, contents, method)` fills `{0}`;
  - `(path, contents)` writes the file.
- The final overload does nothing when `FolderPath` is empty, creates missing directories, and calls `File.WriteAllText`, which writes UTF-8 without a BOM. There is no error handling, so exceptions escape into LiveSplit.
- Multi-line files use `\n` and end with a trailing `\n`. Every list file has the same number of lines, so separate OBS text sources line up.
- Which outputs each setting controls:

  | Setting | Effect |
  |---|---|
  | `OutputTimer` | `Timers\` |
  | `OutputSplitList` | `SplitList\` |
  | `OutputCompletedSplits` | `CompletedSplits\` |
  | `OutputSubsplits` | Whether there is more than one level |

  Everything else is always written. Turning an output off doesn't delete its old files.
- Nothing is written in the background: every write happens synchronously inside the LiveSplit event or `Update()` call.

## Settings

`OutputToFileSettings` exposes these plain properties:

| Property | Type |
|---|---|
| `FolderPath` | string |
| `SplitsBefore` | int |
| `SplitsAfter` | int |
| `OutputTimer` | bool |
| `OutputSplitList` | bool |
| `OutputSubsplits` | bool |
| `OutputCompletedSplits` | bool |

Each one is bound to its control with `DataBindings.Add(..., DataSourceUpdateMode.OnPropertyChanged)` in the constructor.

- Binding is the only thing that updates these properties from the UI. Most of the `*_Changed` handlers in the .cs file aren't connected to any control. The two connected handlers are `checkBoxOutputSplitList_CheckedChanged`, which also enables or disables the split-list group, and `buttonFolderPath_Click`.
- The class doesn't implement `INotifyPropertyChanged`, so setting a property in code won't update its control. `buttonFolderPath_Click` updates the textbox itself for this reason.
- XML keys: `Version` (`0.1`), `FolderPath`, `SplitsBefore`, `SplitsAfter`, `OutputTimer`, `OutputSplitList`, `OutputSubsplits`. `OutputCompletedSplits` is missing ([SAV](Defects.md#sav--completed-splits-setting-is-never-saved)).
- `GetSettingsHashCode()` reuses `CreateSettingsNode(null, null)`, which XORs the hashes of the saved values. Anything not saved isn't part of the hash.
