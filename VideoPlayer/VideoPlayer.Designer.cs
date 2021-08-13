namespace VideoPlayer
{
    partial class VideoPlayer
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.eyeTrackerImageEyeBox2 = new OpenIris.UI.EyeTrackerImageEyeBox();
            this.eyeTrackerImageEyeBox1 = new OpenIris.UI.EyeTrackerImageEyeBox();
            this.videoPlayerUI1 = new OpenIris.UI.VideoPlayerUI();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.eyeTrackerImageEyeBox2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.eyeTrackerImageEyeBox1, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(943, 374);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // eyeTrackerImageEyeBox2
            // 
            this.eyeTrackerImageEyeBox2.BackColor = System.Drawing.Color.Black;
            this.eyeTrackerImageEyeBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.eyeTrackerImageEyeBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eyeTrackerImageEyeBox2.Location = new System.Drawing.Point(474, 3);
            this.eyeTrackerImageEyeBox2.Name = "eyeTrackerImageEyeBox2";
            this.eyeTrackerImageEyeBox2.Size = new System.Drawing.Size(466, 368);
            this.eyeTrackerImageEyeBox2.TabIndex = 2;
            // 
            // eyeTrackerImageEyeBox1
            // 
            this.eyeTrackerImageEyeBox1.BackColor = System.Drawing.Color.Black;
            this.eyeTrackerImageEyeBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.eyeTrackerImageEyeBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eyeTrackerImageEyeBox1.Location = new System.Drawing.Point(3, 3);
            this.eyeTrackerImageEyeBox1.Name = "eyeTrackerImageEyeBox1";
            this.eyeTrackerImageEyeBox1.Size = new System.Drawing.Size(465, 368);
            this.eyeTrackerImageEyeBox1.TabIndex = 1;
            // 
            // videoPlayerUI1
            // 
            this.videoPlayerUI1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.videoPlayerUI1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.videoPlayerUI1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.videoPlayerUI1.Location = new System.Drawing.Point(96, 0);
            this.videoPlayerUI1.Name = "videoPlayerUI1";
            this.videoPlayerUI1.Size = new System.Drawing.Size(871, 37);
            this.videoPlayerUI1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel1.Controls.Add(this.buttonOpen);
            this.panel1.Controls.Add(this.videoPlayerUI1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 405);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(967, 37);
            this.panel1.TabIndex = 5;
            // 
            // buttonOpen
            // 
            this.buttonOpen.Location = new System.Drawing.Point(3, 3);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(87, 31);
            this.buttonOpen.TabIndex = 1;
            this.buttonOpen.Text = "Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.ButtonOpen_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label1.Location = new System.Drawing.Point(12, 386);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "RIGHT";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label2.Location = new System.Drawing.Point(463, 386);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "NOSE";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label3.Location = new System.Drawing.Point(918, 386);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "LEFT";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(967, 442);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private OpenIris.UI.VideoPlayerUI videoPlayerUI1;
        private OpenIris.UI.EyeTrackerImageEyeBox eyeTrackerImageEyeBox1;
        private OpenIris.UI.EyeTrackerImageEyeBox eyeTrackerImageEyeBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;

    }
}

