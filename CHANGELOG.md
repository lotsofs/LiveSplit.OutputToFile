# Changelog

What changed in Output to File between releases: the files it writes, what they contain, its settings, and how it affects LiveSplit. Newest first. Each section describes what that release's download on GitHub does.

## Unreleased

### Action required

- **If your LiveSplit is older than 1.8.37, update it before installing this release.** This release is built and tested with LiveSplit 1.8.37, which needs .NET Framework 4.8.1. It hasn't been tried with older versions of LiveSplit.
- **Some subsplit folders are renamed back to release 6's names, so their OBS text sources need new paths.** With three or more subsplit levels, the folders between `SansSubsplits` and `AllSplits` are numbered after the dashes they leave out: release 7's `SansSubsplits_Level1` is now `SansSubsplits_Level2`, its `SansSubsplits_Level2` is now `SansSubsplits_Level3`, and so on. That goes for the folders in `CurrentSplit\`, `PreviousSplit\` and `SplitList\`, and for the `TotalSplits_SansSubsplits_Level…` and `Timers\…\SegmentTimer_SansSubsplits_Level…` files.
- **`Index.txt` and `ReverseIndex.txt` in `CurrentSplit\` are gone. Point their text sources at `Completed.txt` and `After.txt` instead** (see Added).
  - `Completed.txt` shows what `Index.txt` did, except `0` instead of `-1` before the timer starts.
  - `After.txt` shows what `ReverseIndex.txt` did while the timer runs. Before the start and after the finish it shows `-`, where `ReverseIndex.txt` showed the number of splits and `-1`.
- **The files that compare against your PB moved into a `Current Comparison` folder, so their OBS text sources need new paths.** In every `GameTime` and `RealTime` folder, these moved into `Current Comparison\`:
  - `RunTime.txt` and `SegmentTime.txt` in `CurrentSplit\`;
  - `RunDifference.txt`, `SegmentDifference.txt` and `Sign.txt` in `PreviousSplit\`;
  - `Delta_Run.txt`, `Delta_Segment.txt`, the four `Highlight_Ahead…` and `Highlight_Behind…` files, `Time_Run_Upcoming.txt` and `Time_Segment_Upcoming.txt` in `SplitList\` and `CompletedSplits\`.

  For example, `PreviousSplit\AllSplits\GameTime\Sign.txt` is now `PreviousSplit\AllSplits\GameTime\Current Comparison\Sign.txt`. The files at the old paths are emptied.
- **`Sign.txt`'s values at the finish are renamed. Update anything that looks for them.** At the finish it says `Finished` followed by what it would say during the run: `PBGold` → `FinishedGoldAhead`, `NoPBGold` → `FinishedGoldBehind`, `PB` → `FinishedAhead`, `NoPB` → `FinishedBehind`, `PBGained` → `FinishedGainedAhead`, `NoPBGained` → `FinishedGainedBehind`, `PBLost` → `FinishedLostAhead`, `NoPBLost` → `FinishedLostBehind`. A finish with nothing to compare against, such as the first finish before there is a PB, says `FinishedNotApplicable`, where it used to say `NoPB` or `NoPBGold`.

### Changed

- Differences, `Sign.txt`, the ahead and behind highlights, and the times of the current and upcoming splits compare against the comparison selected in LiveSplit, in the `Current Comparison` folders. They used to compare against Personal Best, whatever was selected.
- Differences are written the way LiveSplit writes them:
  - **Negatives get the minus sign `−`, not `-`**, as do negative times.
  - With the default Flex setting (see Added), negative differences have one decimal under a minute, like positive ones: `−0.5`, `−5.2` (release 7: `-.50`, `-5.23`). From a minute on they have none: `−1:05` (release 7: `-1:05.23`).
  - Under a tenth of a second there are two decimals: `+0.05`, `−0.05` (release 7: `+.05`, `-.05`).
  - No difference at all is `0.00` (release 7: `+.00`).
- **Splitting no longer waits for the files to be written.** They are written in the background, so LiveSplit stays responsive. A file whose contents haven't changed isn't rewritten.
- Split names leave out one space after their leading dashes: a subsplit named `- -40` shows as `-40` (release 7: ` -40`). So to show a name that starts with a dash, put a space before it. With the **Subsplits** setting off, names in `AllSplits` are the same as with it on; release 7 kept their dashes.
- When the layout loads, files left over from outputs that are now turned off, or from subsplit levels your splits no longer have, are emptied. So are files left by older releases, such as release 7's `Index.txt` and `SansSubsplits_Level1` folders, and release 5's `TotalSplits.txt` and `CurrentSplit\Name.txt`, so text sources still pointing at them show nothing.
  - Files are emptied, never deleted.
  - Only `TotalSplits*.txt` and the files in `CurrentSplit\`, `PreviousSplit\`, `SplitList\`, `CompletedSplits\` and `Timers\` are touched. **Don't keep files of your own in those folders:** the component treats everything in them as its own.
- The DLL's version, shown in its file properties, is now the release number, `8.0`. It used to be `1.0.0.0`.

### Added

- Split counts in `CurrentSplit\`, counted the way people count:
  - `Number.txt`: the current split's number, starting at 1.
  - `Completed.txt`: the number of splits done, skipped ones included.
  - `Remaining.txt`: the number of splits not done yet, the current one included.
  - `After.txt`: the number of splits after the current one.

  `Number.txt` and `After.txt` show `-` before the timer starts and after the finish, when no split is current.
- `CompletedSplits\`: every split completed so far, as a list with the same files as `SplitList\`, except `Highlight_CurrentSplit.txt` and the `Time_…_Upcoming.txt` files. Turn it on with the new **Completed splits** setting. Turning it off empties its files.
- Golds in the `SansSubsplits` folders: `GoldTime.txt`, `GoldDifference.txt`, `Delta_Gold.txt`, `Highlight_Gold.txt` and `Time_Gold_Upcoming.txt`, and gold values in `Sign.txt`. A section that groups several splits has its own gold: the fastest you have done the whole section within one attempt, from your splits' history, not its subsplits' golds added up. A section you have never finished in one attempt has no gold yet, so finishing it counts as one.
- A **Comparisons** setting: tick a comparison to get a folder of its own next to `Current Comparison`, with the same files, named after it, such as `Personal Best\` or `Average Segments\`. **All custom comparisons** gives every comparison you added in Edit Splits a folder, in whatever splits you open. Unticking empties the folder.
- `CurrentComparison.txt`: the name of the comparison selected in LiveSplit.
- **Time Format** settings: choose the decimals of times and of differences: Seconds, Tenths, Hundredths, Milliseconds, or Flex, which adds decimals only where a value would otherwise read as zero (a time under a second, a difference under a tenth). Times default to Seconds and differences to Flex, as before. The timers always show whole seconds.
- **Updates from LiveSplit.** When a newer release of Output to File is out, LiveSplit offers it at startup: it shows what changed and asks whether to update. Choosing Yes closes LiveSplit, installs the new release and opens LiveSplit again. This release itself still has to be installed by hand.

### Fixed

- `PreviousSplit\…\Sign.txt` says `GainedAhead` or `GainedBehind` when a split beat the comparison's segment without being a gold. It used to say `LostAhead` or `LostBehind`.
- `Sign.txt` in the `SansSubsplits` folders reports a gold only when the section beat its gold (see Added). Release 7 reported a gold there for every split.
- `Sign.txt` no longer reports a gold for the split after a skipped split.
- A split that has no best segment yet is now filled in `Highlight_Gold.txt`, matching `Sign.txt`, which already called it a gold.
- A skipped split is no longer filled in `Highlight_BehindLost.txt` in the split list. Its line is now blank in every highlight file.
- Pausing, resuming or finishing no longer blanks the differences of earlier splits in the split list, and with them their highlights.
- After the finish, `PreviousSplit\` shows the last split, with its segment time, its differences and its `Sign.txt`, and `CurrentSplit\…\Name.txt` shows `-`. Release 7 showed the name of the split before the last in `PreviousSplit\AllSplits\`, `-` as the segment time and the segment and gold differences, and `PBGold` as the sign, and kept the last split's name in `CurrentSplit\AllSplits\Name.txt`.
- Before the timer starts and after a reset, every `Name.txt` and `Name_Raw.txt` in `CurrentSplit\` and `PreviousSplit\` shows `-`. Release 7 left the last attempt's names in some of them.
- The `Timers\` files now update whenever what they show changes. They used to wait for the seconds digits to change: resetting at 1:00.5 left `RunTimer.txt` at `1:00` until the next start, and game time that started within its first second showed `-` until a second had passed.
- The segment timers in `Timers\` restart at `0:00` as soon as you split, and count whole seconds from the split. They used to keep the previous segment's time until the run timer's next second, and then tick over together with the run timer, up to a second late. After the finish they show the last segment's time (release 7: `0:00`). In the `SansSubsplits` timers they count from the end of that level's previous split; release 7 counted from an earlier one.
- Changing an **Output** setting takes effect straight away. Turning off **Timer**, **Split List** or **Subsplits** empties the files it wrote, and turning one on writes them. Release 7 waited for the next split, and left the files of an output that was turned off as they were.
- A file that can't be written, because it's locked or read-only or its drive is gone, no longer stops the other files from being written. The error is shown in red in the component's settings.
- Changes made in Edit Splits now show in the files straight away: renamed, added or removed splits, and changed times. So does opening another splits file. They used to wait until the timer was next started, paused or reset.
- Typing a folder path in the settings no longer creates a folder full of output files for every letter typed. A typed path is used once you leave the box, press Enter or click OK; closing the settings with the X button drops it. Browse still uses the chosen folder at once.
- **Browse** opens on the output folder that's set. It used to open on the folder containing it.
- Tab moves through the settings in the order they're shown. It used to go from the folder to **Split List Writing** and then to **Output**.
- The **Output to File** tab in Layout Settings no longer has a scrollbar. Release 7 showed one, with only empty space below the settings to scroll to.
- A split with no name, or with only dashes such as `-`, no longer stops the component from loading. Release 7 failed to load it, and starting or splitting the timer then ran into errors.
- A first split with more dashes than there are subsplit levels, such as `---A` when no other split has more than one dash, now shows in `AllSplits`. Release 7 left it out of every folder.
- The last split always counts as a top-level split. Its dashes no longer add a subsplit level, and it shows without them in `SansSubsplits` (release 7: `--C` for a last split named `--C`).
- When LiveSplit is controlled through its server, as some remote controls and stream tools do, a split could now and then make LiveSplit close.

### Removed

- The empty **Time Formats** group in the settings.

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
