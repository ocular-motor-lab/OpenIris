namespace OpenIris.Calibration
{
    using OpenIris.UI;

    partial class EyeCalibrationNPointUI
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
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.imageBoxRightEye = new Emgu.CV.UI.ImageBox();
            this.imageBoxLeftEye = new Emgu.CV.UI.ImageBox();
            this.buttonLeftBack = new System.Windows.Forms.Button();
            this.imageBoxLeftEyeIm = new ImageEyeBox();
            this.imageBoxRightEyeIm = new ImageEyeBox();
            this.buttonRightBack = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEyeIm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEyeIm)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.button1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.buttonRightBack, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.imageBoxRightEye, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.imageBoxLeftEye, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonLeftBack, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(875, 529);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.imageBoxLeftEyeIm);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(440, 247);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(432, 238);
            this.panel2.TabIndex = 6;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.imageBoxRightEyeIm);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 247);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(431, 238);
            this.panel1.TabIndex = 6;
            // 
            // imageBoxRightEye
            // 
            this.imageBoxRightEye.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRightEye.Location = new System.Drawing.Point(3, 3);
            this.imageBoxRightEye.Name = "imageBoxRightEye";
            this.imageBoxRightEye.Size = new System.Drawing.Size(431, 238);
            this.imageBoxRightEye.TabIndex = 2;
            this.imageBoxRightEye.TabStop = false;
            this.imageBoxRightEye.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBoxLeftEye_MouseDown);
            // 
            // imageBoxLeftEye
            // 
            this.imageBoxLeftEye.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeftEye.Location = new System.Drawing.Point(440, 3);
            this.imageBoxLeftEye.Name = "imageBoxLeftEye";
            this.imageBoxLeftEye.Size = new System.Drawing.Size(432, 238);
            this.imageBoxLeftEye.TabIndex = 2;
            this.imageBoxLeftEye.TabStop = false;
            this.imageBoxLeftEye.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBoxLeftEye_MouseDown);
            // 
            // buttonLeftBack
            // 
            this.buttonLeftBack.Location = new System.Drawing.Point(440, 491);
            this.buttonLeftBack.Name = "buttonLeftBack";
            this.buttonLeftBack.Size = new System.Drawing.Size(75, 14);
            this.buttonLeftBack.TabIndex = 7;
            this.buttonLeftBack.Text = "Back";
            this.buttonLeftBack.UseVisualStyleBackColor = true;
            // 
            // imageBoxLeftEyeIm
            // 
            this.imageBoxLeftEyeIm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeftEyeIm.Location = new System.Drawing.Point(0, 0);
            this.imageBoxLeftEyeIm.Name = "imageBoxLeftEyeIm";
            this.imageBoxLeftEyeIm.Size = new System.Drawing.Size(432, 238);
            this.imageBoxLeftEyeIm.TabIndex = 3;
            this.imageBoxLeftEyeIm.TabStop = false;
            // 
            // imageBoxRightEyeIm
            // 
            this.imageBoxRightEyeIm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRightEyeIm.Location = new System.Drawing.Point(0, 0);
            this.imageBoxRightEyeIm.Name = "imageBoxRightEyeIm";
            this.imageBoxRightEyeIm.Size = new System.Drawing.Size(431, 238);
            this.imageBoxRightEyeIm.TabIndex = 3;
            this.imageBoxRightEyeIm.TabStop = false;
            // 
            // buttonRightBack
            // 
            this.buttonRightBack.Location = new System.Drawing.Point(3, 491);
            this.buttonRightBack.Name = "buttonRightBack";
            this.buttonRightBack.Size = new System.Drawing.Size(75, 14);
            this.buttonRightBack.TabIndex = 8;
            this.buttonRightBack.Text = "Back";
            this.buttonRightBack.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 511);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 15);
            this.button1.TabIndex = 9;
            this.button1.Text = "Back";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // EyeCalibrationNPoint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "EyeCalibrationNPoint";
            this.Size = new System.Drawing.Size(875, 529);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEyeIm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEyeIm)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private Emgu.CV.UI.ImageBox imageBoxRightEye;
        private Emgu.CV.UI.ImageBox imageBoxLeftEye;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonRightBack;
        private ImageEyeBox imageBoxLeftEyeIm;
        private ImageEyeBox imageBoxRightEyeIm;
        private System.Windows.Forms.Button buttonLeftBack;
    }
}
