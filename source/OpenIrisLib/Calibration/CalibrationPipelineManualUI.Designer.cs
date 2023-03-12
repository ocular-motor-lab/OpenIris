namespace OpenIris.Calibration
{
    partial class CalibrationPipelineManualUI
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
            this.buttonAccept.Location = new System.Drawing.Point(4, 762);
            this.buttonAccept.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonAccept.Name = "buttonAccept";
            this.buttonAccept.Size = new System.Drawing.Size(1286, 66);
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
            this.tableLayoutPanel1.Location = new System.Drawing.Point(4, 5);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1286, 672);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeV);
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeH);
            this.panel2.Controls.Add(this.sliderTextControlLeftEyeGlobeR);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(647, 341);
            this.panel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(635, 326);
            this.panel2.TabIndex = 6;
            // 
            // sliderTextControlLeftEyeGlobeV
            // 
            this.sliderTextControlLeftEyeGlobeV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeV.EnableValueChangedEvent = true;
            this.sliderTextControlLeftEyeGlobeV.Location = new System.Drawing.Point(4, 158);
            this.sliderTextControlLeftEyeGlobeV.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlLeftEyeGlobeV.Name = "sliderTextControlLeftEyeGlobeV";
            this.sliderTextControlLeftEyeGlobeV.Size = new System.Drawing.Size(627, 68);
            this.sliderTextControlLeftEyeGlobeV.TabIndex = 2;
            this.sliderTextControlLeftEyeGlobeV.Value = 0;
            // 
            // sliderTextControlLeftEyeGlobeH
            // 
            this.sliderTextControlLeftEyeGlobeH.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeH.EnableValueChangedEvent = true;
            this.sliderTextControlLeftEyeGlobeH.Location = new System.Drawing.Point(4, 82);
            this.sliderTextControlLeftEyeGlobeH.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlLeftEyeGlobeH.Name = "sliderTextControlLeftEyeGlobeH";
            this.sliderTextControlLeftEyeGlobeH.Size = new System.Drawing.Size(627, 68);
            this.sliderTextControlLeftEyeGlobeH.TabIndex = 1;
            this.sliderTextControlLeftEyeGlobeH.Value = 0;
            // 
            // sliderTextControlLeftEyeGlobeR
            // 
            this.sliderTextControlLeftEyeGlobeR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlLeftEyeGlobeR.EnableValueChangedEvent = true;
            this.sliderTextControlLeftEyeGlobeR.Location = new System.Drawing.Point(4, 5);
            this.sliderTextControlLeftEyeGlobeR.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlLeftEyeGlobeR.Name = "sliderTextControlLeftEyeGlobeR";
            this.sliderTextControlLeftEyeGlobeR.Size = new System.Drawing.Size(627, 68);
            this.sliderTextControlLeftEyeGlobeR.TabIndex = 3;
            this.sliderTextControlLeftEyeGlobeR.Value = 0;
            this.sliderTextControlLeftEyeGlobeR.Load += new System.EventHandler(this.sliderTextControlLeftEyeGlobeR_Load);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeH);
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeV);
            this.panel1.Controls.Add(this.sliderTextControlRightEyeGlobeR);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(4, 341);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(635, 326);
            this.panel1.TabIndex = 6;
            // 
            // sliderTextControlRightEyeGlobeH
            // 
            this.sliderTextControlRightEyeGlobeH.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeH.EnableValueChangedEvent = true;
            this.sliderTextControlRightEyeGlobeH.Location = new System.Drawing.Point(4, 82);
            this.sliderTextControlRightEyeGlobeH.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlRightEyeGlobeH.Name = "sliderTextControlRightEyeGlobeH";
            this.sliderTextControlRightEyeGlobeH.Size = new System.Drawing.Size(626, 68);
            this.sliderTextControlRightEyeGlobeH.TabIndex = 3;
            this.sliderTextControlRightEyeGlobeH.Value = 0;
            // 
            // sliderTextControlRightEyeGlobeV
            // 
            this.sliderTextControlRightEyeGlobeV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeV.EnableValueChangedEvent = true;
            this.sliderTextControlRightEyeGlobeV.Location = new System.Drawing.Point(4, 140);
            this.sliderTextControlRightEyeGlobeV.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlRightEyeGlobeV.Name = "sliderTextControlRightEyeGlobeV";
            this.sliderTextControlRightEyeGlobeV.Size = new System.Drawing.Size(626, 68);
            this.sliderTextControlRightEyeGlobeV.TabIndex = 3;
            this.sliderTextControlRightEyeGlobeV.Value = 0;
            // 
            // sliderTextControlRightEyeGlobeR
            // 
            this.sliderTextControlRightEyeGlobeR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTextControlRightEyeGlobeR.EnableValueChangedEvent = true;
            this.sliderTextControlRightEyeGlobeR.Location = new System.Drawing.Point(4, 5);
            this.sliderTextControlRightEyeGlobeR.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.sliderTextControlRightEyeGlobeR.Name = "sliderTextControlRightEyeGlobeR";
            this.sliderTextControlRightEyeGlobeR.Size = new System.Drawing.Size(630, 68);
            this.sliderTextControlRightEyeGlobeR.TabIndex = 3;
            this.sliderTextControlRightEyeGlobeR.Value = 0;
            // 
            // imageBoxRightEye
            // 
            this.imageBoxRightEye.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRightEye.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRightEye.Location = new System.Drawing.Point(4, 5);
            this.imageBoxRightEye.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.imageBoxRightEye.Name = "imageBoxRightEye";
            this.imageBoxRightEye.Size = new System.Drawing.Size(635, 326);
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
            this.imageBoxLeftEye.Location = new System.Drawing.Point(647, 5);
            this.imageBoxLeftEye.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.imageBoxLeftEye.Name = "imageBoxLeftEye";
            this.imageBoxLeftEye.Size = new System.Drawing.Size(635, 326);
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
            this.buttonAuto.Location = new System.Drawing.Point(4, 686);
            this.buttonAuto.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonAuto.Name = "buttonAuto";
            this.buttonAuto.Size = new System.Drawing.Size(1286, 66);
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
            this.butCancel.Location = new System.Drawing.Point(4, 832);
            this.butCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.butCancel.Name = "butCancel";
            this.butCancel.Size = new System.Drawing.Size(1286, 66);
            this.butCancel.TabIndex = 6;
            this.butCancel.Text = "Cancel";
            this.butCancel.UseVisualStyleBackColor = true;
            this.butCancel.Click += new System.EventHandler(this.butCancel_Click);
            // 
            // CalibrationPipelineManual
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.butCancel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonAuto);
            this.Controls.Add(this.buttonAccept);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "CalibrationPipelineManual";
            this.Size = new System.Drawing.Size(1294, 903);
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
