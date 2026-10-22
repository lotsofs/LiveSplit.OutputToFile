# LiveSplit.OutputToFile

## About

A component for LiveSplit which outputs information to a text file in a folder you choose, which can then be added to OBS as a text source.

## Updates

From release 8 on, LiveSplit tells you at startup when a newer release is out, shows what changed, and asks whether to update. If you choose Yes, LiveSplit closes, installs the new release and opens again. Release 8 itself has to be installed by hand, by replacing `LiveSplit.OutputToFile.dll` in LiveSplit's `Components` folder while LiveSplit is closed.

## Output Directory Tree

Total Splits is the number of splits.

Number is the current split's number, counting from 1. It's `-` before the timer starts and after the finish.

Completed is the number of splits done, skipped ones included.

Remaining is the number of splits not done yet, the current one included.

After is the number of splits after the current one. It's `-` before the timer starts and after the finish.

Highlights are a shaded bar to layer behind splits.

The files of an output you turn off in the settings, or of a subsplit level your splits no longer have, are emptied rather than deleted, so text sources that use them show nothing. The component treats everything in its folders (`CurrentSplit`, `PreviousSplit`, `SplitList`, `CompletedSplits` and `Timers`) as its own, so don't keep other files there.

## Subsplits
In addition to the ``AllSplits`` folder, which takes into consideration every split regardless of whether it is a subsplit or not, if applicable there will also be a ``SansSubsplits`` folder. These will only include splits that do not start with a dash (``-``) (These appear to be referred to in the official subsplits module as 'Section Headers'). Furthermore, the component supports multi-level subsplits, which are subsplits of subsplits, and are created by putting two (2) or more dashes in front of your splitname. Each of these levels gets a folder named after the dashes it leaves out: ``SansSubsplits_Level2`` has every split except those with two or more dashes, ``SansSubsplits_Level3`` every split except those with three or more, and so on. There is no limit to how many levels deep you can go.

Split names in the output leave out the leading dashes and one space after them. To show a name that starts with a dash, put a space before it: a subsplit named ``- -40`` shows as ``-40``.

Every folder has its own golds. In ``AllSplits`` they are LiveSplit's golds. In the ``SansSubsplits`` folders, a section that groups several splits has its own gold: the fastest you have done the whole section within one attempt, taken from your splits' history. That's usually slower than its subsplits' golds added up, because those come from different attempts. A section you have never finished in one attempt has no gold yet, so finishing it counts as one.

