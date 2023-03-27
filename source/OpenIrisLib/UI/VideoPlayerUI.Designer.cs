namespace OpenIris.UI
{
    partial class VideoPlayerUI
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
            this.labelFrameNumber = new System.Windows.Forms.Label();
            this.hScrollBarPlayBack = new System.Windows.Forms.HScrollBar();
            this.buttonVideoPauseResume = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelFrameNumber
            // 
            this.labelFrameNumber.AutoSize = true;
            this.labelFrameNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFrameNumber.Location = new System.Drawing.Point(104, 0);
            this.labelFrameNumber.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelFrameNumber.Name = "labelFrameNumber";
            this.labelFrameNumber.Size = new System.Drawing.Size(92, 54);
            this.labelFrameNumber.TabIndex = 3;
            this.labelFrameNumber.Text = "0/0";
            this.labelFrameNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelFrameNumber.Click += new System.EventHandler(this.LabelFrameNumber_Click);
            // 
            // hScrollBarPlayBack
            // 
            this.hScrollBarPlayBack.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBarPlayBack.Location = new System.Drawing.Point(200, 0);
            this.hScrollBarPlayBack.Name = "hScrollBarPlayBack";
            this.hScrollBarPlayBack.Size = new System.Drawing.Size(269, 54);
            this.hScrollBarPlayBack.TabIndex = 2;
            this.hScrollBarPlayBack.Scroll += new System.Windows.Forms.ScrollEventHandler(this.HScrollBarPlayBack_Scroll);
            // 
            // buttonVideoPauseResume
            // 
            this.buttonVideoPauseResume.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonVideoPauseResume.Location = new System.Drawing.Point(4, 4);
            this.buttonVideoPauseResume.Margin = new System.Windows.Forms.Padding(4);
            this.buttonVideoPauseResume.Name = "buttonVideoPauseResume";
            this.buttonVideoPauseResume.Size = new System.Drawing.Size(92, 46);
            this.buttonVideoPauseResume.TabIndex = 0;
            this.buttonVideoPauseResume.Text = "Pause";
            this.buttonVideoPauseResume.UseVisualStyleBackColor = true;
            this.buttonVideoPauseResume.Click += new System.EventHandler(this.ButtonVideoPauseResume_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.hScrollBarPlayBack, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelFrameNumber, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonVideoPauseResume, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(469, 54);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // VideoPlayerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "VideoPlayerUI";
            this.Size = new System.Drawing.Size(469, 54);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelFrameNumber;
        private System.Windows.Forms.HScrollBar hScrollBarPlayBack;
        private System.Windows.Forms.Button buttonVideoPauseResume;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
