# TODO

Open work in this repo, by three-letter code. Each code links to its section in the detail doc named in the group heading: what's wrong or wanted, the evidence, a plan, how to verify it, and which docs to update. Within a group, rows are in suggested order of work. Each detail doc notes the dependencies between its plans.

When an item is finished, delete its row here and its section in the detail doc, and log it in [DONE.md](DONE.md). When work turns up a new defect that isn't being fixed now, add both.

## Defects — [Defects.md](Defects.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [IDX](Defects.md#idx--sanssubsplits-segment-timer-measures-from-the-wrong-split) | Timers | `SegmentTimer_SansSubsplits*` indexes the run with a level-local index, so it measures from the wrong split | Medium |
| [CLM](Defects.md#clm--a-first-split-with-two-or-more-dashes-is-dropped) | Subsplits | The depth clamp skips split 0: a first split with 2+ dashes (and no 1-dash split) vanishes from every output, and the next split's segment absorbs its time | Medium |
| [EDT](Defects.md#edt--editing-splits-isnt-picked-up-until-the-next-run-starts) | Change detection | Split count, names and PB times aren't cache keys, so edited splits stay stale in the files until the next run starts | Medium |
| [NEG](Defects.md#neg--negative-deltas-get-a-different-precision-from-positive-ones) | Time format | Negative deltas choose precision from the negative value, so `-5.23` but `+5.2` | Low |
| [UND](Defects.md#und--undo-to-the-first-split-leaves-golddifference-stale) | PreviousSplit | Undoing to the first split resets every `PreviousSplit` file except `GoldDifference.txt` | Low |
| [SKP](Defects.md#skp--skipped-splits-are-highlighted-as-behindlost) | Split list | Skipped splits get a BehindLost highlight instead of none | Low |
| [SEC](Defects.md#sec--run-timer-file-can-get-stuck-after-a-reset) | Timers | `HasTimerChanged` compares the 0–59 seconds component, so a reset at 1:00.x leaves `RunTimer.txt` at `1:00` | Low |
| [CHK](Defects.md#chk--output-checkboxes-dont-take-effect-at-once) | Change detection | The Output checkboxes aren't cache keys, so toggling one does nothing until another change, and outputs turned off leave stale files | Low |
| [LAG](Defects.md#lag--segment-timer-ticks-over-late) | Timers | The segment timer ticks on the run timer's second boundary, lagging up to about 1 s, and shows `0:00` after the finish | Low |
| [DUP](Defects.md#dup--an-extra-duplicate-level-when-only-the-last-split-has-dashes) | Subsplits | When only the last split has dashes, an extra `SansSubsplits` level identical to `AllSplits` is written | Low |
| [THR](Defects.md#thr--timer-events-may-arrive-off-the-ui-thread) | Threading | Not checked: timer events raised off the UI thread (Server component, auto splitters) would race `Update()` on unlocked arrays | Low |

## Performance — [Performance.md](Performance.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [GEN](Performance.md#gen--generatesubsplitlevels-runs-twice) | Subsplits | `CalculateSegments` runs `GenerateSubsplitLevels` twice; its `method` parameter is unused | Low |
| [ATM](Performance.md#atm--file-writes-arent-atomic) | File writing | Writes aren't atomic. Measured: an OBS text source on a timer file goes blank for 1–2 s about once every 4 h of running timer. Not worth fixing unless blanks are seen | Parked |

## Cleanup — [Cleanup.md](Cleanup.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [DEF](Cleanup.md#def--three-different-sets-of-settings-defaults) | Settings | The Designer, the constructor and the `SetSettings` fallbacks each have different defaults (6/4, 10/10, 3/2; Completed splits on or off) | Low |
| [BRW](Cleanup.md#brw--browse-opens-the-parent-of-the-current-folder) | Settings | Browse opens the parent of the current folder, and the dialog is never disposed | Low |
| [HND](Cleanup.md#hnd--dead-change-handlers-in-the-settings-control) | Settings | Unconnected `CheckedChanged`/`ValueChanged` handlers (one sets the wrong property) and an empty `Load` handler | Low |
| [SNT](Cleanup.md#snt--sentinel-indices-instead-of-explicit-phase-handling) | Subsplits | The subsplit index helpers return −2/−3 sentinels that callers rely on implicitly; handle timer phases explicitly | Low |
| [BLK](Cleanup.md#blk--completedsplits-writes-files-that-are-always-blank) | CompletedSplits | The upcoming-time files and `Highlight_CurrentSplit.txt` under `CompletedSplits\` are always blank | Low |
| [DUR](Cleanup.md#dur--durationstring-and-timerstring-are-identical) | Time format | `TimerString` duplicates `DurationString` and has no callers; one caller repeats `DurationString`'s null check | Low |
| [NSP](Cleanup.md#nsp--timeformatter-has-its-own-namespace) | Time format | `TimeFormatter` is in `LiveSplit.OutputToFile.UI.Components.OutputToFile`; everything else is in `LiveSplit.UI.Components` | Low |
| [SVR](Cleanup.md#svr--saved-settings-version-is-never-used) | Settings | The saved settings `Version` is read but never used; bump it when keys change or drop it | Low |
| [USN](Cleanup.md#usn--unused-usings) | Code | Unused usings, including `using static System.Windows.Forms.AxHost` | Low |
| [UNU](Cleanup.md#unu--unused-members-and-variables) | Code | Unused `InternalComponent`, `currentSplitIndex` (the CS0219 warning), `level` and `Settings.Mode` | Low |
| [SGN](Cleanup.md#sgn--some-signs-values-are-never-written) | Sign.txt | The `Signs` values `Undetermined`, `Gained`, `Lost` and `Gold` are never written | Low |
| [CMT](Cleanup.md#cmt--commented-out-code) | Code | Commented-out code blocks in the component | Low |
| [TPL](Cleanup.md#tpl--wrong-or-misleading-template-comments) | Code | Leftover template comments that are wrong (`DrawVertical`, `Update`, `GetSettingsHashCode`, the factory) | Low |
| [LAY](Cleanup.md#lay--settings-layout-leftovers) | Settings | The top table has 5 rows for 4 controls, and tab order is Folder → Split list → Output | Low |

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
| [HNT](Project.md#hnt--livesplit-references-only-resolve-on-this-machine) | Build | The LiveSplit references use a HintPath that only exists on this machine; add an overridable `LiveSplitDir` | Low |
| [PRV](Project.md#prv--livesplit-dlls-are-copied-into-bin) | Build | The LiveSplit DLLs get copied into `bin\` because the references aren't `<Private>false</Private>` | Low |
| [VER](Project.md#ver--version-stuck-at-100-with-no-update-url) | Build | The version is 1.0.0 in both the factory and `AssemblyInfo.cs`, and `UpdateURL` is empty | Low |
| [RCC](Project.md#rcc--unused-release-candidate-configuration) | Build | The solution has an unused "Release Candidate" configuration that maps to Release | Low |
