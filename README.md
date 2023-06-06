# LiveSplit.OutputToFile

## About

A component for LiveSplit which outputs information to a text file in a folder you choose, which can then be added to OBS as a text source.

## Output Directory Tree

Total Splits is the number of splits.

Index is the number of splits finished (position/progress).

Reverse Index is the number of splits remaining.

Highlights are a shaded bar to layer behind splits.

```
│   AttemptCount.txt
│   CategoryName.txt
│   FinishedRunsCount.txt
│   GameName.txt
│   TotalSplits_AllSplits.txt
│
├───CurrentSplit
│   └───AllSplits
│       │   Index.txt
│       │   Name.txt
│       │   ReverseIndex.txt
│       │
│       ├───GameTime
│       │       GoldTime.txt
│       │       RunTime.txt
│       │       SegmentTime.txt
│       │
│       └───RealTime
│               GoldTime.txt
│               RunTime.txt
│               SegmentTime.txt
│
├───PreviousSplit
│   └───AllSplits
│       │   Name.txt
│       │
│       ├───GameTime
│       │       GoldDifference.txt
│       │       RunDifference.txt
│       │       RunTime.txt
│       │       SegmentDifference.txt
│       │       SegmentTime.txt
│       │       Sign.txt
│       │
│       └───RealTime
│               GoldDifference.txt
│               RunDifference.txt
│               RunTime.txt
│               SegmentDifference.txt
│               SegmentTime.txt
│               Sign.txt
│
├───SplitList
│   └───AllSplits
│       │   Highlight_CurrentSplit.txt
│       │   Names.txt
│       │
│       ├───GameTime
│       │       Delta_Gold.txt
│       │       Delta_Run.txt
│       │       Delta_Segment.txt
│       │       Highlight_AheadGained.txt
│       │       Highlight_AheadLost.txt
│       │       Highlight_BehindGained.txt
│       │       Highlight_BehindLost.txt
│       │       Highlight_Gold.txt
│       │       Time_Gold_Upcoming.txt
│       │       Time_Run_Current.txt
│       │       Time_Run_Upcoming.txt
│       │       Time_Segment_Upcoming.txt
│       │
│       └───RealTime
│               Delta_Gold.txt
│               Delta_Run.txt
│               Delta_Segment.txt
│               Highlight_AheadGained.txt
│               Highlight_AheadLost.txt
│               Highlight_BehindGained.txt
│               Highlight_BehindLost.txt
│               Highlight_Gold.txt
│               Time_Gold_Upcoming.txt
│               Time_Run_Current.txt
│               Time_Run_Upcoming.txt
│               Time_Segment_Upcoming.txt
│
└───Timers
    ├───GameTime
    │       RunTimer.txt
    │       SegmentTimer_AllSplits.txt
    │
    └───RealTime
            RunTimer.txt
            SegmentTimer_AllSplits.txt
```
