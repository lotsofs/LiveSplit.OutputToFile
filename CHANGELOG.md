# Changelog

What changed in Output to File between releases: the files it writes, what they contain, its settings, and how it affects LiveSplit. Newest first. Release numbers match the repository's tags.

## Unreleased

### Changed

- **Output folders have moved, so OBS text sources need new paths.** Everything in `CurrentSplit\`, `PreviousSplit\` and `SplitList\` is now in a subfolder per subsplit level (see Added). The `AllSplits` subfolder has the same contents as before, so `CurrentSplit\Name.txt` is now `CurrentSplit\AllSplits\Name.txt`. Two files were renamed:
  - `TotalSplits.txt` is now `TotalSplits_AllSplits.txt`;
  - `Timers\RealTime\SegmentTimer.txt` is now `Timers\RealTime\SegmentTimer_AllSplits.txt`, and likewise under `GameTime`.
- **Times are formatted differently.**
  - Times are always minutes and seconds without decimals: `0:09`, `1:05`, `1:02:05`. Release 6 showed times under a minute with decimals, like `9.50` and `15.2`.
  - Differences under a minute have one decimal: `+5.2` (release 6: `+5.23`), `+15.2`.
  - Differences under a tenth of a second drop the leading zero: `+.05`.
  - Negative differences still have two decimals at every size, like `-5.23` and `-1:05.20`. This is a known bug.
- **Splitting no longer waits for the files to be written.** They are written in the background, so LiveSplit stays responsive. A file whose contents haven't changed isn't rewritten.

### Added

- **Subsplits.** A split whose name starts with `-` is a subsplit, as in LiveSplit's Subsplits component.
  - Next to `AllSplits`, the component writes a `SansSubsplits` folder with only the top-level splits. Each further level of dashes (`--`, `---`, …) gets a folder of its own, named after the dashes it leaves out: `SansSubsplits_Level2` has every split except those with two or more dashes, `SansSubsplits_Level3` every split except those with three or more, and so on.
  - A name in braces at the start of a split names its section: a split named `{Area A}A3` shows as `Area A` in `SansSubsplits` and as `A3` in `AllSplits`.
  - The new **Subsplits** setting turns this off.
- `Name_Raw.txt` in `CurrentSplit\` and `PreviousSplit\`, and `Names_Raw.txt` in the split list. These give split names exactly as typed in LiveSplit. The other name files leave out the leading dashes and the `{…}` part.
- `Names_Indent_0.txt`, `Names_Indent_1.txt`, … in the split list: one file per subsplit depth, with a split's name only on the line of a split at that depth. Each depth can then be its own OBS text source, indented differently.
- `PlatformName.txt`: the platform set in the splits' metadata.
- `CompletedSplits\`: every split completed so far, as a list with the same files as `SplitList\`. Turn it on with the new **Completed splits** setting.
- A **Split list** setting, to turn off `SplitList\`.

### Fixed

- `PreviousSplit\…\Sign.txt` says `GainedAhead` or `GainedBehind` when a split beat its PB segment without being a gold. It used to say `LostAhead` or `LostBehind`.
- `Sign.txt` no longer reports a gold for the split after a skipped split.
- A split that has no best segment yet is now filled in `Highlight_Gold.txt`, matching `Sign.txt`, which already called it a gold.
- A file that can't be written, because it's locked or read-only or its drive is gone, no longer stops the other files from being written. The error is shown in red in the component's settings.

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

- `CurrentSplit_ReverseIndex.txt`: the number of splits after the current one.
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
