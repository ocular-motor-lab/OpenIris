namespace OpenIris
{
    partial class Form1
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
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonStartRecording = new System.Windows.Forms.Button();
            this.buttonStopRecording = new System.Windows.Forms.Button();
            this.textBoxIP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelError = new System.Windows.Forms.Label();
            this.labelDataRight = new System.Windows.Forms.Label();
            this.labelDataLeft = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelImageGrabbingStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelTimestamp = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelProcessingTimeLeftEye = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelRecording = new System.Windows.Forms.ToolStripStatusLabel();
            this.buttonResetReference = new System.Windows.Forms.Button();
            this.buttonIncreaseDarkThresholdRight = new System.Windows.Forms.Button();
            this.buttonDecreaseDarkThresholdRight = new System.Windows.Forms.Button();
            this.buttonIncreaseDarkThresholdLeft = new System.Windows.Forms.Button();
            this.buttonDecreaseDarkThresholdLeft = new System.Windows.Forms.Button();
            this.labelDarkThresholdRight = new System.Windows.Forms.Label();
            this.labelDarkThresholdLeft = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.eyeTrackerImageEyeBoxRight = new OpenIris.UI.ImageEyeBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.eyeTrackerImageEyeBoxLeft = new OpenIris.UI.ImageEyeBox();
            this.buttonDowndloadFile = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonConnect
            // 
            this.buttonConnect.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonConnect.Location = new System.Drawing.Point(12, 47);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(111, 63);
            this.buttonConnect.TabIndex = 0;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonStartRecording
            // 
            this.buttonStartRecording.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStartRecording.Location = new System.Drawing.Point(9, 157);
            this.buttonStartRecording.Name = "buttonStartRecording";
            this.buttonStartRecording.Size = new System.Drawing.Size(114, 56);
            this.buttonStartRecording.TabIndex = 1;
            this.buttonStartRecording.Text = "Start Recording";
            this.buttonStartRecording.UseVisualStyleBackColor = true;
            this.buttonStartRecording.Click += new System.EventHandler(this.buttonStartRecording_Click);
            // 
            // buttonStopRecording
            // 
            this.buttonStopRecording.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStopRecording.Location = new System.Drawing.Point(9, 219);
            this.buttonStopRecording.Name = "buttonStopRecording";
            this.buttonStopRecording.Size = new System.Drawing.Size(114, 59);
            this.buttonStopRecording.TabIndex = 2;
            this.buttonStopRecording.Text = "Stop Recording";
            this.buttonStopRecording.UseVisualStyleBackColor = true;
            this.buttonStopRecording.Click += new System.EventHandler(this.buttonStopRecording_Click);
            // 
            // textBoxIP
            // 
            this.textBoxIP.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxIP.Location = new System.Drawing.Point(41, 12);
            this.textBoxIP.Name = "textBoxIP";
            this.textBoxIP.Size = new System.Drawing.Size(82, 26);
            this.textBoxIP.TabIndex = 10;
            this.textBoxIP.Text = "127.0.0.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(4, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 24);
            this.label1.TabIndex = 11;
            this.label1.Text = "IP:";
            // 
            // labelError
            // 
            this.labelError.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelError.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelError.Location = new System.Drawing.Point(8, 472);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(801, 72);
            this.labelError.TabIndex = 12;
            // 
            // labelDataRight
            // 
            this.labelDataRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelDataRight.AutoSize = true;
            this.labelDataRight.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDataRight.Location = new System.Drawing.Point(13, 292);
            this.labelDataRight.Name = "labelDataRight";
            this.labelDataRight.Size = new System.Drawing.Size(171, 24);
            this.labelDataRight.TabIndex = 13;
            this.labelDataRight.Text = "DATA RIGHT EYE:";
            // 
            // labelDataLeft
            // 
            this.labelDataLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelDataLeft.AutoSize = true;
            this.labelDataLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDataLeft.Location = new System.Drawing.Point(8, 292);
            this.labelDataLeft.Name = "labelDataLeft";
            this.labelDataLeft.Size = new System.Drawing.Size(161, 24);
            this.labelDataLeft.TabIndex = 14;
            this.labelDataLeft.Text = "DATA LEFT EYE:";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelImageGrabbingStatus,
            this.toolStripStatusLabelTimestamp,
            this.toolStripStatusLabelProcessingTimeLeftEye,
            this.toolStripStatusLabelRecording});
            this.statusStrip1.Location = new System.Drawing.Point(0, 544);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(827, 24);
            this.statusStrip1.TabIndex = 15;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelImageGrabbingStatus
            // 
            this.toolStripStatusLabelImageGrabbingStatus.Name = "toolStripStatusLabelImageGrabbingStatus";
            this.toolStripStatusLabelImageGrabbingStatus.Size = new System.Drawing.Size(42, 19);
            this.toolStripStatusLabelImageGrabbingStatus.Text = "0.00Hz";
            // 
            // toolStripStatusLabelTimestamp
            // 
            this.toolStripStatusLabelTimestamp.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.toolStripStatusLabelTimestamp.Name = "toolStripStatusLabelTimestamp";
            this.toolStripStatusLabelTimestamp.Size = new System.Drawing.Size(112, 19);
            this.toolStripStatusLabelTimestamp.Text = "Camera not started";
            // 
            // toolStripStatusLabelProcessingTimeLeftEye
            // 
            this.toolStripStatusLabelProcessingTimeLeftEye.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.toolStripStatusLabelProcessingTimeLeftEye.Name = "toolStripStatusLabelProcessingTimeLeftEye";
            this.toolStripStatusLabelProcessingTimeLeftEye.Size = new System.Drawing.Size(136, 19);
            this.toolStripStatusLabelProcessingTimeLeftEye.Text = "Processing Time = 0ms;";
            this.toolStripStatusLabelProcessingTimeLeftEye.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabelRecording
            // 
            this.toolStripStatusLabelRecording.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.toolStripStatusLabelRecording.Name = "toolStripStatusLabelRecording";
            this.toolStripStatusLabelRecording.Size = new System.Drawing.Size(160, 19);
            this.toolStripStatusLabelRecording.Text = "Frames dropped video rec: 0";
            // 
            // buttonResetReference
            // 
            this.buttonResetReference.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonResetReference.Location = new System.Drawing.Point(8, 293);
            this.buttonResetReference.Name = "buttonResetReference";
            this.buttonResetReference.Size = new System.Drawing.Size(115, 59);
            this.buttonResetReference.TabIndex = 16;
            this.buttonResetReference.Text = "Reset reference";
            this.buttonResetReference.UseVisualStyleBackColor = true;
            this.buttonResetReference.Click += new System.EventHandler(this.buttonResetReference_Click);
            // 
            // buttonIncreaseDarkThresholdRight
            // 
            this.buttonIncreaseDarkThresholdRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonIncreaseDarkThresholdRight.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonIncreaseDarkThresholdRight.Location = new System.Drawing.Point(229, 378);
            this.buttonIncreaseDarkThresholdRight.Name = "buttonIncreaseDarkThresholdRight";
            this.buttonIncreaseDarkThresholdRight.Size = new System.Drawing.Size(41, 35);
            this.buttonIncreaseDarkThresholdRight.TabIndex = 17;
            this.buttonIncreaseDarkThresholdRight.Text = "+";
            this.buttonIncreaseDarkThresholdRight.UseVisualStyleBackColor = true;
            this.buttonIncreaseDarkThresholdRight.Click += new System.EventHandler(this.buttonIncreaseDarkThresholdRight_Click);
            // 
            // buttonDecreaseDarkThresholdRight
            // 
            this.buttonDecreaseDarkThresholdRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDecreaseDarkThresholdRight.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDecreaseDarkThresholdRight.Location = new System.Drawing.Point(276, 378);
            this.buttonDecreaseDarkThresholdRight.Name = "buttonDecreaseDarkThresholdRight";
            this.buttonDecreaseDarkThresholdRight.Size = new System.Drawing.Size(41, 35);
            this.buttonDecreaseDarkThresholdRight.TabIndex = 18;
            this.buttonDecreaseDarkThresholdRight.Text = "-";
            this.buttonDecreaseDarkThresholdRight.UseVisualStyleBackColor = true;
            this.buttonDecreaseDarkThresholdRight.Click += new System.EventHandler(this.buttonDecreaseDarkThresholdRight_Click);
            // 
            // buttonIncreaseDarkThresholdLeft
            // 
            this.buttonIncreaseDarkThresholdLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonIncreaseDarkThresholdLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonIncreaseDarkThresholdLeft.Location = new System.Drawing.Point(219, 378);
            this.buttonIncreaseDarkThresholdLeft.Name = "buttonIncreaseDarkThresholdLeft";
            this.buttonIncreaseDarkThresholdLeft.Size = new System.Drawing.Size(41, 35);
            this.buttonIncreaseDarkThresholdLeft.TabIndex = 19;
            this.buttonIncreaseDarkThresholdLeft.Text = "+";
            this.buttonIncreaseDarkThresholdLeft.UseVisualStyleBackColor = true;
            this.buttonIncreaseDarkThresholdLeft.Click += new System.EventHandler(this.buttonIncreaseDarkThresholdLeft_Click);
            // 
            // buttonDecreaseDarkThresholdLeft
            // 
            this.buttonDecreaseDarkThresholdLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDecreaseDarkThresholdLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDecreaseDarkThresholdLeft.Location = new System.Drawing.Point(266, 378);
            this.buttonDecreaseDarkThresholdLeft.Name = "buttonDecreaseDarkThresholdLeft";
            this.buttonDecreaseDarkThresholdLeft.Size = new System.Drawing.Size(41, 35);
            this.buttonDecreaseDarkThresholdLeft.TabIndex = 20;
            this.buttonDecreaseDarkThresholdLeft.Text = "-";
            this.buttonDecreaseDarkThresholdLeft.UseVisualStyleBackColor = true;
            this.buttonDecreaseDarkThresholdLeft.Click += new System.EventHandler(this.buttonDecreaseDarkThresholdLeft_Click);
            // 
            // labelDarkThresholdRight
            // 
            this.labelDarkThresholdRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelDarkThresholdRight.AutoSize = true;
            this.labelDarkThresholdRight.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDarkThresholdRight.Location = new System.Drawing.Point(13, 378);
            this.labelDarkThresholdRight.Name = "labelDarkThresholdRight";
            this.labelDarkThresholdRight.Size = new System.Drawing.Size(154, 24);
            this.labelDarkThresholdRight.TabIndex = 13;
            this.labelDarkThresholdRight.Text = "Pupil threshokld: ";
            // 
            // labelDarkThresholdLeft
            // 
            this.labelDarkThresholdLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelDarkThresholdLeft.AutoSize = true;
            this.labelDarkThresholdLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDarkThresholdLeft.Location = new System.Drawing.Point(15, 378);
            this.labelDarkThresholdLeft.Name = "labelDarkThresholdLeft";
            this.labelDarkThresholdLeft.Size = new System.Drawing.Size(154, 24);
            this.labelDarkThresholdLeft.TabIndex = 14;
            this.labelDarkThresholdLeft.Text = "Pupil threshokld: ";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.panel2, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(129, 15);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(686, 454);
            this.tableLayoutPanel2.TabIndex = 21;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.eyeTrackerImageEyeBoxRight);
            this.panel1.Controls.Add(this.labelDataRight);
            this.panel1.Controls.Add(this.buttonIncreaseDarkThresholdRight);
            this.panel1.Controls.Add(this.buttonDecreaseDarkThresholdRight);
            this.panel1.Controls.Add(this.labelDarkThresholdRight);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(337, 448);
            this.panel1.TabIndex = 0;
            // 
            // eyeTrackerImageEyeBoxRight
            // 
            this.eyeTrackerImageEyeBoxRight.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.eyeTrackerImageEyeBoxRight.Location = new System.Drawing.Point(7, 3);
            this.eyeTrackerImageEyeBoxRight.Name = "eyeTrackerImageEyeBoxRight";
            this.eyeTrackerImageEyeBoxRight.Size = new System.Drawing.Size(327, 281);
            this.eyeTrackerImageEyeBoxRight.TabIndex = 15;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.eyeTrackerImageEyeBoxLeft);
            this.panel2.Controls.Add(this.buttonDecreaseDarkThresholdLeft);
            this.panel2.Controls.Add(this.labelDataLeft);
            this.panel2.Controls.Add(this.buttonIncreaseDarkThresholdLeft);
            this.panel2.Controls.Add(this.labelDarkThresholdLeft);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(346, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(337, 448);
            this.panel2.TabIndex = 1;
            // 
            // eyeTrackerImageEyeBoxLeft
            // 
            this.eyeTrackerImageEyeBoxLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.eyeTrackerImageEyeBoxLeft.Location = new System.Drawing.Point(3, 3);
            this.eyeTrackerImageEyeBoxLeft.Name = "eyeTrackerImageEyeBoxLeft";
            this.eyeTrackerImageEyeBoxLeft.Size = new System.Drawing.Size(331, 281);
            this.eyeTrackerImageEyeBoxLeft.TabIndex = 16;
            // 
            // buttonDowndloadFile
            // 
            this.buttonDowndloadFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDowndloadFile.Location = new System.Drawing.Point(8, 410);
            this.buttonDowndloadFile.Name = "buttonDowndloadFile";
            this.buttonDowndloadFile.Size = new System.Drawing.Size(115, 59);
            this.buttonDowndloadFile.TabIndex = 22;
            this.buttonDowndloadFile.Text = "Download file ...";
            this.buttonDowndloadFile.UseVisualStyleBackColor = true;
            this.buttonDowndloadFile.Click += new System.EventHandler(this.buttonDowndloadFile_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 568);
            this.Controls.Add(this.buttonDowndloadFile);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.buttonResetReference);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.labelError);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxIP);
            this.Controls.Add(this.buttonStopRecording);
            this.Controls.Add(this.buttonStartRecording);
            this.Controls.Add(this.buttonConnect);
            this.MinimumSize = new System.Drawing.Size(500, 550);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonStartRecording;
        private System.Windows.Forms.Button buttonStopRecording;
        private System.Windows.Forms.TextBox textBoxIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.Label labelDataRight;
        private System.Windows.Forms.Label labelDataLeft;
        private UI.ImageEyeBox eyeTrackerImageEyeBoxRight;
        private UI.ImageEyeBox eyeTrackerImageEyeBoxLeft;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelImageGrabbingStatus;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelTimestamp;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelProcessingTimeLeftEye;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelRecording;
        private System.Windows.Forms.Button buttonResetReference;
        private System.Windows.Forms.Button buttonIncreaseDarkThresholdRight;
        private System.Windows.Forms.Button buttonDecreaseDarkThresholdRight;
        private System.Windows.Forms.Button buttonIncreaseDarkThresholdLeft;
        private System.Windows.Forms.Button buttonDecreaseDarkThresholdLeft;
        private System.Windows.Forms.Label labelDarkThresholdRight;
        private System.Windows.Forms.Label labelDarkThresholdLeft;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button buttonDowndloadFile;
    }
}

