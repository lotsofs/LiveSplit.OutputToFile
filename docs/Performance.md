# Performance

The index is [TODO.md](TODO.md). Line numbers are as of commit `30d30fa`.

WRT and [WER](Defects.md#wer--file-write-errors-are-thrown-into-livesplit) both change `MakeFile`. GEN edits `GenerateSubsplitLevels`, as do several items in [Defects.md](Defects.md).

## WRT — Every split rewrites about 160 files

All of them are written synchronously, and in LiveSplit that happens on the UI thread, on every split. Each write also calls `Directory.Exists`.

**Evidence.** Measured 2026-10-14 with the headless harness in [testing.md](testing.md): in a 40-split test with one subsplit level and every output turned on, each `Split()` took about 125 ms on this machine.

**Plan.** Ideas, biggest payoff first:

- Remember the last content written to each path and skip unchanged files. Most files don't change on any given split.
- Write each per-method duplicate once. `Names.txt`, `Names_Raw.txt`, `Names_Indent_*.txt` and `Highlight_CurrentSplit.txt` are written for both RealTime and GameTime ([:622-651](../UI/Components/OutputToFileComponent.cs#L622-L651), [:756-785](../UI/Components/OutputToFileComponent.cs#L756-L785)).
- Create directories once per recalculation, not per write.
- Move file I/O to a background writer that keeps only the latest content per path. See [THR](Defects.md#thr--timer-events-may-arrive-off-the-ui-thread) and ATM.

**Verify.** Repeat the 40-split measurement. Diff the output folder against a run from before the change: identical.

**Docs.** architecture.md "Writing files", and the writing-files bullet in CLAUDE.md "Must-know before editing", if `MakeFile` gains a cache or a background writer.

## LSG — CalculateLiveSegment runs once per level

`CalculateLiveSegment` already loops over every level, but `WriteSplitTimes` calls it inside its own per-level loop ([:830](../UI/Components/OutputToFileComponent.cs#L830)).

**Evidence.** Read.

**Plan.** Call it once, before the loop. It goes away entirely if [PAU](Defects.md#pau--pausing-or-finishing-wipes-stored-deltas) computes segments on demand.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** None.

## GEN — GenerateSubsplitLevels runs twice

`CalculateSegments` runs `GenerateSubsplitLevels` once per timing method ([:203-204](../UI/Components/OutputToFileComponent.cs#L203-L204)). Its `method` parameter is unused, and the second call overwrites the first.

**Evidence.** Read.

**Plan.** Drop the parameter and the second call.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** architecture.md "Per-level data", which notes the double build.

## ATM — File writes aren't atomic

OBS could read a half-written or empty file.

**Evidence.** Read. No flicker has been seen yet.

**Plan.** First check whether flicker actually happens in OBS. If it does, write to a temp file in the same folder, then swap it in with `File.Replace`.

**Verify.** An OBS text source on a file that is rewritten rapidly shows no blank frames.

**Docs.** None.
