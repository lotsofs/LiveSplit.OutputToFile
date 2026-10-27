# Features and limitations

Things the component doesn't do yet. The index is [TODO.md](TODO.md).

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
