# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md). Line numbers are as of commit `30d30fa`.

Do DRY before [SKP](Defects.md#skp--skipped-splits-are-highlighted-as-behindlost) and before any other change to the split list output, so that each change is made once. DUR and NSP both edit `TimeFormatter.cs`.

## DRY — Split list writers are near-duplicates

`WriteSplitList` and `WriteCompletedSplitList` are about 110 nearly identical lines each ([:541-654](../UI/Components/OutputToFileComponent.cs#L541-L654), [:656-788](../UI/Components/OutputToFileComponent.cs#L656-L788)). Only the `first`/`last` range and the file names differ, so every fix to one has to be mirrored in the other.

**Evidence.** Read.

**Plan.** Extract one method that takes the range and the output folder (`SplitList` or `CompletedSplits`; the file names under each are the same). Keep the `FILE_*` templates as the single place paths are defined.

**Verify.** For a full run with subsplits and both outputs on, the output folder is identical before and after.

**Docs.** CLAUDE.md "Must-know before editing" (the two-list-writers bullet) and "Making changes" (step 3 of adding an output file). architecture.md "Writing files".

## DEF — Three different sets of settings defaults

- Designer: 6 before, 4 after, Completed splits checked ([Designer.cs:161](../UI/Components/OutputToFileSettings.Designer.cs#L161), [:231](../UI/Components/OutputToFileSettings.Designer.cs#L231), [:244](../UI/Components/OutputToFileSettings.Designer.cs#L244)).
- Constructor: 10, 10, unchecked ([OutputToFileSettings.cs:24-29](../UI/Components/OutputToFileSettings.cs#L24-L29)).
- `SetSettings` fallback: 3, 2 ([:75-76](../UI/Components/OutputToFileSettings.cs#L75-L76)).

**Evidence.** Read.

**Plan.** Pick one set. Make the constructor the source, and make the Designer and the `SetSettings` fallbacks agree with it.

**Verify.** A newly added component and a layout saved without these keys both show the chosen defaults.

**Docs.** architecture.md "Settings". README, if [DOC](Project.md#doc--readme-is-missing-outputs-and-settings) has documented the settings by then.

## BRW — Browse opens the parent of the current folder

[OutputToFileSettings.cs:87](../UI/Components/OutputToFileSettings.cs#L87) should use `FolderPath` rather than `Path.GetDirectoryName(FolderPath)`. The `FolderBrowserDialog` is never disposed either.

**Evidence.** Read.

**Plan.** Use `FolderPath`, and wrap the dialog in `using`.

**Verify.** With a folder set, Browse opens on that folder.

**Docs.** None.

## HND — Dead change handlers in the settings control

These handlers aren't connected to any control: `checkBoxOutputTimer_CheckedChanged`, `checkBoxOutputSubsplits_CheckedChanged`, `checkBoxOutputCompletedSplits_CheckedChanged`, `numericUpDownSplitListBefore_ValueChanged` and `numericUpDownSplitListAfter_ValueChanged`. Data binding does their job. The Subsplits one also sets the wrong property ([:115](../UI/Components/OutputToFileSettings.cs#L115)). `OutputToFileSettings_Load` is connected but empty.

**Evidence.** Read: the Designer only wires up `buttonFolderPath_Click`, `checkBoxOutputSplitList_CheckedChanged` and `Load`.

**Plan.** Delete the unconnected handlers, and the empty `Load` handler with its Designer wiring.

**Verify.** Build. Change every setting, save the layout and reload it: all values kept.

**Docs.** architecture.md "Settings", if it lists the handlers.

## SNT — Sentinel indices instead of explicit phase handling

`GetPreviousSubsplitIndex` returns −2 when the timer isn't running. `WriteSplitTimes` ([:829](../UI/Components/OutputToFileComponent.cs#L829)) relies on −2 ≠ −1 to reach the branch that writes `-`. The "shouldn't happen" −3 ([:803](../UI/Components/OutputToFileComponent.cs#L803), [:820](../UI/Components/OutputToFileComponent.cs#L820)) would surface as an index crash.

**Evidence.** Read.

**Plan.** Handle NotRunning and Ended explicitly at each call site, and have the helpers return only real indices, or −1 for none.

**Verify.** Output unchanged for a full run with subsplits, including undo, finish and reset.

**Docs.** CLAUDE.md "Must-know before editing" (the timer-phases bullet). architecture.md "Phase-dependent indexes".

## BLK — CompletedSplits writes files that are always blank

`Time_Run_Upcoming`, `Time_Segment_Upcoming`, `Time_Gold_Upcoming` and `Highlight_CurrentSplit` under `CompletedSplits\` never get content, because its range stops before the current split.

**Evidence.** Reproduced 2026-10-14.

**Plan.** Stop writing them (simplest after DRY), or document that they're blank.

**Verify.** A fresh output folder after a run with Completed splits on doesn't contain them.

**Docs.** output-files.md "CompletedSplits". README, if DOC has documented `CompletedSplits\` by then.

## IND — Names_Indent arrays are initialised at the wrong end

In the `Names_Indent` code ([:639-642](../UI/Components/OutputToFileComponent.cs#L639-L642), [:773-776](../UI/Components/OutputToFileComponent.cs#L773-L776)), the init loop fills entries `lvl..Length-1`, but the code uses entries `0..lvl`. It only works because `null + string` gives a string in C#.

**Evidence.** Read.

**Plan.** Size the array `lvl + 1` and fill all of it.

**Verify.** `Names_Indent_*.txt` unchanged for a run with two subsplit levels.

**Docs.** None.

## DUR — DurationString and TimerString are identical

`TimeFormatter.TimerString` is the same code as `DurationString` and has no callers. The `HasValue ? … : "-"` check at [OutputToFileComponent.cs:889](../UI/Components/OutputToFileComponent.cs#L889) repeats null handling that `DurationString` already does.

**Evidence.** Read.

**Plan.** Delete `TimerString` and the redundant check.

**Verify.** Build. `CurrentSplit\AllSplits\*\GoldTime.txt` still shows `-` for a split with no best segment.

**Docs.** output-files.md "Formatting", if it lists `TimerString`.

## NSP — TimeFormatter has its own namespace

`TimeFormatter` is in `LiveSplit.OutputToFile.UI.Components.OutputToFile` ([TimeFormatter.cs:8](../UI/Components/TimeFormatter.cs#L8)), while everything else is in `LiveSplit.UI.Components`.

**Evidence.** Read.

**Plan.** Move it to `LiveSplit.UI.Components` and drop the matching `using` lines from the component.

**Verify.** Build.

**Docs.** testing.md, whose reflection example uses the old namespace.

## SVR — Saved settings Version is never used

The saved `Version` (`0.1`) is read into a variable that's never used ([OutputToFileSettings.cs:72](../UI/Components/OutputToFileSettings.cs#L72)).

**Evidence.** Read.

**Plan.** If settings will ever need migrating, bump the version whenever keys change and branch on it in `SetSettings`. Otherwise drop the read.

**Verify.** A layout saved with the old version still loads with the right values.

**Docs.** CLAUDE.md "Making changes" (adding a setting), if bumping becomes a step.

## USN — Unused usings

Including an accidental `using static System.Windows.Forms.AxHost` ([:2-13](../UI/Components/OutputToFileComponent.cs#L2-L13)). Most of the usings in `TimeFormatter.cs` are unused too.

**Evidence.** Read.

**Plan.** Remove them.

**Verify.** Build.

**Docs.** None.

## UNU — Unused members and variables

- `InternalComponent` ([:18](../UI/Components/OutputToFileComponent.cs#L18)).
- `currentSplitIndex` ([:814](../UI/Components/OutputToFileComponent.cs#L814)), which gave the build's only warning on 2026-10-14.
- `level` in `WriteSplitInformation` ([:902](../UI/Components/OutputToFileComponent.cs#L902)).
- `Settings.Mode`, which is set ([:172](../UI/Components/OutputToFileComponent.cs#L172)) but never read.

**Evidence.** Read.

**Plan.** Remove them.

**Verify.** Build with no warnings.

**Docs.** CLAUDE.md "Build / deploy" and testing.md "Build", which both name CS0219 as the expected warning.

## SGN — Some Signs values are never written

`Undetermined`, `Gained`, `Lost` and `Gold` ([:38-60](../UI/Components/OutputToFileComponent.cs#L38-L60)).

**Evidence.** Read.

**Plan.** Remove the unused values, or start writing them if a use turns up.

**Verify.** Build.

**Docs.** output-files.md "Sign values". README, if DOC has listed the `Sign.txt` values by then.

## CMT — Commented-out code

At [:250-253](../UI/Components/OutputToFileComponent.cs#L250-L253), [:278-280](../UI/Components/OutputToFileComponent.cs#L278-L280), [:468](../UI/Components/OutputToFileComponent.cs#L468), [:956-958](../UI/Components/OutputToFileComponent.cs#L956-L958) and [:970-972](../UI/Components/OutputToFileComponent.cs#L970-L972).

**Evidence.** Read.

**Plan.** Delete it. Git history keeps it.

**Verify.** Build.

**Docs.** None.

## TPL — Wrong or misleading template comments

- `DrawVertical` says "two rows" ([:168](../UI/Components/OutputToFileComponent.cs#L168)).
- `Update` says "tell the internal component to display it" ([:508-510](../UI/Components/OutputToFileComponent.cs#L508-L510)).
- `GetSettingsHashCode` says "I do not know what this is for" ([:998](../UI/Components/OutputToFileComponent.cs#L998)). LiveSplit uses that hash only to decide whether to redraw the layout.
- The factory's "Fill in this empty string" comments ([OutputToFileFactory.cs:25-27](../UI/Components/OutputToFileFactory.cs#L25-L27)).

**Evidence.** Read.

**Plan.** Correct or delete each one.

**Verify.** Build.

**Docs.** None.

## LAY — Settings layout leftovers

`tableLayoutPanelTop` has 5 rows for 3 controls ([Designer.cs:67-72](../UI/Components/OutputToFileSettings.Designer.cs#L67-L72)), and tab order goes Folder → Split list → Output.

**Evidence.** Read.

**Plan.** Remove the spare rows and set the tab order to match the visual order.

**Verify.** Open the settings in LiveSplit: same look, and Tab moves top to bottom.

**Docs.** None.
