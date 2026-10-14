# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

Line numbers are as of commit `30d30fa`. "Reproduced" means seen on 2026-10-14 by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. Where [testing.md](testing.md#regression-checks) has a regression check for an item, its row is marked with the item's code; update that row when the item is fixed.

## Dependencies between plans

- FIN, NUL, UND and SKP touch the stored deltas, or the `Sign.txt` and highlight chains that use them. PAU's preferred plan (compute deltas when writing instead of storing them at split time) rewrites that code. FIN's one-line fix is still worth doing first. Recheck the others after PAU.
- NUL's final-split case only goes away once FIN is fixed.
- NUL and GAI edit the same `Sign.txt` chains. Do them together.
- SKP edits a highlight chain that exists twice, in `WriteSplitList` and `WriteCompletedSplitList`. Doing [DRY](Cleanup.md#dry--split-list-writers-are-near-duplicates) first halves that edit.
- CLM, EMP and DUP all edit `GenerateSubsplitLevels`, as does [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice).
- PAU's minimal plan, EDT, KEY and CHK all change the cache keys in `Update()`.
- SEC and LAG both change `HasTimerChanged`.
- WER and [WRT](Performance.md#wrt--every-split-rewrites-about-160-files) both change `MakeFile`.

## SAV — Completed splits setting is never saved

`OutputCompletedSplits` is missing from `CreateSettingsNode` ([OutputToFileSettings.cs:45-55](../UI/Components/OutputToFileSettings.cs#L45-L55)) and `SetSettings` ([:69-80](../UI/Components/OutputToFileSettings.cs#L69-L80)). It turns off again on every layout load. Toggling it also doesn't change the settings hash, so LiveSplit doesn't offer to save the layout.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Add the key to `CreateSettingsNode`, and to `SetSettings` with a fallback of `false` for layouts saved before it existed. Making the toggle trigger a rewrite is CHK.

**Verify.** The SAV regression check in testing.md gives `True`. In LiveSplit, toggling it makes LiveSplit ask to save the layout on close.

**Docs.** architecture.md "Settings" (the XML key list). testing.md SAV row.

## WER — File write errors are thrown into LiveSplit

`MakeFile` ([OutputToFileComponent.cs:976-984](../UI/Components/OutputToFileComponent.cs#L976-L984)) has no try/catch, so any I/O error escapes into whichever LiveSplit call triggered the write. LiveSplit then skips any event handlers registered after this component's (see [livesplit-notes.md](livesplit-notes.md#exceptions-verified)).

**Evidence.** Reproduced 2026-10-14: with one output file locked by another process, `TimerModel.Split()` threw `IOException` to its caller. The split itself still registered. Other triggers, read only: characters that are invalid in a path in the folder setting (on .NET Framework `Path.Combine` throws, and would do so on every `Update()`), a missing drive, or no write permission.

**Plan.** Catch the error per file in `MakeFile`, so one bad file doesn't stop the rest. Report it once (for example in the settings panel), not on every write.

**Verify.** The WER regression check in testing.md doesn't throw, and the other files still update. Set the folder to `a|b`: no exception from `Update()`.

**Docs.** architecture.md "Writing files". testing.md WER row. README, if the error gets shown in the settings panel.

## FIN — Final split's segment time and deltas are lost

At the finish, `CalculateLiveSegment` stores the final segment at index `level.LiveSegmentTimes.Count - 1` ([:353](../UI/Components/OutputToFileComponent.cs#L353), [:356](../UI/Components/OutputToFileComponent.cs#L356)). `LiveSegmentTimes` is the `Dictionary<TimingMethod, …>`, so `.Count` is always 2. The final segment overwrites slot 1, and the last slot is never set.

**Evidence.** Reproduced 2026-10-14: with 3+ splits at a level, after finishing, `PreviousSplit\AllSplits\*\SegmentTime.txt`, `SegmentDifference.txt` and `GoldDifference.txt` showed `-`, and `Sign.txt` showed `PBGold`/`NoPBGold` (see NUL).

**Plan.** Use `level.LiveSegmentTimes[method].Length - 1`. If PAU computes segments on demand, this code goes away with it.

**Verify.** The FIN regression check in testing.md (after the finish) shows the last segment's time and differences.

**Docs.** output-files.md "PreviousSplit" (⚠ marks). architecture.md "Per-level data" (the `.Count` note). testing.md FIN row.

## NUL — Sign reports a gold whenever the gold delta is null

[:855](../UI/Components/OutputToFileComponent.cs#L855) and [:861](../UI/Components/OutputToFileComponent.cs#L861) treat `goldDelta == null` as a gold. It is null in cases that are not golds:

- At every subsplit level, because `CalculatePreviousGoldDelta` returns null there ([:385](../UI/Components/OutputToFileComponent.cs#L385)). So `PreviousSplit\SansSubsplits*\*\Sign.txt` always says `GoldAhead`/`GoldBehind`, or `PBGold`/`NoPBGold` at the finish.
- On the split after a skipped split.
- On the final split, because of FIN. Every finished run ends with `PBGold`/`NoPBGold`.

Only "no best segment exists yet" should count as a gold. The highlight code ([:587](../UI/Components/OutputToFileComponent.cs#L587), [:721](../UI/Components/OutputToFileComponent.cs#L721)) treats null as "not a gold", so `Sign.txt` and `Highlight_Gold.txt` also disagree.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Decide "is a gold" separately from `goldDelta`: true only at the `AllSplits` level, when the live segment time exists and either the best segment is null or the delta is negative. Best segments don't change during a run ([livesplit-notes.md](livesplit-notes.md#comparison-data-verified)), so this is stable until reset. Use the same rule in the highlight chains. Do this together with GAI. The final-split case needs FIN.

**Verify.** The NUL regression checks in testing.md give the non-gold values. A finish whose last segment isn't a gold isn't `PBGold`/`NoPBGold`. `Sign.txt` and `Highlight_Gold.txt` agree.

**Docs.** output-files.md "Sign values" (⚠ marks). architecture.md "How the values are calculated". testing.md NUL rows.

## GAI — Sign never says GainedAhead or GainedBehind

The mid-run chain at [:860-863](../UI/Components/OutputToFileComponent.cs#L860-L863) has no `segmentDelta < 0` branch; the end-of-run chain does ([:857](../UI/Components/OutputToFileComponent.cs#L857)). A split that gains time without being a gold is reported as `LostAhead`/`LostBehind`. Example: segment 9.5 s vs PB 10 s, run −0.5 s gives `LostAhead`.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Add a `segmentDelta < TimeSpan.Zero` branch giving `GainedAhead`/`GainedBehind` before the final `else`, mirroring the end-of-run chain. Do this together with NUL.

**Verify.** The GAI regression check in testing.md gives `GainedAhead`.

**Docs.** output-files.md "Sign values" (⚠ marks). architecture.md "How the values are calculated". testing.md GAI row. [SGN](Cleanup.md#sgn--some-signs-values-are-never-written) loses `GainedAhead`/`GainedBehind`.

## PAU — Pausing or finishing wipes stored deltas

`TimerPhase` is one of the cache keys in `Update()` ([:525](../UI/Components/OutputToFileComponent.cs#L525)). Any phase change (Running→Paused, Paused→Running, Running→Ended) therefore runs `CalculateSegments()` ([:527](../UI/Components/OutputToFileComponent.cs#L527)). That recreates every `SplitLevel` array ([:256-259](../UI/Components/OutputToFileComponent.cs#L256-L259), [:300-304](../UI/Components/OutputToFileComponent.cs#L300-L304)), and with them the deltas `WriteSplitTimes` stored at each split. Changing Splits before/after or the folder mid-run does the same.

**Evidence.** Reproduced 2026-10-14: after one pause, every completed split except the latest showed `-` in `Delta_*.txt` and moved to `Highlight_BehindLost.txt`. After finishing, every split except the last was BehindLost.

**Plan.** Preferred: compute deltas from the segments' `SplitTime`s and PB times whenever files are written, instead of storing them at split time. That drops the stored `RunDeltae`/`SegmentDeltae`/`GoldDeltae`/`LiveSegmentTimes` arrays, removes this whole class of stale-delta bugs, and makes the call order in `WriteSplits()` irrelevant. Minimal: don't rebuild the level structure on a phase change. Let `TimerPhase` and the list settings trigger a rewrite only, and run `CalculateSegments()` only when the run or `OutputSubsplits` changes.

**Verify.** The PAU regression check in testing.md keeps every delta after the pause. Also finish the run, and change Splits before mid-run: `Delta_*.txt` and `Highlight_*.txt` keep every split's values.

**Docs.** CLAUDE.md "Must-know before editing" (the stored-deltas bullet). architecture.md "Lifecycle" and "Per-level data". output-files.md "SplitList" (⚠ under the highlight table). testing.md PAU row.

## RET — AllSplits current and previous files stop updating at finish and reset

`WriteSplitInformation` uses `return` instead of `continue` in its NotRunning and Ended branches ([:911](../UI/Components/OutputToFileComponent.cs#L911), [:919](../UI/Components/OutputToFileComponent.cs#L919)). Only level 0 (`SansSubsplits`) gets written, so with subsplits the other levels stay stale. The same branches never write `CurrentSplit\*\Name_Raw.txt`, so it keeps the last split's raw name after a finish or reset, even at level 0.

**Evidence.** Reproduced 2026-10-14: after a finish and a reset, `CurrentSplit\AllSplits\Index.txt` was still `4`, `Name.txt` was still the last split, and `PreviousSplit\AllSplits\Name.txt` was still the second-to-last split.

**Plan.** `continue` instead of `return`, and write `-` to `Name_Raw.txt` in both branches.

**Verify.** The RET regression checks in testing.md give the correct values. `Name_Raw.txt` shows `-` after a finish and after a reset.

**Docs.** output-files.md "CurrentSplit" and "PreviousSplit" (⚠ marks). testing.md RET rows.

## IDX — SansSubsplits segment timer measures from the wrong split

[:481](../UI/Components/OutputToFileComponent.cs#L481) uses a level-local index as a global one (`State.Run[previousSplitIndex]`). The `indices` list fetched at [:469](../UI/Components/OutputToFileComponent.cs#L469) is never used.

**Evidence.** Reproduced 2026-10-14: top-level split at 28 s, first subsplit at 9.5 s, timer at 33 s. `SegmentTimer_SansSubsplits.txt` showed `0:23` instead of `0:05`.

**Plan.** `State.Run[indices[previousSplitIndex]]`.

**Verify.** The IDX regression check in testing.md shows `0:05`.

**Docs.** output-files.md "Timers" (⚠ mark). testing.md IDX row.

## CLM — A first split with two or more dashes is dropped

The depth clamp loop at [:232](../UI/Components/OutputToFileComponent.cs#L232) starts at `i = 1`, so split 0 is never clamped. When no split has exactly one dash, a first split with 2+ dashes keeps a depth that no level includes, and the run crashes at the finish.

**Evidence.** Reproduced 2026-10-14: with splits `--Intro`, `A`, `B`, the `AllSplits` output contained only A and B. The final split then threw `IndexOutOfRangeException` in `CalculatePreviousGoldDelta` ([:390](../UI/Components/OutputToFileComponent.cs#L390)).

**Plan.** Start the loop at 0.

**Verify.** The CLM regression check in testing.md lists all three names and finishes.

**Docs.** architecture.md "Depth" (the note that the loop skips split 0). testing.md CLM row.

## EMP — An empty or dash-only split name crashes the component

`name[0]` at [:274](../UI/Components/OutputToFileComponent.cs#L274) and in `GetSectionHeader` ([:285](../UI/Components/OutputToFileComponent.cs#L285)) assumes a non-empty string. A split named `""` crashes the constructor. A split named `-` crashes once its dash is stripped, if there are 2+ levels.

**Evidence.** Reproduced 2026-10-14: `IndexOutOfRangeException` in `String.get_Chars`.

**Plan.** Check the length before reading `name[0]` in both places. An all-dash name can show as an empty line at the levels where its dashes are stripped.

**Verify.** The EMP regression check in testing.md doesn't throw, and a run with those names finishes and writes its files.

**Docs.** architecture.md "Display names", if it mentions the empty-name case. testing.md EMP row.

## EDT — Editing splits isn't picked up until the next run starts

The cache keys in `Update()` ([:518-525](../UI/Components/OutputToFileComponent.cs#L518-L525)) don't cover split count, names or PB times.

**Evidence.** Reproduced 2026-10-14: after adding a split and renaming one, `TotalSplits_AllSplits.txt` and `Names.txt` still showed the old splits.

**Plan.** Add `Run.Count` plus a hash of the names and times to the cache keys, or handle `state.RunManuallyModified`. Keep it cheap: `Update()` runs every frame.

**Verify.** With the timer stopped, add a split and rename one: `TotalSplits_*.txt` and `Names.txt` update without starting a run.

**Docs.** architecture.md "When files get written" (the cache keys).

## KEY — Typing a folder path creates a folder per keystroke

The textbox updates `FolderPath` on every change ([OutputToFileSettings.cs:31](../UI/Components/OutputToFileSettings.cs#L31)), and `FolderPath` is a cache key, so each keystroke writes a full set of files to a new path.

**Evidence.** Reproduced 2026-10-14: typing `…\Stream` created `S`, `St`, `Str` and `Stream`, each holding a full set of output files.

**Plan.** Apply the path on Browse or when the textbox loses focus, or only write when the chosen folder already exists (creating subfolders inside it is still fine).

**Verify.** Type a new path one character at a time: no partial folders appear. Browse still applies at once.

**Docs.** architecture.md "Settings". README, if writing starts to require an existing folder.

## NEG — Negative deltas get a different precision from positive ones

[TimeFormatter.cs:55](../UI/Components/TimeFormatter.cs#L55) passes the negative value to `DetermineTimeFormat`, so every negative delta counts as "under 100 ms" and gets centiseconds.

**Evidence.** Reproduced 2026-10-14: `+5.2` vs `-5.23`, and `+1:05` vs `-1:05.20`.

**Plan.** Pass `t.Value.Negate()`.

**Verify.** The NEG regression check in testing.md gives `-5.2`, and −65.2 s gives `-1:05`.

**Docs.** output-files.md "Formatting" (⚠ mark). testing.md NEG row, and the note under the regression table about negative precision.

## UND — Undo to the first split leaves GoldDifference stale

The "no previous split" branch ([:865-871](../UI/Components/OutputToFileComponent.cs#L865-L871)) resets every other `PreviousSplit` file, but not `PreviousSplit\AllSplits\*\GoldDifference.txt`.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Write `-` to it in that branch, keeping the `AllSplits`-only guard.

**Verify.** Split once, then undo: `GoldDifference.txt` shows `-`.

**Docs.** output-files.md "PreviousSplit" (⚠ mark on `GoldDifference.txt`).

## SKP — Skipped splits are highlighted as BehindLost

The highlight chains ([:587-596](../UI/Components/OutputToFileComponent.cs#L587-L596), [:721-730](../UI/Components/OutputToFileComponent.cs#L721-L730)) fall through to BehindLost whenever a PB time exists, even for a split with no time.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Give a split with no `SplitTime` no highlight. Easier after DRY.

**Verify.** Skip a split: its line is blank in every `Highlight_*.txt`.

**Docs.** output-files.md "SplitList" (⚠ mark on `Highlight_BehindLost.txt`).

## SEC — Run timer file can get stuck after a reset

`HasTimerChanged` ([:430-436](../UI/Components/OutputToFileComponent.cs#L430-L436)) compares `time.Seconds`, the 0–59 seconds component, rather than total seconds. After resetting at 1:00.5, the seconds component is 0 both before and after, so `Timers\*\RunTimer.txt` stays at `1:00` until the next start.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Cache `(long)time.TotalSeconds` together with the timer phase.

**Verify.** The SEC regression check in testing.md shows `0:00`.

**Docs.** output-files.md "Timers" (⚠ mark). testing.md SEC row, and its tip about advancing the clock in steps of a second or less.

## CHK — Output checkboxes don't take effect at once

`OutputTimer`, `OutputSplitList`, `OutputSubsplits` and `OutputCompletedSplits` aren't cache keys, so changing one does nothing until something else triggers a rewrite. Changing `OutputSubsplits` only changes the level structure at the next `CalculateSegments()`. Turning an output off leaves its old files on disk with stale values.

**Evidence.** Read.

**Plan.** Add these settings to the cache keys. For an output turned off, decide between blanking its files once and deleting them; deleting must touch only files this component writes.

**Verify.** With the timer stopped, toggle each checkbox: its files appear, or are cleared, straight away.

**Docs.** architecture.md "When files get written" (the cache keys). output-files.md, on what turning an output off does. README.

## LAG — Segment timer ticks over late

Timer files are only rewritten when the *run* timer's seconds change ([:439](../UI/Components/OutputToFileComponent.cs#L439)). The segment timer's second boundaries are offset by the fraction at which the previous split happened, so it can lag by up to about a second. After the run ends it shows `0:00`; it should probably show the last segment's time or `-`.

**Evidence.** The lag was read. The `0:00` after the finish was reproduced 2026-10-14.

**Plan.** Give each level's segment timer its own whole-seconds cache key, so it ticks on its own boundary. At the finish, write the last segment's time or `-`.

**Verify.** Split at x.7 s: the segment timer ticks over 0.3 s after the run timer does. After the finish it shows the chosen value.

**Docs.** architecture.md "When files get written". output-files.md "Timers".

## DUP — An extra duplicate level when only the last split has dashes

Levels are counted ([:224-229](../UI/Components/OutputToFileComponent.cs#L224-L229)) before the last split is forced to depth 0 ([:237](../UI/Components/OutputToFileComponent.cs#L237)). Splits `A`, `B`, `-C` therefore produce a `SansSubsplits` folder identical to `AllSplits`.

**Evidence.** Read.

**Plan.** Force the last split to depth 0 before counting levels.

**Verify.** With splits `A`, `B`, `-C`, only `AllSplits` is written.

**Docs.** architecture.md "Depth" and "Levels".

## THR — Timer events may arrive off the UI thread

Some callers may call `TimerModel` from a thread other than the UI thread; some auto splitters or the LiveSplit Server component might. If so, `WriteSplits()` could run at the same time as `Update()`, and the shared arrays have no locking.

**Evidence.** Not checked.

**Plan.** Find out which thread raises `OnSplit` and the other timer events when the Server component or an auto splitter drives the timer. If any isn't the UI thread, lock around the writes or marshal them to the UI thread.

**Verify.** Drive splits through the Server component while logging the thread ID in the event handlers and in `Update()`.

**Docs.** livesplit-notes.md "Not verified" (move the answer into a verified section). architecture.md "Lifecycle", if handlers need to lock or marshal.
