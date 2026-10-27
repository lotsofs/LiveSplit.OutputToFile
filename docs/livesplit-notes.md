# LiveSplit behavior this component relies on

Facts about LiveSplit that the code depends on. Most of them can't be seen from this repo.

**Verified** facts were observed by driving the installed `LiveSplit.Core.dll` from a headless test program (see [testing.md](testing.md)).
- The installed copy is `D:\Programs\Streaming\LiveSplit`: LiveSplit 1.8.37, ProductVersion `1.0.0+683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1`. LiveSplit doesn't stamp a release number into its DLLs, so the commit hash is the version; the 1.8.37 tag points at that commit.
- The facts were first checked on 2026-10-14 at `e16f72c` (May 2024), then all of them again on 2026-10-16 after the update to 1.8.37. Only `PlatformName` had changed.
- Re-check these facts after updating LiveSplit.
- From 1.8.30, LiveSplit is built for .NET Framework 4.8.1, so a project referencing its DLLs must target 4.8.1 too; 1.8.29 and earlier target 4.6.1. Read in LiveSplit's `Directory.Build.props` at those tags on 2026-10-21. The installed 1.8.37 was the first 4.8.1 build used here.

## Timer state by phase (verified)

| Phase | `CurrentSplitIndex` | `CurrentSplit` | `CurrentTime` |
|---|---|---|---|
| `NotRunning` | −1 | — | Real time is 0, whatever the splits' "Start timer at" (`Run.Offset`; checked 2026-10-23 with 5:00). Game time is `null` until game time is first initialized, then `0` after a reset. |
| `Running` | Index of the split in progress | that segment | live |
| `Paused` | unchanged | unchanged | Real time frozen at the pause |
| `Ended` | `Run.Count` | `null` | Frozen at the final time |

