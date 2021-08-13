namespace OpenIris.UI
{
    partial class BatchProcessing
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeaderFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderCalibration = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderProgress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonLoadBatchDescription = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonSaveBatchDescription = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFile,
            this.columnHeaderCalibration,
            this.columnHeaderProgress});
            this.listView1.Location = new System.Drawing.Point(12, 12);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(620, 582);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderFile
            // 
            this.columnHeaderFile.Text = "File";
            this.columnHeaderFile.Width = 231;
            // 
            // columnHeaderCalibration
            // 
            this.columnHeaderCalibration.Text = "Calibration";
            this.columnHeaderCalibration.Width = 235;
            // 
            // columnHeaderProgress
            // 
            this.columnHeaderProgress.Text = "Progress";
            this.columnHeaderProgress.Width = 66;
            // 
            // buttonAdd
            // 
            this.buttonAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAdd.Location = new System.Drawing.Point(3, 3);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(232, 39);
            this.buttonAdd.TabIndex = 1;
            this.buttonAdd.Text = "Add session";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.ButtonAdd_Click);
            // 
            // buttonRemove
            // 
            this.buttonRemove.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRemove.Location = new System.Drawing.Point(3, 48);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(232, 39);
            this.buttonRemove.TabIndex = 2;
            this.buttonRemove.Text = "Remove Session";
            this.buttonRemove.UseVisualStyleBackColor = true;
            this.buttonRemove.Click += new System.EventHandler(this.ButtonRemove_Click);
            // 
            // buttonStart
            // 
            this.buttonStart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(0, 337);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(232, 104);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStart_Click);
            // 
            // buttonLoadBatchDescription
            // 
            this.buttonLoadBatchDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonLoadBatchDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadBatchDescription.Location = new System.Drawing.Point(641, 492);
            this.buttonLoadBatchDescription.Name = "buttonLoadBatchDescription";
            this.buttonLoadBatchDescription.Size = new System.Drawing.Size(232, 39);
            this.buttonLoadBatchDescription.TabIndex = 3;
            this.buttonLoadBatchDescription.Text = "Load batch description";
            this.buttonLoadBatchDescription.UseVisualStyleBackColor = true;
            this.buttonLoadBatchDescription.Click += new System.EventHandler(this.buttonLoadBatchDescription_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.buttonStart);
            this.panel1.Controls.Add(this.buttonAdd);
            this.panel1.Controls.Add(this.buttonRemove);
            this.panel1.Location = new System.Drawing.Point(638, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(238, 444);
            this.panel1.TabIndex = 4;
            // 
            // buttonSaveBatchDescription
            // 
            this.buttonSaveBatchDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSaveBatchDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSaveBatchDescription.Location = new System.Drawing.Point(641, 550);
            this.buttonSaveBatchDescription.Name = "buttonSaveBatchDescription";
            this.buttonSaveBatchDescription.Size = new System.Drawing.Size(232, 39);
            this.buttonSaveBatchDescription.TabIndex = 4;
            this.buttonSaveBatchDescription.Text = "Save batch description";
            this.buttonSaveBatchDescription.UseVisualStyleBackColor = true;
            this.buttonSaveBatchDescription.Click += new System.EventHandler(this.buttonSaveBatchDescription_Click);
            // 
            // BatchProcessing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(888, 606);
            this.Controls.Add(this.buttonSaveBatchDescription);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.buttonLoadBatchDescription);
            this.Name = "BatchProcessing";
            this.Text = "BatchAnalysis";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BatchProcessing_FormClosing);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.ColumnHeader columnHeaderFile;
        private System.Windows.Forms.ColumnHeader columnHeaderCalibration;
        private System.Windows.Forms.ColumnHeader columnHeaderProgress;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonLoadBatchDescription;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonSaveBatchDescription;
    }
}