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
		string _writeErrorFile;

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

			this.textBoxFolderPath.DataBindings.Add("Text", this, "FolderPath", false, DataSourceUpdateMode.OnPropertyChanged);
			this.numericUpDownSplitListBefore.DataBindings.Add("Value", this, "SplitsBefore", false, DataSourceUpdateMode.OnPropertyChanged);
			this.numericUpDownSplitListAfter.DataBindings.Add("Value", this, "SplitsAfter", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputTimer.DataBindings.Add("Checked", this, "OutputTimer", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSplitList.DataBindings.Add("Checked", this, "OutputSplitList", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputSubsplits.DataBindings.Add("Checked", this, "OutputSubsplits", false, DataSourceUpdateMode.OnPropertyChanged);
			this.checkBoxOutputCompletedSplits.DataBindings.Add("Checked", this, "OutputCompletedSplits", false, DataSourceUpdateMode.OnPropertyChanged);
		}

		private void OutputToFileSettings_Load(object sender, EventArgs e)
		{

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
			SplitsBefore = SettingsHelper.ParseInt(element["SplitsBefore"], 3);
			SplitsAfter = SettingsHelper.ParseInt(element["SplitsAfter"], 2);
			OutputTimer = SettingsHelper.ParseBool(element["OutputTimer"], false);
			OutputSplitList = SettingsHelper.ParseBool(element["OutputSplitList"], true);
			OutputSubsplits = SettingsHelper.ParseBool(element["OutputSubsplits"], true);
			OutputCompletedSplits = SettingsHelper.ParseBool(element["OutputCompletedSplits"], false);
		}

		/// <summary>
		/// Show a failed file write. It stays until that file is written successfully, or another write fails.
		/// </summary>
		/// <param name="fileName">Path relative to the folder</param>
		public void ShowWriteError(string fileName, Exception ex)
		{
			_writeErrorFile = fileName;
			WriteError = $"Couldn't write {fileName} ({DateTime.Now:T}): {ex.Message}";
			labelWriteError.Text = WriteError;
			labelWriteError.Visible = true;
		}

		/// <summary>
		/// A file was written successfully. Hide the error if it was about that file.
		/// </summary>
		/// <param name="fileName">Path relative to the folder</param>
		public void ClearWriteError(string fileName)
		{
			if (fileName != _writeErrorFile) return;
			_writeErrorFile = null;
			WriteError = null;
			labelWriteError.Text = string.Empty;
			labelWriteError.Visible = false;
		}

		private void buttonFolderPath_Click(object sender, EventArgs e)
		{
			var dialog = new FolderBrowserDialog();
			if (Directory.Exists(FolderPath))
			{
				dialog.SelectedPath = Path.GetDirectoryName(FolderPath);
			}
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				FolderPath = this.textBoxFolderPath.Text = dialog.SelectedPath;
			}
		}

		private void numericUpDownSplitListBefore_ValueChanged(object sender, EventArgs e)
		{
			SplitsBefore = (int)numericUpDownSplitListBefore.Value;
		}

		private void numericUpDownSplitListAfter_ValueChanged(object sender, EventArgs e)
		{
			SplitsAfter = (int)numericUpDownSplitListAfter.Value;
		}

		private void checkBoxOutputTimer_CheckedChanged(object sender, EventArgs e) {
			OutputTimer = checkBoxOutputTimer.Checked;
		}

		private void checkBoxOutputSplitList_CheckedChanged(object sender, EventArgs e) {
			OutputSplitList = checkBoxOutputSplitList.Checked;
			groupBoxSplitList.Enabled = checkBoxOutputSplitList.Checked;
		}

		private void checkBoxOutputSubsplits_CheckedChanged(object sender, EventArgs e) {
			OutputSplitList = checkBoxOutputSplitList.Checked;
		}

		private void checkBoxOutputCompletedSplits_CheckedChanged(object sender, EventArgs e) {
			OutputCompletedSplits = checkBoxOutputCompletedSplits.Checked;
		}
	}
}
