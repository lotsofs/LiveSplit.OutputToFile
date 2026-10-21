using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
	public partial class OutputToFileSettings : UserControl
	{
		public string FolderPath { get; set; }
		public LayoutMode Mode { get; set; }
		public int SplitsBefore { get; set; }
		public int SplitsAfter { get; set; }
		public bool OutputTimer { get; set; }
		public bool OutputSplitList { get; set; }
		public bool OutputSubsplits { get; set; }
		public bool OutputCompletedSplits { get; set; }

		// The latest failed file write, as shown in the settings, or null. Not a setting: it isn't saved.
		public string WriteError { get; private set; }

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

			// Applied when the textbox loses focus, not per keystroke: FolderPath is a cache key, so every partial path would get a full set of files
			this.textBoxFolderPath.DataBindings.Add("Text", this, "FolderPath", false, DataSourceUpdateMode.OnValidation);
			this.numericUpDownSplitListBefore.DataBindings.Add("Value", this, "SplitsBefore", false, DataSourceUpdateMode.OnPropertyChanged);
			this.numericUpDownSplitListAfter.DataBindings.Add("Value", this, "SplitsAfter", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputTimer.DataBindings.Add("Checked", this, "OutputTimer", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSplitList.DataBindings.Add("Checked", this, "OutputSplitList", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSubsplits.DataBindings.Add("Checked", this, "OutputSubsplits", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputCompletedSplits.DataBindings.Add("Checked", this, "OutputCompletedSplits", false, DataSourceUpdateMode.OnPropertyChanged);
		}

		private int CreateSettingsNode(XmlDocument document, XmlElement parent)
		{
			return SettingsHelper.CreateSetting(document, parent, "Version", "0.1") ^
				SettingsHelper.CreateSetting(document, parent, "FolderPath", FolderPath) ^
				SettingsHelper.CreateSetting(document, parent, "SplitsBefore", SplitsBefore) ^
				SettingsHelper.CreateSetting(document, parent, "SplitsAfter", SplitsAfter) ^
				SettingsHelper.CreateSetting(document, parent, "OutputTimer", OutputTimer) ^
				SettingsHelper.CreateSetting(document, parent, "OutputSplitList", OutputSplitList) ^
				SettingsHelper.CreateSetting(document, parent, "OutputSubsplits", OutputSubsplits) ^
				SettingsHelper.CreateSetting(document, parent, "OutputCompletedSplits", OutputCompletedSplits)
				;
		}

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
			Version version = SettingsHelper.ParseVersion(element["Version"]);

			FolderPath = SettingsHelper.ParseString(element["FolderPath"], string.Empty);
			SplitsBefore = SettingsHelper.ParseInt(element["SplitsBefore"], 10);
			SplitsAfter = SettingsHelper.ParseInt(element["SplitsAfter"], 10);
			OutputTimer = SettingsHelper.ParseBool(element["OutputTimer"], false);
			OutputSplitList = SettingsHelper.ParseBool(element["OutputSplitList"], true);
			OutputSubsplits = SettingsHelper.ParseBool(element["OutputSubsplits"], true);
			OutputCompletedSplits = SettingsHelper.ParseBool(element["OutputCompletedSplits"], false);
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
