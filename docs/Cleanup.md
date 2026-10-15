# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

DUR and NSP both edit `TimeFormatter.cs`.

## DEF — Three different sets of settings defaults

- Designer: 6 before, 4 after, Completed splits checked (`InitializeComponent` in [OutputToFileSettings.Designer.cs](../UI/Components/OutputToFileSettings.Designer.cs)).
- Constructor: 10, 10, unchecked (the `OutputToFileSettings` constructor in [OutputToFileSettings.cs](../UI/Components/OutputToFileSettings.cs)).
- `SetSettings` fallback: 3, 2.

**Evidence.** Read.

**Plan.** Pick one set. Make the constructor the source, and make the Designer and the `SetSettings` fallbacks agree with it.

**Verify.** A newly added component and a layout saved without these keys both show the chosen defaults.

**Docs.** architecture.md "Settings". README, if [DOC](Project.md#doc--readme-is-missing-outputs-and-settings) has documented the settings by then.

## BRW — Browse opens the parent of the current folder

`buttonFolderPath_Click` ([OutputToFileSettings.cs](../UI/Components/OutputToFileSettings.cs)) should use `FolderPath` rather than `Path.GetDirectoryName(FolderPath)`. The `FolderBrowserDialog` is never disposed either.

**Evidence.** Read.

**Plan.** Use `FolderPath`, and wrap the dialog in `using`.

**Verify.** With a folder set, Browse opens on that folder.

**Docs.** None.

## HND — Dead change handlers in the settings control

These handlers aren't connected to any control: `checkBoxOutputTimer_CheckedChanged`, `checkBoxOutputSubsplits_CheckedChanged`, `checkBoxOutputCompletedSplits_CheckedChanged`, `numericUpDownSplitListBefore_ValueChanged` and `numericUpDownSplitListAfter_ValueChanged`. Data binding does their job. The Subsplits one also sets the wrong property (`OutputSplitList`). `OutputToFileSettings_Load` is connected but empty.

**Evidence.** Read: the Designer only wires up `buttonFolderPath_Click`, `checkBoxOutputSplitList_CheckedChanged` and `Load`.

**Plan.** Delete the unconnected handlers, and the empty `Load` handler with its Designer wiring.

**Verify.** Build. Change every setting, save the layout and reload it: all values kept.

**Docs.** architecture.md "Settings", if it lists the handlers.

## SNT — Sentinel indices instead of explicit phase handling

`GetPreviousSubsplitIndex` ([OutputWriter.cs](../UI/Components/OutputWriter.cs)) returns −2 when the timer isn't running. `WritePreviousSplitTimes` ([SplitFiles.cs](../UI/Components/SplitFiles.cs)) relies on −2 ≠ −1 to reach the branch that writes `-`. The "shouldn't happen" −3 that both index helpers return would surface as an index crash.

**Evidence.** Read.

**Plan.** Handle NotRunning and Ended explicitly at each call site, and have the helpers return only real indices, or −1 for none.

**Verify.** Output unchanged for a full run with subsplits, including undo, finish and reset.

**Docs.** CLAUDE.md "Must-know before editing" (the timer-phases bullet). architecture.md "Phase-dependent indexes".

## BLK — CompletedSplits writes files that are always blank

`Time_Run_Upcoming`, `Time_Segment_Upcoming`, `Time_Gold_Upcoming` and `Highlight_CurrentSplit` under `CompletedSplits\` never get content, because its range stops before the current split.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Stop writing them: don't create them in `ListLines` when the folder is `FOLDER_COMPLETEDSPLITS`. Nothing appends to them there, because every line is completed. Or document that they're blank.

**Verify.** A fresh output folder after a run with Completed splits on doesn't contain them.

**Docs.** output-files.md "CompletedSplits". README, if DOC has documented `CompletedSplits\` by then.

## DUR — DurationString and TimerString are identical

`TimeFormatter.TimerString` is the same code as `DurationString` and has no callers. The `HasValue ? … : "-"` check for `GoldTime.txt` in `WriteCurrentSplitTimes` ([SplitFiles.cs](../UI/Components/SplitFiles.cs)) repeats null handling that `DurationString` already does.

**Evidence.** Read.

**Plan.** Delete `TimerString` and the redundant check.

**Verify.** Build. `CurrentSplit\AllSplits\*\GoldTime.txt` still shows `-` for a split with no best segment.

**Docs.** output-files.md "Formatting", if it lists `TimerString`.

## NSP — TimeFormatter has its own namespace

`TimeFormatter` is in `LiveSplit.OutputToFile.UI.Components.OutputToFile` ([TimeFormatter.cs](../UI/Components/TimeFormatter.cs)), while everything else is in `LiveSplit.UI.Components`.

**Evidence.** Read.

**Plan.** Move it to `LiveSplit.UI.Components` and drop the matching `using` lines from `OutputToFileComponent`, `TimerFiles`, `SplitFiles` and `SplitListFiles`.

**Verify.** Build.

**Docs.** testing.md, whose reflection example uses the old namespace.

## SVR — Saved settings Version is never used

The saved `Version` (`0.1`) is read into a variable in `SetSettings` ([OutputToFileSettings.cs](../UI/Components/OutputToFileSettings.cs)) that's never used.

**Evidence.** Read.

**Plan.** If settings will ever need migrating, bump the version whenever keys change and branch on it in `SetSettings`. Otherwise drop the read.

**Verify.** A layout saved with the old version still loads with the right values.

**Docs.** CLAUDE.md "Making changes" (adding a setting), if bumping becomes a step.

## USN — Unused usings

In [OutputToFileComponent.cs](../UI/Components/OutputToFileComponent.cs), including an accidental `using static System.Windows.Forms.AxHost`. Most of the usings in `TimeFormatter.cs` are unused too.

**Evidence.** Read.

**Plan.** Remove them.

**Verify.** Build.

**Docs.** None.

## UNU — Unused members and variables

- `InternalComponent` in `OutputToFileComponent`.
- `currentSplitIndex` in `GetCurrentSubsplitIndex` ([OutputWriter.cs](../UI/Components/OutputWriter.cs)), which gives the build's only warning, CS0219.
- `level` in `WriteSplitInformation` ([SplitFiles.cs](../UI/Components/SplitFiles.cs)).
- `Settings.Mode`, which `GetSettingsControl` sets but nothing reads.

**Evidence.** Read.

**Plan.** Remove them.

**Verify.** Build with no warnings.

**Docs.** CLAUDE.md "Build / deploy" and testing.md "Build", which both name CS0219 as the expected warning.

## SGN — Some Signs values are never written

`Undetermined`, `Gained`, `Lost` and `Gold`, in [Signs.cs](../UI/Components/Signs.cs).

**Evidence.** Read.

**Plan.** Remove the unused values, or start writing them if a use turns up.

**Verify.** Build.

**Docs.** output-files.md "Sign values". README, if DOC has listed the `Sign.txt` values by then.

## CMT — Commented-out code

One block each in `GenerateLevels` and `GenerateNames` ([SplitLevels.cs](../UI/Components/SplitLevels.cs)), one line in `WriteTimer` ([TimerFiles.cs](../UI/Components/TimerFiles.cs)), and one block in each of the two template-filling `MakeFile` overloads ([OutputWriter.cs](../UI/Components/OutputWriter.cs)).

**Evidence.** Read.

**Plan.** Delete it. Git history keeps it.

**Verify.** Build.

**Docs.** None.

## TPL — Wrong or misleading template comments

In [OutputToFileComponent.cs](../UI/Components/OutputToFileComponent.cs) unless another file is named:

- `DrawVertical` says "two rows".
- `Update` says "tell the internal component to display it".
- `GetSettingsHashCode` says "I do not know what this is for". LiveSplit uses that hash only to decide whether to redraw the layout.
- The factory's "Fill in this empty string" comments on `UpdateURL` and `XMLURL` ([OutputToFileFactory.cs](../UI/Components/OutputToFileFactory.cs)).

**Evidence.** Read.

**Plan.** Correct or delete each one.

**Verify.** Build.

**Docs.** None.

## LAY — Settings layout leftovers

`tableLayoutPanelTop` has 5 rows for 4 controls (the last is the write-error label) ([OutputToFileSettings.Designer.cs](../UI/Components/OutputToFileSettings.Designer.cs)), and tab order goes Folder → Split list → Output.

**Evidence.** Read.

**Plan.** Remove the spare rows and set the tab order to match the visual order.

**Verify.** Open the settings in LiveSplit: same look, and Tab moves top to bottom.

**Docs.** None.
