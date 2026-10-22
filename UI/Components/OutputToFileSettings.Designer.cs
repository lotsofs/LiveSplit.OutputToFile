namespace LiveSplit.UI.Components.OutputToFile
{
    partial class OutputToFileSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.tableLayoutPanelTop = new System.Windows.Forms.TableLayoutPanel();
			this.groupBoxFolderPath = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanelFolderPath = new System.Windows.Forms.TableLayoutPanel();
			this.textBoxFolderPath = new System.Windows.Forms.TextBox();
			this.buttonFolderPath = new System.Windows.Forms.Button();
			this.groupBoxOutputSelection = new System.Windows.Forms.GroupBox();
			this.checkBoxOutputSubsplits = new System.Windows.Forms.CheckBox();
			this.checkBoxOutputCompletedSplits = new System.Windows.Forms.CheckBox();
			this.checkBoxOutputSplitList = new System.Windows.Forms.CheckBox();
			this.checkBoxOutputTimer = new System.Windows.Forms.CheckBox();
			this.groupBoxSplitList = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanelSplitList = new System.Windows.Forms.TableLayoutPanel();
			this.numericUpDownSplitListBefore = new System.Windows.Forms.NumericUpDown();
			this.numericUpDownSplitListAfter = new System.Windows.Forms.NumericUpDown();
			this.labelSplitListBefore = new System.Windows.Forms.Label();
			this.labelSplitListAfter = new System.Windows.Forms.Label();
			this.groupBoxComparisons = new System.Windows.Forms.GroupBox();
			this.checkBoxAllCustomComparisons = new System.Windows.Forms.CheckBox();
			this.checkedListBoxComparisons = new System.Windows.Forms.CheckedListBox();
			this.labelComparisons = new System.Windows.Forms.Label();
			this.groupBoxTimeFormat = new System.Windows.Forms.GroupBox();
			this.labelTimeAccuracy = new System.Windows.Forms.Label();
			this.comboBoxTimeAccuracy = new System.Windows.Forms.ComboBox();
			this.labelDeltaAccuracy = new System.Windows.Forms.Label();
			this.comboBoxDeltaAccuracy = new System.Windows.Forms.ComboBox();
			this.labelTimers = new System.Windows.Forms.Label();
			this.labelWriteError = new System.Windows.Forms.Label();
			this.tableLayoutPanelTop.SuspendLayout();
			this.groupBoxFolderPath.SuspendLayout();
			this.tableLayoutPanelFolderPath.SuspendLayout();
			this.groupBoxOutputSelection.SuspendLayout();
			this.groupBoxSplitList.SuspendLayout();
			this.groupBoxComparisons.SuspendLayout();
			this.groupBoxTimeFormat.SuspendLayout();
			this.tableLayoutPanelSplitList.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownSplitListBefore)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownSplitListAfter)).BeginInit();
			this.SuspendLayout();
			// 
			// tableLayoutPanelTop
			// 
			this.tableLayoutPanelTop.AutoSize = true;
			this.tableLayoutPanelTop.ColumnCount = 1;
			this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanelTop.Controls.Add(this.groupBoxFolderPath, 0, 0);
			this.tableLayoutPanelTop.Controls.Add(this.groupBoxOutputSelection, 0, 1);
			this.tableLayoutPanelTop.Controls.Add(this.groupBoxSplitList, 0, 2);
			this.tableLayoutPanelTop.Controls.Add(this.groupBoxComparisons, 0, 3);
			this.tableLayoutPanelTop.Controls.Add(this.groupBoxTimeFormat, 0, 4);
			this.tableLayoutPanelTop.Controls.Add(this.labelWriteError, 0, 5);
			this.tableLayoutPanelTop.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanelTop.Name = "tableLayoutPanelTop";
			this.tableLayoutPanelTop.RowCount = 6;
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 51F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 77F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 166F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTop.Size = new System.Drawing.Size(454, 549);
			this.tableLayoutPanelTop.TabIndex = 0;
			// 
			// groupBoxFolderPath
			// 
			this.groupBoxFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxFolderPath.AutoSize = true;
			this.groupBoxFolderPath.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.groupBoxFolderPath.Controls.Add(this.tableLayoutPanelFolderPath);
			this.groupBoxFolderPath.Location = new System.Drawing.Point(3, 3);
			this.groupBoxFolderPath.Name = "groupBoxFolderPath";
			this.groupBoxFolderPath.Size = new System.Drawing.Size(448, 45);
			this.groupBoxFolderPath.TabIndex = 0;
			this.groupBoxFolderPath.TabStop = false;
			this.groupBoxFolderPath.Text = "Folder Path";
			// 
			// tableLayoutPanelFolderPath
			// 
			this.tableLayoutPanelFolderPath.AutoSize = true;
			this.tableLayoutPanelFolderPath.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanelFolderPath.ColumnCount = 2;
			this.tableLayoutPanelFolderPath.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 68.8F));
			this.tableLayoutPanelFolderPath.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.2F));
			this.tableLayoutPanelFolderPath.Controls.Add(this.textBoxFolderPath, 0, 0);
			this.tableLayoutPanelFolderPath.Controls.Add(this.buttonFolderPath, 1, 0);
			this.tableLayoutPanelFolderPath.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelFolderPath.Location = new System.Drawing.Point(3, 16);
			this.tableLayoutPanelFolderPath.Name = "tableLayoutPanelFolderPath";
			this.tableLayoutPanelFolderPath.RowCount = 1;
			this.tableLayoutPanelFolderPath.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelFolderPath.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelFolderPath.Size = new System.Drawing.Size(442, 26);
			this.tableLayoutPanelFolderPath.TabIndex = 0;
			// 
			// textBoxFolderPath
			// 
			this.textBoxFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFolderPath.Location = new System.Drawing.Point(3, 3);
			this.textBoxFolderPath.Name = "textBoxFolderPath";
			this.textBoxFolderPath.Size = new System.Drawing.Size(298, 20);
			this.textBoxFolderPath.TabIndex = 0;
			// 
			// buttonFolderPath
			// 
			this.buttonFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonFolderPath.AutoSize = true;
			this.buttonFolderPath.CausesValidation = false;
			this.buttonFolderPath.Location = new System.Drawing.Point(307, 3);
			this.buttonFolderPath.MinimumSize = new System.Drawing.Size(75, 15);
			this.buttonFolderPath.Name = "buttonFolderPath";
			this.buttonFolderPath.Size = new System.Drawing.Size(132, 20);
			this.buttonFolderPath.TabIndex = 1;
			this.buttonFolderPath.Text = "Browse";
			this.buttonFolderPath.UseVisualStyleBackColor = true;
			this.buttonFolderPath.Click += new System.EventHandler(this.buttonFolderPath_Click);
			// 
			// groupBoxOutputSelection
			// 
			this.groupBoxOutputSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputSubsplits);
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputCompletedSplits);
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputSplitList);
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputTimer);
			this.groupBoxOutputSelection.Location = new System.Drawing.Point(3, 54);
			this.groupBoxOutputSelection.Name = "groupBoxOutputSelection";
			this.groupBoxOutputSelection.Size = new System.Drawing.Size(448, 68);
			this.groupBoxOutputSelection.TabIndex = 1;
			this.groupBoxOutputSelection.TabStop = false;
			this.groupBoxOutputSelection.Text = "Output";
			// 
			// checkBoxOutputSubsplits
			// 
			this.checkBoxOutputSubsplits.AutoSize = true;
			this.checkBoxOutputSubsplits.Checked = true;
			this.checkBoxOutputSubsplits.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOutputSubsplits.Location = new System.Drawing.Point(119, 19);
			this.checkBoxOutputSubsplits.Name = "checkBoxOutputSubsplits";
			this.checkBoxOutputSubsplits.Size = new System.Drawing.Size(68, 17);
			this.checkBoxOutputSubsplits.TabIndex = 1;
			this.checkBoxOutputSubsplits.Text = "Subsplits";
			this.checkBoxOutputSubsplits.UseVisualStyleBackColor = true;
			// 
			// checkBoxOutputCompletedSplits
			// 
			this.checkBoxOutputCompletedSplits.AutoSize = true;
			this.checkBoxOutputCompletedSplits.Location = new System.Drawing.Point(245, 19);
			this.checkBoxOutputCompletedSplits.Name = "checkBoxOutputCompletedSplits";
			this.checkBoxOutputCompletedSplits.Size = new System.Drawing.Size(102, 17);
			this.checkBoxOutputCompletedSplits.TabIndex = 2;
			this.checkBoxOutputCompletedSplits.Text = "Completed splits";
			this.checkBoxOutputCompletedSplits.UseVisualStyleBackColor = true;
			// 
			// checkBoxOutputSplitList
			// 
			this.checkBoxOutputSplitList.AutoSize = true;
			this.checkBoxOutputSplitList.Checked = true;
			this.checkBoxOutputSplitList.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOutputSplitList.Location = new System.Drawing.Point(6, 42);
			this.checkBoxOutputSplitList.Name = "checkBoxOutputSplitList";
			this.checkBoxOutputSplitList.Size = new System.Drawing.Size(65, 17);
			this.checkBoxOutputSplitList.TabIndex = 3;
			this.checkBoxOutputSplitList.Text = "Split List";
			this.checkBoxOutputSplitList.UseVisualStyleBackColor = true;
			this.checkBoxOutputSplitList.CheckedChanged += new System.EventHandler(this.checkBoxOutputSplitList_CheckedChanged);
			// 
			// checkBoxOutputTimer
			// 
			this.checkBoxOutputTimer.AutoSize = true;
			this.checkBoxOutputTimer.Location = new System.Drawing.Point(6, 19);
			this.checkBoxOutputTimer.Name = "checkBoxOutputTimer";
			this.checkBoxOutputTimer.Size = new System.Drawing.Size(52, 17);
			this.checkBoxOutputTimer.TabIndex = 0;
			this.checkBoxOutputTimer.Text = "Timer";
			this.checkBoxOutputTimer.UseVisualStyleBackColor = true;
			// 
			// groupBoxSplitList
			// 
			this.groupBoxSplitList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxSplitList.Controls.Add(this.tableLayoutPanelSplitList);
			this.groupBoxSplitList.Location = new System.Drawing.Point(3, 128);
			this.groupBoxSplitList.Name = "groupBoxSplitList";
			this.groupBoxSplitList.Size = new System.Drawing.Size(448, 71);
			this.groupBoxSplitList.TabIndex = 2;
			this.groupBoxSplitList.TabStop = false;
			this.groupBoxSplitList.Text = "Split List Writing";
			// 
			// tableLayoutPanelSplitList
			// 
			this.tableLayoutPanelSplitList.ColumnCount = 2;
			this.tableLayoutPanelSplitList.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 59.55414F));
			this.tableLayoutPanelSplitList.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40.44586F));
			this.tableLayoutPanelSplitList.Controls.Add(this.numericUpDownSplitListBefore, 1, 0);
			this.tableLayoutPanelSplitList.Controls.Add(this.numericUpDownSplitListAfter, 1, 1);
			this.tableLayoutPanelSplitList.Controls.Add(this.labelSplitListBefore, 0, 0);
			this.tableLayoutPanelSplitList.Controls.Add(this.labelSplitListAfter, 0, 1);
			this.tableLayoutPanelSplitList.Dock = System.Windows.Forms.DockStyle.Top;
			this.tableLayoutPanelSplitList.Location = new System.Drawing.Point(3, 16);
			this.tableLayoutPanelSplitList.Name = "tableLayoutPanelSplitList";
			this.tableLayoutPanelSplitList.RowCount = 2;
			this.tableLayoutPanelSplitList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelSplitList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelSplitList.Size = new System.Drawing.Size(442, 53);
			this.tableLayoutPanelSplitList.TabIndex = 0;
			// 
			// numericUpDownSplitListBefore
			// 
			this.numericUpDownSplitListBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.numericUpDownSplitListBefore.Location = new System.Drawing.Point(319, 3);
			this.numericUpDownSplitListBefore.Name = "numericUpDownSplitListBefore";
			this.numericUpDownSplitListBefore.Size = new System.Drawing.Size(120, 20);
			this.numericUpDownSplitListBefore.TabIndex = 0;
			this.numericUpDownSplitListBefore.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// numericUpDownSplitListAfter
			// 
			this.numericUpDownSplitListAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.numericUpDownSplitListAfter.Location = new System.Drawing.Point(319, 29);
			this.numericUpDownSplitListAfter.Name = "numericUpDownSplitListAfter";
			this.numericUpDownSplitListAfter.Size = new System.Drawing.Size(120, 20);
			this.numericUpDownSplitListAfter.TabIndex = 1;
			this.numericUpDownSplitListAfter.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// labelSplitListBefore
			// 
			this.labelSplitListBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.labelSplitListBefore.AutoSize = true;
			this.labelSplitListBefore.Location = new System.Drawing.Point(3, 6);
			this.labelSplitListBefore.Name = "labelSplitListBefore";
			this.labelSplitListBefore.Size = new System.Drawing.Size(257, 13);
			this.labelSplitListBefore.TabIndex = 2;
			this.labelSplitListBefore.Text = "Splits before current";
			// 
			// labelSplitListAfter
			// 
			this.labelSplitListAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.labelSplitListAfter.AutoSize = true;
			this.labelSplitListAfter.Location = new System.Drawing.Point(3, 33);
			this.labelSplitListAfter.Name = "labelSplitListAfter";
			this.labelSplitListAfter.Size = new System.Drawing.Size(257, 13);
			this.labelSplitListAfter.TabIndex = 3;
			this.labelSplitListAfter.Text = "Splits after current";
			//
			// groupBoxComparisons
			//
			this.groupBoxComparisons.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxComparisons.Controls.Add(this.checkBoxAllCustomComparisons);
			this.groupBoxComparisons.Controls.Add(this.checkedListBoxComparisons);
			this.groupBoxComparisons.Controls.Add(this.labelComparisons);
			this.groupBoxComparisons.Location = new System.Drawing.Point(3, 205);
			this.groupBoxComparisons.Name = "groupBoxComparisons";
			this.groupBoxComparisons.Size = new System.Drawing.Size(448, 160);
			this.groupBoxComparisons.TabIndex = 3;
			this.groupBoxComparisons.TabStop = false;
			this.groupBoxComparisons.Text = "Comparisons";
			//
			// checkBoxAllCustomComparisons
			//
			this.checkBoxAllCustomComparisons.AutoSize = true;
			this.checkBoxAllCustomComparisons.Location = new System.Drawing.Point(6, 19);
			this.checkBoxAllCustomComparisons.Name = "checkBoxAllCustomComparisons";
			this.checkBoxAllCustomComparisons.Size = new System.Drawing.Size(138, 17);
			this.checkBoxAllCustomComparisons.TabIndex = 0;
			this.checkBoxAllCustomComparisons.Text = "All custom comparisons";
			this.checkBoxAllCustomComparisons.UseVisualStyleBackColor = true;
			this.checkBoxAllCustomComparisons.CheckedChanged += new System.EventHandler(this.checkBoxAllCustomComparisons_CheckedChanged);
			//
			// checkedListBoxComparisons
			//
			this.checkedListBoxComparisons.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.checkedListBoxComparisons.CheckOnClick = true;
			this.checkedListBoxComparisons.IntegralHeight = false;
			this.checkedListBoxComparisons.Location = new System.Drawing.Point(6, 42);
			this.checkedListBoxComparisons.Name = "checkedListBoxComparisons";
			this.checkedListBoxComparisons.Size = new System.Drawing.Size(436, 94);
			this.checkedListBoxComparisons.TabIndex = 1;
			this.checkedListBoxComparisons.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxComparisons_ItemCheck);
			//
			// labelComparisons
			//
			this.labelComparisons.AutoSize = true;
			this.labelComparisons.Location = new System.Drawing.Point(6, 140);
			this.labelComparisons.Name = "labelComparisons";
			this.labelComparisons.Size = new System.Drawing.Size(380, 13);
			this.labelComparisons.TabIndex = 2;
			this.labelComparisons.Text = "Current Comparison is always written. Each ticked comparison gets a folder too.";
			//
			// groupBoxTimeFormat
			//
			this.groupBoxTimeFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxTimeFormat.Controls.Add(this.labelTimeAccuracy);
			this.groupBoxTimeFormat.Controls.Add(this.comboBoxTimeAccuracy);
			this.groupBoxTimeFormat.Controls.Add(this.labelDeltaAccuracy);
			this.groupBoxTimeFormat.Controls.Add(this.comboBoxDeltaAccuracy);
			this.groupBoxTimeFormat.Controls.Add(this.labelTimers);
			this.groupBoxTimeFormat.Location = new System.Drawing.Point(3, 371);
			this.groupBoxTimeFormat.Name = "groupBoxTimeFormat";
			this.groupBoxTimeFormat.Size = new System.Drawing.Size(448, 94);
			this.groupBoxTimeFormat.TabIndex = 4;
			this.groupBoxTimeFormat.TabStop = false;
			this.groupBoxTimeFormat.Text = "Time Format";
			//
			// labelTimeAccuracy
			//
			this.labelTimeAccuracy.AutoSize = true;
			this.labelTimeAccuracy.Location = new System.Drawing.Point(6, 22);
			this.labelTimeAccuracy.Name = "labelTimeAccuracy";
			this.labelTimeAccuracy.Size = new System.Drawing.Size(35, 13);
			this.labelTimeAccuracy.TabIndex = 0;
			this.labelTimeAccuracy.Text = "Times";
			//
			// comboBoxTimeAccuracy
			//
			this.comboBoxTimeAccuracy.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxTimeAccuracy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTimeAccuracy.FormattingEnabled = true;
			this.comboBoxTimeAccuracy.Items.AddRange(new object[] {
            "Flex (0:05, 0:00.4, 0:00.05)",
            "Seconds (0:05)",
            "Tenths (0:05.2)",
            "Hundredths (0:05.23)",
            "Milliseconds (0:05.234)"});
			this.comboBoxTimeAccuracy.Location = new System.Drawing.Point(100, 19);
			this.comboBoxTimeAccuracy.Name = "comboBoxTimeAccuracy";
			this.comboBoxTimeAccuracy.Size = new System.Drawing.Size(342, 21);
			this.comboBoxTimeAccuracy.TabIndex = 1;
			this.comboBoxTimeAccuracy.SelectedIndexChanged += new System.EventHandler(this.comboBoxTimeAccuracy_SelectedIndexChanged);
			//
			// labelDeltaAccuracy
			//
			this.labelDeltaAccuracy.AutoSize = true;
			this.labelDeltaAccuracy.Location = new System.Drawing.Point(6, 49);
			this.labelDeltaAccuracy.Name = "labelDeltaAccuracy";
			this.labelDeltaAccuracy.Size = new System.Drawing.Size(61, 13);
			this.labelDeltaAccuracy.TabIndex = 2;
			this.labelDeltaAccuracy.Text = "Differences";
			//
			// comboBoxDeltaAccuracy
			//
			this.comboBoxDeltaAccuracy.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxDeltaAccuracy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDeltaAccuracy.FormattingEnabled = true;
			this.comboBoxDeltaAccuracy.Items.AddRange(new object[] {
            "Flex (+5.2, +0.05)",
            "Seconds (+5, +0)",
            "Tenths (+5.2, +0.0)",
            "Hundredths (+5.23, +0.05)",
            "Milliseconds (+5.234, +0.052)"});
			this.comboBoxDeltaAccuracy.Location = new System.Drawing.Point(100, 46);
			this.comboBoxDeltaAccuracy.Name = "comboBoxDeltaAccuracy";
			this.comboBoxDeltaAccuracy.Size = new System.Drawing.Size(342, 21);
			this.comboBoxDeltaAccuracy.TabIndex = 3;
			this.comboBoxDeltaAccuracy.SelectedIndexChanged += new System.EventHandler(this.comboBoxDeltaAccuracy_SelectedIndexChanged);
			//
			// labelTimers
			//
			this.labelTimers.AutoSize = true;
			this.labelTimers.Location = new System.Drawing.Point(6, 73);
			this.labelTimers.Name = "labelTimers";
			this.labelTimers.Size = new System.Drawing.Size(175, 13);
			this.labelTimers.TabIndex = 4;
			this.labelTimers.Text = "Timers always show whole seconds.";
			//
			// labelWriteError
			//
			this.labelWriteError.AutoSize = true;
			this.labelWriteError.ForeColor = System.Drawing.Color.Firebrick;
			this.labelWriteError.Location = new System.Drawing.Point(3, 471);
			this.labelWriteError.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.labelWriteError.MaximumSize = new System.Drawing.Size(448, 0);
			this.labelWriteError.Name = "labelWriteError";
			this.labelWriteError.Size = new System.Drawing.Size(0, 13);
			this.labelWriteError.TabIndex = 5;
			this.labelWriteError.Visible = false;
			// 
			// OutputToFileSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.tableLayoutPanelTop);
			this.Name = "OutputToFileSettings";
			this.Size = new System.Drawing.Size(457, 619);
			this.tableLayoutPanelTop.ResumeLayout(false);
			this.tableLayoutPanelTop.PerformLayout();
			this.groupBoxFolderPath.ResumeLayout(false);
			this.groupBoxFolderPath.PerformLayout();
			this.tableLayoutPanelFolderPath.ResumeLayout(false);
			this.tableLayoutPanelFolderPath.PerformLayout();
			this.groupBoxOutputSelection.ResumeLayout(false);
			this.groupBoxOutputSelection.PerformLayout();
			this.groupBoxSplitList.ResumeLayout(false);
			this.groupBoxComparisons.ResumeLayout(false);
			this.groupBoxComparisons.PerformLayout();
			this.groupBoxTimeFormat.ResumeLayout(false);
			this.groupBoxTimeFormat.PerformLayout();
			this.tableLayoutPanelSplitList.ResumeLayout(false);
			this.tableLayoutPanelSplitList.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownSplitListBefore)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownSplitListAfter)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTop;
        private System.Windows.Forms.GroupBox groupBoxFolderPath;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelFolderPath;
        private System.Windows.Forms.TextBox textBoxFolderPath;
        private System.Windows.Forms.Button buttonFolderPath;
		private System.Windows.Forms.GroupBox groupBoxOutputSelection;
		private System.Windows.Forms.CheckBox checkBoxOutputTimer;
		private System.Windows.Forms.GroupBox groupBoxSplitList;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanelSplitList;
		private System.Windows.Forms.NumericUpDown numericUpDownSplitListBefore;
		private System.Windows.Forms.NumericUpDown numericUpDownSplitListAfter;
		private System.Windows.Forms.Label labelSplitListBefore;
		private System.Windows.Forms.Label labelSplitListAfter;
		private System.Windows.Forms.CheckBox checkBoxOutputSplitList;
		private System.Windows.Forms.CheckBox checkBoxOutputSubsplits;
		private System.Windows.Forms.CheckBox checkBoxOutputCompletedSplits;
		private System.Windows.Forms.GroupBox groupBoxComparisons;
		private System.Windows.Forms.CheckBox checkBoxAllCustomComparisons;
		private System.Windows.Forms.CheckedListBox checkedListBoxComparisons;
		private System.Windows.Forms.Label labelComparisons;
		private System.Windows.Forms.GroupBox groupBoxTimeFormat;
		private System.Windows.Forms.Label labelTimeAccuracy;
		private System.Windows.Forms.ComboBox comboBoxTimeAccuracy;
		private System.Windows.Forms.Label labelDeltaAccuracy;
		private System.Windows.Forms.ComboBox comboBoxDeltaAccuracy;
		private System.Windows.Forms.Label labelTimers;
		private System.Windows.Forms.Label labelWriteError;
	}
}
