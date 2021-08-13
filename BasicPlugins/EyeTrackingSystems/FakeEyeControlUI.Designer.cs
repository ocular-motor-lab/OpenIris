namespace OpenIris.EyeTrackingSystems
{
    partial class FakeEyeControlUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FakeEyeControlUI));
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.HarmsTorsion = new OpenIris.UI.SliderTextControl();
            this.HarmsVertical = new OpenIris.UI.SliderTextControl();
            this.HarmsHorizontal = new OpenIris.UI.SliderTextControl();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.ListingTorsion = new OpenIris.UI.SliderTextControl();
            this.ListingEccentricity = new OpenIris.UI.SliderTextControl();
            this.ListingAngle = new OpenIris.UI.SliderTextControl();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.HessTorsion = new OpenIris.UI.SliderTextControl();
            this.HessVertical = new OpenIris.UI.SliderTextControl();
            this.HessHorizontal = new OpenIris.UI.SliderTextControl();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.HelmholtzTorsion = new OpenIris.UI.SliderTextControl();
            this.HelmholtzVertical = new OpenIris.UI.SliderTextControl();
            this.HelmholtzHorizontal = new OpenIris.UI.SliderTextControl();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.FickTorsion = new OpenIris.UI.SliderTextControl();
            this.FickVertical = new OpenIris.UI.SliderTextControl();
            this.FickHorizontal = new OpenIris.UI.SliderTextControl();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.RotationVectorZ = new OpenIris.UI.SliderTextControl();
            this.RotationVectorY = new OpenIris.UI.SliderTextControl();
            this.RotationVectorX = new OpenIris.UI.SliderTextControl();
            this.panel2.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel1.Location = new System.Drawing.Point(12, 104);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(387, 300);
            this.panel1.TabIndex = 0;
            this.panel1.DoubleClick += new System.EventHandler(this.panel1_DoubleClick);
            this.panel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseMove);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.AutoScroll = true;
            this.panel2.Controls.Add(this.groupBox6);
            this.panel2.Controls.Add(this.groupBox7);
            this.panel2.Controls.Add(this.groupBox8);
            this.panel2.Controls.Add(this.groupBox9);
            this.panel2.Controls.Add(this.groupBox10);
            this.panel2.Location = new System.Drawing.Point(480, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(742, 841);
            this.panel2.TabIndex = 0;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.HarmsTorsion);
            this.groupBox6.Controls.Add(this.HarmsVertical);
            this.groupBox6.Controls.Add(this.HarmsHorizontal);
            this.groupBox6.Location = new System.Drawing.Point(12, 743);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(367, 173);
            this.groupBox6.TabIndex = 19;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Harms -- (Vertical and horizontal axes both eye-fixed) ";
            // 
            // HarmsTorsion
            // 
            this.HarmsTorsion.Location = new System.Drawing.Point(6, 119);
            this.HarmsTorsion.Name = "HarmsTorsion";
            this.HarmsTorsion.Size = new System.Drawing.Size(355, 44);
            this.HarmsTorsion.TabIndex = 7;
            // 
            // HarmsVertical
            // 
            this.HarmsVertical.Location = new System.Drawing.Point(6, 69);
            this.HarmsVertical.Name = "HarmsVertical";
            this.HarmsVertical.Size = new System.Drawing.Size(355, 44);
            this.HarmsVertical.TabIndex = 6;
            // 
            // HarmsHorizontal
            // 
            this.HarmsHorizontal.Location = new System.Drawing.Point(6, 19);
            this.HarmsHorizontal.Name = "HarmsHorizontal";
            this.HarmsHorizontal.Size = new System.Drawing.Size(355, 44);
            this.HarmsHorizontal.TabIndex = 5;
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.ListingTorsion);
            this.groupBox7.Controls.Add(this.ListingEccentricity);
            this.groupBox7.Controls.Add(this.ListingAngle);
            this.groupBox7.Location = new System.Drawing.Point(12, 19);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(367, 177);
            this.groupBox7.TabIndex = 17;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Listing --  (Based on polar geometry.)  ";
            // 
            // ListingTorsion
            // 
            this.ListingTorsion.Location = new System.Drawing.Point(6, 119);
            this.ListingTorsion.Name = "ListingTorsion";
            this.ListingTorsion.Size = new System.Drawing.Size(355, 44);
            this.ListingTorsion.TabIndex = 10;
            // 
            // ListingEccentricity
            // 
            this.ListingEccentricity.Location = new System.Drawing.Point(6, 69);
            this.ListingEccentricity.Name = "ListingEccentricity";
            this.ListingEccentricity.Size = new System.Drawing.Size(355, 44);
            this.ListingEccentricity.TabIndex = 9;
            // 
            // ListingAngle
            // 
            this.ListingAngle.Location = new System.Drawing.Point(6, 19);
            this.ListingAngle.Name = "ListingAngle";
            this.ListingAngle.Size = new System.Drawing.Size(355, 44);
            this.ListingAngle.TabIndex = 8;
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.HessTorsion);
            this.groupBox8.Controls.Add(this.HessVertical);
            this.groupBox8.Controls.Add(this.HessHorizontal);
            this.groupBox8.Location = new System.Drawing.Point(12, 564);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(367, 173);
            this.groupBox8.TabIndex = 15;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "Hess -- (Vertical and horizontal axes both head-fixed) ";
            // 
            // HessTorsion
            // 
            this.HessTorsion.Location = new System.Drawing.Point(6, 119);
            this.HessTorsion.Name = "HessTorsion";
            this.HessTorsion.Size = new System.Drawing.Size(355, 44);
            this.HessTorsion.TabIndex = 7;
            // 
            // HessVertical
            // 
            this.HessVertical.Location = new System.Drawing.Point(6, 69);
            this.HessVertical.Name = "HessVertical";
            this.HessVertical.Size = new System.Drawing.Size(355, 44);
            this.HessVertical.TabIndex = 6;
            // 
            // HessHorizontal
            // 
            this.HessHorizontal.Location = new System.Drawing.Point(6, 19);
            this.HessHorizontal.Name = "HessHorizontal";
            this.HessHorizontal.Size = new System.Drawing.Size(355, 44);
            this.HessHorizontal.TabIndex = 5;
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.HelmholtzTorsion);
            this.groupBox9.Controls.Add(this.HelmholtzVertical);
            this.groupBox9.Controls.Add(this.HelmholtzHorizontal);
            this.groupBox9.Location = new System.Drawing.Point(12, 385);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(367, 173);
            this.groupBox9.TabIndex = 16;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Helmholtz -- (Eye-fixed vertical axis/ Head-fixed horizontal axis) ";
            // 
            // HelmholtzTorsion
            // 
            this.HelmholtzTorsion.Location = new System.Drawing.Point(6, 120);
            this.HelmholtzTorsion.Name = "HelmholtzTorsion";
            this.HelmholtzTorsion.Size = new System.Drawing.Size(355, 44);
            this.HelmholtzTorsion.TabIndex = 4;
            // 
            // HelmholtzVertical
            // 
            this.HelmholtzVertical.Location = new System.Drawing.Point(6, 70);
            this.HelmholtzVertical.Name = "HelmholtzVertical";
            this.HelmholtzVertical.Size = new System.Drawing.Size(355, 44);
            this.HelmholtzVertical.TabIndex = 3;
            // 
            // HelmholtzHorizontal
            // 
            this.HelmholtzHorizontal.Location = new System.Drawing.Point(6, 20);
            this.HelmholtzHorizontal.Name = "HelmholtzHorizontal";
            this.HelmholtzHorizontal.Size = new System.Drawing.Size(355, 44);
            this.HelmholtzHorizontal.TabIndex = 2;
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.FickTorsion);
            this.groupBox10.Controls.Add(this.FickVertical);
            this.groupBox10.Controls.Add(this.FickHorizontal);
            this.groupBox10.Location = new System.Drawing.Point(12, 202);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(367, 177);
            this.groupBox10.TabIndex = 10;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "Fick --  (Head-fixed vertical axis/ Eye-fixed horizontal axis) ";
            // 
            // FickTorsion
            // 
            this.FickTorsion.Location = new System.Drawing.Point(6, 119);
            this.FickTorsion.Name = "FickTorsion";
            this.FickTorsion.Size = new System.Drawing.Size(355, 44);
            this.FickTorsion.TabIndex = 1;
            // 
            // FickVertical
            // 
            this.FickVertical.Location = new System.Drawing.Point(6, 69);
            this.FickVertical.Name = "FickVertical";
            this.FickVertical.Size = new System.Drawing.Size(355, 44);
            this.FickVertical.TabIndex = 1;
            // 
            // FickHorizontal
            // 
            this.FickHorizontal.Location = new System.Drawing.Point(6, 19);
            this.FickHorizontal.Name = "FickHorizontal";
            this.FickHorizontal.Size = new System.Drawing.Size(355, 44);
            this.FickHorizontal.TabIndex = 0;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(559, 943);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(327, 13);
            this.linkLabel1.TabIndex = 3;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "http://www.opt.indiana.edu/v665/CD/CD_Version/CH3/CH3.HTM";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.RotationVectorZ);
            this.groupBox1.Controls.Add(this.RotationVectorY);
            this.groupBox1.Controls.Add(this.RotationVectorX);
            this.groupBox1.Location = new System.Drawing.Point(21, 462);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(367, 177);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Rotation vector";
            // 
            // RotationVectorZ
            // 
            this.RotationVectorZ.Location = new System.Drawing.Point(6, 119);
            this.RotationVectorZ.Name = "RotationVectorZ";
            this.RotationVectorZ.Size = new System.Drawing.Size(355, 44);
            this.RotationVectorZ.TabIndex = 10;
            // 
            // RotationVectorY
            // 
            this.RotationVectorY.Location = new System.Drawing.Point(6, 69);
            this.RotationVectorY.Name = "RotationVectorY";
            this.RotationVectorY.Size = new System.Drawing.Size(355, 44);
            this.RotationVectorY.TabIndex = 9;
            // 
            // RotationVectorX
            // 
            this.RotationVectorX.Location = new System.Drawing.Point(6, 19);
            this.RotationVectorX.Name = "RotationVectorX";
            this.RotationVectorX.Size = new System.Drawing.Size(355, 44);
            this.RotationVectorX.TabIndex = 8;
            // 
            // FakeEyeControlUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1234, 874);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "FakeEyeControlUI";
            this.Text = "FakeEyeControlUI";
            this.Load += new System.EventHandler(this.FakeEyeControlUI_Load);
            this.panel2.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox8.ResumeLayout(false);
            this.groupBox9.ResumeLayout(false);
            this.groupBox10.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox6;
        private UI.SliderTextControl HarmsTorsion;
        private UI.SliderTextControl HarmsVertical;
        private UI.SliderTextControl HarmsHorizontal;
        private System.Windows.Forms.GroupBox groupBox7;
        private UI.SliderTextControl ListingTorsion;
        private UI.SliderTextControl ListingEccentricity;
        private UI.SliderTextControl ListingAngle;
        private System.Windows.Forms.GroupBox groupBox8;
        private UI.SliderTextControl HessTorsion;
        private UI.SliderTextControl HessVertical;
        private UI.SliderTextControl HessHorizontal;
        private System.Windows.Forms.GroupBox groupBox9;
        private UI.SliderTextControl HelmholtzTorsion;
        private UI.SliderTextControl HelmholtzVertical;
        private UI.SliderTextControl HelmholtzHorizontal;
        private System.Windows.Forms.GroupBox groupBox10;
        private UI.SliderTextControl FickTorsion;
        private UI.SliderTextControl FickVertical;
        private UI.SliderTextControl FickHorizontal;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private UI.SliderTextControl RotationVectorZ;
        private UI.SliderTextControl RotationVectorY;
        private UI.SliderTextControl RotationVectorX;
    }
}