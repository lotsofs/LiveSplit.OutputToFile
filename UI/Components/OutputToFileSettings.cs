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

        public OutputToFileSettings()
        {
            InitializeComponent();
            FolderPath = string.Empty;
            SplitsBefore = 10;
            SplitsAfter = 10;

            this.textBoxFolderPath.DataBindings.Add("Text", this, "FolderPath", false, DataSourceUpdateMode.OnPropertyChanged);
            this.numericUpDownSplitListBefore.DataBindings.Add("Value", this, "SplitsBefore", false, DataSourceUpdateMode.OnPropertyChanged);
            this.numericUpDownSplitListAfter.DataBindings.Add("Value", this, "SplitsAfter", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void OutputToFileSettings_Load(object sender, EventArgs e)
        {

        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "0.1") ^
                SettingsHelper.CreateSetting(document, parent, "FolderPath", FolderPath) ^
                SettingsHelper.CreateSetting(document, parent, "SplitsBefore", SplitsBefore) ^
                SettingsHelper.CreateSetting(document, parent, "SplitsAfter", SplitsAfter)
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
    }
}
