# Testing changes

The tests in `Tests\` drive the built component headless, the way LiveSplit does, and read back the files it writes. Run them after every change. What they can't check is checked by hand: in LiveSplit, or headless with a temporary test or program. Driving the DLL headless is also how every bug in [TODO.md](TODO.md) marked "reproduced" was found.

## Build and run the tests

```sh
dotnet build LiveSplit.OutputToFile.sln -c Debug   # the component and the tests
dotnet test LiveSplit.OutputToFile.sln             # builds first, then runs every test, in about 10 s
```

- The build has no warnings, so any warning is new. The component needs the .NET Framework 4.8.1 targeting pack (see CLAUDE.md). The test project is SDK-style and gets the reference assemblies from NuGet.
- The test project builds the component through a project reference, so `dotnet test` always tests the current code. With `--no-build`, it tests whatever was built last.
- Like the component, it takes LiveSplit's folder from `LiveSplitDir`. It references `LiveSplit.Core.dll` and `UpdateManager.dll` itself, copied into its `bin\`, because the component's build doesn't copy them.
- Its packages (xunit, the test SDK) restore from the NuGet cache, so the build works offline once they're there.
- `deploy.cmd` builds only the component, not the tests.

## The tests

Each test plays a run through LiveSplit's `TimerModel`, lets the component write its files into a new folder in `%TEMP%`, and checks them. Tests don't run in parallel: LiveSplit calls the component from one thread.

| File | Holds |
|---|---|
| `TestRun.cs` | `TestRun`, which every test uses: it builds a run, the timer and the component with every output on, and plays it (`Start`, `Split(s)`, `Skip`, `Undo`, `Pause`, `Reset`, `PlayAttempt`, `Edit`, `SelectComparison`). Each action is followed by `Tick()`, one LiveSplit frame that waits for the writes. `Read`, `Lines` and `Exists` read the files. |
| `Snapshot.cs` | Records a run's whole output, step by step, and compares it with `Snapshots\{name}.txt` |
| `SnapshotTests.cs` | Whole runs through every phase: [the base run](#the-base-run), the same without subsplits, and a run with a segment history and a second comparison |
| `*Tests.cs` | Tests of one value or behaviour each, named after what they check, grouped by output folder or class |

The tests use the internal classes directly (`TimeFormatter`, `FileQueue`, `SplitLevels`): `AssemblyInfo.cs` makes them visible to the test project. The test project is SDK-style, so a new file is built without being added to the csproj.

### Snapshots

A snapshot lists, for each step of a run, every file whose contents changed since the step before; the first step lists every file. Each line of a file's contents is shown after `  |`, so a list file's final `\n` shows as a last empty line. `RealTime` files show only `(real time)` or `(empty)`, since real time drifts.

When the output differs, the test fails with the first differing line, its step and file, and writes the whole output to `Snapshots\{name}.received.txt` (gitignored). If the change is intended:

```sh
UPDATE_SNAPSHOTS=1 dotnet test LiveSplit.OutputToFile.sln   # PowerShell: $env:UPDATE_SNAPSHOTS=1; dotnet test …; Remove-Item Env:UPDATE_SNAPSHOTS
```

This writes the snapshots instead of comparing them. Then read the snapshots' git diff: it shows exactly which files changed, at which step. A change that alters the output updates the snapshots in the same change.

### Adding a test

- A fixed bug gets a test that fails without the fix. Check that it does, by undoing the fix once.
- Work out the expected values from [output-files.md](output-files.md) first, then run. When a test fails, find out why; don't copy in what the code writes.
- A test for an open item in [Defects.md](Defects.md) asserts the correct output and is skipped until the item is fixed: `[Fact(Skip = "CODE: what's wrong")]`. Fixing the item removes the `Skip`.
- Expected strings use LiveSplit's minus sign `−` (U+2212) for negatives.

### The base run

`TestRun.Base()`: `-A1` (PB 10), `-A2` (PB 20), `{Area A}A3` (PB 30), `-B1` (PB 40), `{Area B}B2` (PB 50). Every best segment is 9 s. `PlayBaseRun()` splits at 9.5, 21, 28, 39 and 48 s of game time. With every output on and no comparison ticked, it produces 169 files.

The base run has no segment history. So in `SansSubsplits`, `Area A` and `Area B`, which span several splits, have no best segment: their gold files show `-`, and completing them counts as a gold.

## In LiveSplit

1. Close LiveSplit. It locks the DLL while it's open.
2. Build and copy the DLL into LiveSplit's components folder with `deploy.cmd` in the repo root (`.\deploy` from PowerShell). It refuses while LiveSplit is running, and copies only after a successful build.

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

### Driving LiveSplit's UI

A small UI Automation program (references `UIAutomationClient`, `UIAutomationTypes` and `WindowsBase`) can open the settings in the real LiveSplit, take screenshots and press Tab. Seen working 2026-10-21 with LiveSplit 1.8.37. It shows windows, takes focus and moves the mouse, so ask whoever is at the machine first.

- **Layout.** `LiveSplit.exe -l <layout>`, started in LiveSplit's folder, loads that layout. Use a test layout with only this component and a scratch output folder, so nothing is written to the real one.
- **Settings.** Back up `settings.cfg` and restore it afterwards: LiveSplit adds the layout to its recent layouts when it closes.
- **Foreground.** A program in the background can't bring LiveSplit to the front with `SetForegroundWindow` alone. Attach to the foreground thread's input with `AttachThreadInput` first. Before every click or key press, check that the window there is LiveSplit's; otherwise the click lands on whatever is in front.
- **Dialogs.** Right-click the main window for its menu. **Edit Layout...** opens the Layout Editor, and its **Layout Settings** button opens a dialog with a tab per component. UI Automation shows these dialogs as children of the window that owns them, not as top-level windows.
- **Tab order.** A WinForms control's `Name` is its UI Automation `AutomationId`, so reading `AutomationElement.FocusedElement` after each `{TAB}` gives the tab order. A focused `NumericUpDown` shows up as the panel that holds it.
- **Screenshots.** A window's bounds include an invisible border about 7 px wide that shows what's behind it. Compare screenshots inside that border.
- **Closing.** Cancel the dialogs before closing LiveSplit; an open dialog stops it from closing.

## Headless

Driving the built component from code, as the tests do: build a `Run`, drive it through `TimerModel`, call `Update()` to stand in for LiveSplit's frames, and read the files back. `TestRun` in `Tests\TestRun.cs` does all of this, and shows how to set up `LiveSplitState` outside LiveSplit.

For a one-off check that doesn't belong in the tests, such as a timing, a long stress run or a comparison of two builds:
- write a temporary test with `TestRun`, and delete it before committing;
- or write a console app outside the repo. It targets `net481`, and references `LiveSplit.Core.dll`, `UpdateManager.dll` and the built component, as the test project does.

Things to know:

- **Check `GameTime` outputs.** Game time is exact, while real time drifts. Usually that is a few milliseconds, but two runs of the same build through a long multi-scenario harness differed by up to 0.3 s in `RealTime` deltas (seen 2026-10-15). When comparing two builds, compare `RealTime` files only by which ones exist.
- **Call `Tick()` after every event.** The timer events only count, and nothing is written until the next `Update()`. `TestRun`'s actions do.
- **Files are written in the background.**
  - `Update()` only queues them, so call `comp.WaitForFileWrites()` before reading files; `TestRun.Tick()` does.
  - A failed write shows up in `comp.Settings.WriteError` on the next `Update()` after it fails.
  - When timing a split, time `Split()` and the next `Update()` together, since that `Update()` does the split's work. Time `WaitForFileWrites()` separately for the writes.
- **Real time.** Setting `AdjustedStartTime` only affects real time while the timer is `Running`; while paused, real time stays frozen. `model.Pause()` toggles between pause and resume.
- **Editing splits.** Edit Splits changes the run in place, then raises `RunManuallyModified`. To do the same, change `state.Run` (a name, a time, `Add`, `RemoveAt`), call `state.CallRunManuallyModified()`, then `Tick()`.
- **Segment history.** Section golds come from the history, so a test of them needs attempts in it. Play each attempt through `TimerModel` and end it with `model.Reset()`, which records it as LiveSplit does. Print `SegmentHistory` to see what was recorded.
  - To make the holes that edits leave, change a copy of the run (`(IRun)state.Run.Clone()`), then call `run.FixSplits()` as the editor does. Clearing a split's best segment is `run[i].BestSegmentTime = new Time()`. Clean Sum of Best is `run[i].SegmentHistory.Remove(attempt)`.
  - Then `state.Run = run`, `state.CallRunManuallyModified()` and `Tick()`.
  - Reach the bests through the component's private `_levels` field by reflection: `SplitLevels.Count`, its indexer, and each `SplitLevel`'s `BestSegmentTimes`.
- **Comparisons.**
  - `LiveSplitState.CurrentComparison` is null until set; `TestRun` sets it to Personal Best.
  - To switch, as the right-click menu or the server does, set `state.CurrentComparison`, then `Tick()`.
  - Generated comparisons (Best Segments, Average Segments) stay empty until something calls each generator's `Generate(state.Settings)`. LiveSplit's UI does that after every reset and edit, so do it after `model.Reset()`, before the `Tick()`.
  - A custom comparison is `run.CustomComparisons.Add(name)`, plus `run[i].Comparisons[name] = time` for each split.
  - Tick one in the settings with `comp.Settings.SetComparisonTicked(name, true)`, or set `AllCustomComparisons`.
- **Loading a splits file.** `new StandardFormatsRunFactory(stream, path).Create(new StandardComparisonGeneratorsFactory())`, with `livesplit_core.dll` from LiveSplit's `x64\` folder next to the program or the tests' DLL ([livesplit-notes.md](livesplit-notes.md#running-livesplit-code-outside-livesplit-verified)).
- **Time with averages.** The first `Split()` includes JIT compilation, around 100 ms.
- **Don't delete output files mid-run.** `FileQueue` skips contents it has already written, so a deleted file comes back only when its contents change or at reset. Use a fresh folder, or a new component, instead.
- **Changing a setting.** Set the property, such as `comp.Settings.OutputSplitList = false`, then `Tick()`. The `Output*` settings are cache keys, so the next `Update()` rewrites everything and empties the files that are no longer written.
- **Reusing a folder.** The first `Update()` of a new component empties every file in the component's folders (and every root `TotalSplits*.txt`) that it doesn't write now, such as another level's from an earlier run. Other files in the output folder are left alone.
- **Test the current build.** `dotnet test` builds the component first. With `--no-build`, or in a program that references `bin\Debug`, whatever was built last is tested.
- **A game name downloads speedrun.com's game list.** Once a run has a game name, LiveSplit.Core downloads speedrun.com's game list (about 2 MB) and saves it as `speedruncom_game_names.json` in the current directory (seen 2026-10-15). Without that file, the tests took about a minute longer (seen 2026-10-22). So `TestRun`'s runs have no game name, and it sets the current directory to the tests' `bin\`. Don't run a program that sets a game name from the repo.
- **Timer events from another thread.** LiveSplit's server raises the timer events off the UI thread. So did an Auto Splitting Runtime script activated in Edit Splits, before LiveSplit 1.8.36 ([livesplit-notes.md](livesplit-notes.md#which-thread-raises-the-timer-events-mostly-read-in-source)).
  - To do the same, call `TimerModel` from a second thread, such as a `System.Timers.Timer` like ASR's. Meanwhile the main thread calls `Update()` at LiveSplit's pace: sleep `1000 / RefreshRate` ms (25 at 40), then update.
  - To go through the real server, call `new LiveSplit.Server.CommandServer(state).StartNamedPipe()`. Then write lines such as `starttimer`, `split` and `reset` to a `NamedPipeClientStream(".", "LiveSplit")`, and call `StopAll()` at the end.
  - Close LiveSplit first. Its own server has a pipe of the same name, so the client could connect to it and drive the real timer.
  - To find wrong files:
    - After an action, let two frames pass and call `WaitForFileWrites()`.
    - Have a fresh component on the same state write to a second folder: one `Update()`, `WaitForFileWrites()`, then `Dispose()`.
    - Compare the non-empty files in the two folders, leaving out `RealTime` ones, since real time moves between the two writes.
    - With every action on the main thread, the folders match.
    - Damage needs the handler and an `Update()` to overlap, so a long run can compare only after actions that overlapped one, plus a sample of the rest.
- **The settings control.** To test the settings UI, add `comp.Settings` to a `Form` you `Show()`, reach its private controls by reflection, type with `PostMessage(textBox.Handle, WM_CHAR, c, 0)`, and pump with `Application.DoEvents()`. Bindings only see focus changes and keys that go through the message loop. Remove the control before closing the form, which would dispose it. These forms appear on screen and take focus from whatever else is open, so warn whoever is at the machine first.
  - To check only the bindings, no window is needed. Add `comp.Settings` to a `Form` that is never shown, and call the internal `Control.CreateControl(bool)` on the form by reflection, with `true`. That creates the handles, and the bindings then work both ways: setting a control's `Value` or `Checked` in code updates its property (seen headless, 2026-10-21). Typing and focus changes still need a shown form.
  - To see the control in LiveSplit's own Layout Settings dialog, still without a window (seen 2026-10-22):
    - Load `LiveSplit.View.dll` from LiveSplit's folder, and resolve its dependencies from there too, with `AppDomain.AssemblyResolve`.
    - Create `LiveSplit.View.LayoutSettingsDialog` by reflection. Pass it the layout settings, a `Layout` whose `LayoutComponents` holds `new LayoutComponent("LiveSplit.OutputToFile.dll", component)`, and the component.
    - Create its handles as above, select the component's page in its private `tabControl`, and call the dialog's `PerformLayout()`.
    - The page's `WS_VSCROLL` window style then says whether the tab scrolls.
- **A run needs a split.** The component's constructor throws on a run without splits (seen 2026-10-22). LiveSplit refuses to load a splits file without splits ([livesplit-notes.md](livesplit-notes.md#running-livesplit-code-outside-livesplit-verified)).
- **Private members.** `TimeFormatter` is internal. Reach it by reflection: `typeof(OutputToFileComponent).Assembly.GetType("LiveSplit.UI.Components.OutputToFile.TimeFormatter")`. Its `DurationString` and `DeltaString` take a `TimeSpan?` and a `FormatAccuracy`, which is public.
- **Formats.** Expected strings use LiveSplit's minus sign `−` (U+2212) for negatives, and the default accuracies: Seconds for times, Flex for differences. Change them with `comp.Settings.TimeAccuracy` and `DeltaAccuracy`, then `Tick()`.

### Reading files as OBS does

OBS 32's text source reads a file with `_wfopen(path, "rb")` and then sizes and reads it with the CRT (`os_quick_read_utf8_file` and `os_fread_utf8` in libobs). Calling the same CRT functions from `ucrtbase.dll` gives the same sharing mode and the same view of a half-written file:

```csharp
[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
static extern IntPtr _wfopen(string path, string mode);
[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
static extern int fseek(IntPtr f, int offset, int origin);
[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
static extern long _ftelli64(IntPtr f);
[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
static extern UIntPtr fread(byte[] buf, UIntPtr size, UIntPtr count, IntPtr f);
[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
static extern int fclose(IntPtr f);

// Null where OBS would show nothing: the open failed, the file was empty, or it was truncated between the size and the read.
// Leaves out os_fread_utf8's BOM check; the component writes no BOM.
static string ReadLikeObs(string path)
{
    IntPtr f = _wfopen(path, "rb");
    if (f == IntPtr.Zero) return null;
    try
    {
        fseek(f, 0, 2); // SEEK_END
        long size = _ftelli64(f);
        if (size <= 0) return null;
        var buf = new byte[size];
        fseek(f, 0, 0); // SEEK_SET
        if ((long)fread(buf, (UIntPtr)1, (UIntPtr)size, f).ToUInt64() < size) return null;
        return Encoding.UTF8.GetString(buf);
    }
    finally { fclose(f); }
}
```

To measure torn reads:
- Call it in a loop on a second thread while the main thread writes one file through `FileQueue` with new contents each time.
- Count the reads that return null or contents that were never written.
- `FileQueue` is internal: create it with `Activator.CreateInstance(type, true)` and call `Write` and `WaitForWrites` by reflection.
- Record each value before writing it, so a read never sees a value it doesn't know.
- Spin a random 0–400 µs between reads so they start at random moments. Back-to-back reads understate the share, because a read that hits a write takes several times longer than one that doesn't.

The share of bad reads is then the chance that an OBS read lands on a write. Divided by the writes per second, it gives how long each write leaves the file unreadable.

## Manual regression checks

Checks of tracked items that are too long or too uncertain to be tests; the Code column names the item in [TODO.md](TODO.md). Each one shows today's output and the correct output, so a fix can be confirmed. Bugs that a test can show are tests instead (see [Adding a test](#adding-a-test)).

| Code | Check | File | Today | Correct |
|---|---|---|---|---|
| ATM | `FileQueue` rewrites a timer-sized file 10 times a second for 180 s while [OBS-style reads](#reading-files-as-obs-does) with random gaps run | that file | 491 of 726,347 reads empty or short | none |
