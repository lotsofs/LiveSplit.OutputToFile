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
