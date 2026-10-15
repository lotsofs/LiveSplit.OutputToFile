# Performance

The index is [TODO.md](TODO.md).

WRT's background writer needs [WER](Defects.md#wer--file-write-errors-are-thrown-into-livesplit)'s error handling first, and could take in ATM. GEN edits `GenerateSubsplitLevels`, as do several items in [Defects.md](Defects.md).

## LSG — CalculateLiveSegment runs once per level

`CalculateLiveSegment` already loops over every level, but `WriteSplitTimes` ([SplitFiles.cs](../UI/Components/SplitFiles.cs)) calls it inside its own per-level loop.

**Evidence.** Read.

**Plan.** Call it once, before the loop. It goes away entirely if [PAU](Defects.md#pau--pausing-or-finishing-wipes-stored-deltas) computes segments on demand.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** None.

## GEN — GenerateSubsplitLevels runs twice

`CalculateSegments` ([OutputToFileComponent.cs](../UI/Components/OutputToFileComponent.cs)) runs `SplitLevels.GenerateSubsplitLevels` once per timing method. Its `method` parameter is unused, and the second call overwrites the first.

**Evidence.** Read.

**Plan.** Drop the parameter and the second call.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** architecture.md "Per-level data", which notes the double build.

## WRT — Changed files are written on the UI thread

Every write happens synchronously inside the LiveSplit event, which runs on the UI thread. `MakeFile` already skips files whose contents haven't changed. But on a split, many files do change: the `SplitList\` window slides and the `CompletedSplits\` list grows, at every level and for both timing methods.

**Evidence.** Measured 2026-10-15 with the headless harness in [testing.md](testing.md): 40 splits, every fourth a section end (two levels), every output on, a 2/2 list window. Each `Split()` took about 38 ms and wrote about 66 of the 159 files.

**Plan.** Move file I/O to a background writer. `MakeFile` keeps its content check, then queues the write, and the worker writes only the latest content for each path. Before starting:

- Do [WER](Defects.md#wer--file-write-errors-are-thrown-into-livesplit) first. An unhandled exception on a background thread ends LiveSplit, so the worker must catch every write error.
- `Dispose` has to flush pending writes.
- The headless harness needs a way to wait until the queue is empty before it reads files.
- See [THR](Defects.md#thr--timer-events-may-arrive-off-the-ui-thread). The worker could also make writes atomic ([ATM](#atm--file-writes-arent-atomic)).

**Verify.** Repeat the 40-split measurement: `Split()` time no longer grows with the number of files written. Snapshots after every step, taken after the queue drains, match a run from before the change.

**Docs.** architecture.md "Writing files" (the last bullet says nothing is written in the background). The writing-files bullet in CLAUDE.md "Must-know before editing". testing.md, on waiting for writes.

## ATM — File writes aren't atomic

OBS could read a half-written or empty file.

**Evidence.** Read. No flicker has been seen yet.

**Plan.** First check whether flicker actually happens in OBS. If it does, write to a temp file in the same folder, then swap it in with `File.Replace`.

**Verify.** An OBS text source on a file that is rewritten rapidly shows no blank frames.

**Docs.** None.