```
|   AttemptCount.txt
|   CategoryName.txt
|   FinishedRunsCount.txt
|   GameName.txt
|   TotalSplits_AllSplits.txt
|   TotalSplits_SansSubsplits.txt
|   TotalSplits_SansSubsplits_Level2.txt
|   TotalSplits_SansSubsplits_Level3.txt
|   
+---CurrentSplit
|   +---AllSplits
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldTime.txt
|   |   |       RunTime.txt
|   |   |       SegmentTime.txt
|   |   |       
|   |   \---RealTime
|   |           GoldTime.txt
|   |           RunTime.txt
|   |           SegmentTime.txt
|   |           
|   +---SansSubsplits
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldTime.txt
|   |   |       RunTime.txt
|   |   |       SegmentTime.txt
|   |   |       
|   |   \---RealTime
|   |           GoldTime.txt
|   |           RunTime.txt
|   |           SegmentTime.txt
|   |           
|   +---SansSubsplits_Level2
|   |   |   After.txt
|   |   |   Completed.txt
|   |   |   Name.txt
|   |   |   Number.txt
|   |   |   Remaining.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldTime.txt
|   |   |       RunTime.txt
|   |   |       SegmentTime.txt
|   |   |       
|   |   \---RealTime
|   |           GoldTime.txt
|   |           RunTime.txt
|   |           SegmentTime.txt
|   |           
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
+---PreviousSplit
|   +---AllSplits
|   |   |   Name.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldDifference.txt
|   |   |       RunDifference.txt
|   |   |       RunTime.txt
|   |   |       SegmentDifference.txt
|   |   |       SegmentTime.txt
|   |   |       Sign.txt
|   |   |       
|   |   \---RealTime
|   |           GoldDifference.txt
|   |           RunDifference.txt
|   |           RunTime.txt
|   |           SegmentDifference.txt
|   |           SegmentTime.txt
|   |           Sign.txt
|   |           
|   +---SansSubsplits
|   |   |   Name.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldDifference.txt
|   |   |       RunDifference.txt
|   |   |       RunTime.txt
|   |   |       SegmentDifference.txt
|   |   |       SegmentTime.txt
|   |   |       Sign.txt
|   |   |       
|   |   \---RealTime
|   |           GoldDifference.txt
|   |           RunDifference.txt
|   |           RunTime.txt
|   |           SegmentDifference.txt
|   |           SegmentTime.txt
|   |           Sign.txt
|   |           
|   +---SansSubsplits_Level2
|   |   |   Name.txt
|   |   |   
|   |   +---GameTime
|   |   |       GoldDifference.txt
|   |   |       RunDifference.txt
|   |   |       RunTime.txt
|   |   |       SegmentDifference.txt
|   |   |       SegmentTime.txt
|   |   |       Sign.txt
|   |   |       
|   |   \---RealTime
|   |           GoldDifference.txt
|   |           RunDifference.txt
|   |           RunTime.txt
|   |           SegmentDifference.txt
|   |           SegmentTime.txt
|   |           Sign.txt
|   |           
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
+---SplitList
|   +---AllSplits
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   
|   |   +---GameTime
|   |   |       Delta_Gold.txt
|   |   |       Delta_Run.txt
|   |   |       Delta_Segment.txt
|   |   |       Highlight_AheadGained.txt
|   |   |       Highlight_AheadLost.txt
|   |   |       Highlight_BehindGained.txt
|   |   |       Highlight_BehindLost.txt
|   |   |       Highlight_Gold.txt
|   |   |       Time_Gold_Upcoming.txt
|   |   |       Time_Run_Current.txt
|   |   |       Time_Run_Upcoming.txt
|   |   |       Time_Segment_Upcoming.txt
|   |   |       
|   |   \---RealTime
|   |           Delta_Gold.txt
|   |           Delta_Run.txt
|   |           Delta_Segment.txt
|   |           Highlight_AheadGained.txt
|   |           Highlight_AheadLost.txt
|   |           Highlight_BehindGained.txt
|   |           Highlight_BehindLost.txt
|   |           Highlight_Gold.txt
|   |           Time_Gold_Upcoming.txt
|   |           Time_Run_Current.txt
|   |           Time_Run_Upcoming.txt
|   |           Time_Segment_Upcoming.txt
|   |           
|   +---SansSubsplits
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   
|   |   +---GameTime
|   |   |       Delta_Gold.txt
|   |   |       Delta_Run.txt
|   |   |       Delta_Segment.txt
|   |   |       Highlight_AheadGained.txt
|   |   |       Highlight_AheadLost.txt
|   |   |       Highlight_BehindGained.txt
|   |   |       Highlight_BehindLost.txt
|   |   |       Highlight_Gold.txt
|   |   |       Time_Gold_Upcoming.txt
|   |   |       Time_Run_Current.txt
|   |   |       Time_Run_Upcoming.txt
|   |   |       Time_Segment_Upcoming.txt
|   |   |       
|   |   \---RealTime
|   |           Delta_Gold.txt
|   |           Delta_Run.txt
|   |           Delta_Segment.txt
|   |           Highlight_AheadGained.txt
|   |           Highlight_AheadLost.txt
|   |           Highlight_BehindGained.txt
|   |           Highlight_BehindLost.txt
|   |           Highlight_Gold.txt
|   |           Time_Gold_Upcoming.txt
|   |           Time_Run_Current.txt
|   |           Time_Run_Upcoming.txt
|   |           Time_Segment_Upcoming.txt
|   |           
|   +---SansSubsplits_Level2
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   
|   |   +---GameTime
|   |   |       Delta_Gold.txt
|   |   |       Delta_Run.txt
|   |   |       Delta_Segment.txt
|   |   |       Highlight_AheadGained.txt
|   |   |       Highlight_AheadLost.txt
|   |   |       Highlight_BehindGained.txt
|   |   |       Highlight_BehindLost.txt
|   |   |       Highlight_Gold.txt
|   |   |       Time_Gold_Upcoming.txt
|   |   |       Time_Run_Current.txt
|   |   |       Time_Run_Upcoming.txt
|   |   |       Time_Segment_Upcoming.txt
|   |   |       
|   |   \---RealTime
|   |           Delta_Gold.txt
|   |           Delta_Run.txt
|   |           Delta_Segment.txt
|   |           Highlight_AheadGained.txt
|   |           Highlight_AheadLost.txt
|   |           Highlight_BehindGained.txt
|   |           Highlight_BehindLost.txt
|   |           Highlight_Gold.txt
|   |           Time_Gold_Upcoming.txt
|   |           Time_Run_Current.txt
|   |           Time_Run_Upcoming.txt
|   |           Time_Segment_Upcoming.txt
|   |           
|   \---SansSubsplits_Level3
|       |   etc.
|       …
|               
\---Timers
    +---GameTime
    |       RunTimer.txt
    |       SegmentTimer_AllSplits.txt
    |       
    \---RealTime
            RunTimer.txt
            SegmentTimer_AllSplits.txt
            SegmentTimer_SansSubsplits.txt
            SegmentTimer_SansSubsplits_Level2.txt
            SegmentTimer_SansSubsplits_Level3.txt
```
