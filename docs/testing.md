# Testing changes

There is no test project. A change can be checked in two ways: in LiveSplit by hand, or headless by driving the built DLL from a small console program. The headless way is faster, gives exact times, and is how every bug in [TODO.md](TODO.md) marked "reproduced" was found.

## Build

```sh
dotnet build LiveSplit.OutputToFile.sln -c Debug
```

The build has one known warning, CS0219 (`currentSplitIndex`). Any other warning is new.

## In LiveSplit

1. Close LiveSplit. It locks the DLL while it's open.
2. Copy the DLL into LiveSplit's components folder:

   ```sh
   cp bin/Debug/LiveSplit.OutputToFile.dll "D:/Programs/Streaming/LiveSplit/Components/"
   ```

   The folder also holds `.backup` copies of older builds; leave them alone.
3. Open LiveSplit. Add the component if the layout doesn't have it yet: Edit Layout → + → Other → Output to File. Set the folder.
4. Go through each phase and check the files after every step:
   - before starting;
   - a split that gains time, one that loses time, and a gold;
   - a skip, then an undo;
   - a pause and a resume;
   - the finish;
   - a reset.

   Look at both `AllSplits` and `SansSubsplits`.
5. Repeat with a splits file that has no subsplits. There is only one level then, and level 0 is `AllSplits`.

## Headless

A .NET Framework 4.8 console app that references `LiveSplit.Core.dll` and the built component. It builds a `Run`, drives it through `TimerModel`, calls `Update()` to stand in for LiveSplit's frames, and reads the files back. Keep it outside the repo, for example in a scratch folder.

`Harness.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Windows.Forms" />
    <Reference Include="LiveSplit.Core"><HintPath>D:\Programs\Streaming\LiveSplit\LiveSplit.Core.dll</HintPath></Reference>
    <Reference Include="UpdateManager"><HintPath>D:\Programs\Streaming\LiveSplit\UpdateManager.dll</HintPath></Reference>
    <Reference Include="LiveSplit.OutputToFile"><HintPath>D:\Repositories\LiveSplit.OutputToFile\bin\Debug\LiveSplit.OutputToFile.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```

`Program.cs`:

```csharp
using System;
using System.IO;
using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.Options.SettingsFactories;
using LiveSplit.UI;
using LiveSplit.UI.Components;

static class Program
{
    static LiveSplitState state;
    static TimerModel model;
    static OutputToFileComponent comp;
    static string outDir;

    [STAThread]
    static void Main(string[] args)
    {
        outDir = args[0]; // use a fresh folder each run; old files linger otherwise

        // (name, PB split time, best segment), in seconds
        var run = new Run(new StandardComparisonGeneratorsFactory()) { GameName = "Game", CategoryName = "Any%" };
        foreach (var (name, pb, best) in new[] { ("-A1", 10.0, 9.0), ("-A2", 20.0, 9.0), ("{Area A}A3", 30.0, 9.0) })
            run.Add(new Segment(name, T(pb), T(best), null, new Time()));

        // StandardSettingsFactory throws outside LiveSplit; construct Settings directly.
        state = new LiveSplitState(run, null, new Layout(), new StandardLayoutSettingsFactory().Create(), new LiveSplit.Options.Settings());
        model = new TimerModel { CurrentState = state };
        state.CurrentComparison = Run.PersonalBestComparisonName;

        comp = new OutputToFileComponent(state);
        comp.Settings.FolderPath = outDir;
        comp.Settings.OutputTimer = true;
        comp.Settings.OutputCompletedSplits = true;
        Tick();

        model.Start();
        model.InitializeGameTime();
        state.IsGameTimePaused = true; // from here on, game time changes only through SetGameTime
        At(9.5); model.Split(); Tick();
        Console.WriteLine(Read(@"PreviousSplit\AllSplits\GameTime\Sign.txt"));
    }

    static Time T(double s) => new Time(TimeSpan.FromSeconds(s), TimeSpan.FromSeconds(s));

    // One LiveSplit frame, then wait for the files it queued. Call it after every event: phase changes are only acted on in Update().
    static void Tick()
    {
        comp.Update(null, state, 0, 0, LayoutMode.Vertical);
        comp.WaitForFileWrites();
    }

    // Set both clocks. Game time is exact; real time is accurate to a few milliseconds.
    static void At(double s)
    {
        state.AdjustedStartTime = TimeStamp.Now - TimeSpan.FromSeconds(s);
        state.SetGameTime(TimeSpan.FromSeconds(s));
    }

    static string Read(string rel) => File.ReadAllText(Path.Combine(outDir, rel)).Replace("\n", " | ");
}
```

