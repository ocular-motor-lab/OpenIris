namespace OpenIris.ImageGrabbing
{
    partial class ImageGrabberUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageGrabberUI));
            this.groupBoxCameraPosition = new System.Windows.Forms.GroupBox();
            this.buttonCenterEyes = new System.Windows.Forms.Button();
            this.buttonMoveLeftEyeDown = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList();
            this.buttonMoveRightEyeRight = new System.Windows.Forms.Button();
            this.buttonMoveLeftEyeUp = new System.Windows.Forms.Button();
            this.buttonMoveLeftEyeRight = new System.Windows.Forms.Button();
            this.buttonMoveRightEyeUp = new System.Windows.Forms.Button();
            this.buttonMoveLeftEyeLeft = new System.Windows.Forms.Button();
            this.buttonMoveRightEyeDown = new System.Windows.Forms.Button();
            this.buttonMoveRightEyeLeft = new System.Windows.Forms.Button();
            this.groupBoxCameraPosition.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxCameraPosition
            // 
            this.groupBoxCameraPosition.Controls.Add(this.buttonCenterEyes);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveLeftEyeDown);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveRightEyeRight);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveLeftEyeUp);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveLeftEyeRight);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveRightEyeUp);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveLeftEyeLeft);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveRightEyeDown);
            this.groupBoxCameraPosition.Controls.Add(this.buttonMoveRightEyeLeft);
            this.groupBoxCameraPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxCameraPosition.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBoxCameraPosition.Location = new System.Drawing.Point(0, 0);
            this.groupBoxCameraPosition.Name = "groupBoxCameraPosition";
            this.groupBoxCameraPosition.Size = new System.Drawing.Size(222, 119);
            this.groupBoxCameraPosition.TabIndex = 31;
            this.groupBoxCameraPosition.TabStop = false;
            this.groupBoxCameraPosition.Text = "Camera position";
            // 
            // buttonCenterEyes
            // 
            this.buttonCenterEyes.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCenterEyes.Location = new System.Drawing.Point(69, 22);
            this.buttonCenterEyes.Name = "buttonCenterEyes";
            this.buttonCenterEyes.Size = new System.Drawing.Size(83, 86);
            this.buttonCenterEyes.TabIndex = 4;
            this.buttonCenterEyes.Text = "Center eyes";
            this.buttonCenterEyes.UseVisualStyleBackColor = true;
            // 
            // buttonMoveLeftEyeDown
            // 
            this.buttonMoveLeftEyeDown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveLeftEyeDown.ImageIndex = 0;
            this.buttonMoveLeftEyeDown.ImageList = this.imageList1;
            this.buttonMoveLeftEyeDown.Location = new System.Drawing.Point(158, 84);
            this.buttonMoveLeftEyeDown.Name = "buttonMoveLeftEyeDown";
            this.buttonMoveLeftEyeDown.Size = new System.Drawing.Size(54, 24);
            this.buttonMoveLeftEyeDown.TabIndex = 29;
            this.buttonMoveLeftEyeDown.UseVisualStyleBackColor = true;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "down.ico");
            this.imageList1.Images.SetKeyName(1, "left.ico");
            this.imageList1.Images.SetKeyName(2, "right.ico");
            this.imageList1.Images.SetKeyName(3, "up.ico");
            // 
            // buttonMoveRightEyeRight
            // 
            this.buttonMoveRightEyeRight.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveRightEyeRight.ImageIndex = 2;
            this.buttonMoveRightEyeRight.ImageList = this.imageList1;
            this.buttonMoveRightEyeRight.Location = new System.Drawing.Point(39, 52);
            this.buttonMoveRightEyeRight.Name = "buttonMoveRightEyeRight";
            this.buttonMoveRightEyeRight.Size = new System.Drawing.Size(24, 24);
            this.buttonMoveRightEyeRight.TabIndex = 27;
            this.buttonMoveRightEyeRight.UseVisualStyleBackColor = true;
            // 
            // buttonMoveLeftEyeUp
            // 
            this.buttonMoveLeftEyeUp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveLeftEyeUp.ImageIndex = 3;
            this.buttonMoveLeftEyeUp.ImageList = this.imageList1;
            this.buttonMoveLeftEyeUp.Location = new System.Drawing.Point(158, 22);
            this.buttonMoveLeftEyeUp.Name = "buttonMoveLeftEyeUp";
            this.buttonMoveLeftEyeUp.Size = new System.Drawing.Size(54, 24);
            this.buttonMoveLeftEyeUp.TabIndex = 28;
            this.buttonMoveLeftEyeUp.UseVisualStyleBackColor = true;
            // 
            // buttonMoveLeftEyeRight
            // 
            this.buttonMoveLeftEyeRight.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveLeftEyeRight.ImageIndex = 2;
            this.buttonMoveLeftEyeRight.ImageList = this.imageList1;
            this.buttonMoveLeftEyeRight.Location = new System.Drawing.Point(188, 52);
            this.buttonMoveLeftEyeRight.Name = "buttonMoveLeftEyeRight";
            this.buttonMoveLeftEyeRight.Size = new System.Drawing.Size(24, 24);
            this.buttonMoveLeftEyeRight.TabIndex = 27;
            this.buttonMoveLeftEyeRight.UseVisualStyleBackColor = true;
            // 
            // buttonMoveRightEyeUp
            // 
            this.buttonMoveRightEyeUp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveRightEyeUp.ImageIndex = 3;
            this.buttonMoveRightEyeUp.ImageList = this.imageList1;
            this.buttonMoveRightEyeUp.Location = new System.Drawing.Point(9, 21);
            this.buttonMoveRightEyeUp.Name = "buttonMoveRightEyeUp";
            this.buttonMoveRightEyeUp.Size = new System.Drawing.Size(54, 24);
            this.buttonMoveRightEyeUp.TabIndex = 25;
            this.buttonMoveRightEyeUp.UseVisualStyleBackColor = true;
            // 
            // buttonMoveLeftEyeLeft
            // 
            this.buttonMoveLeftEyeLeft.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveLeftEyeLeft.ImageIndex = 1;
            this.buttonMoveLeftEyeLeft.ImageList = this.imageList1;
            this.buttonMoveLeftEyeLeft.Location = new System.Drawing.Point(158, 52);
            this.buttonMoveLeftEyeLeft.Name = "buttonMoveLeftEyeLeft";
            this.buttonMoveLeftEyeLeft.Size = new System.Drawing.Size(24, 24);
            this.buttonMoveLeftEyeLeft.TabIndex = 27;
            this.buttonMoveLeftEyeLeft.UseVisualStyleBackColor = true;
            // 
            // buttonMoveRightEyeDown
            // 
            this.buttonMoveRightEyeDown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveRightEyeDown.ImageIndex = 0;
            this.buttonMoveRightEyeDown.ImageList = this.imageList1;
            this.buttonMoveRightEyeDown.Location = new System.Drawing.Point(9, 83);
            this.buttonMoveRightEyeDown.Name = "buttonMoveRightEyeDown";
            this.buttonMoveRightEyeDown.Size = new System.Drawing.Size(54, 24);
            this.buttonMoveRightEyeDown.TabIndex = 26;
            this.buttonMoveRightEyeDown.UseVisualStyleBackColor = true;
            // 
            // buttonMoveRightEyeLeft
            // 
            this.buttonMoveRightEyeLeft.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonMoveRightEyeLeft.ImageIndex = 1;
            this.buttonMoveRightEyeLeft.ImageList = this.imageList1;
            this.buttonMoveRightEyeLeft.Location = new System.Drawing.Point(9, 52);
            this.buttonMoveRightEyeLeft.Name = "buttonMoveRightEyeLeft";
            this.buttonMoveRightEyeLeft.Size = new System.Drawing.Size(24, 24);
            this.buttonMoveRightEyeLeft.TabIndex = 27;
            this.buttonMoveRightEyeLeft.UseVisualStyleBackColor = true;
            // 
            // ImageGrabberUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxCameraPosition);
            this.Name = "ImageGrabberUI";
            this.Size = new System.Drawing.Size(222, 119);
            this.groupBoxCameraPosition.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxCameraPosition;
        private System.Windows.Forms.Button buttonCenterEyes;
        private System.Windows.Forms.Button buttonMoveLeftEyeDown;
        private System.Windows.Forms.Button buttonMoveRightEyeRight;
        private System.Windows.Forms.Button buttonMoveLeftEyeUp;
        private System.Windows.Forms.Button buttonMoveLeftEyeRight;
        private System.Windows.Forms.Button buttonMoveRightEyeUp;
        private System.Windows.Forms.Button buttonMoveLeftEyeLeft;
        private System.Windows.Forms.Button buttonMoveRightEyeDown;
        private System.Windows.Forms.Button buttonMoveRightEyeLeft;
        private System.Windows.Forms.ImageList imageList1;
    }
}
