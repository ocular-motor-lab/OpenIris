namespace OpenIris.UI
{
    partial class SelectVideoDialog
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
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            this.fileLeftButton = new System.Windows.Forms.Button();
            this.systemComboBox = new System.Windows.Forms.ComboBox();
            this.labelVideo1 = new System.Windows.Forms.Label();
            this.file1TextBox = new System.Windows.Forms.TextBox();
            this.file2TextBox = new System.Windows.Forms.TextBox();
            this.labelVideo2 = new System.Windows.Forms.Label();
            this.fileRightButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.acceptButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.textBoxCalibration = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonCalibrationLeftEye = new System.Windows.Forms.Button();
            this.checkBoxSaveProcessedVideo = new System.Windows.Forms.CheckBox();
            this.textBoxToFrame = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxFromFrame = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBoxCustomRange = new System.Windows.Forms.CheckBox();
            this.panelVideo1 = new System.Windows.Forms.Panel();
            this.panelSystem = new System.Windows.Forms.Panel();
            this.panelCalibration = new System.Windows.Forms.Panel();
            this.panelCustomRange = new System.Windows.Forms.Panel();
            this.panelRange = new System.Windows.Forms.Panel();
            this.panelSaveProcessedVideo = new System.Windows.Forms.Panel();
            this.panelAcceptCancel = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.panelVideo2 = new System.Windows.Forms.Panel();
            this.panelVideo1.SuspendLayout();
            this.panelSystem.SuspendLayout();
            this.panelCalibration.SuspendLayout();
            this.panelCustomRange.SuspendLayout();
            this.panelRange.SuspendLayout();
            this.panelSaveProcessedVideo.SuspendLayout();
            this.panelAcceptCancel.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panelVideo2.SuspendLayout();
            this.SuspendLayout();
            // 
            // fileLeftButton
            // 
            this.fileLeftButton.Location = new System.Drawing.Point(885, 2);
            this.fileLeftButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.fileLeftButton.Name = "fileLeftButton";
            this.fileLeftButton.Size = new System.Drawing.Size(50, 35);
            this.fileLeftButton.TabIndex = 1;
            this.fileLeftButton.Tag = "Left";
            this.fileLeftButton.Text = "...";
            this.fileLeftButton.UseVisualStyleBackColor = true;
            this.fileLeftButton.Click += new System.EventHandler(this.File1Button_Click);
            // 
            // systemComboBox
            // 
            this.systemComboBox.FormattingEnabled = true;
            this.systemComboBox.Location = new System.Drawing.Point(176, 5);
            this.systemComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.systemComboBox.Name = "systemComboBox";
            this.systemComboBox.Size = new System.Drawing.Size(374, 28);
            this.systemComboBox.TabIndex = 0;
            this.systemComboBox.SelectedIndexChanged += new System.EventHandler(this.systemComboBox_SelectedIndexChanged);
            // 
            // labelVideo1
            // 
            this.labelVideo1.AutoSize = true;
            this.labelVideo1.Location = new System.Drawing.Point(32, 9);
            this.labelVideo1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelVideo1.Name = "labelVideo1";
            this.labelVideo1.Size = new System.Drawing.Size(131, 20);
            this.labelVideo1.TabIndex = 0;
            this.labelVideo1.Text = "Left eye video file";
            // 
            // file1TextBox
            // 
            this.file1TextBox.Location = new System.Drawing.Point(176, 5);
            this.file1TextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.file1TextBox.Name = "file1TextBox";
            this.file1TextBox.Size = new System.Drawing.Size(698, 26);
            this.file1TextBox.TabIndex = 0;
            // 
            // file2TextBox
            // 
            this.file2TextBox.Location = new System.Drawing.Point(176, 5);
            this.file2TextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.file2TextBox.Name = "file2TextBox";
            this.file2TextBox.Size = new System.Drawing.Size(698, 26);
            this.file2TextBox.TabIndex = 0;
            // 
            // labelVideo2
            // 
            this.labelVideo2.AutoSize = true;
            this.labelVideo2.Location = new System.Drawing.Point(21, 9);
            this.labelVideo2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelVideo2.Name = "labelVideo2";
            this.labelVideo2.Size = new System.Drawing.Size(141, 20);
            this.labelVideo2.TabIndex = 2;
            this.labelVideo2.Text = "Right eye video file";
            // 
            // fileRightButton
            // 
            this.fileRightButton.Location = new System.Drawing.Point(885, 0);
            this.fileRightButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.fileRightButton.Name = "fileRightButton";
            this.fileRightButton.Size = new System.Drawing.Size(50, 35);
            this.fileRightButton.TabIndex = 1;
            this.fileRightButton.Tag = "Right";
            this.fileRightButton.Text = "...";
            this.fileRightButton.UseVisualStyleBackColor = true;
            this.fileRightButton.Click += new System.EventHandler(this.File1Button_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 9);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 20);
            this.label3.TabIndex = 7;
            this.label3.Text = "Eye tracking system";
            // 
            // acceptButton
            // 
            this.acceptButton.Location = new System.Drawing.Point(700, 5);
            this.acceptButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(112, 35);
            this.acceptButton.TabIndex = 0;
            this.acceptButton.Text = "Accept";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.acceptButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(822, 5);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(112, 35);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // textBoxCalibration
            // 
            this.textBoxCalibration.Location = new System.Drawing.Point(176, 6);
            this.textBoxCalibration.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxCalibration.Name = "textBoxCalibration";
            this.textBoxCalibration.Size = new System.Drawing.Size(698, 26);
            this.textBoxCalibration.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(58, 11);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 20);
            this.label5.TabIndex = 11;
            this.label5.Text = "Calibration file";
            // 
            // buttonCalibrationLeftEye
            // 
            this.buttonCalibrationLeftEye.Location = new System.Drawing.Point(885, 3);
            this.buttonCalibrationLeftEye.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonCalibrationLeftEye.Name = "buttonCalibrationLeftEye";
            this.buttonCalibrationLeftEye.Size = new System.Drawing.Size(50, 35);
            this.buttonCalibrationLeftEye.TabIndex = 1;
            this.buttonCalibrationLeftEye.Text = "...";
            this.buttonCalibrationLeftEye.UseVisualStyleBackColor = true;
            this.buttonCalibrationLeftEye.Click += new System.EventHandler(this.ButtonCalibrationLeftEye_Click);
            // 
            // checkBoxSaveProcessedVideo
            // 
            this.checkBoxSaveProcessedVideo.AutoSize = true;
            this.checkBoxSaveProcessedVideo.Location = new System.Drawing.Point(10, 5);
            this.checkBoxSaveProcessedVideo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.checkBoxSaveProcessedVideo.Name = "checkBoxSaveProcessedVideo";
            this.checkBoxSaveProcessedVideo.Size = new System.Drawing.Size(190, 24);
            this.checkBoxSaveProcessedVideo.TabIndex = 0;
            this.checkBoxSaveProcessedVideo.Text = "Save processed video";
            this.checkBoxSaveProcessedVideo.UseVisualStyleBackColor = true;
            // 
            // textBoxToFrame
            // 
            this.textBoxToFrame.Location = new System.Drawing.Point(376, 6);
            this.textBoxToFrame.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxToFrame.Name = "textBoxToFrame";
            this.textBoxToFrame.Size = new System.Drawing.Size(150, 26);
            this.textBoxToFrame.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(339, 11);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(27, 20);
            this.label6.TabIndex = 19;
            this.label6.Text = "to:";
            // 
            // textBoxFromFrame
            // 
            this.textBoxFromFrame.Location = new System.Drawing.Point(189, 6);
            this.textBoxFromFrame.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxFromFrame.Name = "textBoxFromFrame";
            this.textBoxFromFrame.Size = new System.Drawing.Size(139, 26);
            this.textBoxFromFrame.TabIndex = 0;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 11);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(167, 20);
            this.label7.TabIndex = 17;
            this.label7.Text = "Play only frames from: ";
            // 
            // checkBoxCustomRange
            // 
            this.checkBoxCustomRange.AutoSize = true;
            this.checkBoxCustomRange.Checked = true;
            this.checkBoxCustomRange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCustomRange.Location = new System.Drawing.Point(10, 8);
            this.checkBoxCustomRange.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.checkBoxCustomRange.Name = "checkBoxCustomRange";
            this.checkBoxCustomRange.Size = new System.Drawing.Size(129, 24);
            this.checkBoxCustomRange.TabIndex = 9;
            this.checkBoxCustomRange.Text = "Play full video";
            this.checkBoxCustomRange.UseVisualStyleBackColor = true;
            this.checkBoxCustomRange.CheckedChanged += new System.EventHandler(this.CheckBoxCustomRange_CheckedChanged);
            // 
            // panelVideo1
            // 
            this.panelVideo1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelVideo1.Controls.Add(this.file1TextBox);
            this.panelVideo1.Controls.Add(this.fileLeftButton);
            this.panelVideo1.Controls.Add(this.labelVideo1);
            this.panelVideo1.Location = new System.Drawing.Point(19, 79);
            this.panelVideo1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelVideo1.Name = "panelVideo1";
            this.panelVideo1.Size = new System.Drawing.Size(939, 49);
            this.panelVideo1.TabIndex = 0;
            // 
            // panelSystem
            // 
            this.panelSystem.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelSystem.Controls.Add(this.systemComboBox);
            this.panelSystem.Controls.Add(this.label3);
            this.panelSystem.Location = new System.Drawing.Point(19, 20);
            this.panelSystem.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelSystem.Name = "panelSystem";
            this.panelSystem.Size = new System.Drawing.Size(939, 49);
            this.panelSystem.TabIndex = 2;
            // 
            // panelCalibration
            // 
            this.panelCalibration.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelCalibration.Controls.Add(this.textBoxCalibration);
            this.panelCalibration.Controls.Add(this.buttonCalibrationLeftEye);
            this.panelCalibration.Controls.Add(this.label5);
            this.panelCalibration.Location = new System.Drawing.Point(19, 193);
            this.panelCalibration.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelCalibration.Name = "panelCalibration";
            this.panelCalibration.Size = new System.Drawing.Size(939, 49);
            this.panelCalibration.TabIndex = 3;
            // 
            // panelCustomRange
            // 
            this.panelCustomRange.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelCustomRange.Controls.Add(this.panelRange);
            this.panelCustomRange.Controls.Add(this.checkBoxCustomRange);
            this.panelCustomRange.Location = new System.Drawing.Point(19, 302);
            this.panelCustomRange.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelCustomRange.Name = "panelCustomRange";
            this.panelCustomRange.Size = new System.Drawing.Size(939, 46);
            this.panelCustomRange.TabIndex = 4;
            // 
            // panelRange
            // 
            this.panelRange.Controls.Add(this.textBoxFromFrame);
            this.panelRange.Controls.Add(this.textBoxToFrame);
            this.panelRange.Controls.Add(this.label6);
            this.panelRange.Controls.Add(this.label7);
            this.panelRange.Location = new System.Drawing.Point(278, 2);
            this.panelRange.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelRange.Name = "panelRange";
            this.panelRange.Size = new System.Drawing.Size(657, 40);
            this.panelRange.TabIndex = 10;
            // 
            // panelSaveProcessedVideo
            // 
            this.panelSaveProcessedVideo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelSaveProcessedVideo.Controls.Add(this.checkBoxSaveProcessedVideo);
            this.panelSaveProcessedVideo.Location = new System.Drawing.Point(19, 252);
            this.panelSaveProcessedVideo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelSaveProcessedVideo.Name = "panelSaveProcessedVideo";
            this.panelSaveProcessedVideo.Size = new System.Drawing.Size(939, 40);
            this.panelSaveProcessedVideo.TabIndex = 3;
            // 
            // panelAcceptCancel
            // 
            this.panelAcceptCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAcceptCancel.Controls.Add(this.cancelButton);
            this.panelAcceptCancel.Controls.Add(this.acceptButton);
            this.panelAcceptCancel.Location = new System.Drawing.Point(19, 358);
            this.panelAcceptCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelAcceptCancel.Name = "panelAcceptCancel";
            this.panelAcceptCancel.Size = new System.Drawing.Size(939, 51);
            this.panelAcceptCancel.TabIndex = 5;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.panelSystem);
            this.flowLayoutPanel1.Controls.Add(this.panelVideo1);
            this.flowLayoutPanel1.Controls.Add(this.panelVideo2);
            this.flowLayoutPanel1.Controls.Add(this.panelCalibration);
            this.flowLayoutPanel1.Controls.Add(this.panelSaveProcessedVideo);
            this.flowLayoutPanel1.Controls.Add(this.panelCustomRange);
            this.flowLayoutPanel1.Controls.Add(this.panelAcceptCancel);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(15, 15, 15, 15);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(982, 423);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // panelVideo2
            // 
            this.panelVideo2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelVideo2.Controls.Add(this.file2TextBox);
            this.panelVideo2.Controls.Add(this.labelVideo2);
            this.panelVideo2.Controls.Add(this.fileRightButton);
            this.panelVideo2.Location = new System.Drawing.Point(19, 138);
            this.panelVideo2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panelVideo2.Name = "panelVideo2";
            this.panelVideo2.Size = new System.Drawing.Size(939, 45);
            this.panelVideo2.TabIndex = 1;
            // 
            // SelectVideoDialog
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(982, 423);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SelectVideoDialog";
            this.Text = "Select video";
            this.Load += new System.EventHandler(this.SelectVideoDialog_Load);
            this.panelVideo1.ResumeLayout(false);
            this.panelVideo1.PerformLayout();
            this.panelSystem.ResumeLayout(false);
            this.panelSystem.PerformLayout();
            this.panelCalibration.ResumeLayout(false);
            this.panelCalibration.PerformLayout();
            this.panelCustomRange.ResumeLayout(false);
            this.panelCustomRange.PerformLayout();
            this.panelRange.ResumeLayout(false);
            this.panelRange.PerformLayout();
            this.panelSaveProcessedVideo.ResumeLayout(false);
            this.panelSaveProcessedVideo.PerformLayout();
            this.panelAcceptCancel.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panelVideo2.ResumeLayout(false);
            this.panelVideo2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button fileLeftButton;
        private System.Windows.Forms.ComboBox systemComboBox;
        private System.Windows.Forms.Label labelVideo1;
        private System.Windows.Forms.TextBox file1TextBox;
        private System.Windows.Forms.TextBox file2TextBox;
        private System.Windows.Forms.Label labelVideo2;
        private System.Windows.Forms.Button fileRightButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox textBoxCalibration;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button buttonCalibrationLeftEye;
        private System.Windows.Forms.CheckBox checkBoxSaveProcessedVideo;
        private System.Windows.Forms.TextBox textBoxToFrame;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxFromFrame;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox checkBoxCustomRange;
        private System.Windows.Forms.Panel panelVideo1;
        private System.Windows.Forms.Panel panelSystem;
        private System.Windows.Forms.Panel panelCalibration;
        private System.Windows.Forms.Panel panelCustomRange;
        private System.Windows.Forms.Panel panelSaveProcessedVideo;
        private System.Windows.Forms.Panel panelAcceptCancel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Panel panelVideo2;
        private System.Windows.Forms.Panel panelRange;
    }
}