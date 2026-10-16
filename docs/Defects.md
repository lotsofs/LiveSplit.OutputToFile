# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. Where [testing.md](testing.md#regression-checks) has a regression check for an item, its row is marked with the item's code; update that row when the item is fixed.

## THR — Timer events may arrive off the UI thread

Some callers may call `TimerModel` from a thread other than the UI thread; some auto splitters or the LiveSplit Server component might. If so, `WriteSplits()` could run at the same time as `Update()`, and the shared arrays have no locking. The file writes themselves are safe: `FileQueue` locks all of its state.

**Evidence.** Not checked.

**Plan.** Find out which thread raises `OnSplit` and the other timer events when the Server component or an auto splitter drives the timer. If any isn't the UI thread, lock around the `SplitLevels` work or marshal the handlers to the UI thread. Either way, only `Update()` may touch the settings control.

**Verify.** Drive splits through the Server component while logging the thread ID in the event handlers and in `Update()`.

**Docs.** livesplit-notes.md "Not verified" (move the answer into a verified section). architecture.md "Lifecycle", if handlers need to lock or marshal.
