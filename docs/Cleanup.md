# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

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
