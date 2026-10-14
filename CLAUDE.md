# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A LiveSplit layout component (.NET Framework 4.8 class library, WinForms) that draws nothing. Instead it writes split and timer information to `.txt` files in a user-chosen folder, for use as OBS text sources. `README.md` is the end-user documentation of the output folder tree.

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

Line numbers in the docs are as of commit `30d30fa`. When a change makes a doc wrong, update the doc in the same change. When you finish a TODO item, delete its index row and its detail section, and add a row to docs/DONE.md. When you find a defect you aren't fixing now, add both.

## Build / deploy

```sh
dotnet build LiveSplit.OutputToFile.sln -c Debug      # or -c Release
cp bin/Debug/LiveSplit.OutputToFile.dll "D:/Programs/Streaming/LiveSplit/Components/"   # deploy
```

- This is an old-style (non-SDK) csproj. `dotnet build` works without Visual Studio as long as the .NET Framework 4.8 targeting pack is installed. The one expected build warning is CS0219 (`currentSplitIndex`).
- `LiveSplit.Core.dll` and `UpdateManager.dll` are referenced by HintPath `..\..\Programs\Streaming\LiveSplit\`, which is `D:\Programs\Streaming\LiveSplit`. The build fails if LiveSplit isn't installed there.
- Deploying overwrites the DLL that LiveSplit actually loads. LiveSplit must be closed first, because it locks that DLL. `LiveSplitComponents.lnk` in the repo root is a gitignored shortcut to the `Components` folder. That folder also holds the user's `.backup` copies of older builds; leave them alone.
- There are no tests and no linter. Verify changes headless (see docs/testing.md), or in LiveSplit.

## Must-know before editing

All logic is in `UI/Components/OutputToFileComponent.cs`.

- **Levels.** A split's depth is its number of leading `-`. Level `lvl` contains the splits with depth ≤ `lvl`.
  - The **last** level holds every split and is tagged `AllSplits`. Level 0 is `SansSubsplits`; levels in between are `SansSubsplits_Level{n}`. With a single level, level 0 is `AllSplits`.
  - `_splitLevelIndices[lvl]` maps **local** index → **global** index, i.e. into `State.Run`. Mixing the two up is a recurring bug source. Only at the last level are they the same.
  - Golds exist only at the last level. Keep the `lvl == _splitLevels.Length - 1` guard on any gold output.
- **Timer phases.** `GetCurrentSubsplitIndex` and `GetPreviousSubsplitIndex` return sentinel values depending on the phase (−1/−2 when not running, `Count`/`Count−1` when finished). Every write path needs correct output for NotRunning, Running/Paused and Ended. Otherwise files keep stale values.
- **Stored deltas.** Deltas are stored at split time by `WriteSplitTimes` and read later by the split lists, so the call order in `WriteSplits()` matters. `CalculateSegments()` rebuilds every array and discards those stored deltas, and it runs on every cache-key change, including timer phase changes.
- **`Update()` runs every frame.** Keep work behind its `Cache` check. Only the keys listed there trigger a rewrite.
- **Writing files.** Always write through the `MakeFile` overloads and the `FILE_*` path templates (`{0}` timing method, `{1}` level tag). Paths use backslashes, so this is Windows-only.
- **Two list writers.** `WriteSplitList` and `WriteCompletedSplitList` are near-duplicates. Mirror changes in both.

## Making changes

**Adding an output file:**
1. Add a `FILE_*` template.
2. Write it from the right `Write*` function, in every phase branch.
3. If it's a list column, add it to both list writers.
4. Write it once per timing method only if its path contains `{0}`.
5. Document it in `docs/output-files.md` and `README.md`.

**Adding a setting** (`OutputToFileSettings`, a WinForms `UserControl` with its layout in `.Designer.cs`/`.resx`):
1. Add the property, plus its default and `DataBindings.Add` call in the constructor. Binding is what updates the property from the control.
2. Add the control in the Designer file. A `*_Changed` handler is only needed for side effects, like enabling a group box.
3. Add it to `CreateSettingsNode`. That method handles both the XML save and `GetSettingsHashCode`. LiveSplit uses the hash only to decide whether to redraw. The save-on-close prompt doesn't depend on it.
4. Add it to `SetSettings`, which loads the XML. The fallback value applies to layouts saved before the key existed.
5. If the setting affects output, add it to the `Cache[...]` keys in `Update()`.
