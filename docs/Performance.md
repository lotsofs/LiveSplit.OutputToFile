# Performance

The index is [TODO.md](TODO.md).

GEN edits `GenerateSubsplitLevels`, as do several items in [Defects.md](Defects.md).

## CAT — CompletedSplits makes long runs split slowly

Every split rewrites `CompletedSplits\` for every level and timing method, one line per completed member. `WriteSplitListLevel` ([SplitListFiles.cs](../UI/Components/SplitListFiles.cs)) builds about fifteen strings per call with `+=` per line, so each call copies on the order of n² characters. All of this runs on the UI thread, inside `Split()`. Only the file writes are on the background thread.

**Evidence.** Measured 2026-10-15, headless: a 1,000-split run with a subsplit level averaged 10.1 ms per `Split()` with `CompletedSplits` on, and 0.2 ms with it off. The cause, string concatenation, was read, not profiled. The largest splits file in `D:\Repositories\Stream-Resources\Splits` has 1,317 splits.

**Plan.** Profile first, to confirm where the time goes. If it is the concatenation, build each file with a `StringBuilder`, or a `string.Join` over per-line arrays.

**Verify.** The same 1,000-split timing drops well below 10 ms. Output is identical to the old build.

**Docs.** None.

## GEN — GenerateSubsplitLevels runs twice

`CalculateSegments` ([OutputToFileComponent.cs](../UI/Components/OutputToFileComponent.cs)) runs `SplitLevels.GenerateSubsplitLevels` once per timing method. Its `method` parameter is unused, and the second call overwrites the first.

**Evidence.** Read.

**Plan.** Drop the parameter and the second call.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** architecture.md "Per-level data", which notes the double build.

## ATM — File writes aren't atomic

OBS could read a half-written or empty file.

**Evidence.** Read. No flicker has been seen yet.

**Plan.** First check whether flicker actually happens in OBS. If it does, write to a temp file in the same folder, then swap it in with `File.Replace`. The one place to change is `FileQueue.WriteFile` ([FileQueue.cs](../UI/Components/FileQueue.cs)), which runs on the background drain, so the extra I/O doesn't cost LiveSplit's UI thread anything.

**Verify.** An OBS text source on a file that is rewritten rapidly shows no blank frames.

**Docs.** None.
