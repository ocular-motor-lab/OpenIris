namespace OpenIris.Calibration
{
    partial class CalibrationPipelineManual
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
            this.buttonAccept = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.sliderTextControlLeftEyeGlobeV = new OpenIris.UI.SliderTextControl();
            this.sliderTextControlLeftEyeGlobeH = new OpenIris.UI.SliderTextControl();
            this.sliderTextControlLeftEyeGlobeR = new OpenIris.UI.SliderTextControl();
            this.panel1 = new System.Windows.Forms.Panel();
            this.sliderTextControlRightEyeGlobeH = new OpenIris.UI.SliderTextControl();
            this.sliderTextControlRightEyeGlobeV = new OpenIris.UI.SliderTextControl();
            this.sliderTextControlRightEyeGlobeR = new OpenIris.UI.SliderTextControl();
            this.imageBoxRightEye = new Emgu.CV.UI.ImageBox();
            this.imageBoxLeftEye = new Emgu.CV.UI.ImageBox();
            this.buttonAuto = new System.Windows.Forms.Button();
            this.butCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEye)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonAccept
            // 
            this.buttonAccept.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAccept.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAccept.Location = new System.Drawing.Point(3, 495);
            this.buttonAccept.Name = "buttonAccept";
            this.buttonAccept.Size = new System.Drawing.Size(857, 43);
            this.buttonAccept.TabIndex = 4;
            this.buttonAccept.Text = "Set reference and accept";
            this.buttonAccept.UseVisualStyleBackColor = true;
            this.buttonAccept.Click += new System.EventHandler(this.buttonAccept_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.imageBoxRightEye, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.imageBoxLeftEye, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(857, 437);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeV);
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeH);
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeR);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(431, 221);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(423, 213);
            this.panel2.TabIndex = 6;
            // 
            // sliderTextControlLeftEyeGlobeV
            // 
            this.sliderTextControlLeftEyeGlobeV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeV.Location = new System.Drawing.Point(3, 103);
            this.sliderTextControlLeftEyeGlobeV.Name = "sliderTextControlLeftEyeGlobeV";
            this.sliderTextControlLeftEyeGlobeV.Size = new System.Drawing.Size(417, 44);
            this.sliderTextControlLeftEyeGlobeV.TabIndex = 2;
            // 
            // sliderTextControlLeftEyeGlobeH
            // 
            this.sliderTextControlLeftEyeGlobeH.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeH.Location = new System.Drawing.Point(3, 53);
            this.sliderTextControlLeftEyeGlobeH.Name = "sliderTextControlLeftEyeGlobeH";
            this.sliderTextControlLeftEyeGlobeH.Size = new System.Drawing.Size(417, 44);
            this.sliderTextControlLeftEyeGlobeH.TabIndex = 1;
            // 
            // sliderTextControlLeftEyeGlobeR
            // 
            this.sliderTextControlLeftEyeGlobeR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeR.Location = new System.Drawing.Point(3, 3);
            this.sliderTextControlLeftEyeGlobeR.Name = "sliderTextControlLeftEyeGlobeR";
            this.sliderTextControlLeftEyeGlobeR.Size = new System.Drawing.Size(417, 44);
            this.sliderTextControlLeftEyeGlobeR.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeH);
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeV);
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeR);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 221);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(422, 213);
            this.panel1.TabIndex = 6;
            // 
            // sliderTextControlRightEyeGlobeH
            // 
            this.sliderTextControlRightEyeGlobeH.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeH.Location = new System.Drawing.Point(3, 53);
            this.sliderTextControlRightEyeGlobeH.Name = "sliderTextControlRightEyeGlobeH";
            this.sliderTextControlRightEyeGlobeH.Size = new System.Drawing.Size(416, 44);
            this.sliderTextControlRightEyeGlobeH.TabIndex = 3;
            // 
            // sliderTextControlRightEyeGlobeV
            // 
            this.sliderTextControlRightEyeGlobeV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeV.Location = new System.Drawing.Point(3, 91);
            this.sliderTextControlRightEyeGlobeV.Name = "sliderTextControlRightEyeGlobeV";
            this.sliderTextControlRightEyeGlobeV.Size = new System.Drawing.Size(416, 44);
            this.sliderTextControlRightEyeGlobeV.TabIndex = 3;
            // 
            // sliderTextControlRightEyeGlobeR
            // 
            this.sliderTextControlRightEyeGlobeR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeR.Location = new System.Drawing.Point(3, 3);
            this.sliderTextControlRightEyeGlobeR.Name = "sliderTextControlRightEyeGlobeR";
            this.sliderTextControlRightEyeGlobeR.Size = new System.Drawing.Size(419, 44);
            this.sliderTextControlRightEyeGlobeR.TabIndex = 3;
            // 
            // imageBoxRightEye
            // 
            this.imageBoxRightEye.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRightEye.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRightEye.Location = new System.Drawing.Point(3, 3);
            this.imageBoxRightEye.Name = "imageBoxRightEye";
            this.imageBoxRightEye.Size = new System.Drawing.Size(422, 212);
            this.imageBoxRightEye.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxRightEye.TabIndex = 2;
            this.imageBoxRightEye.TabStop = false;
            this.imageBoxRightEye.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBoxRightEye_MouseDown);
            this.imageBoxRightEye.MouseMove += new System.Windows.Forms.MouseEventHandler(this.imageBoxRightEye_MouseMove);
            // 
            // imageBoxLeftEye
            // 
            this.imageBoxLeftEye.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeftEye.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxLeftEye.Location = new System.Drawing.Point(431, 3);
            this.imageBoxLeftEye.Name = "imageBoxLeftEye";
            this.imageBoxLeftEye.Size = new System.Drawing.Size(423, 212);
            this.imageBoxLeftEye.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxLeftEye.TabIndex = 2;
            this.imageBoxLeftEye.TabStop = false;
            this.imageBoxLeftEye.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBoxLeftEye_MouseDown);
            this.imageBoxLeftEye.MouseMove += new System.Windows.Forms.MouseEventHandler(this.imageBoxLeftEye_MouseMove);
            // 
            // buttonAuto
            // 
            this.buttonAuto.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAuto.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAuto.Location = new System.Drawing.Point(3, 446);
            this.buttonAuto.Name = "buttonAuto";
            this.buttonAuto.Size = new System.Drawing.Size(857, 43);
            this.buttonAuto.TabIndex = 4;
            this.buttonAuto.Text = "Center globe";
            this.buttonAuto.UseVisualStyleBackColor = true;
            this.buttonAuto.Click += new System.EventHandler(this.buttonAuto_Click);
            // 
            // butCancel
            // 
            this.butCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.butCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butCancel.Location = new System.Drawing.Point(3, 541);
            this.butCancel.Name = "butCancel";
            this.butCancel.Size = new System.Drawing.Size(857, 43);
            this.butCancel.TabIndex = 6;
            this.butCancel.Text = "Cancel";
            this.butCancel.UseVisualStyleBackColor = true;
            this.butCancel.Click += new System.EventHandler(this.butCancel_Click);
            // 
            // EyeCalibrationManualUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.butCancel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonAuto);
            this.Controls.Add(this.buttonAccept);
            this.Name = "EyeCalibrationManualUI";
            this.Size = new System.Drawing.Size(863, 587);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRightEye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeftEye)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private UI.SliderTextControl sliderTextControlLeftEyeGlobeH;
        private UI.SliderTextControl sliderTextControlLeftEyeGlobeV;
        private UI.SliderTextControl sliderTextControlLeftEyeGlobeR;
        private UI.SliderTextControl sliderTextControlRightEyeGlobeH;
        private UI.SliderTextControl sliderTextControlRightEyeGlobeV;
        private UI.SliderTextControl sliderTextControlRightEyeGlobeR;
        private System.Windows.Forms.Button buttonAccept;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private Emgu.CV.UI.ImageBox imageBoxRightEye;
        private Emgu.CV.UI.ImageBox imageBoxLeftEye;
        private System.Windows.Forms.Button buttonAuto;
        private System.Windows.Forms.Button butCancel;
    }
}
