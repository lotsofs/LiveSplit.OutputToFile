# TODO

Open work in this repo, by three-letter code. Each code links to its section in the detail doc named in the group heading: what's wrong or wanted, the evidence, a plan, how to verify it, and which docs to update. Within a group, rows are in suggested order of work. Each detail doc notes the dependencies between its plans.

When an item is finished, delete its row here and its section in the detail doc, and log it in [DONE.md](DONE.md). When work turns up a new defect that isn't being fixed now, add both. An item decided not to be worth fixing moves to the last group, Won't fix.

## Defects — [Defects.md](Defects.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [SSN](Defects.md#ssn--a-split-renamed-through-livesplits-server-shows-only-at-the-next-split) | Change detection | A split renamed through LiveSplit's server (`setsplitname`, `setcurrentsplitname`) keeps its old name in the files until the next split, start, skip, undo or reset: the server raises no event | Low |

## Performance — [Performance.md](Performance.md)

| Code | Area | Summary | Severity |
|---|---|---|---|

## Cleanup — [Cleanup.md](Cleanup.md)

| Code | Area | Summary | Severity |
|---|---|---|---|

## Features and limitations — [Features.md](Features.md)

| Code | Area | Summary | Severity |
|---|---|---|---|
| [SCL](Features.md#scl--the-split-list-doesnt-follow-livesplits-scrolling) | Split list | Scrolling in LiveSplit (mouse wheel or hotkeys) doesn't move `SplitList\`; make every level's list follow it, one of its own lines per notch, reset on split, start, reset, skip and undo | Medium |
| [CRN](Features.md#crn--a-ticked-comparison-renamed-in-edit-splits-is-no-longer-written) | Comparisons | Renaming a ticked comparison in Edit Splits empties its folder and leaves the old name ticked; follow LiveSplit's `ComparisonRenamed` event, as its own components do | Low |
| [CLO](Features.md#clo--files-keep-their-contents-after-livesplit-closes) | File writing | When LiveSplit closes, every file keeps its last contents; GitHub issue #5 asks for the split list to be cleared. Decide what the files show while LiveSplit is closed | Low |

## Project and README — [Project.md](Project.md)

| Code | Area | Summary | Severity |
|---|---|---|---|

## Won't fix — [WontFix.md](WontFix.md)

Known and measured, but not worth fixing. Kept with their evidence and plan, in case that changes.

| Code | Area | Summary | Severity |
|---|---|---|---|
| [ATM](WontFix.md#atm--file-writes-arent-atomic) | File writing | Writes aren't atomic. Measured: an OBS text source on a timer file goes blank for 1–2 s about once every 4 h of running timer. Revisit only if blanks are seen | Low |
