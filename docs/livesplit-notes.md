# LiveSplit behavior this component relies on

Facts about LiveSplit that the code depends on. Most of them can't be seen from this repo.

**Verified** facts were observed by driving the installed `LiveSplit.Core.dll` from a headless test program (see [testing.md](testing.md)).
- The installed copy is `D:\Programs\Streaming\LiveSplit`: LiveSplit 1.8.37, ProductVersion `1.0.0+683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1`. LiveSplit doesn't stamp a release number into its DLLs, so the commit hash is the version; the 1.8.37 tag points at that commit.
- The facts were first checked on 2026-10-14 at `e16f72c` (May 2024), then all of them again on 2026-10-16 after the update to 1.8.37. Only `PlatformName` had changed.
- Re-check these facts after updating LiveSplit.
- From 1.8.37, LiveSplit is built for .NET Framework 4.8.1, so a project referencing its DLLs must target 4.8.1 too.

## Timer state by phase (verified)

| Phase | `CurrentSplitIndex` | `CurrentSplit` | `CurrentTime` |
|---|---|---|---|
| `NotRunning` | −1 | — | Real time is 0 with the default start offset; it's expected to equal the offset otherwise, but that wasn't checked. Game time is `null` until game time is first initialized, then `0` after a reset. |
| `Running` | Index of the split in progress | that segment | live |
| `Paused` | unchanged | unchanged | Real time frozen at the pause |
| `Ended` | `Run.Count` | `null` | Frozen at the final time |

- Game time is `null` until `TimerModel.InitializeGameTime()` runs, which is usually done by an auto splitter or load remover. Every `GameTime` file shows `-` until then.
- A skipped split's `SplitTime` stays empty (`null`).
- Undo moves the index back and **clears** the undone split's `SplitTime`. Undo from `Ended` returns the timer to `Running`.

## Event order (verified)

Each event fires **after** the state change, so a handler sees the new state:

| Event | State when the handler runs |
|---|---|
| `OnStart` | `Running`, index 0. `AttemptCount` has already been incremented. |
| `OnSplit` | The completed split's `SplitTime` is set and the index has moved forward. On the last split the phase is already `Ended` and the index is `Run.Count`. |
| `OnSkipSplit` | Index has moved forward; the skipped split's time is empty |
| `OnUndoSplit` | Index has moved back |
| `OnPause` / `OnResume` | Phase is `Paused` / `Running` |
| `OnReset` | `NotRunning`, index −1. PB split times and best segments are **already updated** if the run improved them, and the attempt is already in `AttemptHistory`. The `TimerPhase` argument is the phase *before* the reset. |

## Comparison data (verified)

- `PersonalBestSplitTime` and `BestSegmentTime` **don't change during a run**. LiveSplit updates them on reset, before `OnReset` fires. Comparisons made during the run, including in the finished state, therefore use the values from before the run.
- `AttemptHistory` gets one entry per reset attempt. Unfinished attempts have a `null` time.
- `Run.Metadata` is never null. With no platform set, `PlatformName` depends on where the run came from:
  - `""` in a run loaded from a splits file (`StandardFormatsRunFactory`);
  - `null` in a new `Run`, and in the blank run of new or closed splits (`StandardRunFactory`).

  At `e16f72c` a new `Run` had `""`. The component writes either as an empty `PlatformName.txt`.

## `GraphicsCache` (verified)

- The indexer setter sets `HasChanged` when the key is new or the value differs (by `Equals`, so boxed enums compare by value).
- `Restart()` only clears the flag.
- One instance can serve unrelated keys. This component uses one, for the change detection in `Update()`.

## Exceptions (verified)

An exception thrown by a state-event handler comes out of the `TimerModel` call that raised the event, for example `Split()`. By then the split has already been recorded. Any handlers registered after this component's won't run.

## Which thread raises the timer events (mostly read in source)

`TimerModel` raises each event synchronously on the thread that called it, and takes no lock ([TimerModel.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/TimerModel.cs)). So the thread depends on the caller.

