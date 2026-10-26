# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. An item that a test can show gets a test that asserts the correct output, skipped with the item's code until it's fixed ([testing.md](testing.md#adding-a-test)). One that can't be a test gets a row, marked with its code, in testing.md's [manual regression checks](testing.md#manual-regression-checks).

## SSN — A split renamed through LiveSplit's server shows only at the next split

LiveSplit's server commands `setsplitname` and `setcurrentsplitname` rename a split while LiveSplit runs. The name files (`Name.txt`, `Name_Raw.txt`, `Names*.txt`) keep the old name until the next start, split, skip, undo or reset. A rename that adds or removes dashes or a `{section}` also changes the levels, which stay as they were until then.

**Evidence.**
- Reproduced 2026-10-23 in LiveSplit 1.8.37 with release 8:
  - The test sent `setcurrentsplitname -renamed by server` through the named pipe while `{Area B}-b` was current.
  - 400 ms later, `CurrentSplit\AllSplits\Name.txt` still said `b`, and `Name_Raw.txt` said `{Area B}-b`.
  - The next split showed the new name in `SplitList\`.
  - A headless replay of the same commands gave the same files.
- Read in source: the server sets `State.Run[index].Name` and `Run.HasChanged`, and raises no event (`CommandServer.ProcessMessage`, LiveSplit `683cd60`). None of the cache keys in `Update()` covers split names.
- Skipped test: `ChangeDetectionTests.RenamingASplitWithoutAnEventRewritesTheFiles`.

**Plan.** In `Update()`, notice a renamed split cheaply, without a full rewrite every frame:
- Keep the run's names as of the last rewrite in an array.
- Each frame, compare each split's `Name` with it by reference. A rename assigns a new string, so a reference comparison is enough, and costs one comparison per split.
- A difference sets a cache key.
- Measure `Update()` without a rewrite on the 1,317-split GTA SA file, before and after.
- Remove the test's `Skip`.

**Verify.** The test passes. In LiveSplit, `setcurrentsplitname` shows in `CurrentSplit\…\Name.txt` within a frame.

**Docs.**
- architecture.md: the cache keys.
- livesplit-notes.md: that the server renames splits without an event.
- CHANGELOG.md: under Unreleased, Fixed.
