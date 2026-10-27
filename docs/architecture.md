# Architecture

How the component works internally, in enough detail to change it safely. For the list of output files see [output-files.md](output-files.md). For the LiveSplit behavior this code depends on see [livesplit-notes.md](livesplit-notes.md). Known bugs are in [TODO.md](TODO.md); this document describes what the code *does*, and flags the places where that differs from what it is meant to do.

Code is referred to by member name. The table below says which file each class is in.

## Files

| File | Role |
|---|---|
| `Properties/AssemblyInfo.cs` | `[assembly: ComponentFactory(typeof(OutputToFileFactory))]`. This attribute is how LiveSplit finds the component in the DLL. Also the version, `AssemblyVersion`, which is the release number as `N.0`. |
| `UI/Components/OutputToFileFactory.cs` | Name, description and category (Other) shown in the Layout Editor. `Create(state)` builds the component. Its update members point LiveSplit's startup update check at `update.xml` on `main` and at the GitHub release downloads; `Version` is the assembly's version ([releasing.md](releasing.md)). |
| `update.xml` | The update feed LiveSplit reads at startup: one entry per release since 8, newest first. Changed only when releasing. |
| `UI/Components/OutputToFileComponent.cs` | The LiveSplit side: event handling, change detection in `Update()`, and when each writer runs. Owns the one `SplitLevels` instance, the one `FileQueue` and the four writers. |
| `UI/Components/SplitLevels.cs` | `SplitLevels` (depths, display names, the levels) and `SplitLevel` (one level's members and arrays). Chooses the comparisons to write, and builds the levels, the comparisons' segment times and the best segments. |
| `UI/Components/OutputWriter.cs` | Abstract base of the writers. Holds the state, settings, levels and file queue they share, the `MakeFile` overloads, `GetCurrentSubsplitIndex`/`GetPreviousSubsplitIndex`, and the live times and deltas (`LiveRunTime`, `LiveSegmentTime`, `ComparisonTime`, `RunDelta`, `SegmentDelta`, `GoldDelta`, `IsGold`). |
| `UI/Components/FileQueue.cs` | Writes the files on a background thread: skips unchanged contents, keeps the latest contents per file, records the latest write error, and empties the output files a full rewrite no longer writes. |
| `UI/Components/RunInfoFiles.cs` | Writer for the root files. |
| `UI/Components/TimerFiles.cs` | Writer for `Timers\`, with its own change check, `HasTimerChanged`. |
| `UI/Components/SplitFiles.cs` | Writer for `CurrentSplit\` and `PreviousSplit\`: the previous split's times and deltas, and `Sign.txt`. |
| `UI/Components/SplitListFiles.cs` | Writer for `SplitList\` and `CompletedSplits\`. |
| `UI/Components/Signs.cs` | The `Signs` enum, whose names are the `Sign.txt` values. |
| `UI/Components/OutputToFileSettings.cs` (+ `.Designer.cs`, `.resx`) | The settings `UserControl` and its XML save/load. |
| `UI/Components/TimeFormatter.cs` | Converts `TimeSpan?` to display strings with LiveSplit's formatters, at a `FormatAccuracy` (the enum is there too). The writers call it through `OutputWriter.DurationString` and `DeltaString`, which pass the Time Format settings; `TimerFiles` asks for whole seconds. |

## Lifecycle

1. LiveSplit loads the layout and calls `OutputToFileFactory.Create(state)`. The component's constructor creates the settings control with default values, subscribes to the timer events and `RunManuallyModified`, creates the shared `SplitLevels`, the `FileQueue` and the four writers, and runs `CalculateSegments()`.
2. LiveSplit then calls `SetSettings(xml)` with the saved settings. This does **not** trigger a recalculation by itself. In practice the first `Update()` does it, because every cache key is new at that point.
3. `Update()` is called repeatedly for as long as the layout is open. The component has zero size, and `DrawVertical`/`DrawHorizontal` are empty.
4. `GetSettings()` runs when the layout is saved. `GetSettingsHashCode()` is called on every refresh, but LiveSplit only uses it to decide whether to redraw, not to detect unsaved changes ([livesplit-notes.md](livesplit-notes.md#layout-save-prompt-and-the-settings-hash-read-in-source)).
5. `Dispose()` unsubscribes from the events, runs one last `Update()`, then waits up to 2 seconds for queued files to be written.
   - LiveSplit resets the timer when it closes, and on Close Splits. It then disposes the component with no frame in between ([livesplit-notes.md](livesplit-notes.md#closing-livesplit-and-changing-layouts-read-in-source)), so the last `Update()` is what writes that reset.
   - When nothing has changed since the last frame, as when another layout is opened, that `Update()` writes nothing.
   - An exception from it goes to LiveSplit's log, `Log.Error`. Thrown out of `Dispose()` while LiveSplit closes, it would stop LiveSplit from disposing the other components.

**Threads.**
- The timer events can arrive on a thread other than the UI thread. LiveSplit's server raises them on its connection's thread ([livesplit-notes.md](livesplit-notes.md#which-thread-raises-the-timer-events-mostly-read-in-source)).
- So the event handlers only count events (`Interlocked.Increment`). Everything else runs in `Update()`, on the UI thread, so the levels, the writers' caches and the settings control are only ever used from one thread. `FileQueue` locks its own state for its drain task.
- The caller still changes LiveSplit's state outside the component. An `Update()` can therefore read a split half made, and an exception from that is caught and logged by LiveSplit.
- `Update()` reads the event counts before any state, and an event always comes after its change. So such a change is counted, and the files are rewritten from the finished state on the next frame.
- Don't do work in a handler. When the server drives the timer, a handler would race `Update()` over the levels and could throw. The server doesn't catch handler exceptions, so that closes LiveSplit.

## When files get written

| Trigger | What runs |
|---|---|
| `OnStart`, `OnSplit`, `OnSkipSplit`, `OnUndoSplit`, `OnReset` | Counts the event (`_timerEvents`), a cache key, so the next `Update()` rewrites everything. `OnReset` also counts the reset (`_resets`). Several events in one frame cost one rewrite. |
| `RunManuallyModified` | Counts the edit (`_runEdits`), a cache key, so the next `Update()` rewrites everything. Several edits in one frame cost one rewrite. |
| `Update()` (every frame) | 0. Read the three counts, before any state. If the latest write error changed, show it in the settings (`ShowWriteError`). If the reset count changed, `FileQueue.ForgetWrittenFiles()`, so each attempt ends with every file written in full. <br>1. If any cache key or split name changed (below), a full rewrite: `FileQueue.BeginSweep()`, `CalculateSegments()`, `RunInfoFiles.Write()` (game, category, platform, attempts, finished runs, total splits), then `WriteSplits()`. <br>2. If `OutputTimer` is on: `TimerFiles.WriteTimer(RealTime)` and `WriteTimer(GameTime)`. Each returns immediately unless what one of that method's files shows changed since its last write: the run timer's or a level's segment timer's whole second (truncated, sign included), or the timer phase. `TimerFiles` keeps each level's segment start, the time its segment timer counts from, and finds them again only after `SegmentStartsChanged()`, which `CalculateSegments()` calls. A frame therefore costs a subtraction per level, not a search. After a full rewrite every timer file is written, because finding the segment starts again also forgets what the files show. <br>3. After a full rewrite: `FileQueue.EndSweep()`, which empties the output files the rewrite didn't write (see [Writing files](#writing-files)). |
| `Dispose()` | One last `Update()`, as above, so that a reset LiveSplit makes just before disposing the component reaches the files ([Lifecycle](#lifecycle)). With the base run mid-run, it takes about 17 ms until every file is written, and with 590 files about 65–110 ms (seen headless, 2026-10-26). |

The cache keys in `Update()` are:
- attempt count, game name, category name, platform name;
- the `RunManuallyModified` count, the timer event count, the `Run` object (by reference);
- `FolderPath`, `SplitsBefore`, `SplitsAfter`, the four `Output*` settings, `AllCustomComparisons`, `TickedComparisonsVersion`, `TimeAccuracy` and `DeltaAccuracy`;
- `CurrentPhase`, and `state.CurrentComparison`, since selecting a comparison raises no event;
- for each comparison written, its time at the last split (`_comparisonChecks`). LiveSplit regenerates its generated comparisons without a timer event after a reset through the server and from its Settings dialog ([livesplit-notes.md](livesplit-notes.md#comparisons-read-in-source-partly-checked-headless)). Their cumulative times then change at the last split, so this key catches it.

Split edits are seen through `RunManuallyModified`: LiveSplit raises it after each change in Edit Splits, which edits the run in place, and after opening splits. Close Splits replaces the run without raising it, and so does every reset after it while LiveSplit is in timer-only mode. The `Run` key catches both. See [livesplit-notes.md](livesplit-notes.md#editing-and-opening-splits-read-in-source).

Split names are checked apart from the cache keys, because LiveSplit's server renames a split in place and raises no event (`setsplitname`, `setcurrentsplitname`).
- `CalculateSegments()` copies the names into `_splitNames` before the levels and the writers read them. A rename after that copy is seen on the next frame.
- Every frame, `SplitNamesChanged()` compares each split's name with that copy. A difference causes a full rewrite, which also rebuilds the levels when a rename adds or removes dashes or a `{section}`.
- The string comparison checks the reference first, so a run without renames costs one reference comparison per split. A name set again to the same text doesn't cause a rewrite.
- On the 1,317-split GTA SA file, mid-run with every output on, a frame without a rewrite takes about 10 µs instead of 1.5 µs (Release build, measured 2026-10-26).

So every start, split, skip, undo and reset is written by a full rewrite on the next frame. So are a pause and a resume, through the phase key. Every rewrite rebuilds the levels, which is harmless because they hold nothing about the current attempt (see [Per-level data](#per-level-data-splitlevel)).
- A mid-run split's `Update()` takes about 0.3 ms with 5 splits and 0.6 ms with 40 (Debug build, every output on).
- The files reach the disk up to one frame after the event: about 25 ms at `RefreshRate` 40, or later if a LiveSplit message box is open.

LiveSplit fires every event *after* it has updated the timer state, and the rewrite comes after the event, so it sees the new state. Details are in [livesplit-notes.md](livesplit-notes.md).

`WriteSplits()` runs these in order:

1. `SplitFiles.WriteSplitInformation()`: names and indexes.
2. `SplitFiles.WriteSplitTimes(RealTime)`, `WriteSplitTimes(GameTime)`: the CurrentSplit and PreviousSplit time files.
3. `SplitListFiles.WriteSplitLists(RealTime)`, `WriteSplitLists(GameTime)`: for each level, the `SplitList\` window and the `CompletedSplits\` list. `WriteSplitLists` works out each list's range of splits (`SplitListRange` for the window), and `WriteSplitListLevel` writes the files for that range.

The steps don't depend on each other: each computes what it needs from the run.

## Subsplit levels

This is the central idea. Every output except the root info files and the run timer is written once per **level**.

### Depth

A split's depth is its number of leading `-` characters (`SplitLevels.Depths`). `SplitLevels.GenerateSubsplitLevels` sets the depths, the display names and the levels, each in its own method. The first, `CalculateDepths`, does the following:

1. Forces the last split to depth 0. The last split is always a top-level split. This comes before the count, so its dashes add no level.
2. Counts levels: start at 1, and add a level for each consecutive depth 1, 2, … that some split actually has.
3. Clamps any depth ≥ the level count to `levels - 1`, so a split deeper than any level goes into the deepest one.

The depths are counted even when the Subsplits setting is off, because the display names are built from them. After that, every depth is set to 0 and there is one level.

### Levels

Level `lvl` contains every split whose depth is ≤ `lvl`, in order. `Levels[lvl].Indices` lists their **global** indices, meaning indices into `State.Run`. Position within that list is the **local** index.

- Level 0 contains top-level splits only.
- The last level (`Levels.Count - 1`) contains every split. Local index equals global index there.

Folder names come from `MakeFile` in `OutputWriter`:

| Level | Folder tag |
|---|---|
| last | `AllSplits` |
| 0 (when not also the last) | `SansSubsplits` |
| 1 … last−1 | `SansSubsplits_Level{lvl+1}` |

A middle level is named after the shallowest depth it leaves out, so level 1, which drops the splits with 2+ dashes, is `SansSubsplits_Level2`. The README documents these names.

With a single level (no subsplits, or the setting off), level 0 *is* the last level, so it's tagged `AllSplits` and there is no `SansSubsplits` folder.

### Display names

`SplitLevels.Names[global][lvl]` holds a split's name as displayed at each level. Building it (`GenerateNames`) walks the levels from 0 upwards:

- If the name starts with `{Header}` (the convention from LiveSplit's own Subsplits component), the level shows `Header`, and the `{Header}` prefix is removed for the deeper levels.
- Otherwise the level shows the current name without its leading dashes and one space after them (`WithoutDashes`), and one leading `-` is removed for the deeper levels.

A split only appears at levels ≥ its depth. Usually its dashes are all gone by then, but some splits have more dashes than their depth, and showing the name without all of them is what keeps those clean:
- a split deeper than any level: with only depths 0 and 1, `---C` shows as `C`;
- the last split: `-Final` shows as `Final`, also in `SansSubsplits`;
- dashes after a `{Header}` prefix, which the walk removes a level late because the level that removes the header removes no dash: `{Area A}-A3` shows as `A3` at `AllSplits`.

The space lets a name start with a dash. A subsplit named `- -40` has depth 1 and shows as `-40`, and a top-level split named ` -40` shows as `-40`. Only the first space after the dashes comes off, so `---- Part 2 ----` shows as `Part 2 ----` and `-  x` as ` x`. It comes off even when there are no dashes before it, so `{Area A} A3` shows as `A3`. A `{Header}` after dashes isn't read: `---{Sub}C`, deeper than any level, shows as `{Sub}C`.

With the Subsplits setting off, the names are built for the levels the setting would make, and each split keeps its name at the deepest of them. So `AllSplits` shows the same names with the setting on or off.

A name that is empty, or becomes empty once its `{Header}`, dashes and space are removed (`""`, `-`, `- `, `{}`), shows as an empty line. The raw `Segment.Name` goes to the `*_Raw.txt` files.

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
- At level 0, "Area A" is a single segment covering A1 + A2 + A3. Its segment time in a comparison is the comparison's split time at split 2.
- While the runner is on A1 (global 0), level 0's *current* split is "Area A" (local 0) and it has no previous split.
- `SplitList\AllSplits\Names_Indent_0.txt` contains only `A3` and `B2`. `Names_Indent_1.txt` contains only `A1`, `A2` and `B1`. Every other line is blank, so each file can be a separately indented OBS text source.

With depths 0/1/2 (splits `A`, `-B`, `--C`, `D`) there are three levels: `SansSubsplits` = A, D; `SansSubsplits_Level2` = A, B, D; `AllSplits` = all four.

## Per-level data: `SplitLevel`

Each `SplitLevel` holds its members, `Indices` (local index → global index), and arrays by timing method (`Dictionary<TimingMethod, TimeSpan?[]>`), indexed by **local** index:

- `ComparisonSegmentTimes`, by comparison name, then by timing method: the comparison's split time at this member minus its split time at the previous member. The first member gets its split time. An entry is null if either value is missing. `CalculateComparisonSegments` fills it for each name in `SplitLevels.Comparisons` (see [Comparisons](#comparisons)).
- `BestSegmentTimes`: the member's best segment, which golds are measured against. `CalculateBestSegments` fills it (below).

`CalculateSegments()` fills both, for both timing methods.

`CalculateSegments()` rebuilds the levels and their arrays in place, in the one `SplitLevels` instance the writers share. It runs on every cache-key change, including every timer phase change. So a level must hold nothing about the current attempt; anything stored there would be wiped by a pause.

A dictionary's `.Count` is the number of timing methods (2), not the array length; use `[method].Length`.

### Best segments

A member that is one split (it directly follows the previous member, or the start) gets that split's `BestSegmentTime`, so `AllSplits`, where every member is one split, shows LiveSplit's own golds.

A member of several splits, a *section*, gets the fastest time it took within one attempt, or null if no attempt completed it. It isn't the sum of its splits' best segments: what happens in one split can affect a later one, so that sum is a lower bound, not a time anyone ran.

`CalculateBestSegments` takes those times from the segment history ([livesplit-notes.md](livesplit-notes.md#segment-history-partly-verified)):

1. List the section members of every level. With none (no subsplits), stop: nothing reads the history.
2. Collect every attempt key on any split, with the last split it has an entry for. Every split counts, not only the first, because edits can remove an attempt's entry on any split.
3. For each attempt, `AttemptSplitTimes` rebuilds its split times by adding up its entries, up to its last entry:
   - A null entry is a skipped split. It has no time, and its time is in the next entry.
   - A missing entry is a hole, left by an edit (clearing a best segment, Clean Sum of Best, moving a split). The next split with an entry starts the count again from itself (its *anchor*), because its entry includes time from before the hole.
4. A section counts that attempt when the attempt has a time at its last split and at the split before its first (or the section starts the run), and both count from the same anchor. Its time is the difference; the best is the minimum over attempts.

The history changes only on reset and when splits are edited, both of which cause a full rewrite. Each rewrite recomputes the bests anyway; it is cheap. With the 1,317-split, seven-level GTA SA 100% file (25 attempts in its history), it adds up to about 1 ms. A mid-run split's `Update()` there takes about 11 ms instead of 9 ms without section golds, mostly for the extra gold files (Debug build, every output on, measured 2026-10-21).

A section's best can be faster than its splits' best segments added up, when those disagree with the history. In the GTA SA file, an attempt that ran before a split was inserted, or that skipped one, has one entry covering both splits, faster than their two best segments together.

### Live times and deltas

This attempt's values are computed from the run whenever files are written, by these `OutputWriter` helpers. Each takes a timing method, a level and a local index, which must be a member's: callers handle −1 from the index helpers below first. Subtracting a null gives null.

| Helper | Value |
|---|---|
| `LiveRunTime` | The member's `SplitTime` |
| `LiveSegmentTime` | `LiveRunTime` minus the previous member's. The first member gets its `LiveRunTime`. |
| `ComparisonTime` | The comparison's split time for the member: `Comparisons[name]` |
| `RunDelta` | `LiveRunTime − ComparisonTime` |
| `SegmentDelta` | `LiveSegmentTime − ComparisonSegmentTimes` |
| `GoldDelta` | `LiveSegmentTime − BestSegmentTimes` |
| `IsGold` | See [How the values are calculated](#how-the-values-are-calculated) |

The comparison helpers take the name of one of `Levels.Comparisons`. Computing them on every write is safe because the comparisons, best segments and the segment history don't change during a run ([livesplit-notes.md](livesplit-notes.md#comparison-data-verified)). A completed split's `SplitTime` changes only when it is undone, and then it no longer counts as completed.

## Comparisons

The files that compare with a comparison are written once per entry of `SplitLevels.Comparisons`, each time into that comparison's folder inside the method folder (`{3}` in their templates). [output-files.md](output-files.md) lists which files these are. `CalculateSegments()` chooses the comparisons on every rewrite (`ChooseComparisons`):

1. `CurrentComparison`: the comparison selected in LiveSplit, `state.CurrentComparison`.
   - `Update()` reads it once, as a cache key, and passes it in, so every file of a rewrite uses the same one.
   - Null, as outside LiveSplit, counts as Personal Best.
   - A name the run doesn't have gives `-` everywhere, because LiveSplit returns an empty `Time` for it.
2. Then, in the run's order (`Run.Comparisons`), each comparison ticked in the settings. With **All custom comparisons** on, also every custom one (`IsCustomComparison`: in `CustomComparisons`, and not Personal Best, which LiveSplit keeps there too).

The folder is the name without its spaces, like the other output folders, and with `_` for each character a folder name can't have and for trailing dots, which Windows drops (`ComparisonFolder`). Folder names are compared ignoring case, as Windows does. A comparison whose folder is taken is skipped, so a custom comparison named `Current Comparison` gets no folder.

A comparison can be written twice, as `CurrentComparison` and in its own folder. Its segment times are computed once.

The writers loop over `Levels.Comparisons` for those files, passing `comparison.Folder` to `MakeFile` and `comparison.Name` to `ComparisonTime`, `RunDelta` and `SegmentDelta`. `RunInfoFiles` writes `Levels.CurrentComparison` to `CurrentComparison.txt`.

**Settings.**
- Ticked names are kept in `OutputToFileSettings`, changed with `SetComparisonTicked`, which counts the changes in `TickedComparisonsVersion`, a cache key. `ChooseComparisons` asks `IsComparisonTicked`; the tests read the whole set from `TickedComparisons`. They're saved as `<Comparisons><Comparison>…</Comparison></Comparisons>`, in a fixed order, and are in the settings hash.
- `GetSettingsControl` passes `State.Run` to `ShowRunComparisons`. The list then shows the run's comparisons, without the custom ones while All custom comparisons is on (they're all written then), plus any ticked names the run doesn't have, so they can be unticked.
- The checkbox's `CheckedChanged` fills the list again from the checkbox, because it runs before the binding updates `AllCustomComparisons`.

**Cost.** Each comparison written adds its files at every level and timing method. On the 1,317-split GTA SA file, a split's `Update()` takes about 12 ms with only `CurrentComparison`, and 18 ms with three more ticked (Debug build, every output on, measured 2026-10-22).

## Phase-dependent indexes

Most write functions start from these two helpers, which return a **local** index, or −1 for none:

| Timer phase | `GetCurrentSubsplitIndex(lvl)` | `GetPreviousSubsplitIndex(lvl)` |
|---|---|---|
| NotRunning | −1 | −1 |
| Running / Paused, before the level's first member is completed | 0 | −1 |
| Running / Paused, otherwise | local index of the first member whose global index ≥ `CurrentSplitIndex` | that − 1 |
| Ended | −1 | `Count − 1` |

- The previous index is the last completed member, so the members up to it are the completed ones in every phase. That is the `CompletedSplits\` range, and what `WriteSplitListLevel` treats as completed.
- The last split is a member of every level, so a running timer always has a current member. If none is found, the levels don't match the run, and `GetCurrentSubsplitIndex` throws.
- The callers check the phase, or −1, before using an index. Check every phase when changing one of them.

## How the values are calculated

- **Comparisons:** see [Comparisons](#comparisons). The run delta, segment delta, `Sign.txt`, the ahead and behind highlights and the comparison times are worked out once per comparison written, against that comparison's times.
- **Timing methods:** every timed file is written once for `RealTime` and once for `GameTime`. Game time is `null`, written as `-`, until the game time is initialized.
- **Run delta:** live split time minus the comparison's split time, for the same split.
- **Segment delta:** live segment minus the comparison's segment, at this level.
- **Gold delta:** live segment minus the member's best segment at this level (`BestSegmentTimes`, see [Best segments](#best-segments)). Best segments, the history and the comparisons don't change during a run; LiveSplit updates them on reset. Comparisons made during the run therefore use the values from before the run.
- **Gold** (`OutputWriter.IsGold`): the live segment exists and either the member has no best segment yet or the live segment beats it. A section that no attempt has completed has none, so its first completion is a gold. A skipped member, and the member after it, have no live segment, so they aren't golds. `Sign.txt` and the highlights both use this rule. It doesn't depend on the comparison.
- **`Sign.txt`** (`SplitFiles.PreviousSign`): a decision chain over the run delta, gold and segment delta. "Ahead" means run delta < 0. After the finish, the value has `Finished` in front. The values are listed in [output-files.md](output-files.md#sign-values).
- **Highlights** (`SplitListFiles.AppendCompletedLine` and `HighlightFile`): each completed line gets at most one highlight per comparison. A skipped split, which has no live split time, gets none. Otherwise the first that applies, in this order:
  1. Gold: the segment is a gold. It fills `Highlight_Gold.txt` in the method folder, and none of the comparisons' highlights.
  2. AheadGained: run delta < 0 and segment delta < 0.
  3. AheadLost: run delta < 0.
  4. BehindGained: segment delta < 0.
  5. BehindLost: the comparison has a time for the split.

  2–5 go in each comparison's folder, by that comparison's deltas. A split that matches none of these gets no highlight. `HIGHLIGHT_FILL` is 43 × `█`.
- **Completed / Remaining:** the previous index + 1, i.e. the number of members completed at this level, and `Count − Completed`. They're numbers in every phase: 0 / `Count` when the timer isn't running, `Count` / 0 after the finish.
- **Number / After:** the current index + 1, and `Count − current index − 1`. With no current member (current index −1) they're `-`, like `Name.txt`.
- **Run timer:** `State.CurrentTime[method]`. While the timer isn't running, `State.Run.Offset` (the splits' "Start timer at") for both methods instead, as LiveSplit's Timer component shows ([livesplit-notes.md](livesplit-notes.md#timer-state-by-phase-verified)). **Segment timer** (`TimerFiles.SegmentTime`): the run timer minus the `SplitTime` of the level's previous member. It equals the run timer on the first member and when the timer isn't running. After the finish it is the last member's `LiveSegmentTime`, frozen.
- **FinishedRunsCount:** the number of attempts in history with a real time, plus 1 while the timer is `Ended`, because the current attempt joins the history only on reset.

## Writing files

- Every path is a `const string FILE_*` template, declared in the writer for its folder, with these placeholders:
  - `{0}` the timing method (`RealTime` / `GameTime`);
  - `{1}` the level tag;
  - `{2}` the indent index, `Names_Indent_*` only. Those files are pre-formatted with `{2}` filled in and `{1}` re-inserted as a literal (`IndentFile` in `SplitListFiles`);
  - `{3}` the comparison's folder, for the files written once per comparison (see [Comparisons](#comparisons)). It always comes right after the method folder.
- The split list templates (`FILE_LIST_*`) are relative to the list folder. `WriteSplitListLevel` prefixes them with `FOLDER_SPLITLIST` (`SplitList\`) or `FOLDER_COMPLETEDSPLITS` (`CompletedSplits\`), so both lists write the same files, except the files only current and upcoming lines fill, which `CompletedSplits\` has none of.
- `WriteSplitListLevel` builds a list in a new `ListLines`: one `StringBuilder` field per file, which the helpers append to directly, and a list of each field with its template and comparison folder, for ending lines and writing.
  - The files that depend on a comparison are in a `ComparisonLines` per entry of `Levels.Comparisons` (`ListLines.Comparisons`). Its constructor adds each of them to the list's files, with that comparison's folder.
  - The upcoming fields (`GoldTimeUpcoming`, `CurrentSplitHighlight`, and each comparison's `RunTimeUpcoming` and `SegmentTimeUpcoming`) exist only when `WriteSplitLists` passes `upcoming`, for `SplitList\`, and are null otherwise; nothing appends to them then, because the range ends at the previous member.
  - `Indents` has one per depth up to the level.
  - `EndLine` ends a line in every file at once, so each file gets one line per split, blank where nothing was appended.
- There are four `MakeFile` overloads, in `OutputWriter`:
  - `(template, contents, method?, level, comparisonFolder)` fills `{0}`, `{1}` and `{3}`;
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
  - `WaitForWrites(timeout)` blocks until the drain task is done, including any search for old files. `Dispose()` uses it with 2 seconds; tests use `OutputToFileComponent.WaitForFileWrites()`.
- **Emptying old files.** Output files that the component no longer writes, because an output was turned off or a level is gone, are written empty once. They are never deleted.
  - The component's own files are those in the folders named by `OUTPUT_FOLDERS` in the component (`CurrentSplit`, `PreviousSplit`, `SplitList`, `CompletedSplits`, `Timers`), and the root files that match `OUTPUT_ROOT_FILES` (`TotalSplits*.txt`). The other root files are always written. Anything else in those folders counts as the component's, including older releases' paths.
  - A full rewrite in `Update()` runs between `FileQueue.BeginSweep()` and `EndSweep()`. In between, `Write` records every path it is given, including those it skips as unchanged. Which files get written depends only on the folder, the settings and the levels, never on the timer phase, so the record is the complete set. **Every write path must therefore write the same files in every phase.** A file written in only some phases would be emptied by a rewrite in another.
  - `EndSweep` compares the set with the previous rewrite's. Usually they're equal (a phase change) and nothing more happens. When they differ, and on the first rewrite after loading, it drops queued contents and `_written` entries for paths outside the set. Then, once nothing is pending, the drain searches the component's own files and empties every one that isn't in the set and isn't empty already. The set is compared ignoring case.
  - So the first rewrite after loading also empties leftovers from earlier sessions. Other files in the output folder are never touched, and neither is a folder the setting has moved away from. A file that can't be emptied is reported like a failed write, and tried again at the next search.
- Multi-line files use `\n` and end with a trailing `\n`. Every list file has the same number of lines, so separate OBS text sources line up.
- Which outputs each setting controls:

  | Setting | Effect |
  |---|---|
  | `OutputTimer` | `Timers\` |
  | `OutputSplitList` | `SplitList\` |
  | `OutputCompletedSplits` | `CompletedSplits\` |
  | `OutputSubsplits` | Whether there is more than one level |

  Everything else is always written. Turning an output off empties its files on the next frame (see "Emptying old files" above).
- The file *contents* are still built synchronously, inside `Update()` on the UI thread; only the writing is in the background. After a split, that is under a millisecond on the UI thread, and the writes take about 30 ms on the drain task (40 splits, every output on).

## Settings

`OutputToFileSettings` exposes these plain properties:

| Property | Type | Default | Set from the UI by |
|---|---|---|---|
| `FolderPath` | string | empty | binding (`OnValidation`), and Browse |
| `SplitsBefore` | int | 10 | binding |
| `SplitsAfter` | int | 10 | binding |
| `OutputTimer` | bool | off | binding |
| `OutputSplitList` | bool | on | binding |
| `OutputSubsplits` | bool | on | binding |
| `OutputCompletedSplits` | bool | off | binding |
| `AllCustomComparisons` | bool | off | binding |
| `TimeAccuracy` | `FormatAccuracy` | Seconds | `comboBoxTimeAccuracy_SelectedIndexChanged` |
| `DeltaAccuracy` | `FormatAccuracy` | Flex | `comboBoxDeltaAccuracy_SelectedIndexChanged` |

The ticked comparisons are a set, not a property; see [Comparisons](#comparisons).

The bound properties are bound to their controls with `DataBindings.Add` in the constructor. All but `FolderPath` use `DataSourceUpdateMode.OnPropertyChanged`, so a change applies at once.

- The constructor sets the defaults. The `SetSettings` fallbacks, used when a key is missing from the layout, are the same (`SettingsTests.NewSettingsHaveTheDefaults`). So are the bound controls' initial values in the Designer file. Until a control's handle is created, it shows its Designer value rather than the bound property.
- The accuracies aren't bound: a `ComboBox` has no event a binding could use for every change of its selection. Each drop-down list's index is the `FormatAccuracy` value, so the lists follow the enum's order. The Designer selects nothing; `ShowAccuracies` selects both, from the constructor and from `SetSettings`, and the `SelectedIndexChanged` handlers set the properties back. A value in the layout that isn't one of the choices loads the default (`ParseAccuracy`).

- `FolderPath` uses `OnValidation`: a typed path applies only when focus leaves the textbox, through Tab, a click on another control, OK, or Enter (which clicks the dialog's AcceptButton; see [livesplit-notes.md](livesplit-notes.md#layout-settings-dialog-read-in-source-size-checked-headless)). Applying every keystroke would write a full set of files to each partial path, because `FolderPath` is a cache key.
  - The Browse button has `CausesValidation = false`, so moving focus to it doesn't apply a partly typed path. Its handler sets `FolderPath` and the textbox itself. A path typed and then left unbrowsed applies at the next focus change to another control.
  - Closing a modal dialog with X doesn't validate the focused control (seen headless, 2026-10-15), so a path typed but never left is dropped.
- The control's event handlers:
  - `buttonFolderPath_Click` sets `FolderPath`;
  - `checkBoxOutputSplitList_CheckedChanged` enables or disables the split-list group;
  - `checkBoxAllCustomComparisons_CheckedChanged` and `checkedListBoxComparisons_ItemCheck` fill the comparison list and tick comparisons (see [Comparisons](#comparisons));
  - `comboBoxTimeAccuracy_SelectedIndexChanged` and `comboBoxDeltaAccuracy_SelectedIndexChanged` set the accuracies.
- The class doesn't implement `INotifyPropertyChanged`, so setting a property in code won't update its control while it is shown. `buttonFolderPath_Click` updates the textbox itself for this reason. The next time the control is shown in a new dialog, the bindings read the properties again (seen headless for the textbox, 2026-10-15), so after Cancel restores the settings the dialog shows them on reopening. The drop-down lists aren't bound, so they show a new accuracy only through `SetSettings`, which Cancel calls.
- XML keys: `FolderPath`, `SplitsBefore`, `SplitsAfter`, `OutputTimer`, `OutputSplitList`, `OutputSubsplits`, `OutputCompletedSplits`, `AllCustomComparisons`, `TimeAccuracy`, `DeltaAccuracy`, and `Comparisons`, which holds a `Comparison` per ticked name. There is no version key; a layout from an older release is recognized by its missing keys, and gets their fallbacks. Layouts saved by earlier builds also have `Version` `0.1`, which nothing reads and LiveSplit's next save drops.
- `WriteError` isn't a setting: it holds the latest failed file write, shown in red in `labelWriteError` under the Time Format group, and is neither saved nor hashed. `Update()` sets and clears it through `ShowWriteError` (see "Writing files").
- The control and `tableLayoutPanelTop` are `AutoSize` with `GrowAndShrink`, so the control is as tall as its groups, plus the error while one is shown. LiveSplit's Layout Settings tab scrolls once the control is taller than 561 px ([livesplit-notes.md](livesplit-notes.md#layout-settings-dialog-read-in-source-size-checked-headless)). `ShowWriteError` hides the label before emptying it: while the control isn't on screen, only the text change makes the table shrink back.
- `GetSettingsHashCode()` reuses `CreateSettingsNode(null, null)`, which XORs the hashes of the saved values, so two changes can cancel out. That's harmless here: LiveSplit only uses the hash to decide whether to redraw, and this component draws nothing.