- LiveSplit's Timer component doesn't show `CurrentTime` while not running: it shows `Run.Offset`, for either timing method (read in source, `GetTime` in LiveSplit.Timer `6c9e478`). `Timers\` does the same.
- Game time is `null` until `TimerModel.InitializeGameTime()` runs, which is usually done by an auto splitter or load remover. Every `GameTime` file shows `-` until then, except the timers before the start.
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
| `OnReset` | `NotRunning`, index −1. PB split times and best segments are **already updated** if the run improved them, and the attempt is already in `AttemptHistory`. The `TimerPhase` argument is the phase *before* the reset. The segment history already has the attempt too, and `Run.FixSplits()`, which tidies it, runs only after the handlers (both read in source). |

## Comparison data (verified)

- `PersonalBestSplitTime` and `BestSegmentTime` **don't change during a run**. LiveSplit updates them on reset, before `OnReset` fires. Comparisons made during the run, including in the finished state, therefore use the values from before the run. The [segment history](#segment-history-partly-verified) also gains the attempt only on reset, and the generated [comparisons](#comparisons-read-in-source-partly-checked-headless) are regenerated only after a reset or an edit (read in source).
- `AttemptHistory` gets one entry per reset attempt. Unfinished attempts have a `null` time.
- `Run.Metadata` is never null. With no platform set, `PlatformName` depends on where the run came from:
  - `""` in a run loaded from a splits file (`StandardFormatsRunFactory`);
  - `null` in a new `Run`, and in the blank run of new or closed splits (`StandardRunFactory`).

  At `e16f72c` a new `Run` had `""`. The component writes either as an empty `PlatformName.txt`.

## Segment history (partly verified)

The component takes the best segments of sections from the history ([architecture.md](architecture.md#best-segments)). The source was read on 2026-10-21 at `683cd60` (1.8.37): `UpdateSegmentHistory` in [TimerModel.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/TimerModel.cs), `FixSplits` and the import methods in [IRun.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/IRun.cs), `Clean` in [SumOfBest.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/SumOfBest.cs), and the split edits in [RunEditorDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/RunEditorDialog.cs).

Verified headless on 2026-10-21, by dumping the history after resets and edits through `TimerModel` and `Run.FixSplits()`:
- `ISegment.SegmentHistory` is a `SegmentHistory`, a `Dictionary<int, Time>` keyed by attempt index. Attempt indices start at 1.
- On reset, each split reached in the attempt gets an entry: its split time minus the attempt's last *recorded* split time, or minus zero for the first. Splits from the one in progress on are left out.
- A skipped split gets a null entry, and its time is in the next recorded entry. So adding up an attempt's non-null entries for splits 0 … i gives its split time at i, when entry i isn't null.
- `FixSplits()` removes null entries that no recorded entry follows, such as a skip just before a reset.
- Clearing a split's best segment, then `FixSplits()`, removes every entry of that split that has a time for that method. Null entries stay. Nothing is added to the next split, so the attempts have a hole there. `FixSplits()` then sets the best segment again from the PB segment, if the PB has one.

Read in source only:
- A reset that doesn't update the splits (`Reset(false)`) records nothing.
- Clean Sum of Best removes each entry it flags and the user confirms, which leaves a hole.
- Moving a split up or down removes both swapped splits' entries in each attempt where one is null and the other isn't.
- Deleting a split adds each of its entries to the attempt's next non-null entry. Where it had no entry, the next split's entry is removed instead.
- Inserting a split gives it a null entry for every attempt, as if every attempt had skipped it.
- `ImportSegmentHistory` adds the PB as two pseudo-attempts with keys ≤ 0, one per timing method. `ImportBestSegment` adds one entry, on one split, with a new key ≤ 0. `FixSplits()` moves entries whose key is above the highest attempt index to new keys ≤ 0.
- `FixSplits()` raises an entry that is faster than its split's best segment to the best segment. That only happens after a best segment was edited to be slower. The split times rebuilt from such an attempt are then too slow by the difference.
- `FixSplits()` removes an entry with a key ≤ 0 whose times all equal another attempt's entry on the same split.

## Comparisons (read in source, partly checked headless)

The component writes one folder per comparison ([architecture.md](architecture.md#comparisons)). The source was read on 2026-10-22 at `683cd60` (1.8.37): [CompositeComparisons.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/Comparisons/CompositeComparisons.cs), [Run.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/Run.cs), [TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs), [CommandServer.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Server/CommandServer.cs) and [RunEditorDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/RunEditorDialog.cs).

Checked headless on 2026-10-22:
- `segment.Comparisons[name]` gives an empty `Time` for a name the segment doesn't have, rather than throwing. Setting it adds the name.
- `Run.Comparisons` lists the custom comparisons (`CustomComparisons`, Personal Best first), then the generators' names. A new `Run(new StandardComparisonGeneratorsFactory())` has the generators Best Segments and Average Segments.
- With Personal Best selected, every file is the same as when the component read `PersonalBestSplitTime`, which is `Comparisons["Personal Best"]`.

Read in source only:
- A comparison can have any name that doesn't start with `[Race]` and isn't taken. So a name can contain characters that aren't allowed in a folder name.
- `LiveSplitState.CurrentComparison` is a plain property, null until something sets it. Setting it raises nothing. Only the switch hotkeys go through `TimerModel`, which raises `OnSwitchComparisonNext` or `OnSwitchComparisonPrevious` after setting it. Other places set it directly:
  - the right-click menu (`TimerForm.SwitchComparison`, which falls back to Personal Best for a name the run doesn't have);
  - the server's `setcomparison`, on the server's thread, with any name, unchecked;
  - Edit Splits, when the selected comparison is renamed or removed.
- Generated comparisons are filled by `TimerForm.RegenerateComparisons()`, in LiveSplit's UI code, not by `TimerModel`. Outside LiveSplit, call each generator's `Generate(state.Settings)`. It runs:
  - in TimerForm's `OnReset` handler;
  - when splits are opened (`SetRun`);
  - after each change in Edit Splits, just before `CallRunManuallyModified()`;
  - when the Sum of Best mode or the enabled generators change in LiveSplit's Settings dialog.
  
  Nothing regenerates them during a run.
- At startup, `TimerForm.Init` loads the layout before it subscribes to `OnReset`. So this component's reset handler runs before TimerForm's, which regenerates the comparisons.
  - When the reset comes from the UI thread, both handlers finish before the next `Update()`.
  - When it comes through the server, TimerForm regenerates on the server's thread while `Update()` may already be rewriting.
  - A layout loaded later subscribes after TimerForm.

## Time formatters (verified)

The component formats times and differences with `LiveSplit.TimeFormatters` in `LiveSplit.Core.dll` ([architecture.md](architecture.md#files)). Read in the source at `683cd60` (1.8.37), [GeneralTimeFormatter.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/TimeFormatters/GeneralTimeFormatter.cs). Each fact below was also checked headless on 2026-10-22, by formatting a table of values with the installed DLL.

- `GeneralTimeFormatter.Format` truncates, through `TimeSpan.ToString` with `f`, `ff` or `fff`.
- Negatives get `TimeFormatConstants.MINUS`, the Unicode minus `−` (U+2212), not `-`. Zero gets no sign, even with `ShowPlus`.
- `TimeAccuracy` has Seconds, Tenths, Hundredths and Milliseconds.
- `RegularTimeFormatter(accuracy)` shows minutes (`DigitsFormat.SingleDigitMinutes`): `0:05`, `1:02:05`. It writes a null as `0`, or `0.0` and so on (`NullFormat.ZeroWithAccuracy`), so the component sets `NullFormat.Dash` to get `-`.
- `DeltaTimeFormatter` is Tenths, with `ShowPlus` and `DropDecimals`: no decimals from a minute on, whatever the accuracy (`+1:05`). Its nulls are `-`.

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
- `StandardFormatsRunFactory` refuses a splits file without splits: it throws "Run factory created a run without at least one segment" (seen 2026-10-22).

## How LiveSplit finds the component (verified)

- `ComponentManager.LoadAllFactories` loads every DLL in `<BasePath>\Components\`. For each one it creates the factory named by `[assembly: ComponentFactory(typeof(…))]`, which here is in `Properties\AssemblyInfo.cs`. It keys the factory by the DLL's file name, `LiveSplit.OutputToFile.dll`.
- `ComponentManager.LoadLayoutComponent(path, state)` creates a component from that file name, and loads the factories itself on first use. LiveSplit's layout loader passes it each component's `<Path>` from the layout file (not checked against a saved layout here).
- So namespaces and class names can change without breaking saved layouts, but renaming the DLL would break them. Seen 2026-10-21: after the classes moved namespace, a headless program with `BasePath` set to a folder holding `Components\LiveSplit.OutputToFile.dll` got the factory and the component this way.

## Update check (read in source, seen in LiveSplit)

Read on 2026-10-21 in the LiveSplit source at `683cd60` (1.8.37) and on master: [UpdateHelper.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Updates/UpdateHelper.cs), and `Updater.cs`, `Update.cs` and `FileChange.cs` in [src/UpdateManager](https://github.com/LiveSplit/LiveSplit/tree/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/UpdateManager). [releasing.md](releasing.md) has the release steps that follow from these facts.

- At startup, `TimerForm.CheckForUpdates` checks LiveSplit itself, its updater, and every factory in `ComponentManager.ComponentFactories` (each component DLL in `Components\`, [above](#how-livesplit-finds-the-component-verified)).
  - The check runs on a background task, and each factory type is checked only once per run of LiveSplit.
  - It uses the factory's `IUpdateable` members: `UpdateName`, `XMLURL`, `UpdateURL`, `Version`.
- The feed is read with `XmlReader.Create(XMLURL)`. Each child of the root element is parsed as an `<update version="…">`, with `<changelog>` holding `<change>` lines and `<files>` holding `<file path="…" localPath="…" status="Added|Changed|Removed">` (`localPath` is optional).
  - Any exception is swallowed and keeps the entries parsed so far. A feed that can't be fetched therefore just means no update. So does an entry without `<files>` or `<changelog>`, an XML comment, or a one-part `version` such as `8`, for that entry and the ones after it.
- There's an update when any entry's `version` is greater than the factory's `Version` (`System.Version` comparison). The dialog, "New updates are available", lists `UpdateName (highest version)` and the `<change>` lines of every newer entry.
- On Yes, LiveSplit downloads `UpdateManagerV2.exe` and its config from `livesplit.org/update/`, starts it, and kills its own process.
  - The updater downloads each file from `UpdateURL + path` to `localPath` (or `path`), relative to the LiveSplit folder, then starts LiveSplit again.
  - Files are collected by `path` across all the newer entries, and downloaded in feed order.
- The hosted `UpdateManagerV2.exe` was last modified 2026-02-22. Its strings include `localPath`, and it targets .NET Framework 4.8.1, so it can download over GitHub's TLS 1.2.

Checked headless on 2026-10-21 with the installed `UpdateManager.dll` (1.8.37). A test program called `Updater`'s check, version and changelog methods and the internal `PerformUpdate` on local feeds, with `file://` downloads into a scratch LiveSplit folder:
- the version comparisons, the changelog lines, and the parse failures above behaved as described;
- with two entries that both list a file, newest first, the older release's file was installed last and won.

