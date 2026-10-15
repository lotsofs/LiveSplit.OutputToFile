# LiveSplit behavior this component relies on

Facts about LiveSplit that the code depends on. Most of them can't be seen from this repo.

**Verified** facts were observed by driving the installed `LiveSplit.Core.dll` from a headless test program (see [testing.md](testing.md)) on 2026-10-14. The installed copy is `D:\Programs\Streaming\LiveSplit`, ProductVersion `1.0.0+e16f72c3fbae37a722674346cde12995453696ee`. LiveSplit doesn't stamp a release number, so the commit hash is the version. Re-check these facts after updating LiveSplit.

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
- `Run.Metadata` is never null. `PlatformName` is `""` when not set.

## `GraphicsCache` (verified)

- The indexer setter sets `HasChanged` when the key is new or the value differs (by `Equals`, so boxed enums compare by value).
- `Restart()` only clears the flag.
- One instance can serve unrelated keys. This component uses one, for the change detection in `Update()`.

## Exceptions (verified)

An exception thrown by a state-event handler comes out of the `TimerModel` call that raised the event, for example `Split()`. By then the split has already been recorded. Any handlers registered after this component's won't run.

## Running LiveSplit code outside LiveSplit (verified)

- `new StandardSettingsFactory().Create()` throws `NullReferenceException` outside the app. Use `new LiveSplit.Options.Settings()` instead.
- `new StandardLayoutSettingsFactory().Create()`, `new Layout()`, `new Run(new StandardComparisonGeneratorsFactory())` and `new LiveSplitState(run, null, layout, layoutSettings, settings)` all work with a `null` form.

## Layout save prompt and the settings hash (read in source)

Read in the LiveSplit source at the installed commit `e16f72c` on 2026-10-15. These facts weren't checked by running LiveSplit.

- The "save your layout?" prompt on close depends only on `Layout.HasChanged`. The Layout Editor sets it when:
  - a component is added, removed or moved;
  - the orientation changes;
  - the Layout Settings dialog, where component settings are edited, closes with OK, whether or not anything changed ([LayoutEditorDialog.cs:242](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/LayoutEditorDialog.cs#L242)).
- `GetSettingsHashCode()` plays no part in that prompt. On each refresh, `TimerForm` combines the layout settings and every component's `GetSettingsHashCode()` into one value, and redraws when it changes ([TimerForm.cs:1183](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/TimerForm.cs#L1183)). For this component, which draws nothing, the hash has no visible effect.
- `SettingsHelper.CreateSetting` returns `value.GetHashCode()`, so a bool contributes 0 or 1. The layout hash, and the built-in Splits, Subsplits, Timer and Title components, combine values with the same `^` chain as this component, so two changes can cancel out there too.

## Layout Settings dialog (read in source)

Read in the LiveSplit source at the installed commit `e16f72c` on 2026-10-15, and matched against the method calls in the installed `LiveSplit.View.dll`. Not checked by running LiveSplit.

- Component settings controls are edited in `LayoutSettingsDialog`, one tab per component, holding the control that `GetSettingsControl` returns ([LayoutSettingsDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/LayoutSettingsDialog.cs)). The same control instance comes back each time the dialog opens.
- When it opens, the dialog saves each component's `GetSettings()`. Cancel calls `SetSettings` with that saved XML on every component, then closes.
- OK only closes. It is the dialog's AcceptButton, so Enter in a textbox clicks it ([LayoutSettingsDialog.Designer.cs:92](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/LayoutSettingsDialog.Designer.cs#L92)).
- There is no CancelButton and no closing handler, so closing with X restores nothing and Escape doesn't cancel.

## Editing and opening splits (read in source)

Read in the LiveSplit source at the installed commit `e16f72c` on 2026-10-15 ([TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/TimerForm.cs), [RunEditorDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/e16f72c3fbae37a722674346cde12995453696ee/src/LiveSplit.View/View/RunEditorDialog.cs)). Not checked by running LiveSplit. Two facts were checked by reflection on the installed `LiveSplit.Core.dll`: `LiveSplitState` has the `RunManuallyModified` event and `CallRunManuallyModified()`, and `Run` doesn't override `Equals`.

- Edit Splits (`RunEditorDialog`) edits `CurrentState.Run` in place. After each change it raises `RunEdited`, and `TimerForm` responds with `CallRunManuallyModified()`. A change is any edit to a name, a time, a best segment, the game, the category, the attempt count, the metadata, an icon or a comparison, and any added, removed or moved split.
- Splits can only be added or removed while the timer is `NotRunning`. Other edits are allowed during a run.
- OK only closes the editor. Cancel puts back a copy of the run taken when the editor opened (`SetRun`), then calls `CallRunManuallyModified()`.
- The offset and the auto splitter settings change the run without raising the event.
- Opening splits from a file, a URL or speedrun.com calls `SetRun`, then `CallRunManuallyModified()`.
- Close Splits calls `SetRun` with a blank run and doesn't raise the event. `LiveSplitState.Run` is a plain property, so replacing the run raises nothing by itself.
- Not known: whether LiveSplit keeps calling `Update()` while the Edit Splits dialog is open. If it doesn't, the files catch up when the dialog closes.

## Not verified

These come from LiveSplit conventions and from this component working in practice:

- LiveSplit calls `Update()` on every layout component on each refresh, including zero-size components like this one.
- `SetSettings` is called after the constructor when a layout loads.
- While LiveSplit is running, it holds a lock on the component DLL in `Components\`.
- Whether every caller of `TimerModel` (hotkeys, auto splitters, the LiveSplit Server component) does so on the UI thread. If some don't, `WriteSplits()` can run at the same time as `Update()`. Tracked as [THR](Defects.md#thr--timer-events-may-arrive-off-the-ui-thread).
