# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. Where [testing.md](testing.md#regression-checks) has a regression check for an item, its row is marked with the item's code; update that row when the item is fixed.

## Dependencies between plans

- DUP edits `CalculateDepths`, and DSH and SOF edit `GenerateNames`. [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice) changes `GenerateSubsplitLevels`, which calls both.

## DUP — An extra duplicate level when only the last split has dashes

In `CalculateDepths` ([SplitLevels.cs](../UI/Components/SplitLevels.cs)), levels are counted before the last split is forced to depth 0. Splits `A`, `B`, `-C` therefore produce a `SansSubsplits` folder identical to `AllSplits`.

**Evidence.** Read.

**Plan.** Force the last split to depth 0 before counting levels.

**Verify.** With splits `A`, `B`, `-C`, only `AllSplits` is written.

**Docs.** architecture.md "Depth" and "Levels".

## DSH — Moved splits keep leftover dashes in their names

Building the display names in `GenerateNames` ([SplitLevels.cs](../UI/Components/SplitLevels.cs)) removes one leading `-` per subsplit level. A split's depth normally equals its number of dashes, so they are all gone by the levels it appears at. Two kinds of split end up at a shallower level than their dashes, and keep the dashes that weren't removed:

- a split deeper than the deepest level, which is moved into that level;
- the last split, which is always top level.

Whether that is wanted is undecided. The code comment on the move mentions long all-dash names like `-----------------`, which keep a line of dashes this way.

**Evidence.** Reproduced 2026-10-15:
- `A`, `-B`, `---C`, `D`: `AllSplits` shows `--C`.
- `--Intro`, `A`, `B`: `AllSplits` shows `--Intro`.
- `A`, `-B`, `-`: `SansSubsplits` shows `-` for the last split.

**Plan.** Decide whether displayed names should lose every leading dash. If so, remove them all in the `Names` loop at the levels a split appears at. A name made only of dashes then shows as an empty line, as `-` already does where its dash is removed. `*_Raw.txt` keeps the name as typed.

**Verify.** The examples above show `C`, `Intro` and an empty line.

**Docs.** architecture.md "Display names". CHANGELOG.md, if Subsplits has been released by then.

## SOF — Subsplits off shows section headers and dashes

With the Subsplits setting off there is one level, level 0, tagged `AllSplits`. `GenerateNames` gives level 0 the names it gives `SansSubsplits` when the setting is on: a `{Header}` split shows its header, and leading dashes stay. So the names in `AllSplits` change when the setting is turned off. Release 6, which had no subsplits, showed raw names.

Whether that is wanted is undecided.

**Evidence.** Reproduced 2026-10-16, the same in builds from that day and the day before. With the base run and Subsplits off, `SplitList\AllSplits\Names.txt` shows `-A1 | -A2 | Area A | -B1 | Area B`. With Subsplits on it shows `A1 | A2 | A3 | B1 | B2`.

**Plan.** Decide what `AllSplits` should show with the setting off: the same names as with it on, or the raw names. For the same names, build the names from the dashes and headers even when the setting is off, and use the deepest form at the one level.

**Verify.** With Subsplits off, the base run's `Names.txt` shows the chosen names.

**Docs.** architecture.md "Display names". output-files.md. CHANGELOG.md's Subsplits entry, while it is unreleased.

## THR — Timer events may arrive off the UI thread

Some callers may call `TimerModel` from a thread other than the UI thread; some auto splitters or the LiveSplit Server component might. If so, `WriteSplits()` could run at the same time as `Update()`, and the shared arrays have no locking. The file writes themselves are safe: `FileQueue` locks all of its state.

**Evidence.** Not checked.

**Plan.** Find out which thread raises `OnSplit` and the other timer events when the Server component or an auto splitter drives the timer. If any isn't the UI thread, lock around the `SplitLevels` work or marshal the handlers to the UI thread. Either way, only `Update()` may touch the settings control.

**Verify.** Drive splits through the Server component while logging the thread ID in the event handlers and in `Update()`.

**Docs.** livesplit-notes.md "Not verified" (move the answer into a verified section). architecture.md "Lifecycle", if handlers need to lock or marshal.
