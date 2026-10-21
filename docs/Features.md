# Features and limitations

Things the component doesn't do yet. The index is [TODO.md](TODO.md).

TST makes the Verify steps in the other detail docs repeatable, so it pays to do it early. Its test project can take LiveSplit's folder from the `LiveSplitDir` property, like the component (see CLAUDE.md "Build / deploy"). It has to reference LiveSplit's DLLs itself, as the harness does, because the component's build doesn't copy them.

## TST — No automated tests

There is no test project. Changes are checked by hand, in LiveSplit or with a headless harness that lives outside the repo.

**Evidence.** [testing.md](testing.md) has the harness as a copy-paste program and a table of regression checks; both worked on 2026-10-14.

**Plan.** Turn the harness into a net481 test project in the repo, and the regression checks into tests that assert today's correct values, marking the ones for open [Defects.md](Defects.md) items as expected failures.

**Verify.** The tests pass, and a reintroduced bug makes one fail.

**Docs.** CLAUDE.md "Build / deploy", which says there are no tests. [testing.md](testing.md).

## CMP — Deltas ignore the selected comparison

Deltas always compare against Personal Best. The comparison selected in LiveSplit (`state.CurrentComparison`) is ignored.

**Evidence.** Read: every delta uses `PersonalBestSplitTime`.

**Plan.** Read times from `Comparisons[state.CurrentComparison]` instead, and add the comparison to the cache keys in `Update()`. Decide what the `PB`/`NoPB` values of `Sign.txt` mean against other comparisons.

**Verify.** Switch the comparison mid-run: deltas and highlights change to match.

**Docs.** README. output-files.md, wherever it says "PB". architecture.md "How the values are calculated" and "When files get written" (the cache keys).

## FMT — Time format is fixed

`TimeFormatter.DetermineTimeFormat` ([TimeFormatter.cs](../UI/Components/TimeFormatter.cs)) picks the precision from hard-coded thresholds, and durations always use `MSS`. There's no setting. A `// TODO` comment above it says the same; this item replaces it.

**Evidence.** Read.

**Plan.** Delete the `// TODO` comment. Add settings for duration and delta formats, following "Adding a setting" in CLAUDE.md. Consider LiveSplit's own formatters in `LiveSplit.TimeFormatters`, which the component already imports.

**Verify.** Change the format setting: every time file uses it after the next write.

**Docs.** output-files.md "Formatting". README settings section.

## GLD — Golds only exist at the AllSplits level

`GoldDelta` and `IsGold` ([OutputWriter.cs](../UI/Components/OutputWriter.cs)) give no gold at any other level, and gold files are only written for `AllSplits`. LiveSplit only keeps a best segment (`BestSegmentTime`) for each individual split, so a section that spans several splits has no gold of its own.

A section's gold is the fastest time the whole section took **within a single attempt**. It is not the sum of its splits' golds. What happens in one split can affect a later one, so the sum of golds is a lower bound, not a time anyone actually ran.

**Evidence.** Read; data source checked 2026-10-14.

- The data is already in memory, so nothing needs to read the `.lss`. `ISegment.SegmentHistory` is a `Dictionary<int, Time>` keyed by attempt index. The type was checked by reflection on the installed `LiveSplit.Core.dll`.
- How LiveSplit fills it is recalled from its source (`TimerModel.UpdateSegmentHistory`) and not yet verified against the installed DLL. On reset, every split reached in the attempt gets an entry: its `SplitTime` minus the last *recorded* split time in that attempt, or minus zero at the start. A skipped split gets a null entry, and its time is folded into the next recorded entry. Splits after the reset point get no key. Imported or hand-edited golds may add entries on single splits, under keys below the lowest attempt index.
- So adding up one attempt's non-null entries for splits 0..i gives that attempt's split time at i, whenever entry i is non-null.
- History size across the 262 `.lss` files in `D:\Repositories\Stream-Resources\Splits` (measured):
  - The largest history is 1,317 splits × 33 attempts, or 6,323 entries (GTA SA 100% Splitspam).
  - The most attempts is 1,709, over 13 splits (Swat 4).