The same program read an XML file from `raw.githubusercontent.com` with `XmlReader.Create`, and downloaded a release asset through GitHub's redirect with `WebClient`.

Seen in LiveSplit 1.8.37 on 2026-10-23, with a build set to version 7.9 in `Components\` and release 8 in the live feed:
- At startup, LiveSplit offered the update. Yes closed LiveSplit, and it opened again by itself, as reported by the person at the machine.
- The DLL in `Components\` was then release 8's GitHub download, byte for byte (SHA-256).
- The next start showed no prompt.

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

## Layout Settings dialog (read in source, size checked headless)

Read in the LiveSplit source at `e16f72c` on 2026-10-15, then again at `683cd60` (1.8.37) on 2026-10-16. Both times the source was matched against the method calls in the installed `LiveSplit.View.dll`. Not checked by running LiveSplit.

- Component settings controls are edited in `LayoutSettingsDialog`, one tab per component, holding the control that `GetSettingsControl` returns ([LayoutSettingsDialog.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/LayoutSettingsDialog.cs)). The same control instance comes back each time the dialog opens.
- When it opens, the dialog saves each component's `GetSettings()`. Cancel calls `SetSettings` with that saved XML on every component, then closes.
- OK only closes. It is the dialog's AcceptButton, so Enter in a textbox clicks it ([LayoutSettingsDialog.Designer.cs:92](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/LayoutSettingsDialog.Designer.cs#L92)).
- There is no CancelButton and no closing handler, so closing with X restores nothing and Escape doesn't cancel.

Checked headless on 2026-10-22 with the installed `LiveSplit.View.dll` (1.8.37), building the dialog without showing it ([testing.md](testing.md#headless)):
- The dialog is 520 × 675 px at 96 DPI, and a component's tab page has a client area of 476 × 561 px.
- A settings control taller than 561 px gives the page a vertical scrollbar, which narrows it to 459 px. A shorter one gives it none.

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
- The server renames splits in place and raises no event: `setsplitname <index> <name>` (index from 0) and `setcurrentsplitname <name>` set the split's `Name` and `Run.HasChanged`, on the server's thread ([CommandServer.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Server/CommandServer.cs)). Unlike the rest of this section, this was run:
  - Reproduced 2026-10-23 in LiveSplit 1.8.37, with `setcurrentsplitname` through the named pipe.
  - Checked headless 2026-10-26, by calling the installed `CommandServer.ProcessMessage` from another thread. Both commands renamed the split. Without a check of the names, the component's files didn't change within 10 frames, so none of the events it counts was raised.

## Closing LiveSplit and changing layouts (read in source)

Read in the LiveSplit source at `683cd60` (1.8.37) on 2026-10-26 ([TimerForm.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/View/TimerForm.cs), [TimerModel.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.Core/Model/TimerModel.cs), [StandardRunFactory.cs](https://github.com/LiveSplit/LiveSplit/blob/683cd60a9f3fe2dc31a5054c6d10f5d71f2e6ae1/src/LiveSplit.View/Model/RunFactories/StandardRunFactory.cs)). Not checked by running LiveSplit. The component's test of closing does the same steps headless.

- **Closing LiveSplit resets the timer** (`TimerForm_FormClosing`), then disposes the layout's components:
  1. `WarnUserAboutSplitsSave()` asks whether to save the splits, if they have unsaved changes. Cancel stops the closing. Otherwise it calls `Model.Reset()`, which also updates the history, the best segments and the PB. In timer-only mode it asks nothing and resets.
  2. `WarnUserAboutLayoutSave` asks whether to save the layout, if it has unsaved changes. Cancel stops the closing, but the timer has already been reset.
  3. It saves LiveSplit's settings, calls `Dispose()` on every layout component, deactivates the auto splitter and stops the server.
- No frame runs between that reset and `Dispose()`. The two prompts set `DontRedraw`, which makes the refresh skip `Update()` ([which thread](#which-thread-raises-the-timer-events-mostly-read-in-source)), and the rest runs on the UI thread without returning to the message loop. The exception is the error box shown when saving the settings fails, which doesn't set `DontRedraw`. So a component sees the `OnReset` but no `Update()` after it.
- `TimerModel.Reset()` does nothing while the timer is `NotRunning`, so closing then raises no event.
- **Close Splits** asks about saving the splits and resets as above. If the layout has anything but a single Timer component, it asks about saving the layout. It then resets again, opens a blank run (no game or category, and one split with an empty name), and switches to a new layout with only a timer (`SetLayout`). That disposes the component, again with no frame between the reset and `Dispose()`.
- **`SetLayout(layout)`** disposes every component of the old layout that the new one doesn't hold, compared by reference. Opening a layout from a file creates new components, so every old one is disposed, including an Output to File that the new layout has too.
- **Edit Layout.** OK disposes the components removed in the editor. Cancel puts back the layout from before (`SetLayout`), which disposes the components added in the editor, and restores the other components' settings with `SetSettings`.

## Not verified

These come from LiveSplit conventions and from this component working in practice:

- `SetSettings` is called after the constructor when a layout loads.
- While LiveSplit is running, it holds a lock on the component DLL in `Components\`.
