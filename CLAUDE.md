# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A LiveSplit layout component (.NET Framework 4.8.1 class library, WinForms) that draws nothing. Instead it writes split and timer information to `.txt` files in a user-chosen folder, for use as OBS text sources. `README.md` is the end-user documentation of the output folder tree, and `CHANGELOG.md` the end-user changelog.

Remotes: `origin` is this fork (lotsofs); `joshimuz` is a related fork that changes are sometimes merged from.

## Docs

Read the doc that matches the task before editing:

| Doc | Read it for |
|---|---|
| [docs/architecture.md](docs/architecture.md) | How it works: event flow, subsplit levels (with a worked example), the per-level arrays, phase-dependent indexes, how each value is calculated, file writing, settings |
| [docs/output-files.md](docs/output-files.md) | Every output file, its contents in each timer phase, and the `Sign.txt` values and time formats. More complete and accurate than the README tree. |
| [docs/livesplit-notes.md](docs/livesplit-notes.md) | LiveSplit behavior the code relies on: event order, when PB and best segments update, `GraphicsCache`. Each fact is marked verified or not. |
| [docs/testing.md](docs/testing.md) | Testing in LiveSplit by hand, or headless with a copy-paste test program, plus regression checks for the known bugs |
| [docs/TODO.md](docs/TODO.md) | **Start here for open work.** Index of tracked items (bugs, performance, cleanup, features, build and README) by three-letter code. Each code links to its section in a detail doc, with evidence, a plan and how to verify it. |
| [docs/DONE.md](docs/DONE.md) | Log of finished TODO items: what was done and when, newest first |
| [CHANGELOG.md](CHANGELOG.md) | What changed for users in each release, and what is unreleased |

The docs refer to code by member name and file, not by line number. When a change makes a doc wrong, update the doc in the same change. When you finish a TODO item, delete its index row and its detail section, and add a row to docs/DONE.md. Items finished in the same change share one row. When you find a defect you aren't fixing now, add both.

**Changelog.** When a change is one a user of the latest release would notice (output files, their contents, settings, how LiveSplit behaves), add it under "Unreleased" in CHANGELOG.md, in the same change. Write it for users: what they see in the files or the settings, with no TODO codes or class names. A fix to something added since the latest release isn't an entry of its own; update that feature's entry if needed. Releases are the git tags (`1` … `6`); when one is tagged, "Unreleased" becomes its section.

## Build / deploy

```sh
dotnet build LiveSplit.OutputToFile.sln -c Debug      # or -c Release
cp bin/Debug/LiveSplit.OutputToFile.dll "D:/Programs/Streaming/LiveSplit/Components/"   # deploy
```

