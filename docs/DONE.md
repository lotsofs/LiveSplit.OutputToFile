# Done

Finished [TODO](TODO.md) items, newest first. A code can be reused once its item is finished, so it may appear here more than once; the date tells them apart. An item's detail section (evidence, plan) was deleted when it was finished; to read it, find the commit that removed it with `git log -S "## CODE " -- docs/`.

| Date | Code | Area | What was done |
|---|---|---|---|
| 2026-10-15 | SAV | Settings | "Completed splits" (`OutputCompletedSplits`) is now saved to the layout, read back with a fallback of `false`, and part of the settings hash. Verified headless: it reloads as `True`, the hash changes when it's toggled, and old layouts load it as `False`. |
