# Project and README

Build setup, releases and end-user documentation. The index is [TODO.md](TODO.md). [output-files.md](output-files.md) is the accurate reference that the README follows.

## UPD — Update prompt not yet seen in LiveSplit

LiveSplit's startup update check is wired up, but it has only been checked headless. The feed parsing, the version comparison and the install were tested against the installed `UpdateManager.dll`, with local feeds and `file://` downloads. The prompt, the restart and a download from a real GitHub release haven't been seen in LiveSplit. `update.xml` stays empty until release 8 is published, so this can't be checked before then.

**Evidence.** Read in the LiveSplit source, and checked headless on 2026-10-21 ([livesplit-notes.md](livesplit-notes.md#update-check-read-in-source-checked-headless)).

**Plan.** After release 8 is published and in `update.xml`, which are steps 1–11 of [releasing.md](releasing.md#steps). One step at a time, marked as there:
1. **Files.** Set `AssemblyVersion` and `AssemblyFileVersion` to `7.9`, so that the installed DLL can be told apart from release 8's, and run `deploy.cmd`. It overwrites the DLL in LiveSplit's `Components` folder.
2. **LiveSplit.** Start LiveSplit. This shows windows, so ask whoever is at the machine first.
   - Expect "New updates are available", listing "Output to File (8.0)" and release 8's lines. If it lists anything else, choose No, because Yes would update that too.
   - Choose Yes. LiveSplit should close, the updater should run, and LiveSplit should open again.
   - Close LiveSplit and start it again. There should be no prompt.
3. **Files.** Set the version back to `8.0`.
4. **Files.** Update the docs (below), and finish UPD in TODO.md and DONE.md.
5. **git.** Commit.
6. **GitHub.** Push `main`.

**Verify.** After step 2, `Components\LiveSplit.OutputToFile.dll` has file version 8.0 and the same SHA-256 as release 8's download.

**Docs.** livesplit-notes.md: mark the update-check facts as seen in LiveSplit. Its heading, and so its anchor, changes, so update the link to it in releasing.md.
