# Changelog

What changed in Output to File between releases: the files it writes, what they contain, its settings, and how it affects LiveSplit. Newest first. Each section describes what that release's download on GitHub does.

## Unreleased

## 8 (2026-10-22)

### Action required

- **If your LiveSplit is older than 1.8.37, update it first.** This release is built and tested with 1.8.37, and hasn't been tried with older versions.
- **Some files moved or were renamed. Point their OBS text sources at the new paths.** The files at the old paths are emptied.
  - The files that compare against your PB are now in a `CurrentComparison` folder in each `GameTime` and `RealTime` folder: `RunTime.txt` and `SegmentTime.txt` in `CurrentSplit\`; `RunDifference.txt`, `SegmentDifference.txt` and `Sign.txt` in `PreviousSplit\`; `Delta_Run.txt`, `Delta_Segment.txt`, the `Highlight_Ahead…` and `Highlight_Behind…` files, `Time_Run_Upcoming.txt` and `Time_Segment_Upcoming.txt` in `SplitList\`. For example, `PreviousSplit\AllSplits\GameTime\Sign.txt` is now `PreviousSplit\AllSplits\GameTime\CurrentComparison\Sign.txt`.
  - With three or more subsplit levels, the folders between `SansSubsplits` and `AllSplits` are numbered one higher, as in release 6: release 7's `SansSubsplits_Level1` is now `SansSubsplits_Level2`, and so on. So are the `TotalSplits_SansSubsplits_Level…` and `SegmentTimer_SansSubsplits_Level…` files.
  - `Index.txt` and `ReverseIndex.txt` in `CurrentSplit\` are gone. Use `Completed.txt` and `After.txt` (see Added). They show the same numbers, except that `Completed.txt` shows `0` before the start (was `-1`), and `After.txt` shows `-` before the start and after the finish.
- **`Sign.txt`'s values at the finish are renamed. Update anything that looks for them.** At the finish it says `Finished` followed by what it would say during the run: `PB…` became `Finished…Ahead` and `NoPB…` became `Finished…Behind`, so `PBGold` is now `FinishedGoldAhead`, `NoPBLost` is `FinishedLostBehind` and `PB` is `FinishedAhead`. A finish with nothing to compare against, such as before you have a PB, says `FinishedNotApplicable`.

### Changed

- Differences, `Sign.txt`, the ahead and behind highlights, and the times of the current and upcoming splits compare against the comparison selected in LiveSplit. They used to compare against Personal Best, whatever was selected.
- Differences are written the way LiveSplit writes them. With the default Flex setting (see Added):
  - Negatives get the minus sign `−`, not `-`, as do negative times.
  - Negatives have one decimal under a minute and none from a minute on, like positives: `−0.5`, `−5.2`, `−1:05` (release 7: `-.50`, `-5.23`, `-1:05.23`).
  - Under a tenth of a second there are two decimals: `+0.05` (release 7: `+.05`).
  - No difference at all is `0.00` (release 7: `+.00`).
- **Splitting no longer waits for the files to be written.** They are written in the background, and a file whose contents haven't changed isn't rewritten.
- Split names leave out one space after their leading dashes: `- -40` shows as `-40` (release 7: ` -40`). So to show a name that starts with a dash, put a space before it. With **Subsplits** off, names in `AllSplits` no longer keep their dashes.
- When the layout loads, files that are no longer written are emptied, never deleted: those of outputs turned off, of subsplit levels your splits no longer have, and of older releases, such as release 7's `Index.txt`. **Don't keep files of your own in `CurrentSplit\`, `PreviousSplit\`, `SplitList\`, `CompletedSplits\` or `Timers\`, or name them `TotalSplits*.txt`:** the component treats those as its own.
- The DLL's version in its file properties is now the release number, `8.0` (release 7: `1.0.0.0`).

### Added

- Split counts in `CurrentSplit\`: `Number.txt` (the current split's number, from 1), `Completed.txt` (splits done, skipped ones included), `Remaining.txt` (splits not done yet, the current one included) and `After.txt` (splits after the current one). `Number.txt` and `After.txt` show `-` when no split is current.
- `CompletedSplits\`, turned on with the new **Completed splits** setting: every split completed so far, with the same files as `SplitList\` except `Highlight_CurrentSplit.txt` and the `Time_…_Upcoming.txt` files.
- Golds in the `SansSubsplits` folders: `GoldTime.txt`, `GoldDifference.txt`, `Delta_Gold.txt`, `Highlight_Gold.txt`, `Time_Gold_Upcoming.txt`, and gold values in `Sign.txt`. A section's gold is the fastest you have done the whole section within one attempt, not its splits' golds added up. Until you have, finishing it counts as a gold.
- A **Comparisons** setting: each ticked comparison gets a folder of its own next to `CurrentComparison`, with the same files, named after it without its spaces, such as `AverageSegments\`. **All custom comparisons** does this for every comparison added in Edit Splits. Unticking empties the folder.
- `CurrentComparison.txt`: the name of the comparison selected in LiveSplit.
- **Time Format** settings, in what was the empty **Time Formats** group: the decimals of times and of differences. Choose Seconds, Tenths, Hundredths, Milliseconds, or Flex, which adds decimals only where a value would otherwise read as zero. Times default to Seconds and differences to Flex. The timers always show whole seconds.
- **Updates from LiveSplit.** When a newer release is out, LiveSplit offers it at startup and shows what changed. Choosing Yes closes LiveSplit, installs the release and opens LiveSplit again. This release itself still has to be installed by hand.

### Fixed

- `Sign.txt` says `GainedAhead` or `GainedBehind` when a split beat the comparison's segment without being a gold (release 7: `LostAhead`, `LostBehind`).
- In the `SansSubsplits` folders, `Sign.txt` reports a gold only when a section beats its gold (see Added). Release 7 reported one for every section.
- `Sign.txt` no longer reports a gold for the split after a skipped split.
- A split with no best segment yet is filled in `Highlight_Gold.txt`, matching `Sign.txt`, which already called it a gold.
- A skipped split's line is blank in every highlight file. Release 7 filled it in `Highlight_BehindLost.txt`.
- Pausing, resuming or finishing no longer blanks the differences of earlier splits in the split list, and with them their highlights.
- After the finish, `PreviousSplit\` shows the last split, with its segment time, differences and `Sign.txt`, and `CurrentSplit\…\Name.txt` shows `-`. Release 7 showed the name of the split before the last, `-` for most values and `PBGold` as the sign, and kept the last split's name in `CurrentSplit\`.
- Before the timer starts and after a reset, every `Name.txt` and `Name_Raw.txt` in `CurrentSplit\` and `PreviousSplit\` shows `-`. Release 7 left some of them blank or showing the last attempt's names.
- The `Timers\` files update as soon as what they show changes. Release 7 waited for the seconds to change: a reset at 1:00.5 left `RunTimer.txt` at `1:00`, and game time that started within its first second showed `-` until a second had passed.
- The segment timers restart at `0:00` the moment you split, and count whole seconds from the split. Release 7 kept the previous segment's time until the run timer's next second, then ticked with the run timer. After the finish they show the last segment's time (release 7: `0:00`). In `SansSubsplits` they count from that level's previous split; release 7 counted from the wrong one.
- Changing an **Output** setting takes effect straight away: turning off **Timer**, **Split List** or **Subsplits** empties its files, and turning one on writes them. Release 7 waited for the next split, and left the files of an output that was turned off as they were.
- A file that can't be written, because it's locked or read-only or its drive is gone, no longer stops the other files from being written. The error shows in red in the component's settings.
- Changes made in Edit Splits (renamed, added or removed splits, changed times) and opening another splits file show in the files straight away, not when the timer is next started, paused or reset.
- Typing a folder path in the settings no longer creates a folder full of output files for every letter typed. A typed path is used once you leave the box, press Enter or click OK; closing the settings with the X button drops it. Browse still uses the chosen folder at once.
- **Browse** opens on the output folder that's set, not the folder containing it.
- Tab moves through the settings in the order they're shown.
- The **Output to File** tab in Layout Settings no longer has a scrollbar.
- A split with no name, or with only dashes such as `-`, no longer stops the component from loading.
- A first split with more dashes than any other split, such as `---A`, now shows in `AllSplits`. Release 7 left it out of every folder.
- The last split always counts as a top-level split. Its dashes no longer add a subsplit level, and it shows without them in `SansSubsplits` (release 7: `--C`).
- When LiveSplit is controlled through its server, as some remote controls and stream tools do, a split no longer now and then closes LiveSplit.

## 7 (2024-10-09)

### Action required

- **Subsplit folders between `SansSubsplits` and `AllSplits` are renamed, so their OBS text sources need new paths.** Release 6's `SansSubsplits_Level2` is now `SansSubsplits_Level1`, its `SansSubsplits_Level3` is now `SansSubsplits_Level2`, and so on, in `CurrentSplit\`, `PreviousSplit\`, `SplitList\`, `TotalSplits_…` and `Timers\`. These folders only exist with three or more subsplit levels.

### Changed

- **Times are formatted differently.**
  - Times are always minutes and seconds without decimals: `0:09`, `1:05`, `1:02:05`. Release 6 showed times under a minute with decimals, like `9.50` and `10.0`.
  - The timers in `Timers\` show `0:09` and `1:02:05` (release 6: `00:00:09` and `01:02:05`). Until game time starts, its timers show `-` (release 6: `00:00:00`).
  - Positive differences under a minute have one decimal: `+5.2` (release 6: `+5.23`).
  - Differences under a tenth of a second drop the leading zero: `+.05` and `-.05` (release 6: `+0.05` and `-0.05`). So do negative ones under a second: `-.50` (release 6: `-0.50`).
- Split names leave out their leading dashes: a subsplit named `-A1` shows as `A1`. With the new **Subsplits** setting off, they keep them.
- `ReverseIndex.txt` counts the splits after the current one: `2` on the first of three splits (release 6: `3`). Before the timer starts it shows the number of splits (release 6: one more), and after the finish `-1` (release 6: `0`).
- The settings are wider, and the outputs come first, above **Split List Writing**, in a group now called **Output** (release 6: **Additional Outputs**). **Timer** is a checkbox; in release 6 it was a round button that couldn't be turned off again. The settings also have an empty **Time Formats** group.

### Added

- A name in braces at the start of a split names its section: a split named `{Area A}A3` shows as `Area A` in `SansSubsplits` and as `A3` in `AllSplits`. Release 6 showed it as typed in both.
- A **Subsplits** setting, to turn off the `SansSubsplits` folders.
- A **Split List** setting, to turn off `SplitList\`.
- `Name_Raw.txt` in `CurrentSplit\` and `PreviousSplit\`, and `Names_Raw.txt` in the split list. These give split names exactly as typed in LiveSplit.
- `Names_Indent_0.txt`, `Names_Indent_1.txt`, … in the split list: one file per subsplit depth, with a split's name only on the line of a split at that depth. Each depth can then be its own OBS text source, indented differently.
- `PlatformName.txt`: the platform set in the splits' metadata.

### Fixed

- With subsplits, the split list and the `SansSubsplits` segment timers no longer stop updating in the first section of a run.
- The first split's segment time, segment difference and gold difference are filled in, and its `Sign.txt` no longer calls it a gold. Release 6 showed `-` for them.
- After the finish, the split list shows the run's times. Release 6 went back to the upcoming times.
- `Highlight_CurrentSplit.txt` is blank before the timer starts. Release 6 highlighted the first split.

## 6 (2023-05-29)

The download was replaced on 2023-06-01 with a first version of subsplits, which this section describes.

### Action required

- **Output folders have moved, so OBS text sources need new paths.** Everything in `CurrentSplit\`, `PreviousSplit\` and `SplitList\` is now in a subfolder per subsplit level (see Added). The `AllSplits` subfolder has the same files as before, so `CurrentSplit\Name.txt` is now `CurrentSplit\AllSplits\Name.txt`. Two files were renamed:
  - `TotalSplits.txt` is now `TotalSplits_AllSplits.txt`;
  - `Timers\RealTime\SegmentTimer.txt` is now `Timers\RealTime\SegmentTimer_AllSplits.txt`, and likewise under `GameTime`.

### Added

- **Subsplits.** A split whose name starts with `-` is a subsplit, as in LiveSplit's Subsplits component.
  - Next to `AllSplits`, the component writes a `SansSubsplits` folder with only the top-level splits. Each further level of dashes (`--`, `---`, …) gets a folder of its own, named after the dashes it leaves out: `SansSubsplits_Level2` has every split except those with two or more dashes, `SansSubsplits_Level3` every split except those with three or more, and so on.
  - Split names are written as typed, dashes included.

## 5 (2023-03-31)

The download was replaced on 2023-05-29 with a build that has this fix.

### Fixed

- Negative differences were written as `-`. They now show, like `-0.50`.

## 4 (2023-03-31)

### Added

- A **Timer** setting. It writes the running timer to `Timers\RealTime\RunTimer.txt` and `Timers\GameTime\RunTimer.txt`, and the time since the last split to `SegmentTimer.txt` next to it.

### Changed

- Files are grouped in folders: `CurrentSplit\`, `PreviousSplit\`, `SplitList\` and `Timers\`, with times in a `RealTime\` or `GameTime\` subfolder. Release 3 used flat names such as `CurrentSplit_Name.txt`.

### Fixed

- An error when a time was a whole number of seconds.

## 3 (2023-03-12)

### Added

- `CurrentSplit_ReverseIndex.txt`: the number of splits left, the current one included.
- `TotalSplits.txt` and `FinishedRunsCount.txt`.

### Fixed

- The split list's deltas were written incorrectly.
- The split list didn't work on the first split.
- The split list showed the wrong gold and segment times for the last split.

## 2 (2023-02-17)

### Added

- `SplitList_Names.txt`: the names of the splits in the split list.

## 1 (2023-02-17)

First release. It writes the game and category names, the attempt count, the current and previous split with their times and differences, and a split list with times, deltas and highlights.
