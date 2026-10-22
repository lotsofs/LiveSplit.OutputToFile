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
