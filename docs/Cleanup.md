# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

## LAY — Settings layout leftovers

`tableLayoutPanelTop` has 5 rows for 4 controls (the last is the write-error label) ([OutputToFileSettings.Designer.cs](../UI/Components/OutputToFileSettings.Designer.cs)), and tab order goes Folder → Split list → Output.

**Evidence.** Read.

**Plan.** Remove the spare rows and set the tab order to match the visual order.

**Verify.** Open the settings in LiveSplit: same look, and Tab moves top to bottom.

**Docs.** None.
