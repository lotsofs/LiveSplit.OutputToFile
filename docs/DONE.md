# Done

Finished [TODO](TODO.md) items, newest first. A code can be reused once its item is finished, so it may appear here more than once; the date tells them apart. An item's detail section (evidence, plan) was deleted when it was finished; to read it, find the commit that removed it with `git log -S "## CODE " -- docs/`.

| Date | Code | Area | What was done |
|---|---|---|---|
| 2026-10-15 | FIN | PreviousSplit | At the finish the last segment is now stored in the array's last slot instead of slot `.Count - 1` of the timing-method dictionary (always 1). Verified headless on the base run: after the finish, `PreviousSplit\AllSplits\GameTime\` `SegmentTime`/`SegmentDifference`/`GoldDifference`/`Sign` give `0:09`/`-1.00`/`+.00`/`PBGained` (were `-`/`-`/`-`/`PBGold`), and the last lines of `Delta_Segment.txt`/`Delta_Gold.txt` are filled. |
| 2026-10-15 | SAV | Settings | "Completed splits" (`OutputCompletedSplits`) is now saved to the layout, read back with a fallback of `false`, and part of the settings hash. Verified headless: it reloads as `True`, the hash changes when it's toggled, and old layouts load it as `False`. |
