# Output files

Every file the component writes, what it contains, and when. This reference comes from the `FILE_*` templates in the writer classes (`RunInfoFiles`, `TimerFiles`, `SplitFiles`, `SplitListFiles`, `CustomVariableFiles`) and was checked against the base run in [testing.md](testing.md#the-base-run), which produces 171 files with every output on and no comparison ticked. The README describes the same files for users, in less detail.

How to read the paths:

- `{level}` is `AllSplits`, `SansSubsplits` or `SansSubsplits_Level{n}`, where `{n}` (2 or more) is the shallowest depth that level leaves out; see [architecture.md](architecture.md#levels). Without subsplits, only `AllSplits` exists.
- `{method}` is `RealTime` or `GameTime`. Every timed file exists once per method.
- `{comparison}` is a comparison's folder. The files with it in their path compare with a comparison, and exist once per comparison written:
  - `CurrentComparison`, the comparison selected in LiveSplit, always;
  - each comparison ticked in the Comparisons setting, and with **All custom comparisons** on, every comparison added in Edit Splits. Each folder is named after its comparison without its spaces (`Personal Best` → `PersonalBest`), and with `_` in place of characters a folder name can't have (`A/B` → `A_B`). A comparison whose folder name is taken, such as a custom one named `Current Comparison`, is left out. See [architecture.md](architecture.md#comparisons).
  
  "The comparison" below means that folder's comparison. A name LiveSplit doesn't have, as the server can select, shows `-` in every comparison file.
- "Gold" files compare with a member's best segment at that level. A member that is a single split has that split's best segment, so the gold files in `AllSplits` show LiveSplit's own golds. A section of several splits has the fastest time it took within one attempt, from the splits' history ([architecture.md](architecture.md#best-segments)). It isn't the sum of its splits' golds. A section that no attempt has completed has no best segment, so its first completion is a gold.
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
| `RegionName.txt` | `Run.Metadata.RegionName`, empty if not set |
| `UsesEmulator.txt` | `Emulator` if `Run.Metadata.UsesEmulator`, otherwise empty |
| `Variables.txt` | The variables (`Run.Metadata.VariableValueNames`) on one line, joined with `, `, as LiveSplit adds them to the category in its title: a `Yes` value gives the variable's name without its trailing `?`, `No` gives `No ` and that name, and any other value is written as it is. `Unrestricted, No Glitches`. Variables without a value are left out. Empty with no variables. |
| `AttemptCount.txt` | `Run.AttemptCount`. LiveSplit increments it when a run starts. |
| `FinishedRunsCount.txt` | Number of attempts in history that have a final time, plus 1 while the timer shows a finished run |
| `TotalSplits_{level}.txt` | Number of members in that level |
| `CurrentComparison.txt` | The name of the comparison selected in LiveSplit, the one in the `CurrentComparison` folders |

## `Variables\`

Written with the root files: one file per variable in `Run.Metadata.VariableValueNames`, holding its value as stored. `Glitches?` = `No` gives `Glitches.txt` holding `No`. The order is the dictionary's, which is the order in the splits file unless variables were removed in Edit Splits.

The file name is the variable's name with:
- its trailing `?` dropped, as in `Variables.txt`;
- every character a file name can't hold (`Path.GetInvalidFileNameChars()`, such as `/`, `:` and `"`) replaced with `_`;
- trailing dots and spaces trimmed, which Windows would drop anyway.

Two names that end up the same share a file, and the later variable's value wins. A variable that is removed has its file emptied, as with [outputs turned off](#outputs-turned-off-and-levels-that-are-gone).

## `CustomVariables\` (Custom variables setting)

One file per LiveSplit custom variable (`Run.Metadata.CustomVariables`), holding its value. These are the variables that auto splitters (Auto Splitting Runtime scripts) and tools through LiveSplit's server set, such as a death count. They are unrelated to the speedrun.com variables in `Variables\`. A variable with no value is `-`, as LiveSplit's Text component shows it; LiveSplit adds one like that when the Text component or the server asks for a variable that isn't set. A variable set to an empty value gives an empty file. The order is the dictionary's.

- **When.** Setting a custom variable raises no event, so `Update()` compares their values every frame. A changed value rewrites only these files. A new variable comes with a full rewrite, like every other change to which files exist. Every full rewrite writes them all.
- **Names.** File names follow the rules for `Variables\`: the trailing `?` dropped, characters a file name can't hold replaced with `_`, trailing dots and spaces trimmed. Braces are kept.
- **Gone.** LiveSplit never removes a custom variable, but other splits, opened or put back by Cancel in Edit Splits, have their own. The files of variables they don't have are emptied, as are all of them when the setting is turned off.

## `Timers\{method}\` (Timer setting)

Written from `Update()`, but only when what one of them shows changes: the run timer's or a segment timer's whole second (sign included), or the timer phase. Times have no decimals.

| File | Contents |
|---|---|
| `RunTimer.txt` | The current time, or `-` if that method has no time (game time not initialized). While the timer isn't running, the splits' "Start timer at" time (`0:00` unless set) for both methods, as LiveSplit's timer shows. |
| `SegmentTimer_{level}.txt` | The current time minus the previous member's split time, so it restarts at `0:00` on each split at that level. Equals the run timer on the level's first member and when the timer isn't running. After the finish, the last member's segment time. `-` if the previous member was skipped (after the finish: the member before the last). |

## `CurrentSplit\{level}\`

| File | Not running | Running / paused | Finished |
|---|---|---|---|
| `Name.txt` | `-` | Current member's display name | `-` |
| `Name_Raw.txt` | `-` | Current member's raw name | `-` |
| `Number.txt` | `-` | Current member's number, counting from 1 | `-` |
| `Completed.txt` | `0` | Number of members completed, skipped ones included | `Count` |
| `Remaining.txt` | `Count` | Number of members not completed, the current one included: `Count − Completed` | `0` |
| `After.txt` | `-` | Number of members after the current one: `Remaining − 1` | `-` |
| `{method}\{comparison}\SegmentTime.txt` | `-` | The comparison's segment time for the current member | `-` |
| `{method}\{comparison}\RunTime.txt` | `-` | The comparison's split time for the current member | `-` |
| `{method}\GoldTime.txt` (gold) | `-` | Best segment of the current member | `-` |

## `PreviousSplit\{level}\`

"Previous" is the most recently completed member.

| File | Running, nothing completed at this level yet | Running / paused | Finished |
|---|---|---|---|
| `Name.txt`, `Name_Raw.txt` | `-` | Previous member's name | Last split's name |
| `{method}\SegmentTime.txt` | `-` | This attempt's segment time for that member | The last member's segment time |
| `{method}\RunTime.txt` | `-` | This attempt's split time | Final time |
| `{method}\{comparison}\SegmentDifference.txt` | `-` | Segment time − the comparison's segment | Last segment − the comparison's |
| `{method}\{comparison}\RunDifference.txt` | `-` | Split time − the comparison's split time | Final time − the comparison's |
| `{method}\GoldDifference.txt` (gold) | `-` | Segment time − best segment | Last segment − best segment |
| `{method}\{comparison}\Sign.txt` | `NotApplicable` | See below | See below |

When the timer isn't running, every file shows `-`, and `Sign.txt` shows `NotApplicable`.

### Sign values

These are the names of the `Signs` enum. "Ahead" means the run delta is negative: the split time beat the comparison's. At the finish, the value is the one it would be during the run, with `Finished` in front. The first row that applies wins.

| During the run | At the finish | Meaning |
|---|---|---|
| `NotApplicable` | `FinishedNotApplicable` | No run delta: the comparison has no time for the split, the split was skipped, or the timer isn't running |
| `GoldAhead` / `GoldBehind` | `FinishedGoldAhead` / `FinishedGoldBehind` | The segment beat the member's best segment, or the member has none yet |
| `Ahead` / `Behind` | `FinishedAhead` / `FinishedBehind` | No segment delta |
| `GainedAhead` / `GainedBehind` | `FinishedGainedAhead` / `FinishedGainedBehind` | The segment beat the comparison's segment |
| `LostAhead` / `LostBehind` | `FinishedLostAhead` / `FinishedLostBehind` | The segment wasn't faster than the comparison's segment |

In the Personal Best folder, `FinishedAhead`, `FinishedGoldAhead` and so on mean the run beat the PB.

## `SplitList\{level}\` (Split List setting)

One line per member, for a window of `SplitsBefore + 1 + SplitsAfter` members around the current member. The window is chosen like this:

- If the level has that many members or fewer, the window is the whole level.
- If the timer isn't running, or the current member is among the first `SplitsBefore` members, it's the first members.
- If the run is finished, or the current member is among the last `SplitsAfter` members, it's the last members.
- Otherwise it's centered on the current member.

Scrolling the mouse wheel over LiveSplit's window moves a scrolled-to split, one split of the run per notch. It starts from the current split, or from the first split before the start and the last after the finish, and stops at the first and last split. Every level then chooses its window as above, with the member that holds the scrolled-to split in place of the current member. In `SansSubsplits`, for example, that member is the section the split is in.
- Near the start and end of the run a notch may not move the window, and in a coarser level the window moves only when the scrolled-to split enters another member.
- A start, split, skip, undo or reset goes back to the current split.
- A current member scrolled out of the window has no line in `Highlight_CurrentSplit.txt`.
- `Highlight_ScrolledToSplit.txt` fills the line of the member that holds the scrolled-to split, which is always in the window. It's blank while not scrolled, including after scrolling back to where it started.

Every file has the same number of lines, so separate text sources line up. "Completed" lines are members before the current one. "Upcoming" lines are the current member and every member after it. Lines that don't apply are blank.

| File | Line contents |
|---|---|
| `Names.txt` | Display name |
| `Names_Raw.txt` | Raw name |
| `Names_Indent_{n}.txt` (n = 0 … level) | Display name only on lines whose split depth is `n`. One file per indent level. |
| `Highlight_CurrentSplit.txt` | Fill on the current member's line |
| `Highlight_ScrolledToSplit.txt` | Fill on the line of the member holding the scrolled-to split, only while scrolled |
| `{method}\Time_Run_Current.txt` | Completed: this attempt's split time |
| `{method}\{comparison}\Time_Run_Upcoming.txt` | Upcoming: the comparison's split time |
| `{method}\{comparison}\Time_Segment_Upcoming.txt` | Upcoming: the comparison's segment time |
| `{method}\Time_Gold_Upcoming.txt` (gold) | Upcoming: best segment |
| `{method}\{comparison}\Delta_Run.txt` | Completed: run delta |
| `{method}\{comparison}\Delta_Segment.txt` | Completed: segment delta |
| `{method}\Delta_Gold.txt` (gold) | Completed: gold delta |
| `{method}\Highlight_Gold.txt` (gold) | Fill on completed lines that are golds (see [Sign values](#sign-values)) |
| `{method}\{comparison}\Highlight_AheadGained.txt` | Fill: run delta < 0 and segment delta < 0 |
| `{method}\{comparison}\Highlight_AheadLost.txt` | Fill: run delta < 0, segment not gained |
| `{method}\{comparison}\Highlight_BehindGained.txt` | Fill: segment delta < 0, run delta not < 0 |
| `{method}\{comparison}\Highlight_BehindLost.txt` | Fill: every other completed line for which the comparison has a time |

Each completed line has a fill in at most one of `Highlight_Gold.txt` and one comparison's four highlight files, checked in the table's order from top to bottom. A gold is filled only in `Highlight_Gold.txt`, so its line is blank in every comparison's files. A skipped split's line is blank in all of them, and so is the line of a split that the comparison has no time for and isn't a gold. The fill is 43 × `█`.

## `CompletedSplits\{level}\` (Completed splits setting)

The same files as `SplitList`, but the lines cover every completed member: none while the timer isn't running, and all members once the run is finished. No line is current or upcoming, so `Highlight_CurrentSplit.txt` and the `Time_*_Upcoming.txt` files aren't written. It doesn't scroll, so neither is `Highlight_ScrolledToSplit.txt`.

## Outputs turned off, and levels that are gone

Turning off the Timer, Split list, Completed splits or Custom variables setting empties that output's files straight away. The same happens to:
- the files of a level that is gone, because Subsplits was turned off or the splits no longer have that many dashes;
- `Names_Indent_{n}.txt` for a depth no longer present;
- the files in a comparison's folder, when it's unticked, or when the open splits don't have that comparison;
- the file of a variable, or a custom variable, the splits no longer have.

The files stay, with no contents, and are filled again when the output or level comes back. Leftovers like these from earlier sessions are emptied when the layout is loaded. Nothing is deleted. Only the component's own files are emptied, and only in the folder currently set. Its own files are everything in `CurrentSplit\`, `PreviousSplit\`, `SplitList\`, `CompletedSplits\`, `Timers\`, `Variables\` and `CustomVariables\`, and the `TotalSplits*.txt` files. That includes any other file kept in those folders, and the files older releases wrote there.

## When LiveSplit closes

LiveSplit resets the timer as it closes, so the files are left as after any reset, with the "Not running" contents above. Close Splits also resets the timer, then opens a blank run: no game or category, and one split with an empty name. The files are left showing that run. Opening another layout, or removing the component from the layout, leaves the files as they are. See [livesplit-notes.md](livesplit-notes.md#closing-livesplit-and-changing-layouts-read-in-source).

## Formatting

From `TimeFormatter.cs`, which uses LiveSplit's own formatters (`RegularTimeFormatter` for times, `DeltaTimeFormatter` for differences), so the files read like LiveSplit's components. Values are truncated, not rounded.

- **Times** show minutes: `0:05`, `1:05`, `1:02:05`.
- **Differences** always have a sign, except zero, and have no decimals from a minute on: `+5.2`, `+1:05`.
- **Negatives** get the minus sign `−` (U+2212), not `-`: `−5.2`, `−0:05`. The files are UTF-8.

The **Times** and **Differences** settings (Time Format) set the decimals:

| Choice | Times | Differences |
|---|---|---|
| Flex | `0:05`; under a second `0:00.4`; under a tenth `0:00.05` | `+5.2`; under a tenth `+0.05` |
| Seconds | `0:05` | `+5`, `+0` |
| Tenths | `0:05.2` | `+5.2`, `+0.0` |
| Hundredths | `0:05.23` | `+5.23`, `+0.05` |
| Milliseconds | `0:05.234` | `+5.234`, `+0.052` |

The defaults are Seconds for times and Flex for differences. Flex adds decimals only where a value would otherwise read as zero. A zero difference is `0.00` with Flex.

`Timers\` always show whole seconds, whatever the settings: with decimals, each timer file would be rewritten many times a second.

`null` is written as `-` everywhere.
