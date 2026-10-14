# Done

Finished [TODO](TODO.md) items, newest first. A code can be reused once its item is finished, so it may appear here more than once; the date tells them apart. An item's detail section (evidence, plan) was deleted when it was finished; to read it, find the commit that removed it with `git log -S "## CODE " -- docs/`.

"Change" is one line, usable as a commit message. "Why / how" is the cause, the fix and how it was verified.

| Date | Code | Area | Change | Why / how |
|---|---|---|---|---|
| 2026-10-15 | GAI | Sign.txt | Write `GainedAhead`/`GainedBehind` to `Sign.txt` for a non-gold segment that beats PB | It said `LostAhead`/`LostBehind`: the mid-run sign chain in `WriteSplitTimes` lacked the `segmentDelta < 0` branch the finished-run chain has; added it. Verified headless: A1 at 9.5 gives `GainedAhead`; A1 at 11 and A2 at 20.5 give `GainedBehind`. |
| 2026-10-15 | FIN | PreviousSplit | Fix `PreviousSplit` segment time, differences and sign at the finish | The last segment was stored in slot `.Count - 1` of the timing-method dictionary (always 1) instead of the array's last slot. Verified headless on the base run: after the finish, `PreviousSplit\AllSplits\GameTime\` `SegmentTime`/`SegmentDifference`/`GoldDifference`/`Sign` give `0:09`/`-1.00`/`+.00`/`PBGained` (were `-`/`-`/`-`/`PBGold`), and the last lines of `Delta_Segment.txt`/`Delta_Gold.txt` are filled. |
| 2026-10-15 | SAV | Settings | Save the "Completed splits" setting to the layout | `OutputCompletedSplits` is now saved, read back with a fallback of `false`, and part of the settings hash. Verified headless: it reloads as `True`, the hash changes when it's toggled, and old layouts load it as `False`. |
