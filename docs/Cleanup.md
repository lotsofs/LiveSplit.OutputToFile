# Cleanup

Settings UI and code tidying. None of these change output unless noted. The index is [TODO.md](TODO.md).

## SET — architecture.md's Settings section is out of date

The [Settings](architecture.md#settings) section of architecture.md still describes the settings as they were before the Comparisons and Time Format groups:
- Its property table and its list of XML keys leave out `AllCustomComparisons`, `TimeAccuracy`, `DeltaAccuracy`, and the ticked comparisons saved under `<Comparisons>`.
- It says every property but `FolderPath` is bound with `OnPropertyChanged`. `TimeAccuracy` and `DeltaAccuracy` aren't bound: the drop-down lists' `SelectedIndexChanged` handlers set them, and `ShowAccuracies` selects them in the lists.
- It names `checkBoxOutputSplitList_CheckedChanged` and `buttonFolderPath_Click` as the control's only event handlers. The comparison list, its checkbox and both drop-down lists have handlers too.

**Evidence.** Read 2026-10-22, against `OutputToFileSettings.cs`.

**Plan.** Bring the section in line with `OutputToFileSettings.cs`: add the missing properties, defaults, bindings, handlers and XML keys. For the ticked comparisons, link to [Comparisons](architecture.md#comparisons) rather than repeat it.

**Verify.** Every property, binding, handler and XML key in `OutputToFileSettings.cs` is in the section, and the section names nothing that isn't there.

**Docs.** architecture.md only.
