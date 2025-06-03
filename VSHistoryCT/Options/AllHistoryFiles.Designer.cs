namespace VSHistory
{
    partial class AllHistoryFiles
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
            this.labInfo = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.treeAllHistoryFiles = new System.Windows.Forms.TreeView();
            this.radOrderByFile = new System.Windows.Forms.RadioButton();
            this.radOrderByDate = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labInfo
            // 
            this.labInfo.AutoSize = true;
            this.labInfo.Location = new System.Drawing.Point(17, 37);
            this.labInfo.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labInfo.Name = "labInfo";
            this.labInfo.Size = new System.Drawing.Size(262, 13);
            this.labInfo.TabIndex = 5;
            this.labInfo.Text = "Double-click a history file for a Diff with the current file.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(14, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(301, 24);
            this.label1.TabIndex = 4;
            this.label1.Text = "VS History Files in this Solution";
            // 
            // treeAllHistoryFiles
            // 
            this.treeAllHistoryFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeAllHistoryFiles.FullRowSelect = true;
            this.treeAllHistoryFiles.Location = new System.Drawing.Point(17, 76);
            this.treeAllHistoryFiles.Margin = new System.Windows.Forms.Padding(2);
            this.treeAllHistoryFiles.Name = "treeAllHistoryFiles";
            this.treeAllHistoryFiles.ShowNodeToolTips = true;
            this.treeAllHistoryFiles.Size = new System.Drawing.Size(344, 270);
            this.treeAllHistoryFiles.TabIndex = 3;
            this.treeAllHistoryFiles.DoubleClick += new System.EventHandler(this.TreeAllHistoryFiles_DoubleClick);
            // 
            // radOrderByFile
            // 
            this.radOrderByFile.AutoSize = true;
            this.radOrderByFile.Checked = true;
            this.radOrderByFile.Location = new System.Drawing.Point(0, 2);
            this.radOrderByFile.Name = "radOrderByFile";
            this.radOrderByFile.Size = new System.Drawing.Size(107, 17);
            this.radOrderByFile.TabIndex = 6;
            this.radOrderByFile.TabStop = true;
            this.radOrderByFile.Text = "Order by filename";
            this.radOrderByFile.UseVisualStyleBackColor = true;
            this.radOrderByFile.CheckedChanged += new System.EventHandler(this.radOrderByFile_CheckedChanged);
            // 
            // radOrderByDate
            // 
            this.radOrderByDate.AutoSize = true;
            this.radOrderByDate.Location = new System.Drawing.Point(127, 2);
            this.radOrderByDate.Name = "radOrderByDate";
            this.radOrderByDate.Size = new System.Drawing.Size(113, 17);
            this.radOrderByDate.TabIndex = 7;
            this.radOrderByDate.Text = "Order by date/time";
            this.radOrderByDate.UseVisualStyleBackColor = true;
            this.radOrderByDate.CheckedChanged += new System.EventHandler(this.radOrderByFile_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radOrderByFile);
            this.groupBox1.Controls.Add(this.radOrderByDate);
            this.groupBox1.Location = new System.Drawing.Point(20, 53);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 20);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // AllHistoryFiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.labInfo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.treeAllHistoryFiles);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "AllHistoryFiles";
            this.Size = new System.Drawing.Size(384, 366);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.AllHistoryFiles_Paint);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labInfo;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.RadioButton radOrderByFile;
        public System.Windows.Forms.RadioButton radOrderByDate;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.TreeView treeAllHistoryFiles;
    }
}
