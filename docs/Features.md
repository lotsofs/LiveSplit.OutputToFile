# Features and limitations

Things the component doesn't do yet. The index is [TODO.md](TODO.md).

## SCL — The split list doesn't follow LiveSplit's scrolling

Scrolling in LiveSplit, with the mouse wheel or the Scroll Up/Down hotkeys, shows earlier or later splits in its Splits component. `SplitList\` keeps showing the window around the current split. Wanted: every level's `SplitList\` follows the scrolling. `CompletedSplits\`, `CurrentSplit\` and `PreviousSplit\` stay as they are.

**Evidence.** Read in source, not run:
- The mouse wheel and the hotkeys call `TimerModel.ScrollUp`/`ScrollDown`, which raise `OnScrollUp`/`OnScrollDown` on the state (LiveSplit `683cd60`). The component doesn't subscribe to them.
- LiveSplit doesn't store a scroll position. The Splits component keeps its own `ScrollOffset`, one split per notch (LiveSplit.Splits `20bbf92`):
  - It resets the offset to 0 on start, split, skip, undo and reset.
  - Each frame, it clamps the offset so the window stays within the run.

**Plan.**
- Subscribe to `OnScrollUp` and `OnScrollDown`. As with the timer events, the handlers only count, with `Interlocked`.
- `Update()` applies the count to an offset:
  - It sets the offset to 0 when a start, split, skip, undo or reset was counted.
  - It clamps the offset to the `AllSplits` level, as LiveSplit does.
  - It adds the offset to the `Cache` keys, so a scroll rewrites the files.
- `SplitListRange` shifts each level's window by the offset, one of that level's lines per notch, and clamps it to the level.

**Verify.** Tests:
- One scroll down shifts `SplitList\AllSplits\Names.txt` by one line, and `SansSubsplits` by one section.
- Scrolling past either end stops at the end. Scrolling back moves at once, without having to undo the extra notches first.
- A split, or a reset, brings the window back to the current split.
- Scrolling before the start and after the finish works too, as in LiveSplit.

**Docs.**
- output-files.md: the SplitList section.
- architecture.md: the events and `Update()`.
- README.md: the split list.
- CHANGELOG.md: under Unreleased, Added.

## CRN — A ticked comparison renamed in Edit Splits is no longer written

The Comparisons setting remembers ticked comparisons by name. When a ticked comparison is renamed in Edit Splits:
- its folder is emptied;
- the new name isn't ticked, so nothing is written for it;
- the settings list still shows the old name, ticked, so it can be unticked.

The user has to tick the new name. LiveSplit's own components that store a comparison name, such as Delta, follow the rename instead.

**Evidence.**
- Reproduced 2026-10-23, headless:
  - The test ticked `Rival`, then renamed it to `Rival Two` the way the editor does, raising `ComparisonRenamed` and `RunManuallyModified`.
  - Afterwards `PreviousSplit\AllSplits\GameTime\` held only `CurrentComparison` and an emptied `Rival`, and `Rival` was still the ticked name.
- Read in source (LiveSplit `683cd60`):
  - `RunEditorDialog.RenameComparison` raises `ComparisonRenamed` with a `RenameEventArgs` holding `OldName` and `NewName`, and `TimerForm` passes it on with `state.CallComparisonRenamed`.
  - Removing a comparison raises it too, with `NewName` "Current Comparison".

**Plan.**
- Subscribe to `ComparisonRenamed`. To keep the rule that handlers only count, queue the old and new names in a `ConcurrentQueue`.
- `Update()` applies the queue to the settings, on the UI thread: a ticked old name is unticked, and the new name ticked.
- On a removal, keep the name ticked, so the list shows it for unticking, as for any ticked name the run doesn't have.
- The ticked set is saved with the layout, so LiveSplit asks to save the layout afterwards, as it does for the Delta component.

**Verify.** A test: tick a comparison, rename it with `CallComparisonRenamed`, and check that the new folder is written and the new name ticked.

**Docs.**
- README.md: the Comparisons setting.
- architecture.md: the comparisons.
- CHANGELOG.md: under Unreleased.

## CLO — Files keep their contents after LiveSplit closes

When LiveSplit closes, every file keeps what it last showed: the split list, and if a run was going, that run's values. GitHub issue #5 (2023) asks for the split list to be cleared to `-` "like other things". What the files should show while LiveSplit is closed hasn't been decided.

**Evidence.** Read in source, not run:
- LiveSplit's `TimerForm_FormClosing` doesn't reset the timer. It disposes every layout component (LiveSplit `683cd60`).
- The component's `Dispose` only unsubscribes from the timer events and waits for the queued writes.
- LiveSplit also disposes the component when it's removed from the layout, and when another layout is opened.

**Decide first.** Keep the last contents, as now, so OBS shows the last state? Or write the not-running state, or empty every file, so a closed LiveSplit is obvious on stream?

**Plan, if clearing.** In `Dispose`, queue the chosen contents for every file the last rewrite wrote, then wait for the writes as now. This also happens when the layout changes, so decide whether that's wanted too.

**Verify.** A test that plays part of a run, calls `Dispose()`, and reads the files.

**Docs.** output-files.md, README.md and CHANGELOG.md. GitHub issue #5 can then be closed.