Read on 2026-10-16 in the LiveSplit source at `e16f72c`, then again at `683cd60` (1.8.37). Both reads included the component versions each commit pins (for 1.8.37: ASR `98d91bb`, ASL `9c1e207`, Racetime `051a2bf`). The exceptions are marked in the table. The component's handlers only count events, so the thread they run on doesn't matter to it ([architecture.md](architecture.md#lifecycle)).

| Caller | Thread | How known |
|---|---|---|
| LiveSplit's server ([CommandServer.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Server/CommandServer.cs)). The named pipe `LiveSplit` opens at launch. TCP and WebSocket start from the menu, or at launch through the `ServerStartup` setting. | **Not the UI thread.** For the pipe and TCP: each connection's own reader thread, a foreground thread. For WebSocket: websocket-sharp's `OnMessage` ([WsConnection.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Server/WsConnection.cs)), on that library's thread. Commands go straight to `TimerModel` with no `Invoke`, at both commits and on master. | Reproduced 2026-10-16 at `e16f72c` and at 1.8.37: commands sent through the pipe raised `OnStart`, `OnSplit` and `OnReset` on a foreground thread other than the main one. WebSocket read only. |
| An Auto Splitting Runtime (`.wasm`) script activated in Edit Splits | **Before LiveSplit 1.8.36:** a `System.Timers.Timer` thread. [AutoSplitter.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/AutoSplitter.cs) creates the script with `Create(state, path)`, and that [ASRComponent](https://github.com/LiveSplit/LiveSplit.AutoSplittingRuntime/blob/ce4a5802d1d43126263da67c39e40fe4d2bcdd46/src/LiveSplit.AutoSplittingRuntime/ASRComponent.cs) constructor didn't keep `state.Form`. So the script, and the `Split()` it calls, ran on the timer's thread. **From 1.8.36: the UI thread.** ASR commit `8fb369c` (2025-11-02) keeps the form in that constructor too ([ASRComponent](https://github.com/LiveSplit/LiveSplit.AutoSplittingRuntime/blob/98d91bb713696e6db41633818ccdc7254b01f6ee/src/LiveSplit.AutoSplittingRuntime/ASRComponent.cs)). The fix's only release note is "Fixed an issue with autosplitter settings not working properly when activating an autosplitter in the Splits Editor". The move to the UI thread is a side effect that no note mentions. | Read, and checked by reflection on both installed DLLs. The old constructor didn't set `parentForm`; the new one does, and `UpdateTimerElapsed` → `InvokeIfNeeded` calls `Control.Invoke` when `InvokeRequired`. |
| The Auto Splitting Runtime component added to the layout | UI thread: each step is `Invoke`d onto the form | Read |
| ASL scripts ([Component.cs](https://github.com/LiveSplit/LiveSplit.ScriptableAutoSplit/blob/9c1e207059d80615408750f709309dba87a929ea/src/LiveSplit.ScriptableAutoSplit/Component.cs)) | UI thread: a WinForms `Timer` | Read |
| Hotkeys ([TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs)) | UI thread: a task `Invoke`s each action onto the form | Read |
| The deprecated LiveSplit.Server component ([ServerComponent.cs](https://github.com/LiveSplit/LiveSplit.Server/blob/master/UI/Components/ServerComponent.cs), master) | UI thread: `BeginInvoke` | Read |
| Racetime ([RacetimeChannel.cs](https://github.com/LiveSplit/LiveSplit.Racetime/blob/051a2bf453c585046b0db933f025d3b62f3982e4/src/LiveSplit.Racetime/Controller/RacetimeChannel.cs)) | Async continuations on whatever context `Connect` was called from | Read; the caller wasn't traced |

- `Update()` runs on the UI thread.
  - A background task sleeps `1000 / RefreshRate` ms, then the refresh is `InvokeIfRequired`'d onto the form ([TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs)).
  - The refresh calls `Update()` on every layout component through `UpdateAllComponents`, or on every visible one through `ComponentRenderer.Update` ([ComponentRenderer.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/UI/Components/ComponentRenderer.cs)). Neither skips zero-size components.
  - Exceptions from `Update()` are caught and logged.
  - The installed `settings.cfg` has `RefreshRate` 40, so a frame comes every 25 ms or so.
  - The refresh skips everything while `DontRedraw` is set. LiveSplit sets it only around its own message boxes: error boxes, the "save your splits / layout?" prompts, and "update your best times?" or "save as Personal Best?" around a reset. Minimizing doesn't stop `Update()`.
  - Modal dialogs such as Edit Splits don't stop it either. The refresh is `Invoke`d onto the form, and the dialog's message loop on the UI thread runs it.
- An exception that an event handler throws during a pipe or TCP command isn't caught: `Connection.ReadCommands` wraps only the read ([Connection.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Server/Connection.cs)). Unhandled on a foreground thread, it ends LiveSplit; `LiveSplit.exe.config` has no `legacyUnhandledExceptionPolicy`. Read, not run.
  - `WsConnection.OnMessage` doesn't catch either. Whether websocket-sharp does wasn't checked.

## Running LiveSplit code outside LiveSplit (verified)

- `new StandardSettingsFactory().Create()` throws `NullReferenceException` outside the app. Use `new LiveSplit.Options.Settings()` instead.
- `new StandardLayoutSettingsFactory().Create()`, `new Layout()`, `new Run(new StandardComparisonGeneratorsFactory())` and `new LiveSplitState(run, null, layout, layoutSettings, settings)` all work with a `null` form.
- Loading a splits file (`StandardFormatsRunFactory`) needs the native `livesplit_core.dll` next to the program. LiveSplit ships it in its `x64\` and `x86\` folders.

## How LiveSplit finds the component (verified)

- `ComponentManager.LoadAllFactories` loads every DLL in `<BasePath>\Components\`. For each one it creates the factory named by `[assembly: ComponentFactory(typeof(…))]`, which here is in `Properties\AssemblyInfo.cs`. It keys the factory by the DLL's file name, `LiveSplit.OutputToFile.dll`.
- `ComponentManager.LoadLayoutComponent(path, state)` creates a component from that file name, and loads the factories itself on first use. LiveSplit's layout loader passes it each component's `<Path>` from the layout file (not checked against a saved layout here).
- So namespaces and class names can change without breaking saved layouts, but renaming the DLL would break them. Seen 2026-10-21: after the classes moved namespace, a headless program with `BasePath` set to a folder holding `Components\LiveSplit.OutputToFile.dll` got the factory and the component this way.

## Layout save prompt and the settings hash (read in source)

Read in the LiveSplit source at `e16f72c` on 2026-10-15, then again at `683cd60` (1.8.37) on 2026-10-16. These facts weren't checked by running LiveSplit.

- The "save your layout?" prompt on close depends only on `Layout.HasChanged`. The Layout Editor sets it when:
  - a component is added, removed or moved;
  - the orientation changes;
  - the Layout Settings dialog, where component settings are edited, closes with OK, whether or not anything changed ([LayoutEditorDialog.cs:252](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/LayoutEditorDialog.cs#L252)).
- `GetSettingsHashCode()` plays no part in that prompt.
  - On each refresh, `TimerForm` hashes the layout with `XMLLayoutSaver.CreateLayoutNode(null, null, Layout)`, and redraws when the hash changes ([TimerForm.cs:1292](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs#L1292)).
  - That hash combines the layout settings with every component's `GetSettingsHashCode()`. A component without one contributes its settings XML instead ([XMLLayoutSaver.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/UI/LayoutSavers/XMLLayoutSaver.cs)).
  - For this component, which draws nothing, the hash has no visible effect.
- `SettingsHelper.CreateSetting` returns `value.GetHashCode()` (0 for null), so a bool contributes 0 or 1.
- The layout hash combines values with the same `^` chain as this component, so two changes can cancel out there too. So do the built-in Splits, Subsplits, Timer and Title components, read at `e16f72c` only.

## Layout Settings dialog (read in source)

Read in the LiveSplit source at `e16f72c` on 2026-10-15, then again at `683cd60` (1.8.37) on 2026-10-16. Both times the source was matched against the method calls in the installed `LiveSplit.View.dll`. Not checked by running LiveSplit.

- Component settings controls are edited in `LayoutSettingsDialog`, one tab per component, holding the control that `GetSettingsControl` returns ([LayoutSettingsDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/LayoutSettingsDialog.cs)). The same control instance comes back each time the dialog opens.
- When it opens, the dialog saves each component's `GetSettings()`. Cancel calls `SetSettings` with that saved XML on every component, then closes.
- OK only closes. It is the dialog's AcceptButton, so Enter in a textbox clicks it ([LayoutSettingsDialog.Designer.cs:92](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/LayoutSettingsDialog.Designer.cs#L92)).
- There is no CancelButton and no closing handler, so closing with X restores nothing and Escape doesn't cancel.

## Editing and opening splits (read in source)

Read in the LiveSplit source at `e16f72c` on 2026-10-15, then again at `683cd60` (1.8.37) on 2026-10-16 ([TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs), [RunEditorDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/RunEditorDialog.cs)). Not checked by running LiveSplit. Two facts were checked by reflection on the installed `LiveSplit.Core.dll` at both versions: `LiveSplitState` has the `RunManuallyModified` event and `CallRunManuallyModified()`, and `Run` doesn't override `Equals`.

- Edit Splits (`RunEditorDialog`) edits `CurrentState.Run` in place. After each change it raises `RunEdited`, and `TimerForm` responds with `CallRunManuallyModified()`. A change is any edit to a name, a time, a best segment, the game, the category, the attempt count, the metadata, an icon or a comparison, and any added, removed or moved split.
- Splits can only be added or removed while the timer is `NotRunning`. Other edits are allowed during a run.
- OK only closes the editor. Cancel puts back a copy of the run taken when the editor opened (`SetRun`), then calls `CallRunManuallyModified()`.
- The offset and the auto splitter settings change the run without raising the event.
- Opening splits from a file or a URL calls `SetRun`, then `CallRunManuallyModified()`. At `e16f72c` splits could also be opened from speedrun.com; 1.8.37 no longer has that.
- Close Splits resets the timer, then calls `SetRun` with a blank run, and doesn't raise the event.
  - LiveSplit then stays in timer-only mode. From then on, `TimerForm`'s `OnReset` handler replaces the run with a new blank one on every reset, also without the event.
  - `LiveSplitState.Run` is a plain property, so replacing the run raises nothing by itself.
- LiveSplit keeps calling `Update()` while the Edit Splits dialog is open, so edits reach the files as they're made ([which thread](#which-thread-raises-the-timer-events-mostly-read-in-source)).

## Not verified

These come from LiveSplit conventions and from this component working in practice:

- `SetSettings` is called after the constructor when a layout loads.
- While LiveSplit is running, it holds a lock on the component DLL in `Components\`.
