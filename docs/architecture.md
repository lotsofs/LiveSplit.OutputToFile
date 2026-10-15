# Architecture

How the component works internally, in enough detail to change it safely. For the list of output files see [output-files.md](output-files.md). For the LiveSplit behavior this code depends on see [livesplit-notes.md](livesplit-notes.md). Known bugs are in [TODO.md](TODO.md); this document describes what the code *does*, and flags the places where that differs from what it is meant to do.

Code is referred to by member name. The table below says which file each class is in.

## Files

| File | Role |
|---|---|
| `Properties/AssemblyInfo.cs` | `[assembly: ComponentFactory(typeof(OutputToFileFactory))]`. This attribute is how LiveSplit finds the component in the DLL. |
| `UI/Components/OutputToFileFactory.cs` | Name, description and category (Other) shown in the Layout Editor. `Create(state)` builds the component. |
| `UI/Components/OutputToFileComponent.cs` | The LiveSplit side: event handling, change detection in `Update()`, and when each writer runs. Owns the one `SplitLevels` instance, the one `FileQueue` and the four writers. |
| `UI/Components/SplitLevels.cs` | `SplitLevels` (depths, display names, the levels) and `SplitLevel` (one level's members and arrays). Builds the levels and the PB segment times. |
| `UI/Components/OutputWriter.cs` | Abstract base of the writers. Holds the state, settings, levels and file queue they share, the `MakeFile` overloads, `GetCurrentSubsplitIndex`/`GetPreviousSubsplitIndex`, and the live times and deltas (`LiveRunTime`, `LiveSegmentTime`, `RunDelta`, `SegmentDelta`, `GoldDelta`, `IsGold`). |
| `UI/Components/FileQueue.cs` | Writes the files on a background thread: skips unchanged contents, keeps the latest contents per file, and records the latest write error. |
| `UI/Components/RunInfoFiles.cs` | Writer for the root files. |
| `UI/Components/TimerFiles.cs` | Writer for `Timers\`, with its own `GraphicsCache`. |
| `UI/Components/SplitFiles.cs` | Writer for `CurrentSplit\` and `PreviousSplit\`: the previous split's times and deltas, and `Sign.txt`. |
| `UI/Components/SplitListFiles.cs` | Writer for `SplitList\` and `CompletedSplits\`. |
| `UI/Components/Signs.cs` | The `Signs` enum, whose names are the `Sign.txt` values. |
| `UI/Components/OutputToFileSettings.cs` (+ `.Designer.cs`, `.resx`) | The settings `UserControl` and its XML save/load. |
| `UI/Components/TimeFormatter.cs` | Converts `TimeSpan?` to display strings. |

## Lifecycle

1. LiveSplit loads the layout and calls `OutputToFileFactory.Create(state)`. The component's constructor creates the settings control with default values, subscribes to the timer events and `RunManuallyModified`, creates the shared `SplitLevels`, the `FileQueue` and the four writers, and runs `CalculateSegments()`.
2. LiveSplit then calls `SetSettings(xml)` with the saved settings. This does **not** trigger a recalculation by itself. In practice the first `Update()` does it, because every cache key is new at that point.
3. `Update()` is called repeatedly for as long as the layout is open. The component has zero size, and `DrawVertical`/`DrawHorizontal` are empty.
4. `GetSettings()` runs when the layout is saved. `GetSettingsHashCode()` is called on every refresh, but LiveSplit only uses it to decide whether to redraw, not to detect unsaved changes ([livesplit-notes.md](livesplit-notes.md#layout-save-prompt-and-the-settings-hash-read-in-source)).
5. `Dispose()` unsubscribes from the events, then waits up to 2 seconds for queued files to be written.

## When files get written

| Trigger | What runs |
|---|---|
| `OnStart` | `CalculateSegments()`, then `WriteSplits()` |
| `OnSplit`, `OnSkipSplit`, `OnUndoSplit` | `WriteSplits()` |
| `OnReset` | `FileQueue.ForgetWrittenFiles()`, `WriteSplits()`, then `CalculateSegments()` |
| `RunManuallyModified` | Counts the edit (`_runEdits`), a cache key, so the next `Update()` rewrites everything. Several edits in one frame cost one rewrite. |
| `Update()` (every frame) | 0. If the latest write error changed, show it in the settings (`ShowWriteError`). <br>1. If `OutputTimer` is on: `TimerFiles.WriteTimer(RealTime)` and `WriteTimer(GameTime)`. Each returns immediately unless that method's whole-second value changed. <br>2. If any cache key changed: `CalculateSegments()`, `RunInfoFiles.Write()` (game, category, platform, attempts, finished runs, total splits), then `WriteSplits()`. |

The cache keys in `Update()` are attempt count, game name, category name, platform name, the `RunManuallyModified` count, the `Run` object (by reference), `FolderPath`, `SplitsBefore`, `SplitsAfter` and `CurrentPhase`.

Split edits are seen through `RunManuallyModified`: LiveSplit raises it after each change in Edit Splits, which edits the run in place, and after opening splits. Close Splits replaces the run without raising it, which the `Run` key catches. See [livesplit-notes.md](livesplit-notes.md#editing-and-opening-splits-read-in-source).

Because the phase is a cache key, every start, pause, resume, finish and reset goes through row 2 of `Update()` one frame after the event handler has run, and so rebuilds the levels. That is harmless because the levels hold nothing about the current attempt (see [Per-level data](#per-level-data-splitlevel)).

Not cache keys: the `Output*` checkboxes. Changing one does nothing until something else triggers a rewrite ([CHK](Defects.md#chk--output-checkboxes-dont-take-effect-at-once)).

LiveSplit fires every event *after* it has updated the timer state. A handler therefore sees the new state: on `OnSplit` the index has already moved on, and on the last split the phase is already `Ended`. Details are in [livesplit-notes.md](livesplit-notes.md).

`WriteSplits()` runs these in order:

1. `SplitFiles.WriteSplitInformation()`: names and indexes.
2. `SplitFiles.WriteSplitTimes(RealTime)`, `WriteSplitTimes(GameTime)`: the CurrentSplit and PreviousSplit time files.
3. `SplitListFiles.WriteSplitLists(RealTime)`, `WriteSplitLists(GameTime)`: for each level, the `SplitList\` window and the `CompletedSplits\` list. `WriteSplitLists` works out each list's range of splits, and `WriteSplitListLevel` writes the files for that range.

The steps don't depend on each other: each computes what it needs from the run.

## Subsplit levels

This is the central idea. Every output except the root info files and the run timer is written once per **level**.

### Depth

A split's depth is its number of leading `-` characters (`SplitLevels.Depths`). When the Subsplits setting is off, every depth is 0. `SplitLevels.GenerateSubsplitLevels` then does the following:

1. Counts levels: start at 1, and add a level for each consecutive depth 1, 2, … that some split actually has.
2. Clamps any depth ≥ the level count to `levels - 1`, so a split deeper than any level goes into the deepest one.
3. Forces the last split to depth 0. The last split is always a top-level split.

### Levels

Level `lvl` contains every split whose depth is ≤ `lvl`, in order. `Levels[lvl].Indices` lists their **global** indices, meaning indices into `State.Run`. Position within that list is the **local** index.

- Level 0 contains top-level splits only.
- The last level (`Levels.Count - 1`) contains every split. Local index equals global index there. The current gold time (`State.CurrentSplit.BestSegmentTime` in `WriteSplitTimes`) relies on this.

Folder names come from `MakeFile` in `OutputWriter`:

| Level | Folder tag |
|---|---|
| last | `AllSplits` |
| 0 (when not also the last) | `SansSubsplits` |
| 1 … last−1 | `SansSubsplits_Level{lvl+1}` |

A middle level is named after the shallowest depth it leaves out, so level 1, which drops the splits with 2+ dashes, is `SansSubsplits_Level2`. The README documents these names.

With a single level (no subsplits, or the setting off), level 0 *is* the last level, so it's tagged `AllSplits` and there is no `SansSubsplits` folder.

### Display names

`SplitLevels.Names[global][lvl]` holds a split's name as displayed at each level. Building it walks the levels from 0 upwards:

- If the name starts with `{Header}` (the convention from LiveSplit's own Subsplits component), the level shows `Header`, and the `{Header}` prefix is removed for the deeper levels.
- Otherwise the level shows the current name, and one leading `-` is removed for the deeper levels.

A split only appears at levels ≥ its depth, and usually all its dashes have been removed by then. A split whose depth was lowered, by the clamp or because it is the last split, keeps the dashes that are left ([DSH](Defects.md#dsh--moved-splits-keep-leftover-dashes-in-their-names)). With depths 0 and 1 present, for example, `---C` shows as `--C` at `AllSplits`. With splits `--Intro`, `A`, `B` there is one level, and the first split shows as `--Intro`.

A name that is empty, or becomes empty once its `{Header}` or dashes are removed (`""`, `-`, `{}`), shows as an empty line. The raw `Segment.Name` goes to the `*_Raw.txt` files.

### Worked example

These values were checked against the component's actual output.

| Global | Raw name | Depth | Level 0 name (`SansSubsplits`) | Level 1 name (`AllSplits`) |
|---|---|---|---|---|
| 0 | `-A1` | 1 | (not shown) | `A1` |
| 1 | `-A2` | 1 | (not shown) | `A2` |
| 2 | `{Area A}A3` | 0 | `Area A` | `A3` |
| 3 | `-B1` | 1 | (not shown) | `B1` |
| 4 | `{Area B}B2` | 0 | `Area B` | `B2` |

- The levels' `Indices` are `[2, 4]` and `[0, 1, 2, 3, 4]`.
- At level 0, "Area A" is a single segment covering A1 + A2 + A3. Its PB segment time is the PB split time of split 2.
- While the runner is on A1 (global 0), level 0's *current* split is "Area A" (local 0) and it has no previous split.
- `SplitList\AllSplits\Names_Indent_0.txt` contains only `A3` and `B2`. `Names_Indent_1.txt` contains only `A1`, `A2` and `B1`. Every other line is blank, so each file can be a separately indented OBS text source.

With depths 0/1/2 (splits `A`, `-B`, `--C`, `D`) there are three levels: `SansSubsplits` = A, D; `SansSubsplits_Level2` = A, B, D; `AllSplits` = all four.

## Per-level data: `SplitLevel`

Each `SplitLevel` holds its members, `Indices` (local index → global index), and `PbSegmentTimes`, a `Dictionary<TimingMethod, TimeSpan?[]>` with one array per timing method, indexed by **local** index. Each entry is the PB split time of this member minus the PB split time of the previous member. The first member gets its PB split time. An entry is null if either value is missing. `CalculatePBSegments` fills it during `CalculateSegments()`.

`CalculateSegments()` rebuilds the levels and their arrays in place, in the one `SplitLevels` instance the writers share. It runs on every cache-key change, including every timer phase change, and builds the level structure twice (see [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice)). So a level must hold nothing about the current attempt; anything stored there would be wiped by a pause.

A dictionary's `.Count` is the number of timing methods (2), not the array length; use `[method].Length`.

### Live times and deltas

This attempt's values are computed from the run whenever files are written, by these `OutputWriter` helpers. Each takes a timing method, a level and a local index, and returns null for a negative index, i.e. the sentinels in the table below. Subtracting a null gives null.

| Helper | Value |
|---|---|
| `LiveRunTime` | The member's `SplitTime` |
| `LiveSegmentTime` | `LiveRunTime` minus the previous member's. The first member gets its `LiveRunTime`. |
| `RunDelta` | `LiveRunTime − PersonalBestSplitTime` |
| `SegmentDelta` | `LiveSegmentTime − PbSegmentTimes` |
| `GoldDelta` | `LiveSegmentTime − BestSegmentTime`, last level only |
| `IsGold` | See [How the values are calculated](#how-the-values-are-calculated) |

Computing them on every write is safe because PB times and best segments don't change during a run ([livesplit-notes.md](livesplit-notes.md#comparison-data-verified)). A completed split's `SplitTime` changes only when it is undone, and then it no longer counts as completed.

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
- **Gold** (`OutputWriter.IsGold`): at the last level only, the live segment exists and either the split has no best segment yet or the live segment beats it. A skipped split, and the split after it, have no live segment, so they aren't golds. `Sign.txt` and the highlights both use this rule.
- **`Sign.txt`** (in `SplitFiles.WriteSplitTimes`): a decision chain over gold, run delta and segment delta. "Ahead" or "PB" means run delta < 0. The values are listed in [output-files.md](output-files.md#sign-values).
- **Highlights** (in `SplitListFiles.WriteSplitListLevel`): each completed line gets exactly one highlight, checked in this order:
  1. Gold: the segment is a gold, last level only.
  2. AheadGained: run delta < 0 and segment delta < 0.
  3. AheadLost: run delta < 0.
  4. BehindGained: segment delta < 0.
  5. BehindLost: the split has a PB time.

  `HIGHLIGHT_FILL` is 43 × `█`.
- **Index / ReverseIndex:** the local index of the current member, i.e. the number of members completed at this level, and `Count − Index − 1`. When the timer isn't running these are −1 / `Count`; after the finish they are `Count` / −1.
- **Run timer:** `State.CurrentTime[method]`. **Segment timer:** the run timer minus the `SplitTime` of the level's previous member. It equals the run timer on the first member and when the timer isn't running.
- **FinishedRunsCount:** the number of attempts in history with a real time, plus 1 while the timer is `Ended`, because the current attempt joins the history only on reset.

## Writing files

- Every path is a `const string FILE_*` template, declared in the writer for its folder, with these placeholders:
  - `{0}` the timing method (`RealTime` / `GameTime`);
  - `{1}` the level tag;
  - `{2}` the indent index, `Names_Indent_*` only. Those files are pre-formatted with `{2}` filled in and `{1}` re-inserted as a literal (in `WriteSplitListLevel`).
- The split list templates (`FILE_LIST_*`) are relative to the list folder. `WriteSplitListLevel` prefixes them with `FOLDER_SPLITLIST` (`SplitList\`) or `FOLDER_COMPLETEDSPLITS` (`CompletedSplits\`), so both lists write the same files.
- There are three `MakeFile` overloads, in `OutputWriter`:
  - `(template, contents, method?, level)` fills `{0}` and `{1}`;
  - `(template, contents, method)` fills `{0}`;
  - `(path, contents)` queues the file.
- The final overload does nothing when `FolderPath` is empty. It builds the full path (an `ArgumentException` from invalid characters in the folder setting goes to `FileQueue.ReportError`) and calls `FileQueue.Write`. Nothing is written on the calling thread.
- `FileQueue` (one per component, shared by the writers, every field under one lock):
  - `Write` skips the file when its contents equal what is already queued for that path, or, with nothing queued, what was last handed to the disk (`_written`). On a split, only the files that changed are queued. A file with no `{0}` in its path but written once per timing method, such as `Names.txt`, is skipped the second time.
  - Otherwise it queues the contents in `_pending`, replacing any older contents for that path, and starts a drain task on the thread pool if none is running.
  - `Drain` takes all pending files at once, records them in `_written`, and writes them outside the lock with `File.WriteAllText` (UTF-8 without a BOM). On `DirectoryNotFoundException` it creates the directory and writes again. It repeats until nothing is pending.
  - Every exception from a write is caught, per file. A failed file doesn't stop the others, and nothing reaches LiveSplit or ends the drain. The file is dropped from `_written`, so the next `Write` retries it even with the same contents. That covers a locked or read-only file, a missing drive and no permission.
  - The latest failure is kept as text with the file and time, and is cleared once that file (by its path relative to the folder) is written successfully. `Update()` picks up each change with `TakeErrorChange` and shows it with `OutputToFileSettings.ShowWriteError`, so the settings control is only touched on the UI thread.
  - `OnReset` calls `ForgetWrittenFiles()`, so each attempt ends with every file written in full. A file deleted or edited outside LiveSplit comes back when its contents next change, or at the next reset.
  - `WaitForWrites(timeout)` blocks until the drain task is done. `Dispose()` uses it with 2 seconds; tests use `OutputToFileComponent.WaitForFileWrites()`.
- Multi-line files use `\n` and end with a trailing `\n`. Every list file has the same number of lines, so separate OBS text sources line up.
- Which outputs each setting controls:

  | Setting | Effect |
  |---|---|
  | `OutputTimer` | `Timers\` |
  | `OutputSplitList` | `SplitList\` |
  | `OutputCompletedSplits` | `CompletedSplits\` |
  | `OutputSubsplits` | Whether there is more than one level |

  Everything else is always written. Turning an output off doesn't delete its old files.
- The file *contents* are still built synchronously inside the LiveSplit event or `Update()` call; only the writing is in the background. On a split that is under a millisecond on the calling thread, with the writes taking about 35 ms on the drain task (40 splits, every output on).

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

Each one is bound to its control with `DataBindings.Add` in the constructor. All but `FolderPath` use `DataSourceUpdateMode.OnPropertyChanged`, so a change applies at once.

- `FolderPath` uses `OnValidation`: a typed path applies only when focus leaves the textbox, through Tab, a click on another control, OK, or Enter (which clicks the dialog's AcceptButton; see [livesplit-notes.md](livesplit-notes.md#layout-settings-dialog-read-in-source)). Applying every keystroke would write a full set of files to each partial path, because `FolderPath` is a cache key.
  - The Browse button has `CausesValidation = false`, so moving focus to it doesn't apply a partly typed path. Its handler sets `FolderPath` and the textbox itself. A path typed and then left unbrowsed applies at the next focus change to another control.
  - Closing a modal dialog with X doesn't validate the focused control (seen headless, 2026-10-15), so a path typed but never left is dropped.
- Binding is the only thing that updates these properties from the UI, apart from Browse. Most of the `*_Changed` handlers in the .cs file aren't connected to any control. The two connected handlers are `checkBoxOutputSplitList_CheckedChanged`, which also enables or disables the split-list group, and `buttonFolderPath_Click`.
- The class doesn't implement `INotifyPropertyChanged`, so setting a property in code won't update its control while it is shown. `buttonFolderPath_Click` updates the textbox itself for this reason. The next time the control is shown in a new dialog, the bindings read the properties again (seen headless for the textbox, 2026-10-15), so after Cancel restores the settings the dialog shows them on reopening.
- XML keys: `Version` (`0.1`), `FolderPath`, `SplitsBefore`, `SplitsAfter`, `OutputTimer`, `OutputSplitList`, `OutputSubsplits`, `OutputCompletedSplits`.
- `WriteError` isn't a setting: it holds the latest failed file write, shown in red in `labelWriteError` under the split-list group, and is neither saved nor hashed. `Update()` sets and clears it through `ShowWriteError` (see "Writing files").
- `GetSettingsHashCode()` reuses `CreateSettingsNode(null, null)`, which XORs the hashes of the saved values, so two changes can cancel out. That's harmless here: LiveSplit only uses the hash to decide whether to redraw, and this component draws nothing.
