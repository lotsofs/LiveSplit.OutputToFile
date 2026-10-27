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

## CVR — LiveSplit's custom variables aren't written

LiveSplit's custom variables are names with text values, kept in `Run.Metadata.CustomVariables`. They're LiveSplit's own and unrelated to speedrun.com's variables, which `Variables\` holds. Auto splitters and tools set them, for example to a death count, a collectible count or the current level. LiveSplit's Text component can show one by name. Wanted: `CustomVariables\{name}.txt`, a file per custom variable with its value.

**Evidence.** Read in source, not run (LiveSplit `683cd60`, ASR `98d91bb`, LiveSplit.Text `ca08a3e`):
- **Who sets them:** Auto Splitting Runtime scripts, through the runtime's `setCustomVariable` (`ComponentSettings` in ASR), and the server's `setcustomvariable` command (`CommandServer`). Both call `RunMetadata.SetCustomVariable`. Edit Splits and ASL scripts don't set them.
- **No event.** Setting one raises nothing. An auto splitter can set one on every tick. The server sets it on its connection's thread ([Which thread raises the timer events](livesplit-notes.md#which-thread-raises-the-timer-events-mostly-read-in-source)), adding to the dictionary while the UI thread may be reading it.
- **What's saved.** Only permanent ones are saved in the splits file. Those are the ones loaded from it; one an auto splitter creates during a session isn't saved.
- **At each split.** `TimerModel.Split()` copies every custom variable's value into the split's `CustomVariableValues`.
- **Display.** The Text component shows `CustomVariableValue(name)`, or `-` when the variable isn't set.

**Plan.**
- In `Update()`, compare the values with those last written, every frame, as `SplitNamesChanged` does for split names.
- Copy the dictionary in a `try`, and catch `InvalidOperationException`, the error a change from the server's thread during the copy would throw. Treat a caught copy as changed, and copy again next frame.
- Write only `CustomVariables\` when they change, with its own check like `TimerFiles.HasTimerChanged`, rather than a full rewrite. A value that changes every frame must not rewrite every file.
- Name the files with `RunInfoFiles.VariableFileName`, made internal.
- Add the folder to `OUTPUT_FOLDERS`.
- Decide whether to write `-` for a variable with no value, as the Text component does.

**Verify.**
- Tests: set a custom variable through `SetCustomVariable` with no event, and check that only `CustomVariables\` is written. Change one every frame, and check that the other files aren't rewritten.
- In LiveSplit, or headless through LiveSplit's own `CommandServer`: send `setcustomvariable` through the pipe.

**Docs.**
- output-files.md and README.md: the new folder.
- architecture.md: the change check.
- CHANGELOG.md: under Unreleased.
