# Releasing

How to publish a release so that LiveSplit offers it to users. Releases are numbered by git tag (`1`, `2`, …). Release 8 is the first that LiveSplit can update. Earlier releases have no update URL, so their users have to install a newer release by hand once.

## How users get the update

When LiveSplit starts, it reads the feed at the factory's `XMLURL`: [update.xml](../update.xml) on `main`, served from `raw.githubusercontent.com`. If the feed lists a version above the installed DLL's `AssemblyVersion`, LiveSplit shows "New updates are available". The dialog lists "Output to File (version)" and the changelog lines of every newer entry, then asks "Do you want to update?".

If the user clicks Yes, LiveSplit downloads its updater and closes itself. The updater downloads each `<file>` from the factory's `UpdateURL` plus the file's `path`, which is a GitHub release asset. It saves the file at `localPath` in the LiveSplit folder, and then restarts LiveSplit. [livesplit-notes.md](livesplit-notes.md#update-check-read-in-source-checked-headless) has the details and how they were checked.

## Steps

For release `N`. Take one step at a time, and check its result before starting the next. Each step is marked with what it changes:
- **Files** steps change only the working tree.
- **git** steps change the local repository.
- **GitHub** steps publish.
- **Check** steps change nothing.

Get `main` ready:

1. **git.** Commit everything that goes into the release on `main`. The tests pass.

Cut the release locally:

2. **Files.** Set `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs` to `N.0`. In [CHANGELOG.md](../CHANGELOG.md), turn "Unreleased" into `## N (date)`, and add an empty "Unreleased" above it. Build with `dotnet build LiveSplit.OutputToFile.sln -c Release`, with no warnings, and run the tests.
3. **git.** Commit "Release N".
4. **git.** Tag that commit `N`.

Publish the code:

5. **GitHub.** Push `main` and the tag.
6. **Files.** Build Release again from the tagged commit, with nothing uncommitted. Note the size and SHA-256 of `bin\Release\LiveSplit.OutputToFile.dll`.

Make the GitHub release:

7. **GitHub.** Create release `N` from tag `N`, with that DLL attached under its own name. A prerelease works. A draft doesn't, because its assets can't be downloaded.
8. **Check.** `curl -sIL https://github.com/lotsofs/LiveSplit.OutputToFile/releases/download/N/LiveSplit.OutputToFile.dll` ends in `200` with the DLL's size, and the downloaded file's SHA-256 matches the build's.

Offer it through LiveSplit:

9. **Files.** Add the release to [update.xml](../update.xml) (see [The feed](#the-feed)) and to [Past releases](#past-releases), and log it in [DONE.md](DONE.md).
10. **git.** Commit.
11. **GitHub.** Push `main`. Users are offered the release from their next LiveSplit start. `raw.githubusercontent.com` can serve the old feed for a few minutes. Then check that the live feed still loads (see [The feed](#the-feed)).

Keep that order. Once the feed names a version, users are offered it. If its DLL can't be downloaded yet, the updater shows an error after LiveSplit has already closed.

Build the release from the tagged commit, and don't replace a release's DLL later. The changelog describes each release's download, and a download that doesn't match its tag makes the next comparison harder (see below).

## Past releases

Which code each download on GitHub was built from. Checked on 2026-10-21 by building every tag and comparing it with the download: first each DLL's types, members and string literals, then the output of both through the same headless scenarios. Only real-time highlights differed, by a few milliseconds.

| Release | Tag date | Download | Built from |
|---|---|---|---|
| 1–4 | 2023-02-17 to 2023-03-31 | uploaded with the tag | its tag |
| 5 | 2023-03-31 | replaced 2023-05-29 | tag `6`'s code, which fixed negative differences shown as `-` |
| 6 | 2023-05-29 | replaced 2023-06-01 | `dc1e61b`, the first subsplits commit, a minute before the upload |
| 7 | 2024-10-09 | uploaded with the tag | its tag |

The downloads that were replaced aren't on GitHub any more, so what they contained isn't known. Tags `4` and `5` are the same commit.

## The feed

Release 9's feed, for example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<updates>
  <update version="9.0">
    <files>
      <file path="9/LiveSplit.OutputToFile.dll" localPath="Components/LiveSplit.OutputToFile.dll" status="changed" />
    </files>
    <changelog>
      <change>A short line for users.</change>
    </changelog>
  </update>
  <update version="8.0">
    <files />
    <changelog>
      <change>Release 8's lines, kept for users updating from 7 or earlier.</change>
    </changelog>
  </update>
</updates>
```

- **Newest first.** Add each release at the top, and keep the older entries, so a user several releases behind sees every release's changes.
- **Only the newest entry has a `<file>`.** When adding a release, move the `<file>` line up from the previous entry, change the tag in its `path`, and leave `<files />` behind. Every listed `path` is downloaded to the same `localPath`, in feed order. So an older entry listed below the newest would overwrite the new DLL with the old one.
- **`version` is `N.0`.** It needs at least two parts, because a one-part version like `8` can't be parsed and hides the whole feed. `N.0` against the DLL's `N.0.0.0` doesn't count as newer, so users of the release aren't offered it again. A fix release can be `N.1`, with a matching `AssemblyVersion`.
- **Every entry has both `<files>` and `<changelog>`,** even if empty. An entry missing one hides itself and every entry after it.
- **No comments inside `<updates>`.** LiveSplit reads a comment there as a broken entry, which hides the entries after it. A comment above `<updates>` is fine.
- **Changelog lines** are a few short user-facing lines, taken from that release's CHANGELOG section. The dialog shows every line from every newer entry, each as ` - line`. Put any **Action required** item first.
- **Escape** `&` and `<` in the text as `&amp;` and `&lt;`.

A broken feed shows no error anywhere: LiveSplit just offers no update. After pushing, check in PowerShell that the live feed is still well-formed XML. This line reads it the way LiveSplit does, and prints an error if it isn't:

```powershell
(New-Object Xml.XmlDocument).Load('https://raw.githubusercontent.com/lotsofs/LiveSplit.OutputToFile/main/update.xml')
```

That doesn't check the rules above; read the diff for those.
