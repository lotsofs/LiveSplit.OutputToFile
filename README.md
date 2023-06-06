# LiveSplit.OutputToFile

## About

A component for LiveSplit which outputs information to a text file in a folder you choose, which can then be added to OBS as a text source.

## Output Directory Tree

Total Splits is the number of splits.

Index is the number of splits finished (position/progress).

Reverse Index is the number of splits remaining.

Highlights are a shaded bar to layer behind splits.

## Subsplits
In addition to the ``AllSplits`` folder, which takes into consideration every split regardless of whether it is a subsplit or not, if applicable there will also be a ``SansSubsplits`` folder. These will only include splits that do not start with a dash (``-``) (These appear to be referred to in the official subsplits module as 'Section Headers'). Furthermore, the component supports multi-level subsplits, which are subsplits of subsplits, these show up as ``SansSubsplits_Level2`` and are created by putting two (2) dashes in front of your splitname. There is no limit to how many levels deep you can go.

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
|   |   |   Index.txt
|   |   |   Name.txt
|   |   |   ReverseIndex.txt
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
|   |   |   Index.txt
|   |   |   Name.txt
|   |   |   ReverseIndex.txt
|   |   |   
|   |   +---GameTime
|   |   |       RunTime.txt
|   |   |       SegmentTime.txt
|   |   |       
|   |   \---RealTime
|   |           RunTime.txt
|   |           SegmentTime.txt
|   |           
|   +---SansSubsplits_Level2
|   |   |   Index.txt
|   |   |   Name.txt
|   |   |   ReverseIndex.txt
|   |   |   
|   |   +---GameTime
|   |   |       RunTime.txt
|   |   |       SegmentTime.txt
|   |   |       
|   |   \---RealTime
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
|   |   |       RunDifference.txt
|   |   |       RunTime.txt
|   |   |       SegmentDifference.txt
|   |   |       SegmentTime.txt
|   |   |       Sign.txt
|   |   |       
|   |   \---RealTime
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
|   |   |       RunDifference.txt
|   |   |       RunTime.txt
|   |   |       SegmentDifference.txt
|   |   |       SegmentTime.txt
|   |   |       Sign.txt
|   |   |       
|   |   \---RealTime
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
|   |   |       Delta_Run.txt
|   |   |       Delta_Segment.txt
|   |   |       Highlight_AheadGained.txt
|   |   |       Highlight_AheadLost.txt
|   |   |       Highlight_BehindGained.txt
|   |   |       Highlight_BehindLost.txt
|   |   |       Time_Run_Current.txt
|   |   |       Time_Run_Upcoming.txt
|   |   |       Time_Segment_Upcoming.txt
|   |   |       
|   |   \---RealTime
|   |           Delta_Run.txt
|   |           Delta_Segment.txt
|   |           Highlight_AheadGained.txt
|   |           Highlight_AheadLost.txt
|   |           Highlight_BehindGained.txt
|   |           Highlight_BehindLost.txt
|   |           Time_Run_Current.txt
|   |           Time_Run_Upcoming.txt
|   |           Time_Segment_Upcoming.txt
|   |           
|   +---SansSubsplits_Level2
|   |   |   Highlight_CurrentSplit.txt
|   |   |   Names.txt
|   |   |   
|   |   +---GameTime
|   |   |       Delta_Run.txt
|   |   |       Delta_Segment.txt
|   |   |       Highlight_AheadGained.txt
|   |   |       Highlight_AheadLost.txt
|   |   |       Highlight_BehindGained.txt
|   |   |       Highlight_BehindLost.txt
|   |   |       Time_Run_Current.txt
|   |   |       Time_Run_Upcoming.txt
|   |   |       Time_Segment_Upcoming.txt
|   |   |       
|   |   \---RealTime
|   |           Delta_Run.txt
|   |           Delta_Segment.txt
|   |           Highlight_AheadGained.txt
|   |           Highlight_AheadLost.txt
|   |           Highlight_BehindGained.txt
|   |           Highlight_BehindLost.txt
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
