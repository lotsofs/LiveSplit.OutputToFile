# LiveSplit.OutputToFile

## About

A component for LiveSplit which outputs information to a text file in a folder you choose, which can then be added to OBS as a text source.

## Installing

1. Download `LiveSplit.OutputToFile.dll` from the [releases page](https://github.com/lotsofs/LiveSplit.OutputToFile/releases).
2. Close LiveSplit. Put the DLL in the `Components` folder inside LiveSplit's folder, replacing the older one if it's there.
3. Open LiveSplit, right-click it and choose **Edit Layout...**. Click **+**, then **Other** → **Output to File**.
4. Click **Layout Settings**, open the **Output to File** tab, and choose a folder under **Folder Path**. The files are written there from then on.
5. In OBS, add a Text source, tick **Read from file**, and choose one of the files.

It's built and tested with LiveSplit 1.8.37, and hasn't been tried with older versions. If your LiveSplit is older, update it first.

## Updates

From release 8 on, LiveSplit tells you at startup when a newer release is out, shows what changed, and asks whether to update. If you choose Yes, LiveSplit closes, installs the new release and opens again. Release 8 itself has to be installed by hand, as described above.

## Settings

The settings are in LiveSplit's Layout Settings, on the **Output to File** tab.

- **Folder Path**: the folder the files are written to. Type it, or click **Browse**. A typed path is used once you leave the box, press Enter or click OK. Closing the settings with the X button drops it.
- **Output**: which files to write. Turning one off empties its files.
  - **Timer**: the timers in `Timers`. Off at first.
  - **Subsplits**: the `SansSubsplits` folders (see [Subsplits](#subsplits)). On at first.
  - **Completed splits**: `CompletedSplits`, a list of every split done so far. Off at first.
  - **Split List**: `SplitList`, a list of the splits around the current one. On at first.
- **Split List Writing**: how many splits `SplitList` shows before and after the current one, 10 each at first. Near the start and the end of the run, the list shows the first or the last splits instead, so its length stays the same.
- **Comparisons**: which comparisons get a folder of their own (see [Comparisons](#comparisons)).
- **Time Format**: the decimals of times and of differences (see below).

Times and differences are written the way LiveSplit writes them: `0:05`, `+5.2`, `+1:05`, with the minus sign `−` for negatives. Under **Time Format**, choose their decimals: Seconds, Tenths, Hundredths, Milliseconds, or Flex, which adds decimals only where a value would otherwise read as zero (a time under a second, a difference under a tenth). The defaults are Seconds for times and Flex for differences. The timers always show whole seconds.

If a file can't be written, because another program has it locked, it's read-only, or its drive is gone, the other files are still written. A red line at the bottom of the settings then says which file, when, and why. It goes away once that file has been written.

## Output Files

The [tree](#output-directory-tree) at the end of this page lists every file.

- The main folder holds the run's details: `GameName.txt`, `CategoryName.txt`, `PlatformName.txt` (the platform set in Edit Splits), `AttemptCount.txt`, `FinishedRunsCount.txt`, the number of splits, and `CurrentComparison.txt` (see [Comparisons](#comparisons)).
- `CurrentSplit` is about the split you're on, and `PreviousSplit` about the one you just did.
- `SplitList` is a list of the splits around the current one, and `CompletedSplits` a list of every split done so far.
- `Timers` holds the run's timer, `RunTimer.txt`, and the time since the last split, `SegmentTimer_….txt`.

Each of these has a folder or a file for every subsplit level (see [Subsplits](#subsplits)), and times are written for real time and game time, in `RealTime` and `GameTime` folders.

Total Splits is the number of splits.

Number is the current split's number, counting from 1. It's `-` before the timer starts and after the finish.

Completed is the number of splits done, skipped ones included.

Remaining is the number of splits not done yet, the current one included.

After is the number of splits after the current one. It's `-` before the timer starts and after the finish.

`Name.txt` and `Names.txt` give split names as shown: without their leading dashes, or as their section's name (see [Split Names](#split-names)). `Name_Raw.txt` and `Names_Raw.txt` give them exactly as typed in LiveSplit.

Every file in a list has one line per split, so text sources placed side by side line up. A line with nothing to show is blank. `Names_Indent_0.txt`, `Names_Indent_1.txt`, … have a split's name only on the lines of splits with that many dashes, so each depth can be its own text source, indented differently.

`CompletedSplits` has the same files as `SplitList`, except `Highlight_CurrentSplit.txt` and the `Time_…_Upcoming.txt` files, since none of its splits are current or upcoming.

Highlights are a shaded bar to layer behind splits.

The files of an output you turn off in the settings, of a comparison you untick, or of a subsplit level your splits no longer have, are emptied rather than deleted, so text sources that use them show nothing. The component treats everything in its folders (`CurrentSplit`, `PreviousSplit`, `SplitList`, `CompletedSplits` and `Timers`) as its own, so don't keep other files there.

## Comparisons

Differences, `Sign.txt`, the ahead and behind highlights, and the split and segment times of the current and upcoming splits compare against a comparison, as LiveSplit's own splits do. Those files are in a comparison folder inside `GameTime` and `RealTime`:

- `Current Comparison` follows the comparison selected in LiveSplit (right-click → Compare Against). `CurrentComparison.txt` holds its name.
- Tick a comparison under **Comparisons** in the settings to also get a folder for it, named after it, such as `Personal Best` or `Average Segments`. **All custom comparisons** gives every comparison you added in Edit Splits a folder, in whatever splits you open.

Golds don't depend on the comparison, so their files stay in `GameTime` and `RealTime`.

## Sign.txt

`Sign.txt` in `PreviousSplit` sums up the split you just did, against its folder's comparison. Ahead and Behind say whether your time at that split is ahead of or behind the comparison's. The rest says how the split's segment went. The first value that applies is used:

| Value | Meaning |
|---|---|
| `NotApplicable` | Nothing to compare: the timer isn't running, no split is done yet, the split was skipped, or the comparison has no time for it |
| `GoldAhead`, `GoldBehind` | The segment is a gold: faster than your best, or the first time it was done |
| `Ahead`, `Behind` | There's no segment to compare: the split before it was skipped, or the comparison has no segment time for it |
| `GainedAhead`, `GainedBehind` | The segment was faster than the comparison's |
| `LostAhead`, `LostBehind` | The segment wasn't faster than the comparison's |

At the finish, `Sign.txt` says `Finished` followed by what it would say during the run, such as `FinishedGainedAhead` or `FinishedNotApplicable`. In the `Personal Best` folder, the `FinishedAhead` values mean a new personal best.

## Subsplits
In addition to the ``AllSplits`` folder, which takes into consideration every split regardless of whether it is a subsplit or not, if applicable there will also be a ``SansSubsplits`` folder. These will only include splits that do not start with a dash (``-``) (These appear to be referred to in the official subsplits module as 'Section Headers'). Furthermore, the component supports multi-level subsplits, which are subsplits of subsplits, and are created by putting two (2) or more dashes in front of your splitname. Each of these levels gets a folder named after the dashes it leaves out: ``SansSubsplits_Level2`` has every split except those with two or more dashes, ``SansSubsplits_Level3`` every split except those with three or more, and so on. There is no limit to how many levels deep you can go. The **Subsplits** setting turns these folders off.

### Split Names

Split names in the output leave out the leading dashes and one space after them. To show a name that starts with a dash, put a space before it: a subsplit named ``- -40`` shows as ``-40``.

As in LiveSplit's Subsplits component, a name in braces at the start of a split names the section that split ends. A split named ``{Area A}A3`` shows as ``Area A`` in ``SansSubsplits``, where it stands for the whole section, and as ``A3`` in ``AllSplits``. With more levels, give a split a name in braces for each level it shows in. Splits named ``--Start``, ``-{Stage 1}Exit`` and ``{World 1}{Stage 2}Boss`` show as ``World 1`` in ``SansSubsplits``, as ``Stage 1`` and ``Stage 2`` in ``SansSubsplits_Level2``, and as ``Start``, ``Exit`` and ``Boss`` in ``AllSplits``.

### Golds

Every folder has its own golds. In ``AllSplits`` they are LiveSplit's golds. In the ``SansSubsplits`` folders, a section that groups several splits has its own gold: the fastest you have done the whole section within one attempt, taken from your splits' history. That's usually slower than its subsplits' golds added up, because those come from different attempts. A section you have never finished in one attempt has no gold yet, so finishing it counts as one.

## Output Directory Tree

This is the tree for splits with three levels of subsplits, with every output on and Personal Best ticked under **Comparisons**. To keep it short, the `Personal Best` folder is shown once per output, though every `GameTime` and `RealTime` folder has one, and `etc.` marks a folder that holds the same kind of files as the ones above it.

```
|   AttemptCount.txt
|   CategoryName.txt
|   CurrentComparison.txt
|   FinishedRunsCount.txt
|   GameName.txt
|   PlatformName.txt
|   TotalSplits_AllSplits.txt
|   TotalSplits_SansSubsplits.txt
|   TotalSplits_SansSubsplits_Level2.txt
|   TotalSplits_SansSubsplits_Level3.txt
|   
+---CompletedSplits
|   +---AllSplits
|   |   |   Names.txt
|   |   |   Names_Indent_0.txt
|   |   |   Names_Indent_1.txt
|   |   |   Names_Indent_2.txt
|   |   |   Names_Indent_3.txt
|   |   |   Names_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   Delta_Gold.txt
|   |   |   |   Highlight_Gold.txt
|   |   |   |   Time_Run_Current.txt
|   |   |   |   
|   |   |   +---Current Comparison
|   |   |   |       Delta_Run.txt
|   |   |   |       Delta_Segment.txt
|   |   |   |       Highlight_AheadGained.txt
|   |   |   |       Highlight_AheadLost.txt
|   |   |   |       Highlight_BehindGained.txt
|   |   |   |       Highlight_BehindLost.txt
|   |   |   |       
|   |   |   \---Personal Best
|   |   |           Delta_Run.txt
|   |   |           Delta_Segment.txt
|   |   |           Highlight_AheadGained.txt
|   |   |           Highlight_AheadLost.txt
|   |   |           Highlight_BehindGained.txt
|   |   |           Highlight_BehindLost.txt
|   |   |           
|   |   \---RealTime
|   |       |   Delta_Gold.txt
|   |       |   Highlight_Gold.txt
|   |       |   Time_Run_Current.txt
|   |       |   
|   |       \---Current Comparison
|   |               Delta_Run.txt
|   |               Delta_Segment.txt
|   |               Highlight_AheadGained.txt
|   |               Highlight_AheadLost.txt
|   |               Highlight_BehindGained.txt
|   |               Highlight_BehindLost.txt
|   |               
|   +---SansSubsplits
|   |   |   etc.
|   |   …
|   |               
|   +---SansSubsplits_Level2
|   |   |   etc.
|   |   …
|   |               
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
+---CurrentSplit
|   +---AllSplits
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldTime.txt
|   |   |   |   
|   |   |   +---Current Comparison
|   |   |   |       RunTime.txt
|   |   |   |       SegmentTime.txt
|   |   |   |       
|   |   |   \---Personal Best
|   |   |           RunTime.txt
|   |   |           SegmentTime.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunTime.txt
|   |               SegmentTime.txt
|   |               
|   +---SansSubsplits
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldTime.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           RunTime.txt
|   |   |           SegmentTime.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunTime.txt
|   |               SegmentTime.txt
|   |               
|   +---SansSubsplits_Level2
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldTime.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           RunTime.txt
|   |   |           SegmentTime.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunTime.txt
|   |               SegmentTime.txt
|   |               
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
+---PreviousSplit
|   +---AllSplits
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldDifference.txt
|   |   |   |   RunTime.txt
|   |   |   |   SegmentTime.txt
|   |   |   |   
|   |   |   +---Current Comparison
|   |   |   |       RunDifference.txt
|   |   |   |       SegmentDifference.txt
|   |   |   |       Sign.txt
|   |   |   |       
|   |   |   \---Personal Best
|   |   |           RunDifference.txt
|   |   |           SegmentDifference.txt
|   |   |           Sign.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldDifference.txt
|   |       |   RunTime.txt
|   |       |   SegmentTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunDifference.txt
|   |               SegmentDifference.txt
|   |               Sign.txt
|   |               
|   +---SansSubsplits
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldDifference.txt
|   |   |   |   RunTime.txt
|   |   |   |   SegmentTime.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           RunDifference.txt
|   |   |           SegmentDifference.txt
|   |   |           Sign.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldDifference.txt
|   |       |   RunTime.txt
|   |       |   SegmentTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunDifference.txt
|   |               SegmentDifference.txt
|   |               Sign.txt
|   |               
|   +---SansSubsplits_Level2
|   |   |   Name.txt
|   |   |   Name_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   GoldDifference.txt
|   |   |   |   RunTime.txt
|   |   |   |   SegmentTime.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           RunDifference.txt
|   |   |           SegmentDifference.txt
|   |   |           Sign.txt
|   |   |           
|   |   \---RealTime
|   |       |   GoldDifference.txt
|   |       |   RunTime.txt
|   |       |   SegmentTime.txt
|   |       |   
|   |       \---Current Comparison
|   |               RunDifference.txt
|   |               SegmentDifference.txt
|   |               Sign.txt
|   |               
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
+---SplitList
|   +---AllSplits
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   Names_Indent_0.txt
|   |   |   Names_Indent_1.txt
|   |   |   Names_Indent_2.txt
|   |   |   Names_Indent_3.txt
|   |   |   Names_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   Delta_Gold.txt
|   |   |   |   Highlight_Gold.txt
|   |   |   |   Time_Gold_Upcoming.txt
|   |   |   |   Time_Run_Current.txt
|   |   |   |   
|   |   |   +---Current Comparison
|   |   |   |       Delta_Run.txt
|   |   |   |       Delta_Segment.txt
|   |   |   |       Highlight_AheadGained.txt
|   |   |   |       Highlight_AheadLost.txt
|   |   |   |       Highlight_BehindGained.txt
|   |   |   |       Highlight_BehindLost.txt
|   |   |   |       Time_Run_Upcoming.txt
|   |   |   |       Time_Segment_Upcoming.txt
|   |   |   |       
|   |   |   \---Personal Best
|   |   |           Delta_Run.txt
|   |   |           Delta_Segment.txt
|   |   |           Highlight_AheadGained.txt
|   |   |           Highlight_AheadLost.txt
|   |   |           Highlight_BehindGained.txt
|   |   |           Highlight_BehindLost.txt
|   |   |           Time_Run_Upcoming.txt
|   |   |           Time_Segment_Upcoming.txt
|   |   |           
|   |   \---RealTime
|   |       |   Delta_Gold.txt
|   |       |   Highlight_Gold.txt
|   |       |   Time_Gold_Upcoming.txt
|   |       |   Time_Run_Current.txt
|   |       |   
|   |       \---Current Comparison
|   |               Delta_Run.txt
|   |               Delta_Segment.txt
|   |               Highlight_AheadGained.txt
|   |               Highlight_AheadLost.txt
|   |               Highlight_BehindGained.txt
|   |               Highlight_BehindLost.txt
|   |               Time_Run_Upcoming.txt
|   |               Time_Segment_Upcoming.txt
|   |               
|   +---SansSubsplits
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   Names_Indent_0.txt
|   |   |   Names_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   Delta_Gold.txt
|   |   |   |   Highlight_Gold.txt
|   |   |   |   Time_Gold_Upcoming.txt
|   |   |   |   Time_Run_Current.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           Delta_Run.txt
|   |   |           Delta_Segment.txt
|   |   |           Highlight_AheadGained.txt
|   |   |           Highlight_AheadLost.txt
|   |   |           Highlight_BehindGained.txt
|   |   |           Highlight_BehindLost.txt
|   |   |           Time_Run_Upcoming.txt
|   |   |           Time_Segment_Upcoming.txt
|   |   |           
|   |   \---RealTime
|   |       |   Delta_Gold.txt
|   |       |   Highlight_Gold.txt
|   |       |   Time_Gold_Upcoming.txt
|   |       |   Time_Run_Current.txt
|   |       |   
|   |       \---Current Comparison
|   |               Delta_Run.txt
|   |               Delta_Segment.txt
|   |               Highlight_AheadGained.txt
|   |               Highlight_AheadLost.txt
|   |               Highlight_BehindGained.txt
|   |               Highlight_BehindLost.txt
|   |               Time_Run_Upcoming.txt
|   |               Time_Segment_Upcoming.txt
|   |               
|   +---SansSubsplits_Level2
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   Names_Indent_0.txt
|   |   |   Names_Indent_1.txt
|   |   |   Names_Raw.txt
|   |   |   
|   |   +---GameTime
|   |   |   |   Delta_Gold.txt
|   |   |   |   Highlight_Gold.txt
|   |   |   |   Time_Gold_Upcoming.txt
|   |   |   |   Time_Run_Current.txt
|   |   |   |   
|   |   |   \---Current Comparison
|   |   |           Delta_Run.txt
|   |   |           Delta_Segment.txt
|   |   |           Highlight_AheadGained.txt
|   |   |           Highlight_AheadLost.txt
|   |   |           Highlight_BehindGained.txt
|   |   |           Highlight_BehindLost.txt
|   |   |           Time_Run_Upcoming.txt
|   |   |           Time_Segment_Upcoming.txt
|   |   |           
|   |   \---RealTime
|   |       |   Delta_Gold.txt
|   |       |   Highlight_Gold.txt
|   |       |   Time_Gold_Upcoming.txt
|   |       |   Time_Run_Current.txt
|   |       |   
|   |       \---Current Comparison
|   |               Delta_Run.txt
|   |               Delta_Segment.txt
|   |               Highlight_AheadGained.txt
|   |               Highlight_AheadLost.txt
|   |               Highlight_BehindGained.txt
|   |               Highlight_BehindLost.txt
|   |               Time_Run_Upcoming.txt
|   |               Time_Segment_Upcoming.txt
|   |               
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
\---Timers
    +---GameTime
    |       RunTimer.txt
    |       SegmentTimer_AllSplits.txt
    |       SegmentTimer_SansSubsplits.txt
    |       SegmentTimer_SansSubsplits_Level2.txt
    |       SegmentTimer_SansSubsplits_Level3.txt
    |       
    \---RealTime
            RunTimer.txt
            SegmentTimer_AllSplits.txt
            SegmentTimer_SansSubsplits.txt
            SegmentTimer_SansSubsplits_Level2.txt
            SegmentTimer_SansSubsplits_Level3.txt
```
