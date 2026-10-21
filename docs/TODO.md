# TODO

Open work in this repo, by three-letter code. Each code links to its section in the detail doc named in the group heading: what's wrong or wanted, the evidence, a plan, how to verify it, and which docs to update. Within a group, rows are in suggested order of work. Each detail doc notes the dependencies between its plans.

When an item is finished, delete its row here and its section in the detail doc, and log it in [DONE.md](DONE.md). When work turns up a new defect that isn't being fixed now, add both.

## Defects — [Defects.md](Defects.md)

| Code | Area | Summary | Severity |
|---|---|---|---|

## Performance — [Performance.md](Performance.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [ATM](Performance.md#atm--file-writes-arent-atomic) | File writing | Writes aren't atomic. Measured: an OBS text source on a timer file goes blank for 1–2 s about once every 4 h of running timer. Not worth fixing unless blanks are seen | Parked |

## Cleanup — [Cleanup.md](Cleanup.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [SCR](Cleanup.md#scr--settings-tab-scrolls-for-no-reason) | Settings | The Output to File settings tab has a scrollbar with nothing below the split list group: the control keeps its Designer height of 619 px | Low |

## Features and limitations — [Features.md](Features.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [TST](Features.md#tst--no-automated-tests) | Tests | No test project; turn the headless harness and regression checks in testing.md into one | Medium |
| [CMP](Features.md#cmp--deltas-ignore-the-selected-comparison) | Comparisons | Deltas always use Personal Best, ignoring the comparison selected in LiveSplit | Medium |
| [FMT](Features.md#fmt--time-format-is-fixed) | Time format | Time and delta formats are hard-coded, with no setting; replaces the `// TODO` in `TimeFormatter.cs` | Low |
| [GLD](Features.md#gld--golds-only-exist-at-the-allsplits-level) | Golds | Golds are only reported at `AllSplits`; a section's gold should be its best time within one attempt, from `SegmentHistory`, not a sum of golds | Low |

## Project and README — [Project.md](Project.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [DOC](Project.md#doc--readme-is-missing-outputs-and-settings) | README | Undocumented: `CompletedSplits\`, `PlatformName.txt`, `*_Raw`/`Names_Indent_N` files, `{Header}` names, `Sign.txt` values, settings, install | Medium |
| [GTT](Project.md#gtt--readmes-gametime-timer-list-is-incomplete) | README | The `Timers\GameTime` tree lists only `SegmentTimer_AllSplits.txt`, but every level is written | Low |
| [UPD](Project.md#upd--update-prompt-not-yet-seen-in-livesplit) | Updates | LiveSplit's startup update prompt was only checked headless; after release 8 is published, see it in LiveSplit: the prompt, the install from GitHub and the restart | Medium |
| [RCC](Project.md#rcc--unused-release-candidate-configuration) | Build | The solution has an unused "Release Candidate" configuration that maps to Release | Low |