Build it with `dotnet build`, then run `bin\Debug\net48\Harness.exe <output folder>`.

Things to know:

- **Check `GameTime` outputs.** Game time is exact, while real time drifts by a few milliseconds.
- **Call `Tick()` after every event.** Several bugs only appear on the frame after the event, when `Update()` reacts to the phase change.
- **Files are written in the background.** `Split()` and `Update()` only queue them, so call `comp.WaitForFileWrites()` before reading files; the sample's `Tick()` does. A failed write shows up in `comp.Settings.WriteError` on the next `Update()` after it fails. When timing, a stopwatch around `Split()` measures only the UI thread's part; time `WaitForFileWrites()` separately for the writes.
- **Advance the clock in steps of a second or less** when testing timer files. `HasTimerChanged` only compares the seconds component, so a jump of exactly a minute looks like no change.
- **Real time.** Setting `AdjustedStartTime` only affects real time while the timer is `Running`; while paused, real time stays frozen. `model.Pause()` toggles between pause and resume.
- **Time with averages.** The first `Split()` includes JIT compilation, around 100 ms.
- **Don't delete output files mid-run.** `FileQueue` skips contents it has already written, so a deleted file comes back only when its contents change or at reset. Use a fresh folder, or a new component, instead.
- **Rebuild the component first.** The harness references `bin\Debug`, so it tests whatever was last built.
- **Don't run it from the repo.** LiveSplit.Core saves speedrun.com's game list (about 2 MB) as `speedruncom_game_names.json` in the current directory once a run has a game name. Seen 2026-10-15.
- **Private members.** `TimeFormatter` is internal. Reach it by reflection: `typeof(OutputToFileComponent).Assembly.GetType("LiveSplit.OutputToFile.UI.Components.OutputToFile.TimeFormatter")`.

## Regression checks

These checks reproduce bugs tracked in [TODO.md](TODO.md); the Code column names the item. Each one shows today's output and the correct output, so a fix can be confirmed.

The base run is `-A1` (PB 10), `-A2` (PB 20), `{Area A}A3` (PB 30), `-B1` (PB 40), `{Area B}B2` (PB 50). Every best segment is 9 s. The splits happen at 9.5, 21, 28, 39 and 48 s of game time, with a `Tick()` after each.

| Code | Check | File | Today | Correct |
|---|---|---|---|---|
| NUL | After A3 (Area A: 28 vs 30) | `PreviousSplit\SansSubsplits\GameTime\Sign.txt` | `GoldAhead` | `GainedAhead` |
| IDX | Timer at 33.2 s | `Timers\GameTime\SegmentTimer_SansSubsplits.txt` | `0:23` | `0:05` |
| PAU | Pause after A3, then `Tick()` | `SplitList\AllSplits\GameTime\Delta_Run.txt` | `- \| - \| -2.00 \| \| \|` | `-0.5 \| +1.0 \| -2.0 \| \| \|` |
| RET | After the finish | `PreviousSplit\AllSplits\Name.txt` | `B1` | `B2` |
| RET | Finish, reset, `Tick()` | `CurrentSplit\AllSplits\Index.txt`, `Name.txt` | `4`, `B2` | `-1`, `-` |
| NUL | Splits `A`, `B`, `C`: skip A, split B at 19 | `PreviousSplit\AllSplits\GameTime\Sign.txt` | `GoldAhead` | `Ahead` |
| NEG | `DeltaString(-5.234 s)` | (call it directly) | `-5.23` | `-5.2` |
| EMP | Splits named `""` or `-` | constructor | throws | no exception |
| CLM | Splits `--Intro`, `A`, `B` | `SplitList\AllSplits\Names.txt`; finishing the run | `A \| B`; throws | three names; finishes |
| SEC | Tick every 0.5 s to 60.5 s, then reset | `Timers\GameTime\RunTimer.txt` | `1:00` | `0:00` |

The correct negative values (`-0.5`, `-2.0`) assume the `DeltaString` precision fix. Before that fix, negatives get centiseconds: `-.50`, `-2.00`.
