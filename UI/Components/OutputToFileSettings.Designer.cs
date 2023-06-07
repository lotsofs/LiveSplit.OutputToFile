namespace LiveSplit.UI.Components
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
			this.checkBoxOutputSplitList = new System.Windows.Forms.CheckBox();
			this.checkBoxOutputTimer = new System.Windows.Forms.CheckBox();
			this.groupBoxSplitList = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanelSplitList = new System.Windows.Forms.TableLayoutPanel();
			this.numericUpDownSplitListBefore = new System.Windows.Forms.NumericUpDown();
			this.numericUpDownSplitListAfter = new System.Windows.Forms.NumericUpDown();
			this.labelSplitListBefore = new System.Windows.Forms.Label();
			this.labelSplitListAfter = new System.Windows.Forms.Label();
			this.tableLayoutPanelTop.SuspendLayout();
			this.groupBoxFolderPath.SuspendLayout();
			this.tableLayoutPanelFolderPath.SuspendLayout();
			this.groupBoxOutputSelection.SuspendLayout();
			this.groupBoxSplitList.SuspendLayout();
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
			this.tableLayoutPanelTop.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanelTop.Name = "tableLayoutPanelTop";
			this.tableLayoutPanelTop.RowCount = 5;
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 51F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 71F));
			this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 8F));
			this.tableLayoutPanelTop.Size = new System.Drawing.Size(326, 299);
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
			this.groupBoxFolderPath.Size = new System.Drawing.Size(320, 45);
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
			this.tableLayoutPanelFolderPath.Size = new System.Drawing.Size(314, 26);
			this.tableLayoutPanelFolderPath.TabIndex = 0;
			// 
			// textBoxFolderPath
			// 
			this.textBoxFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFolderPath.Location = new System.Drawing.Point(3, 3);
			this.textBoxFolderPath.Name = "textBoxFolderPath";
			this.textBoxFolderPath.Size = new System.Drawing.Size(210, 20);
			this.textBoxFolderPath.TabIndex = 0;
			// 
			// buttonFolderPath
			// 
			this.buttonFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonFolderPath.AutoSize = true;
			this.buttonFolderPath.Location = new System.Drawing.Point(219, 3);
			this.buttonFolderPath.MinimumSize = new System.Drawing.Size(75, 15);
			this.buttonFolderPath.Name = "buttonFolderPath";
			this.buttonFolderPath.Size = new System.Drawing.Size(92, 20);
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
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputSplitList);
			this.groupBoxOutputSelection.Controls.Add(this.checkBoxOutputTimer);
			this.groupBoxOutputSelection.Location = new System.Drawing.Point(3, 54);
			this.groupBoxOutputSelection.Name = "groupBoxOutputSelection";
			this.groupBoxOutputSelection.Size = new System.Drawing.Size(320, 68);
			this.groupBoxOutputSelection.TabIndex = 2;
			this.groupBoxOutputSelection.TabStop = false;
			this.groupBoxOutputSelection.Text = "Additional Output";
			// 
			// checkBoxOutputSubsplits
			// 
			this.checkBoxOutputSubsplits.AutoSize = true;
			this.checkBoxOutputSubsplits.Checked = true;
			this.checkBoxOutputSubsplits.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOutputSubsplits.Location = new System.Drawing.Point(119, 19);
			this.checkBoxOutputSubsplits.Name = "checkBoxOutputSubsplits";
			this.checkBoxOutputSubsplits.Size = new System.Drawing.Size(68, 17);
			this.checkBoxOutputSubsplits.TabIndex = 3;
			this.checkBoxOutputSubsplits.Text = "Subsplits";
			this.checkBoxOutputSubsplits.UseVisualStyleBackColor = true;
			this.checkBoxOutputSubsplits.CheckedChanged += new System.EventHandler(this.checkBoxOutputSubsplits_CheckedChanged);
			// 
			// checkBoxOutputSplitList
			// 
			this.checkBoxOutputSplitList.AutoSize = true;
			this.checkBoxOutputSplitList.Checked = true;
			this.checkBoxOutputSplitList.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOutputSplitList.Location = new System.Drawing.Point(6, 42);
			this.checkBoxOutputSplitList.Name = "checkBoxOutputSplitList";
			this.checkBoxOutputSplitList.Size = new System.Drawing.Size(65, 17);
			this.checkBoxOutputSplitList.TabIndex = 2;
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
			this.checkBoxOutputTimer.TabIndex = 1;
			this.checkBoxOutputTimer.Text = "Timer";
			this.checkBoxOutputTimer.UseVisualStyleBackColor = true;
			this.checkBoxOutputTimer.CheckedChanged += new System.EventHandler(this.checkBoxOutputTimer_CheckedChanged);
			// 
			// groupBoxSplitList
			// 
			this.groupBoxSplitList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxSplitList.Controls.Add(this.tableLayoutPanelSplitList);
			this.groupBoxSplitList.Location = new System.Drawing.Point(3, 128);
			this.groupBoxSplitList.Name = "groupBoxSplitList";
			this.groupBoxSplitList.Size = new System.Drawing.Size(320, 72);
			this.groupBoxSplitList.TabIndex = 1;
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
			this.tableLayoutPanelSplitList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelSplitList.Location = new System.Drawing.Point(3, 16);
			this.tableLayoutPanelSplitList.Name = "tableLayoutPanelSplitList";
			this.tableLayoutPanelSplitList.RowCount = 2;
			this.tableLayoutPanelSplitList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelSplitList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelSplitList.Size = new System.Drawing.Size(314, 53);
			this.tableLayoutPanelSplitList.TabIndex = 0;
			// 
			// numericUpDownSplitListBefore
			// 
			this.numericUpDownSplitListBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.numericUpDownSplitListBefore.Location = new System.Drawing.Point(191, 3);
			this.numericUpDownSplitListBefore.Name = "numericUpDownSplitListBefore";
			this.numericUpDownSplitListBefore.Size = new System.Drawing.Size(120, 20);
			this.numericUpDownSplitListBefore.TabIndex = 0;
			this.numericUpDownSplitListBefore.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
			this.numericUpDownSplitListBefore.ValueChanged += new System.EventHandler(this.numericUpDownSplitListBefore_ValueChanged);
			// 
			// numericUpDownSplitListAfter
			// 
			this.numericUpDownSplitListAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.numericUpDownSplitListAfter.Location = new System.Drawing.Point(191, 29);
			this.numericUpDownSplitListAfter.Name = "numericUpDownSplitListAfter";
			this.numericUpDownSplitListAfter.Size = new System.Drawing.Size(120, 20);
			this.numericUpDownSplitListAfter.TabIndex = 1;
			this.numericUpDownSplitListAfter.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.numericUpDownSplitListAfter.ValueChanged += new System.EventHandler(this.numericUpDownSplitListAfter_ValueChanged);
			// 
			// labelSplitListBefore
			// 
			this.labelSplitListBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.labelSplitListBefore.AutoSize = true;
			this.labelSplitListBefore.Location = new System.Drawing.Point(3, 6);
			this.labelSplitListBefore.Name = "labelSplitListBefore";
			this.labelSplitListBefore.Size = new System.Drawing.Size(181, 13);
			this.labelSplitListBefore.TabIndex = 2;
			this.labelSplitListBefore.Text = "Splits before current";
			// 
			// labelSplitListAfter
			// 
			this.labelSplitListAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.labelSplitListAfter.AutoSize = true;
			this.labelSplitListAfter.Location = new System.Drawing.Point(3, 33);
			this.labelSplitListAfter.Name = "labelSplitListAfter";
			this.labelSplitListAfter.Size = new System.Drawing.Size(181, 13);
			this.labelSplitListAfter.TabIndex = 3;
			this.labelSplitListAfter.Text = "Splits after current";
			// 
			// OutputToFileSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.tableLayoutPanelTop);
			this.Name = "OutputToFileSettings";
			this.Size = new System.Drawing.Size(329, 330);
			this.Load += new System.EventHandler(this.OutputToFileSettings_Load);
			this.tableLayoutPanelTop.ResumeLayout(false);
			this.tableLayoutPanelTop.PerformLayout();
			this.groupBoxFolderPath.ResumeLayout(false);
			this.groupBoxFolderPath.PerformLayout();
			this.tableLayoutPanelFolderPath.ResumeLayout(false);
			this.tableLayoutPanelFolderPath.PerformLayout();
			this.groupBoxOutputSelection.ResumeLayout(false);
			this.groupBoxOutputSelection.PerformLayout();
			this.groupBoxSplitList.ResumeLayout(false);
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
	}
}
