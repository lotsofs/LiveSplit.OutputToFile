# Changelog

What changed in Output to File between releases: the files it writes, what they contain, its settings, and how it affects LiveSplit. Newest first. Release numbers match the repository's tags.

## Unreleased

### Changed

- **Output folders have moved, so OBS text sources need new paths.** Everything in `CurrentSplit\`, `PreviousSplit\` and `SplitList\` is now in a subfolder per subsplit level (see Added). The `AllSplits` subfolder has the same contents as before, except `Index.txt` and `ReverseIndex.txt` (see Removed), so `CurrentSplit\Name.txt` is now `CurrentSplit\AllSplits\Name.txt`. Two files were renamed:
  - `TotalSplits.txt` is now `TotalSplits_AllSplits.txt`;
  - `Timers\RealTime\SegmentTimer.txt` is now `Timers\RealTime\SegmentTimer_AllSplits.txt`, and likewise under `GameTime`.
- **Times are formatted differently.**
  - Times are always minutes and seconds without decimals: `0:09`, `1:05`, `1:02:05`. Release 6 showed times under a minute with decimals, like `9.50` and `15.2`.
  - Differences under a minute have one decimal: `+5.2` and `-5.2` (release 6: `+5.23` and `-5.23`), `+15.2`.
  - Differences under a tenth of a second drop the leading zero: `+.05`.
- **Splitting no longer waits for the files to be written.** They are written in the background, so LiveSplit stays responsive. A file whose contents haven't changed isn't rewritten.
- When the layout loads, files left over from outputs that are now turned off, or from subsplit levels your splits no longer have, are emptied. So are the files at release 6's paths, such as `TotalSplits.txt` and `CurrentSplit\Name.txt`, so text sources still pointing at them show nothing.
  - Files are emptied, never deleted.
  - Only `TotalSplits*.txt` and the files in `CurrentSplit\`, `PreviousSplit\`, `SplitList\`, `CompletedSplits\` and `Timers\` are touched. **Don't keep files of your own in those folders:** the component treats everything in them as its own.
- The settings are wider, and the outputs come first, above **Split List Writing**, in a group now called **Output** (release 6: **Additional Outputs**).

### Added

- **Subsplits.** A split whose name starts with `-` is a subsplit, as in LiveSplit's Subsplits component.
  - Next to `AllSplits`, the component writes a `SansSubsplits` folder with only the top-level splits. Each further level of dashes (`--`, `---`, …) gets a folder of its own, named after the dashes it leaves out: `SansSubsplits_Level2` has every split except those with two or more dashes, `SansSubsplits_Level3` every split except those with three or more, and so on.
  - A name in braces at the start of a split names its section: a split named `{Area A}A3` shows as `Area A` in `SansSubsplits` and as `A3` in `AllSplits`.
  - The new **Subsplits** setting turns this off. Split names in `AllSplits` stay the same as with it on.
  - When a level goes away, because the setting is turned off or the splits no longer have that many dashes, its files are emptied straight away.
- `Name_Raw.txt` in `CurrentSplit\` and `PreviousSplit\`, and `Names_Raw.txt` in the split list. These give split names exactly as typed in LiveSplit. The other name files leave out the leading dashes, one space after them, and the `{…}` part. To show a name that starts with a dash, put a space before it: a subsplit named `- -40` shows as `-40`.
- `Names_Indent_0.txt`, `Names_Indent_1.txt`, … in the split list: one file per subsplit depth, with a split's name only on the line of a split at that depth. Each depth can then be its own OBS text source, indented differently.
- `PlatformName.txt`: the platform set in the splits' metadata.
- Split counts in `CurrentSplit\`, counted the way people count:
  - `Number.txt`: the current split's number, starting at 1.
  - `Completed.txt`: the number of splits done, skipped ones included.
  - `Remaining.txt`: the number of splits not done yet, the current one included.
  - `After.txt`: the number of splits after the current one.

  `Number.txt` and `After.txt` show `-` before the timer starts and after the finish, when no split is current.
- `CompletedSplits\`: every split completed so far, as a list with the same files as `SplitList\`, except `Highlight_CurrentSplit.txt` and the `Time_…_Upcoming.txt` files. Turn it on with the new **Completed splits** setting. Turning it off empties its files.
- A **Split list** setting, to turn off `SplitList\`. Turning it off empties its files.

### Fixed

- `PreviousSplit\…\Sign.txt` says `GainedAhead` or `GainedBehind` when a split beat its PB segment without being a gold. It used to say `LostAhead` or `LostBehind`.
- `Sign.txt` no longer reports a gold for the split after a skipped split.
- A split that has no best segment yet is now filled in `Highlight_Gold.txt`, matching `Sign.txt`, which already called it a gold.
- A skipped split is no longer filled in `Highlight_BehindLost.txt` in the split list. Its line is now blank in every highlight file.
- The `Timers\` files now update whenever what they show changes. They used to wait for the seconds digits to change: resetting at 1:00.5 left `RunTimer.txt` at `1:00` until the next start, and game time that started within its first second showed `-` until a second had passed.
- Turning off the **Timer** setting empties the files in `Timers\` straight away. They used to keep showing the time from when it was turned off.
- The segment timers in `Timers\` restart at `0:00` as soon as you split, and count whole seconds from the split. They used to keep the previous segment's time until the run timer's next second, and then tick over together with the run timer, up to a second late. After the finish they show the last segment's time.
- A file that can't be written, because it's locked or read-only or its drive is gone, no longer stops the other files from being written. The error is shown in red in the component's settings.
- Changes made in Edit Splits now show in the files straight away: renamed, added or removed splits, and changed times. So does opening another splits file. They used to wait until the timer was next started.
- Typing a folder path in the settings no longer creates a folder full of output files for every letter typed. A typed path is used once you leave the box, press Enter or click OK; closing the settings with the X button drops it. Browse still uses the chosen folder at once.
- **Browse** opens on the output folder that's set. It used to open on the folder containing it.

### Removed

- **`Index.txt` and `ReverseIndex.txt` in `CurrentSplit\` are gone. Point their text sources at `Completed.txt` and `Remaining.txt` instead.**
  - `Completed.txt` shows what `Index.txt` did, except `0` instead of `-1` before the timer starts.
  - `Remaining.txt` shows what `ReverseIndex.txt` did, except that before the timer starts it shows the number of splits, not one more.

## 6 (2023-05-29)

### Fixed

- Negative times were written as blank.

## 5 (2023-03-31)

No changes from release 4.

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
