# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

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
