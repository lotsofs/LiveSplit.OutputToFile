# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. Where [testing.md](testing.md#regression-checks) has a regression check for an item, its row is marked with the item's code; update that row when the item is fixed.

## Dependencies between plans

- DUP edits `CalculateDepths` and DSH edits `GenerateNames`. [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice) changes `GenerateSubsplitLevels`, which calls both.

## CHK — Output checkboxes don't take effect at once

`OutputTimer`, `OutputSplitList`, `OutputSubsplits` and `OutputCompletedSplits` aren't cache keys, so changing one does nothing until something else triggers a rewrite. Changing `OutputSubsplits` only changes the level structure at the next `CalculateSegments()`. Turning an output off leaves its old files on disk with stale values. So does a subsplit level that goes away because the splits were edited or another splits file was opened: its folders keep their last contents.

**Evidence.** The checkboxes: read. The level that goes away: reproduced 2026-10-15. After every dash was removed from the base run's names, `SplitList\SansSubsplits\Names.txt` still showed `Area A | Area B`.

**Plan.** Add these settings to the cache keys. For an output turned off, or a level that is gone, decide between blanking its files once and deleting them; deleting must touch only files this component writes.

**Verify.** With the timer stopped, toggle each checkbox: its files appear, or are cleared, straight away. Remove every dash from the splits: the `SansSubsplits` files are cleared.

**Docs.** architecture.md "When files get written" (the cache keys). output-files.md, on what turning an output off does. README.

## LAG — Segment timer ticks over late

Timer files are only rewritten when the *run* timer's whole second or the timer phase changes (`HasTimerChanged` in [TimerFiles.cs](../UI/Components/TimerFiles.cs)), not on a split. The segment timer's second boundaries are offset by the fraction at which the previous split happened, so it can lag by up to about a second. After the run ends it shows `0:00`; it should probably show the last segment's time or `-`. Release 6 wrote the last segment's time there.

**Evidence.** The lag was read. The `0:00` after the finish was reproduced 2026-10-14, and again on 2026-10-15 for a finish within the same second as the previous frame. Release 6's finish value was read in its code.

**Plan.** Add each level's segment timer whole seconds to what `HasTimerChanged` compares, so it ticks on its own boundary. At the finish, write the last segment's time or `-`.

**Verify.** Split at x.7 s: the segment timer ticks over 0.3 s after the run timer does. After the finish it shows the chosen value.

**Docs.** architecture.md "When files get written". output-files.md "Timers".

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

## THR — Timer events may arrive off the UI thread

Some callers may call `TimerModel` from a thread other than the UI thread; some auto splitters or the LiveSplit Server component might. If so, `WriteSplits()` could run at the same time as `Update()`, and the shared arrays have no locking. The file writes themselves are safe: `FileQueue` locks all of its state.

**Evidence.** Not checked.

**Plan.** Find out which thread raises `OnSplit` and the other timer events when the Server component or an auto splitter drives the timer. If any isn't the UI thread, lock around the `SplitLevels` work or marshal the handlers to the UI thread. Either way, only `Update()` may touch the settings control.

**Verify.** Drive splits through the Server component while logging the thread ID in the event handlers and in `Update()`.

**Docs.** livesplit-notes.md "Not verified" (move the answer into a verified section). architecture.md "Lifecycle", if handlers need to lock or marshal.
