# Performance

The index is [TODO.md](TODO.md).

GEN edits `GenerateSubsplitLevels`, as do several items in [Defects.md](Defects.md).

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

## ATM — File writes aren't atomic

OBS could read a half-written or empty file.

**Evidence.** Read. No flicker has been seen yet.

**Plan.** First check whether flicker actually happens in OBS. If it does, write to a temp file in the same folder, then swap it in with `File.Replace`. The one place to change is `FileQueue.WriteFile` ([FileQueue.cs](../UI/Components/FileQueue.cs)), which runs on the background drain, so the extra I/O doesn't cost LiveSplit's UI thread anything.

**Verify.** An OBS text source on a file that is rewritten rapidly shows no blank frames.

**Docs.** None.
