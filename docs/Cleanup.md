# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

## SCR — Settings tab scrolls for no reason

The Output to File tab in Layout Settings has a vertical scrollbar, with nothing to scroll to below the Comparisons group. The settings control is 619 px tall, though its content ends at 365 px (measured headless 2026-10-22).
- The Designer gives the control `Size` 457 × 619, and `tableLayoutPanelTop` 454 × 549 ([OutputToFileSettings.Designer.cs](../UI/Components/OutputToFileSettings.Designer.cs)).
- Both are `AutoSize` in the default `GrowOnly` mode, so they never shrink below that.
- Release 6's control was 329 × 330, with no scrollbar. Release 7's is already 457 × 619 (measured headless 2026-10-21), so its users have the scrollbar too.

**Evidence.** Reproduced 2026-10-21 in LiveSplit 1.8.37: a screenshot of the tab shows the scrollbar, and the headless layout check measures the control at 619 px. The cause is read.

**Plan.** Make the control and `tableLayoutPanelTop` `AutoSizeMode = GrowAndShrink`, or shrink their Designer sizes to the content, so the control is as tall as its content. A shown write error must still make the control grow to fit it.

**Verify.** In LiveSplit, the tab has no scrollbar. Headless, every group keeps its bounds, and a long write error shows in full.

**Docs.** CHANGELOG.md, under Fixed: release 7 has the scrollbar.
