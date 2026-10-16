# Performance

The index is [TODO.md](TODO.md).

GEN changes `GenerateSubsplitLevels`. DSH and SOF in [Defects.md](Defects.md) edit `GenerateNames`, which it calls.

## GEN — GenerateSubsplitLevels runs twice

`CalculateSegments` ([OutputToFileComponent.cs](../UI/Components/OutputToFileComponent.cs)) runs `SplitLevels.GenerateSubsplitLevels` once per timing method. Its `method` parameter is unused, and the second call overwrites the first.

**Evidence.** Read.

**Plan.** Drop the parameter and the second call.

**Verify.** Output unchanged for a full run with subsplits.

**Docs.** architecture.md "Per-level data", which notes the double build.

## ATM — File writes aren't atomic

`FileQueue.WriteFile` writes with `File.WriteAllText`, which empties the file and then writes it. An OBS text source that reads the file in between shows nothing for 1–2 s. As measured, that happens about once every 4 hours of running timer for each OBS text source on a timer file. Other files are almost never affected.

**Parked, lowest priority.** On 2026-10-15 it was judged not worth fixing for now. It is kept here in case blanks turn up in OBS.

**Evidence.**
- **How OBS reads a file.** Read in the OBS 32 source (`plugins/obs-text/gdiplus/obs-text.cpp`, `libobs/util/platform.c`), not run.
  - `TextSource::Tick` checks the file's mtime once a second (`st_mtime`, whole seconds). When it has changed, the file is read on the next check, about 1 s later.
  - It reads with `_wfopen(path, "rb")`, which shares read and write but not delete, then `os_fread_utf8`. An empty file or a failed open gives an empty source.
  - The write that caused an empty read leaves a newer mtime, so OBS reads the file again within about 2 s.
  - So a blank needs a rewrite of the same file at the moment OBS reads it. Only the timer files are rewritten that often: once a second while the timer runs, so OBS reads them on every check, about 3,540 times an hour at 60 fps. Other files change on events, and OBS reads them a second after each change, when another write is unlikely.
- **Reproduced 2026-10-15, headless.** The OBS-style reader in [testing.md](testing.md#reading-files-as-obs-does) read a timer-sized file while the built `FileQueue` rewrote it. NTFS on the NVMe disk that also holds the real output folder, Defender real-time protection on.
  - **Rewritten back to back for 10 s** (two runs): 8–10% of reads came back empty or short. None failed to open, none mixed two values, and no write failed, so an open OBS handle never blocks today's write.
  - **Rewritten 10 times a second, reads starting at random moments** (180 s, 1,800 writes): 491 of 726,347 reads came back empty or short. So each write leaves the file unreadable for about 68 µs.
  - Weighting back-to-back reads by their duration gives the same 68 µs. Counting them unweighted gives 15–26 µs, at both 10 writes a second and 1. That understates it, because a read that hits a write takes 4–8 times longer.
  - 68 µs × 3,540 reads an hour is 0.24 blanks an hour: about one every 4 hours per OBS text source on a timer file.
  - A whole `File.WriteAllText` call takes about 3.8 ms (median), nearly all of it outside the unreadable window.

**Plan.** Leave it until blanks are actually seen in OBS, then make this change in `FileQueue.WriteFile` ([FileQueue.cs](../UI/Components/FileQueue.cs)), which runs on the background drain:
1. Write `<name>.tmp` in the same folder.
2. Rename it over the target with `MoveFileEx(MOVEFILE_REPLACE_EXISTING)` by P/Invoke. .NET Framework 4.8's `File.Move` can't overwrite, and `File.Replace` needs the target to exist and has partial-failure modes.
3. The rename fails while a reader has the file open, since `_wfopen` doesn't share delete. Retry a few times with a short sleep.
4. If it still fails, fall back to today's direct write, so a reader that keeps a file open never stops its updates.
5. Delete the temp file whenever the rename doesn't happen.

**Verify.**
- The ATM regression check in testing.md finds no empty, short or failed reads, and there are no write errors.
- The target's last-write time changes on every write, because OBS only rereads a file when its mtime is newer.
- Time `WaitForFileWrites()` per split before and after, since every file then costs a temp write and a rename.

**Docs.** architecture.md, the `FileQueue` `Drain` bullet. CHANGELOG.md, under Fixed: an OBS text source on a timer file sometimes went blank for a second or two.
