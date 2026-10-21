# Project and README

Build setup, releases and end-user documentation. The index is [TODO.md](TODO.md). [output-files.md](output-files.md) is the accurate reference to bring the README in line with.

## DOC — README is missing outputs and settings

Not documented:

- the `CompletedSplits\` folder;
- `PlatformName.txt`;
- the `*_Raw.txt` and `Names_Indent_N.txt` files;
- the `{Header}` section-name syntax;
- the possible `Sign.txt` values;
- the settings, including the red line that shows the latest file that couldn't be written;
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

## UPD — Update prompt not yet seen in LiveSplit

LiveSplit's startup update check is wired up, but it has only been checked headless. The feed parsing, the version comparison and the install were tested against the installed `UpdateManager.dll`, with local feeds and `file://` downloads. The prompt, the restart and a download from a real GitHub release haven't been seen in LiveSplit. `update.xml` stays empty until release 8 is published, so this can't be checked before then.

**Evidence.** Read in the LiveSplit source, and checked headless on 2026-10-21 ([livesplit-notes.md](livesplit-notes.md#update-check-read-in-source-checked-headless)).

**Plan.** After release 8 is published and its entry is in `update.xml`, following [releasing.md](releasing.md):
1. Run `curl -sIL` on `https://github.com/lotsofs/LiveSplit.OutputToFile/releases/download/8/LiveSplit.OutputToFile.dll`. It should end in 200 with the asset's size.
2. Build with `AssemblyVersion("7.9")` and deploy.
3. Start LiveSplit. This shows windows, so ask whoever is at the machine first.
   - Expect "New updates are available", listing "Output to File (8.0)" and release 8's lines.
   - Choose Yes. LiveSplit should close, the updater should run, and LiveSplit should open again.
4. Restore the version.

**Verify.** `Components\LiveSplit.OutputToFile.dll` has file version 8.0, and the next start shows no prompt.

**Docs.** livesplit-notes.md: mark the update-check facts as seen in LiveSplit.
