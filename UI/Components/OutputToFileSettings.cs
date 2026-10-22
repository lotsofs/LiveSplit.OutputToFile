using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components.OutputToFile
{
	public partial class OutputToFileSettings : UserControl
	{
		public string FolderPath { get; set; }
		public int SplitsBefore { get; set; }
		public int SplitsAfter { get; set; }
		public bool OutputTimer { get; set; }
		public bool OutputSplitList { get; set; }
		public bool OutputSubsplits { get; set; }
		public bool OutputCompletedSplits { get; set; }
		// A folder for every custom comparison of the open splits, ticked or not
		public bool AllCustomComparisons { get; set; }
		// How times and differences are written. Each drop-down list's index is the value, so the lists follow the enum's order.
		public FormatAccuracy TimeAccuracy { get; set; }
		public FormatAccuracy DeltaAccuracy { get; set; }

		// The comparisons ticked in the list, each written to a folder of its own. Changed through SetComparisonTicked,
		// which counts the changes in TickedComparisonsVersion, a cache key.
		readonly HashSet<string> _tickedComparisons = new HashSet<string>();
		public IEnumerable<string> TickedComparisons => _tickedComparisons;
		public int TickedComparisonsVersion { get; private set; }

		// The latest failed file write, as shown in the settings, or null. Not a setting: it isn't saved.
		public string WriteError { get; private set; }

		// The run whose comparisons the list shows, from ShowRunComparisons. Null until LiveSplit first shows the settings.
		IRun _run;
		// Set while the list is filled in code, so ItemCheck doesn't count it as ticking
		bool _fillingComparisons;

		public OutputToFileSettings()
		{
			InitializeComponent();
			FolderPath = string.Empty;
			SplitsBefore = 10;
			SplitsAfter = 10;
			OutputTimer = false;
			OutputSplitList = true;
			OutputSubsplits = true;
			OutputCompletedSplits = false;
			AllCustomComparisons = false;
			TimeAccuracy = FormatAccuracy.Seconds;
			DeltaAccuracy = FormatAccuracy.Flex;
			ShowAccuracies();

			// Applied when the textbox loses focus, not per keystroke: FolderPath is a cache key, so every partial path would get a full set of files
			this.textBoxFolderPath.DataBindings.Add("Text", this, "FolderPath", false, DataSourceUpdateMode.OnValidation);
			this.numericUpDownSplitListBefore.DataBindings.Add("Value", this, "SplitsBefore", false, DataSourceUpdateMode.OnPropertyChanged);
			this.numericUpDownSplitListAfter.DataBindings.Add("Value", this, "SplitsAfter", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputTimer.DataBindings.Add("Checked", this, "OutputTimer", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSplitList.DataBindings.Add("Checked", this, "OutputSplitList", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSubsplits.DataBindings.Add("Checked", this, "OutputSubsplits", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputCompletedSplits.DataBindings.Add("Checked", this, "OutputCompletedSplits", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxAllCustomComparisons.DataBindings.Add("Checked", this, "AllCustomComparisons", false, DataSourceUpdateMode.OnPropertyChanged);
		}

		private int CreateSettingsNode(XmlDocument document, XmlElement parent)
		{
			return SettingsHelper.CreateSetting(document, parent, "FolderPath", FolderPath) ^
				SettingsHelper.CreateSetting(document, parent, "SplitsBefore", SplitsBefore) ^
				SettingsHelper.CreateSetting(document, parent, "SplitsAfter", SplitsAfter) ^
				SettingsHelper.CreateSetting(document, parent, "OutputTimer", OutputTimer) ^
				SettingsHelper.CreateSetting(document, parent, "OutputSplitList", OutputSplitList) ^
				SettingsHelper.CreateSetting(document, parent, "OutputSubsplits", OutputSubsplits) ^
				SettingsHelper.CreateSetting(document, parent, "OutputCompletedSplits", OutputCompletedSplits) ^
				SettingsHelper.CreateSetting(document, parent, "AllCustomComparisons", AllCustomComparisons) ^
				SettingsHelper.CreateSetting(document, parent, "TimeAccuracy", TimeAccuracy) ^
				SettingsHelper.CreateSetting(document, parent, "DeltaAccuracy", DeltaAccuracy) ^
				CreateTickedComparisonsNode(document, parent)
				;
		}

		// <Comparisons> with a <Comparison> per ticked name, in a fixed order. Without a document, only the hash.
		private int CreateTickedComparisonsNode(XmlDocument document, XmlElement parent)
		{
			XmlElement list = document?.CreateElement("Comparisons");
			int hash = 0;
			foreach (string name in _tickedComparisons.OrderBy(n => n, StringComparer.Ordinal))
			{
				hash ^= SettingsHelper.CreateSetting(document, list, "Comparison", name);
			}
			parent?.AppendChild(list);
			return hash;
		}

		/// <summary>
		/// Tick or untick a comparison in the list's setting
		/// </summary>
		public void SetComparisonTicked(string name, bool ticked)
		{
			if (ticked ? _tickedComparisons.Add(name) : _tickedComparisons.Remove(name))
			{
				TickedComparisonsVersion++;
			}
		}

		public bool IsComparisonTicked(string name) => _tickedComparisons.Contains(name);

		public XmlNode GetSettings(XmlDocument document)
		{
			var parent = document.CreateElement("Settings");
			CreateSettingsNode(document, parent);
			return parent;
		}

		public int GetSettingsHashCode()
		{
			return CreateSettingsNode(null, null);
		}

		public void SetSettings(XmlNode node)
		{
			var element = (XmlElement)node;
			FolderPath = SettingsHelper.ParseString(element["FolderPath"], string.Empty);
			SplitsBefore = SettingsHelper.ParseInt(element["SplitsBefore"], 10);
			SplitsAfter = SettingsHelper.ParseInt(element["SplitsAfter"], 10);
			OutputTimer = SettingsHelper.ParseBool(element["OutputTimer"], false);
			OutputSplitList = SettingsHelper.ParseBool(element["OutputSplitList"], true);
			OutputSubsplits = SettingsHelper.ParseBool(element["OutputSubsplits"], true);
			OutputCompletedSplits = SettingsHelper.ParseBool(element["OutputCompletedSplits"], false);
			AllCustomComparisons = SettingsHelper.ParseBool(element["AllCustomComparisons"], false);
			TimeAccuracy = ParseAccuracy(element["TimeAccuracy"], FormatAccuracy.Seconds);
			DeltaAccuracy = ParseAccuracy(element["DeltaAccuracy"], FormatAccuracy.Flex);
			ShowAccuracies();
			// Layouts saved before the list have no Comparisons: nothing ticked
			_tickedComparisons.Clear();
			XmlElement list = element["Comparisons"];
			if (list != null)
			{
				foreach (XmlElement comparison in list.GetElementsByTagName("Comparison"))
				{
					_tickedComparisons.Add(comparison.InnerText);
				}
			}
			TickedComparisonsVersion++;
			if (_run != null)
			{
				FillComparisonList(AllCustomComparisons);
			}
		}

		/// <summary>
		/// Show the run's comparisons in the list. Call on the UI thread, before the settings are shown.
		/// </summary>
		public void ShowRunComparisons(IRun run)
		{
			_run = run;
			FillComparisonList(AllCustomComparisons);
		}

		// The run's comparisons, without the custom ones while allCustom is on (they're all written then), and the ticked ones the run doesn't have, so they can be unticked
		private void FillComparisonList(bool allCustom)
		{
			var names = _run.Comparisons.Where(name => !(allCustom && SplitLevels.IsCustomComparison(_run, name))).ToList();
			names.AddRange(_tickedComparisons.Where(name => !_run.Comparisons.Contains(name)).OrderBy(name => name, StringComparer.Ordinal));
			_fillingComparisons = true;
			checkedListBoxComparisons.Items.Clear();
			foreach (string name in names)
			{
				checkedListBoxComparisons.Items.Add(name, IsComparisonTicked(name));
			}
			_fillingComparisons = false;
		}

		private void checkedListBoxComparisons_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (_fillingComparisons) return;
			SetComparisonTicked((string)checkedListBoxComparisons.Items[e.Index], e.NewValue == CheckState.Checked);
		}

		// The default when the layout has no such setting (saved before it existed) or a value that isn't one of the choices
		private static FormatAccuracy ParseAccuracy(XmlElement element, FormatAccuracy defaultAccuracy)
		{
			if (SettingsHelper.TryParseEnum(element, out FormatAccuracy accuracy) && Enum.IsDefined(typeof(FormatAccuracy), accuracy))
			{
				return accuracy;
			}
			return defaultAccuracy;
		}

		// Select the accuracies in their drop-down lists
		private void ShowAccuracies()
		{
			comboBoxTimeAccuracy.SelectedIndex = (int)TimeAccuracy;
			comboBoxDeltaAccuracy.SelectedIndex = (int)DeltaAccuracy;
		}

		// A ComboBox has no event a binding could use for every change of the selected item, so these set the properties
		private void comboBoxTimeAccuracy_SelectedIndexChanged(object sender, EventArgs e)
		{
			TimeAccuracy = (FormatAccuracy)comboBoxTimeAccuracy.SelectedIndex;
		}

		private void comboBoxDeltaAccuracy_SelectedIndexChanged(object sender, EventArgs e)
		{
			DeltaAccuracy = (FormatAccuracy)comboBoxDeltaAccuracy.SelectedIndex;
		}

		private void checkBoxAllCustomComparisons_CheckedChanged(object sender, EventArgs e)
		{
			// Runs before the binding updates AllCustomComparisons, so read the checkbox
			if (_run != null)
			{
				FillComparisonList(checkBoxAllCustomComparisons.Checked);
			}
		}

		/// <summary>
		/// Show the latest failed file write, or hide it. Call on the UI thread only.
		/// </summary>
		/// <param name="error">The text to show, or null to hide it</param>
		public void ShowWriteError(string error)
		{
			WriteError = error;
			labelWriteError.Text = error ?? string.Empty;
			labelWriteError.Visible = error != null;
		}

		private void buttonFolderPath_Click(object sender, EventArgs e)
		{
			using (var dialog = new FolderBrowserDialog())
			{
				if (Directory.Exists(FolderPath))
				{
					dialog.SelectedPath = FolderPath;
				}
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					FolderPath = this.textBoxFolderPath.Text = dialog.SelectedPath;
				}
			}
		}

		private void checkBoxOutputSplitList_CheckedChanged(object sender, EventArgs e) {
			groupBoxSplitList.Enabled = checkBoxOutputSplitList.Checked;
		}
	}
}
