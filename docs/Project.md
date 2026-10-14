# Project and README

Build setup and end-user documentation. The index is [TODO.md](TODO.md). Line numbers are as of commit `30d30fa`. [output-files.md](output-files.md) is the accurate reference to bring the README in line with.

## LVL — README level folder names don't match the code

The README says two dashes make `SansSubsplits_Level2`, and its tree shows `_Level2` and `_Level3` folders. The code names a middle level by its level number ([OutputToFileComponent.cs:952](../UI/Components/OutputToFileComponent.cs#L952)).

**Evidence.** Reproduced 2026-10-14: with splits at depths 0, 1 and 2, the code produced `SansSubsplits`, `SansSubsplits_Level1` and `AllSplits`.

**Plan.** Decide which is intended. Changing the code renames folders that existing OBS setups point at. Changing the README means rewriting the subsplits paragraph and the tree.

**Verify.** The README's example names match a run with splits at depths 0, 1 and 2.

**Docs.** README. If the folder tags change: CLAUDE.md "Must-know before editing" (the levels bullet), architecture.md "Levels" and output-files.md.

## DOC — README is missing outputs and settings

Not documented:

- the `CompletedSplits\` folder;
- `PlatformName.txt`;
- the `*_Raw.txt` and `Names_Indent_N.txt` files;
- the `{Header}` section-name syntax;
- the possible `Sign.txt` values;
- the settings;
- how to install.

**Evidence.** Read: the README against the `FILE_*` templates and [output-files.md](output-files.md).

**Plan.** Add each to the README. Install is: copy the DLL into LiveSplit's `Components` folder, then Layout Editor → Other → "Output to File".

**Verify.** Every `FILE_*` template appears in the README tree.

**Docs.** README. output-files.md's opening note that the README is incomplete.

## GTT — README's GameTime timer list is incomplete

Under `Timers\GameTime`, the tree lists only `SegmentTimer_AllSplits.txt`, but the code writes every level for both timing methods.

**Evidence.** Read.

**Plan.** List the same files under `GameTime` as under `RealTime`.

**Verify.** The tree matches a run with subsplits.

**Docs.** README.

## HNT — LiveSplit references only resolve on this machine

The LiveSplit references use a HintPath into `D:\Programs\Streaming\LiveSplit` ([csproj:35](../LiveSplit.OutputToFile.csproj#L35), [:48](../LiveSplit.OutputToFile.csproj#L48)), so the project won't build elsewhere.

**Evidence.** Read.

**Plan.** Add an overridable `LiveSplitDir` MSBuild property defaulting to the current path, and use it in both HintPaths.

**Verify.** Builds unchanged here, and with `-p:LiveSplitDir=<other install>`.

**Docs.** CLAUDE.md "Build / deploy". testing.md "Headless", whose harness uses the same paths.

## PRV — LiveSplit DLLs are copied into bin

The references aren't marked `<Private>false</Private>`, so `LiveSplit.Core.dll` and `UpdateManager.dll` get copied into `bin\`. That's harmless unless the whole `bin` folder gets copied into `Components`.

**Evidence.** Read.

**Plan.** Mark both references `<Private>false</Private>`. A [TST](Features.md#tst--no-automated-tests) project then has to reference them itself.

**Verify.** After a clean build, `bin\Debug` holds only this component's files.

**Docs.** None.

## VER — Version stuck at 1.0.0 with no update URL

The version is 1.0.0 in [OutputToFileFactory.cs:30](../UI/Components/OutputToFileFactory.cs#L30) and in [AssemblyInfo.cs:36-37](../Properties/AssemblyInfo.cs#L36-L37), and `UpdateURL` is empty ([OutputToFileFactory.cs:23](../UI/Components/OutputToFileFactory.cs#L23)).

**Evidence.** Read.

**Plan.** Pick a version and keep the two in step, for example by reading the factory's version from the assembly. Decide whether to publish an update XML. If not, leave `UpdateURL` empty and drop the template comments ([TPL](Cleanup.md#tpl--wrong-or-misleading-template-comments)).

**Verify.** LiveSplit's component list shows the new version.

**Docs.** None.

## RCC — Unused Release Candidate configuration

The solution has a "Release Candidate" configuration that just maps to Release ([sln:11](../LiveSplit.OutputToFile.sln#L11)).

**Evidence.** Read.

**Plan.** Remove it from the solution.

**Verify.** `dotnet build -c Debug` and `-c Release` still work.

**Docs.** None.