**Plan.**

- Add `BestSegmentTimes` to `SplitLevel`, per method and by local index, next to `PbSegmentTimes`.
- Fill it in a new `SplitLevels.CalculateBestSegments(run, method)`, called from `CalculateSegments()` after `CalculatePBSegments`.
  - **When:** history only changes on reset, so once per recalculation is enough, never per split or per `Update()`. The cost is one pass over attempts × splits, which is tens of thousands of lookups for the largest history above.
  - **Range:** a member covers global splits `prev+1 .. global`. `prev` is the previous member's global index, or −1 for local index 0, as in `CalculatePBSegments`.
  - **Single-split member:** `BestSegmentTime`. This keeps `AllSplits` output unchanged. It also respects golds edited by hand or kept after history was cleared.
  - **Multi-split member:** the minimum over attempts of `splitTime[global] − splitTime[prev]`, with zero as the start when `prev` is −1. Only attempts that recorded both ends count. Skips inside the section don't matter.
  - **Which attempts:** iterate `run[0].SegmentHistory.Keys`. Every attempt that recorded anything has a key on split 0, and the imported-gold keys don't. For each attempt, fill one reused `TimeSpan?[run.Count]` with its split times, stopping at the first split without a key. Then update every level's best in place.
  - **No qualifying attempt:** null, meaning no gold yet. That is the same as a split with no best segment.
- `GoldDelta` and `IsGold` ([OutputWriter.cs](../UI/Components/OutputWriter.cs)) drop their level guard, and compare `LiveSegmentTime` with `BestSegmentTimes` at the local index instead of with the split's `BestSegmentTime`.
- Drop the `lvl == Levels.Count - 1` guards on gold output in `WritePreviousSplitTimes` (including the one on the `-` reset of `GoldDifference.txt` when nothing is completed yet) and `WriteCurrentSplitTimes` ([SplitFiles.cs](../UI/Components/SplitFiles.cs)), and in `ListLines`, `AppendCompletedLine` and `AppendUpcomingLine` ([SplitListFiles.cs](../UI/Components/SplitListFiles.cs)). `GoldTime.txt` and `Time_Gold_Upcoming.txt` read `BestSegmentTimes`. They currently use `State.CurrentSplit.BestSegmentTime`, which is the wrong split above `AllSplits`.

**Verify.**

- Headless test ([testing.md](testing.md)) with splits `-A1`, `-A2`, `A`, `B` and game time. Play the attempts through `TimerModel` and reset after each, so LiveSplit writes the history itself.
  - Attempt 1 has segments 10/10/10, and attempt 2 has 5/20/10. The golds are then 5/10/10, which sum to 25. The best single-attempt time to `A` is 30.
  - Attempt 3 reaches `A` at 28. `PreviousSplit\SansSubsplits\GameTime\GoldDifference.txt` should show a 2 s gain against 30, not a 3 s loss against 25. `A` should be filled in `Highlight_Gold.txt` at `SansSubsplits`.
  - An attempt that skips `-A2` but records `A` counts toward the best. An attempt reset before `A` doesn't.
  - Dump `SegmentHistory` after each reset to confirm the history facts under Evidence.
- The `AllSplits` tree is the same as from a build without GLD.
- Load the 1,317-split file through LiveSplit.Core's run factory and time `CalculateSegments()` with a `Stopwatch`.

**Docs.**

- CLAUDE.md "Must-know before editing", which says golds exist only at the last level.
- architecture.md:
  - "Per-level data" (the new `BestSegmentTimes`, and `GoldDelta` in the live times and deltas table);
  - "How the values are calculated" (gold delta and the gold highlight);
  - the note that the current gold time relies on local index = global index at the last level.
- output-files.md, where it says gold files exist only under `AllSplits`.
- livesplit-notes.md: the `SegmentHistory` facts, once verified.
- testing.md: the regression base run has no history, so every section will have no gold yet and its first completion will count as one. Say so where the base run is described.
- README: the gold files under the `SansSubsplits*` folders, and what a section's gold means.