- This is an old-style (non-SDK) csproj. `dotnet build` works without Visual Studio as long as the .NET Framework 4.8.1 targeting pack (the 4.8.1 Developer Pack) is installed. The one expected build warning is CS0219 (`currentSplitIndex`).
- It targets 4.8.1 because LiveSplit does from 1.8.37. With a lower target, MSBuild won't resolve the LiveSplit DLLs (MSB3274), and the build fails with missing `LiveSplit.Model` types.
- Without the Developer Pack, the build can use the NuGet reference assemblies instead. Any SDK-style net481 build, such as the testing.md harness, puts them in the NuGet cache. Then pass `-p:TargetFrameworkRootPath=<NuGet cache>/microsoft.netframework.referenceassemblies.net481/<version>/build/`.
- Old-style also means the csproj lists every source file. A new `.cs` file needs its own `<Compile Include>` line, or it isn't built.
- `LiveSplit.Core.dll` and `UpdateManager.dll` are referenced by HintPath `..\..\Programs\Streaming\LiveSplit\`, which is `D:\Programs\Streaming\LiveSplit`. The build fails if LiveSplit isn't installed there.
- Deploying overwrites the DLL that LiveSplit actually loads. LiveSplit must be closed first, because it locks that DLL. `LiveSplitComponents.lnk` in the repo root is a gitignored shortcut to the `Components` folder. That folder also holds the user's `.backup` copies of older builds; leave them alone.
- There are no tests and no linter. Verify changes headless (see docs/testing.md), or in LiveSplit.

## Must-know before editing

The code is in `UI/Components/`:

| File | Holds |
|---|---|
| `OutputToFileComponent.cs` | The LiveSplit side: timer events, change detection in `Update()`, and when each writer runs (`WriteSplits()`) |
| `SplitLevels.cs` | `SplitLevels` and `SplitLevel`: depths, display names, each level's members and PB segment times |
| `OutputWriter.cs` | Base class of the writers: `MakeFile`, `GetCurrentSubsplitIndex`, `GetPreviousSubsplitIndex`, and this attempt's times and deltas (`LiveSegmentTime`, `RunDelta`, `IsGold`, …) |
| `FileQueue.cs` | Writes the files on a background thread, skipping unchanged contents; keeps the latest write error |
| `RunInfoFiles.cs` | Writes the root files (`GameName.txt`, `TotalSplits_*.txt`, …) |
| `TimerFiles.cs` | Writes `Timers\` |
| `SplitFiles.cs` | Writes `CurrentSplit\` and `PreviousSplit\`, including `Sign.txt` |
| `SplitListFiles.cs` | Writes `SplitList\` and `CompletedSplits\` |
| `Signs.cs` | The `Sign.txt` values |
| `TimeFormatter.cs` | Time and delta strings |
| `OutputToFileSettings.cs` | The settings control and its XML save/load |

- **Levels.** A split's depth is its number of leading `-`. Level `lvl` contains the splits with depth ≤ `lvl`.
  - The **last** level holds every split and is tagged `AllSplits`. Level 0 is `SansSubsplits`; levels in between are `SansSubsplits_Level{lvl+1}`, named after the depth they leave out. With a single level, level 0 is `AllSplits`.
  - `Levels[lvl].Indices` maps **local** index → **global** index, i.e. into `State.Run`. Mixing the two up is a recurring bug source. Only at the last level are they the same.
  - Golds exist only at the last level. Keep the `lvl == Levels.Count - 1` guard on any gold output. `IsGold` decides what counts as a gold and includes that guard; don't test the gold delta instead, because it is also null when there is no gold.
- **Timer phases.** `GetCurrentSubsplitIndex` and `GetPreviousSubsplitIndex` return sentinel values depending on the phase (−1/−2 when not running, `Count`/`Count−1` when finished). Every write path needs correct output for NotRunning, Running/Paused and Ended. Otherwise files keep stale values.
- **Nothing per attempt is stored.** This attempt's times and deltas are computed from the run on every write, by `OutputWriter`'s `LiveRunTime`, `LiveSegmentTime`, `RunDelta`, `SegmentDelta`, `GoldDelta` and `IsGold`. `CalculateSegments()` rebuilds the shared `SplitLevels` in place on every cache-key change, including every timer phase change, so don't keep per-attempt state in it: a pause would wipe it.
- **`Update()` runs every frame.** Keep work behind its `Cache` check. Only the keys listed there trigger a rewrite. The timer files are the exception: `TimerFiles` has its own check, `HasTimerChanged`, on what each timer file shows and the timer phase. It caches each level's segment start; anything new that moves a split time or rebuilds the levels must call `SegmentStartsChanged()`.
- **Writing files.** Always write through `OutputWriter`'s `MakeFile` overloads and a `FILE_*` path template (`{0}` timing method, `{1}` level tag). Each writer declares the templates for its own folder. Paths use backslashes, so this is Windows-only.
  - `MakeFile` only queues: `FileQueue` writes on a thread-pool thread, keeping the latest contents per file and skipping contents already on disk. `OnReset` clears that record. Anything that reads the files (tests, the headless harness) must call `WaitForFileWrites()` first.
  - A full rewrite in `Update()` must queue every file the settings and levels call for, whatever the phase. `FileQueue.EndSweep` empties any file in the component's folders (`OUTPUT_FOLDERS`, plus root files matching `OUTPUT_ROOT_FILES`) that wasn't queued, so a file written in only some phases would be emptied in the others.
  - The drain catches every exception per file, and `Update()` shows the latest in the settings panel (`ShowWriteError`). Only touch the settings control from the UI thread. Don't add file access that bypasses `FileQueue`: on the UI thread an exception would escape into LiveSplit's `Split()`/`Update()`.
- **One list writer.** `SplitList\` and `CompletedSplits\` are both written by `WriteSplitListLevel` in `SplitListFiles`. They differ only in the range of splits that `WriteSplitLists` passes in. Their `FILE_LIST_*` templates are relative to the list folder, so prefix `FOLDER_SPLITLIST` or `FOLDER_COMPLETEDSPLITS`. A `ListLines` builds every file of one list a line at a time, so each file gets one line per split, blank where it has nothing.

## Making changes

**Adding an output file:**
1. Add a `FILE_*` template to the writer for its folder (see the file table above). Root files go in `RunInfoFiles`; a root file that isn't always written must also match `OUTPUT_ROOT_FILES` in the component, or it won't be emptied when it's no longer written.
2. Write it from that writer's `Write*` function, in every phase branch.
3. If it's a list column, make it a `FILE_LIST_*` template, give it a `StringBuilder` field in `ListLines` (created with `Add` in its constructor), and append its line in `AppendNames`, `AppendCompletedLine` or `AppendUpcomingLine`. Both lists then get it.
4. Write it once per timing method only if its path contains `{0}`.
5. Document it in `docs/output-files.md`, `README.md` and `CHANGELOG.md`.

A new output folder gets its own `OutputWriter` subclass. Add it to the csproj, construct it in the component's constructor with the shared `_levels` and `_files`, and call it from `WriteSplits()` or `Update()`. Add the folder to `OUTPUT_FOLDERS`, so its files are emptied when they're no longer written.

**Adding a setting** (`OutputToFileSettings`, a WinForms `UserControl` with its layout in `.Designer.cs`/`.resx`):
1. Add the property, plus its default and `DataBindings.Add` call in the constructor. Binding is what updates the property from the control.
2. Add the control in the Designer file. A `*_Changed` handler is only needed for side effects, like enabling a group box.
3. Add it to `CreateSettingsNode`. That method handles both the XML save and `GetSettingsHashCode`. LiveSplit uses the hash only to decide whether to redraw. The save-on-close prompt doesn't depend on it.
4. Add it to `SetSettings`, which loads the XML. The fallback value applies to layouts saved before the key existed.
5. If the setting affects output, add it to the `Cache[...]` keys in `Update()`.
