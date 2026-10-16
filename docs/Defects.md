# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. Where [testing.md](testing.md#regression-checks) has a regression check for an item, its row is marked with the item's code; update that row when the item is fixed.

## THR — Timer events arrive off the UI thread

Some callers call `TimerModel` from a thread other than the UI thread ([livesplit-notes.md](livesplit-notes.md#which-thread-raises-the-timer-events-mostly-read-in-source)):
- LiveSplit's server, through its pipe, TCP or WebSocket. This holds in every version, including the current 1.8.37.
- Before LiveSplit 1.8.36, also an Auto Splitting Runtime (`.wasm`) script activated in Edit Splits.

The component's event handlers then run on that thread while `Update()` runs on the UI thread, and nothing but `FileQueue` is locked.

- On a start, finish or reset, the next `Update()` sees the phase change and rebuilds the shared `SplitLevels` in place with `CalculateSegments()`. A handler running at the same moment reads the names and levels as they are replaced. On start and reset it rebuilds them too.
- `TimerFiles` shares its segment-start cache the same way.
- What a handler's exception does depends on its thread:
  - On the server's pipe or TCP thread it isn't caught, so it ends LiveSplit. On the WebSocket thread, what websocket-sharp does with it wasn't checked.
  - On the ASR timer thread, before 1.8.36, it goes back through the auto splitter's native runtime; what happens there wasn't checked.
  - An exception in `Update()` is caught and logged by LiveSplit.

This setup's `Components\` folder holds `sar_auto_splitter.wasm`, LiveSplit's listed auto splitter for Portal 2 (SAR), which is activated in Edit Splits.
- Until 2026-10-16 the install was the older LiveSplit at `e16f72c`, so SAR took the off-thread path.
- It is now 1.8.37, where ASR runs these scripts on the UI thread. That is read in source and checked by reflection, not run, and no release note mentions it.
- So this setup is exposed only if something sends commands to LiveSplit's server.

Other components share the exposure to LiveSplit's own state changing off the UI thread, but they do their work in `Update()` and drawing, on the UI thread. The built-in Splits component's handlers only reset `ScrollOffset` (read in source at the versions 1.8.37 and `e16f72c` pin). This component is hit harder because its handlers do the work themselves, rebuilding and reading the levels that `Update()` also rebuilds.

Release 6 wasn't affected: its handlers only set flags, and `Update()` did all the work. The handlers began writing in `468a15e`, after release 6, so the fix needs no changelog entry.

**Evidence.** Reproduced 2026-10-16, headless, against LiveSplit at `e16f72c`, with a Debug build and every output on (see the THR row in [testing.md](testing.md#regression-checks)). The race is in the component, so these numbers stand for 1.8.37's server too:

- Commands sent through LiveSplit's server pipe raised `OnStart`, `OnSplit` and `OnReset` on the connection's thread, not the main one. Seen at `e16f72c` and again at 1.8.37.
- Each action ran on a `System.Timers.Timer` thread, like ASR's, while the main thread called `Update()` every 25 ms, as LiveSplit does at `RefreshRate` 40. An attempt is a start, the splits and a reset.

  | Run | Attempts | Handler exceptions | `Update()` exceptions | Resets that left wrong files |
  |---|---|---|---|---|
  | Base run, 5 splits, 2 levels | 1,000 | 3 (finish 2, reset 1) | 1 | 1 |
  | 40 splits, 3 levels | 250 | 2 (finish 2) | 0 | 0 |

  - The handler exceptions were `NullReferenceException` and `ArgumentOutOfRangeException` in `AppendNames`, `LiveRunTime` and `GenerateNames`. They came from reading names and levels that `Update()` was rebuilding. `Update()` threw once, in `GenerateNames`.
  - The wrong files: after one reset, both segment timers showed `-0:51` instead of `0:00` until the next start.
  - Overall, a handler threw at 4 of 1,250 finishes (about 1 in 300) and 1 of 1,250 resets. With the server, a throw at the finish would close LiveSplit before the reset that records the run.
  - A handler overlapped an `Update()` in 0.4–1.6% of finishes, and about half of those overlaps threw. Resets overlapped more often (3–9%), because the handler runs longer, but rarely threw.
- The same schedule with every action on the main thread gave no exceptions and no wrong files: 700 actions of the base run and 252 of the 40-split run.

**Plan.**
- Add one lock object to the component, and hold it in every event handler and around the body of `Update()`. Then only one thread at a time uses the levels, the writers and their caches.
- In the constructor, subscribe to the events last, so an event from another thread can't reach a handler before the writers exist.
- Only `Update()` touches the settings control, as now.
- The lock doesn't cover LiveSplit's own state, which the caller changes outside it. So an `Update()` can still read a split half made. The handler waits for that `Update()` and then writes from the finished state, and `FileQueue` keeps the latest contents, so the files end right.
- Marshalling the handlers to `State.Form` instead wouldn't work headless, where `Form` is null, and could block the caller against the UI thread.

**Verify.** Repeat the testing.md check with the fix:
- No handler exceptions and no wrong files in either run. A rare `Update()` exception from a half-made split may remain; LiveSplit logs it.
- Idle `Update()` cost stays about the same. An uncontended lock costs tens of nanoseconds.
- With every action on the main thread, the base run's output is unchanged.

**Docs.**
- livesplit-notes.md: drop the THR link from "Which thread raises the timer events".
- architecture.md "Lifecycle": the lock and the order of subscribing.
- CLAUDE.md "Must-know before editing": a bullet saying the handlers can run off the UI thread, so anything new they or `Update()` share must be used under the lock.
- testing.md: the THR regression check.
