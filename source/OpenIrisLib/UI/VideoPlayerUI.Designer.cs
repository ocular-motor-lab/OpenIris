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
            this.SuspendLayout();
            // 
            // labelFrameNumber
            // 
            this.labelFrameNumber.AutoSize = true;
            this.labelFrameNumber.Location = new System.Drawing.Point(80, 8);
            this.labelFrameNumber.Name = "labelFrameNumber";
            this.labelFrameNumber.Size = new System.Drawing.Size(24, 13);
            this.labelFrameNumber.TabIndex = 3;
            this.labelFrameNumber.Text = "0/0";
            this.labelFrameNumber.Click += new System.EventHandler(this.LabelFrameNumber_Click);
            // 
            // hScrollBarPlayBack
            // 
            this.hScrollBarPlayBack.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBarPlayBack.Location = new System.Drawing.Point(165, 0);
            this.hScrollBarPlayBack.Name = "hScrollBarPlayBack";
            this.hScrollBarPlayBack.Size = new System.Drawing.Size(506, 28);
            this.hScrollBarPlayBack.TabIndex = 2;
            this.hScrollBarPlayBack.Scroll += new System.Windows.Forms.ScrollEventHandler(this.HScrollBarPlayBack_Scroll);
            // 
            // buttonVideoPauseResume
            // 
            this.buttonVideoPauseResume.Location = new System.Drawing.Point(2, 3);
            this.buttonVideoPauseResume.Name = "buttonVideoPauseResume";
            this.buttonVideoPauseResume.Size = new System.Drawing.Size(72, 22);
            this.buttonVideoPauseResume.TabIndex = 0;
            this.buttonVideoPauseResume.Text = "Pause";
            this.buttonVideoPauseResume.UseVisualStyleBackColor = true;
            this.buttonVideoPauseResume.Click += new System.EventHandler(this.ButtonVideoPauseResume_Click);
            // 
            // VideoPlayerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonVideoPauseResume);
            this.Controls.Add(this.labelFrameNumber);
            this.Controls.Add(this.hScrollBarPlayBack);
            this.Name = "VideoPlayerUI";
            this.Size = new System.Drawing.Size(671, 28);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelFrameNumber;
        private System.Windows.Forms.HScrollBar hScrollBarPlayBack;
        private System.Windows.Forms.Button buttonVideoPauseResume;
    }
}
