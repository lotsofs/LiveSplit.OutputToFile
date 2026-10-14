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
- One instance can serve unrelated keys. This component shares one cache between `HasTimerChanged` and the change detection in `Update()`.

## Exceptions (verified)

An exception thrown by a state-event handler comes out of the `TimerModel` call that raised the event, for example `Split()`. By then the split has already been recorded. Any handlers registered after this component's won't run.

## Running LiveSplit code outside LiveSplit (verified)

- `new StandardSettingsFactory().Create()` throws `NullReferenceException` outside the app. Use `new LiveSplit.Options.Settings()` instead.
- `new StandardLayoutSettingsFactory().Create()`, `new Layout()`, `new Run(new StandardComparisonGeneratorsFactory())` and `new LiveSplitState(run, null, layout, layoutSettings, settings)` all work with a `null` form.

## Not verified

These come from LiveSplit conventions and from this component working in practice:

- LiveSplit calls `Update()` on every layout component on each refresh, including zero-size components like this one.
- `SetSettings` is called after the constructor when a layout loads.
- LiveSplit compares `GetSettingsHashCode()` values to decide whether the layout has unsaved changes.
- While LiveSplit is running, it holds a lock on the component DLL in `Components\`.
- Whether every caller of `TimerModel` (hotkeys, auto splitters, the LiveSplit Server component) does so on the UI thread. If some don't, `WriteSplits()` can run at the same time as `Update()`. Tracked as [THR](Defects.md#thr--timer-events-may-arrive-off-the-ui-thread).
