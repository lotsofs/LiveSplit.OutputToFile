# Output files

Every file the component writes, what it contains, and when. This reference comes from the `FILE_*` templates in the writer classes (`RunInfoFiles`, `TimerFiles`, `SplitFiles`, `SplitListFiles`) and was checked against a test run that produced 159 files (5 splits, one subsplit level, all outputs on). The README's tree is incomplete ([DOC](Project.md#doc--readme-is-missing-outputs-and-settings), [GTT](Project.md#gtt--readmes-gametime-timer-list-is-incomplete)).

How to read the paths:

- `{level}` is `AllSplits`, `SansSubsplits` or `SansSubsplits_Level{n}`, where `{n}` (2 or more) is the shallowest depth that level leaves out; see [architecture.md](architecture.md#levels). Without subsplits, only `AllSplits` exists.
- `{method}` is `RealTime` or `GameTime`. Every timed file exists once per method.
- "Gold" files exist only under `AllSplits`.
- `-` means no value. Times use the formats in [Formatting](#formatting).
- "Member" means a split that belongs to the level. "Current" and "previous" are relative to that level. For example, the current `SansSubsplits` member while the runner is on a subsplit is the section that subsplit belongs to.

Entries marked ⚠ have a known bug. The code after the ⚠ is its item in [TODO.md](TODO.md).

## Root

Rewritten whenever one of `Update()`'s cache keys changes (run loaded or edited, run started or ended, settings changed, …).

| File | Contents |
|---|---|
| `GameName.txt` | `Run.GameName` |
| `CategoryName.txt` | `Run.CategoryName` |
| `PlatformName.txt` | `Run.Metadata.PlatformName`, empty if not set |
| `AttemptCount.txt` | `Run.AttemptCount`. LiveSplit increments it when a run starts. |
| `FinishedRunsCount.txt` | Number of attempts in history that have a final time, plus 1 while the timer shows a finished run |
| `TotalSplits_{level}.txt` | Number of members in that level |

## `Timers\{method}\` (Timer setting)

Written from `Update()`, but only when the run timer's seconds component changes. Times have no decimals.

| File | Contents |
|---|---|
| `RunTimer.txt` | The current time, or `-` if that method has no time (game time not initialized) ⚠ SEC: can get stuck after a reset |
| `SegmentTimer_{level}.txt` | The current time minus the previous member's split time. Equals the run timer on the level's first member and when the timer isn't running. `-` if the previous member was skipped. ⚠ LAG: shows `0:00` after the finish. |

## `CurrentSplit\{level}\`

| File | Not running | Running / paused | Finished |
|---|---|---|---|
| `Name.txt` | `-` | Current member's display name | `-` |
| `Name_Raw.txt` | `-` | Current member's raw name | `-` |
| `Index.txt` | `-1` | Number of members completed (0-based index of the current one) | `Count` |
| `ReverseIndex.txt` | `Count` | `Count − Index − 1` | `-1` |
| `{method}\SegmentTime.txt` | `-` | PB segment time of the current member | `-` |
| `{method}\RunTime.txt` | `-` | PB split time of the current member | `-` |
| `{method}\GoldTime.txt` (gold) | `-` | Best segment of the current split | `-` |

## `PreviousSplit\{level}\`

"Previous" is the most recently completed member.

| File | Running, nothing completed at this level yet | Running / paused | Finished |
|---|---|---|---|
| `Name.txt`, `Name_Raw.txt` | `-` | Previous member's name | Last split's name |
| `{method}\SegmentTime.txt` | `-` | This attempt's segment time for that member | The last member's segment time |
| `{method}\RunTime.txt` | `-` | This attempt's split time | Final time |
| `{method}\SegmentDifference.txt` | `-` | Segment time − PB segment | Last segment − PB segment |
| `{method}\RunDifference.txt` | `-` | Split time − PB split time | Final time − PB |
| `{method}\GoldDifference.txt` (gold) | `-` | Segment time − best segment | Last segment − best segment |
| `{method}\Sign.txt` | `NotApplicable` | See below | See below |

When the timer isn't running, every file shows `-`, and `Sign.txt` shows `NotApplicable`.

### Sign values

These are the names of the `Signs` enum. "Ahead" and "PB" mean the run delta is negative.

| During the run | At the finish | Meaning |
|---|---|---|
| `NotApplicable` | | No run delta: no PB time, a skipped split, or the timer isn't running |
| `GoldAhead` / `GoldBehind` | `PBGold` / `NoPBGold` | The segment beat the best segment, or the split has no best segment yet. `AllSplits` only. |
| `Ahead` / `Behind` | `PB` / `NoPB` | No segment delta |
| `GainedAhead` / `GainedBehind` | `PBGained` / `NoPBGained` | The segment beat the PB segment |
| `LostAhead` / `LostBehind` | `PBLost` / `NoPBLost` | The segment was slower than the PB segment |

`Undetermined`, `Gained`, `Lost` and `Gold` exist in the enum but are never written.

## `SplitList\{level}\` (Split List setting)

One line per member, for a window of `SplitsBefore + 1 + SplitsAfter` members around the current member. The window is chosen like this:

- If the level has that many members or fewer, the window is the whole level.
- If the timer isn't running, or the current member is among the first `SplitsBefore` members, it's the first members.
- If the run is finished, or the current member is among the last `SplitsAfter` members, it's the last members.
- Otherwise it's centered on the current member.

Every file has the same number of lines, so separate text sources line up. "Completed" lines are members before the current one. "Upcoming" lines are the current member and every member after it. Lines that don't apply are blank.

| File | Line contents |
|---|---|
| `Names.txt` | Display name |
| `Names_Raw.txt` | Raw name |
| `Names_Indent_{n}.txt` (n = 0 … level) | Display name only on lines whose split depth is `n`. One file per indent level. |
| `Highlight_CurrentSplit.txt` | Fill on the current member's line |
| `{method}\Time_Run_Current.txt` | Completed: this attempt's split time |
| `{method}\Time_Run_Upcoming.txt` | Upcoming: PB split time |
| `{method}\Time_Segment_Upcoming.txt` | Upcoming: PB segment time |
| `{method}\Time_Gold_Upcoming.txt` (gold) | Upcoming: best segment |
| `{method}\Delta_Run.txt` | Completed: run delta |
| `{method}\Delta_Segment.txt` | Completed: segment delta |
| `{method}\Delta_Gold.txt` (gold) | Completed: gold delta |
| `{method}\Highlight_Gold.txt` (gold) | Fill on completed lines that are golds (see [Sign values](#sign-values)) |
| `{method}\Highlight_AheadGained.txt` | Fill: run delta < 0 and segment delta < 0 |
| `{method}\Highlight_AheadLost.txt` | Fill: run delta < 0, segment not gained |
| `{method}\Highlight_BehindGained.txt` | Fill: segment delta < 0, run delta not < 0 |
| `{method}\Highlight_BehindLost.txt` | Fill: every other completed line that has a PB time ⚠ SKP: includes skipped splits |

Each completed line has a fill in exactly one highlight file, checked in the table's order from top to bottom. The fill is 43 × `█`.

## `CompletedSplits\{level}\` (Completed splits setting)

The same files as `SplitList`, but the lines cover every completed member: none while the timer isn't running, and all members once the run is finished. `Time_*_Upcoming.txt` and `Highlight_CurrentSplit.txt` are therefore always blank lines.

## Formatting

From `TimeFormatter.cs`. Values are truncated, not rounded.

| Function | Used for | Output |
|---|---|---|
| `DurationString` | Times | `0:05`, `1:05`, `1:02:05`. Minutes are always shown, no decimals. Negatives get a `-` prefix. |
| `DeltaString` | Differences | Always signed. Under 0.1 s: `+.05`. Under 60 s: `+5.2`. Otherwise `+1:05`. Negatives take the same forms: `-.05`, `-5.2`, `-1:05`. |
| `TimerString` | Unused, identical to `DurationString` | |

`null` is written as `-` everywhere.
