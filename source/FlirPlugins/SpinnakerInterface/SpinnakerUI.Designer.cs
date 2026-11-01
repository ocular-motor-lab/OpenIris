namespace SpinnakerInterface
{
    partial class SpinnakerUI
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
            this.butStart = new System.Windows.Forms.Button();
            this.butStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // butStart
            // 
            this.butStart.Location = new System.Drawing.Point(188, 171);
            this.butStart.Name = "butStart";
            this.butStart.Size = new System.Drawing.Size(133, 94);
            this.butStart.TabIndex = 0;
            this.butStart.Text = "Start";
            this.butStart.UseVisualStyleBackColor = true;
            this.butStart.Click += new System.EventHandler(this.Button1_Click);
            // 
            // butStop
            // 
            this.butStop.Location = new System.Drawing.Point(338, 171);
            this.butStop.Name = "butStop";
            this.butStop.Size = new System.Drawing.Size(133, 94);
            this.butStop.TabIndex = 1;
            this.butStop.Text = "Stop";
            this.butStop.UseVisualStyleBackColor = true;
            this.butStop.Click += new System.EventHandler(this.butStop_Click);
            // 
            // SpinnakerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.butStop);
            this.Controls.Add(this.butStart);
            this.Name = "SpinnakerUI";
            this.Text = "SpinnakerUI";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Button butStop;
    }
}