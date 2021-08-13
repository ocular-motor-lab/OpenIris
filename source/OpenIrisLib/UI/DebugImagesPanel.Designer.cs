namespace OpenIris.UI
{
    partial class DebugImagesPanel
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.imageBoxLeft4 = new Emgu.CV.UI.ImageBox();
            this.imageBoxLeft2 = new Emgu.CV.UI.ImageBox();
            this.imageBoxRight1 = new Emgu.CV.UI.ImageBox();
            this.imageBoxRight4 = new Emgu.CV.UI.ImageBox();
            this.imageBoxRight2 = new Emgu.CV.UI.ImageBox();
            this.imageBoxRight3 = new Emgu.CV.UI.ImageBox();
            this.imageBoxLeft1 = new Emgu.CV.UI.ImageBox();
            this.imageBoxLeft3 = new Emgu.CV.UI.ImageBox();
            this.chartBottom = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartBottom)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.listBox1);
            this.splitContainer2.Panel1.Controls.Add(this.tableLayoutPanel4);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.chartBottom);
            this.splitContainer2.Size = new System.Drawing.Size(1115, 718);
            this.splitContainer2.SplitterDistance = 475;
            this.splitContainer2.TabIndex = 10;
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(5, 3);
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.listBox1.Size = new System.Drawing.Size(120, 355);
            this.listBox1.TabIndex = 9;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel4.ColumnCount = 4;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel4.Controls.Add(this.imageBoxLeft4, 3, 1);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxLeft2, 3, 0);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxRight1, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxRight4, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxRight2, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxRight3, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxLeft1, 2, 0);
            this.tableLayoutPanel4.Controls.Add(this.imageBoxLeft3, 2, 1);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(131, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(979, 470);
            this.tableLayoutPanel4.TabIndex = 8;
            // 
            // imageBoxLeft4
            // 
            this.imageBoxLeft4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeft4.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxLeft4.Location = new System.Drawing.Point(735, 238);
            this.imageBoxLeft4.Name = "imageBoxLeft4";
            this.imageBoxLeft4.Size = new System.Drawing.Size(241, 229);
            this.imageBoxLeft4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxLeft4.TabIndex = 5;
            this.imageBoxLeft4.TabStop = false;
            // 
            // imageBoxLeft2
            // 
            this.imageBoxLeft2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeft2.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxLeft2.Location = new System.Drawing.Point(735, 3);
            this.imageBoxLeft2.Name = "imageBoxLeft2";
            this.imageBoxLeft2.Size = new System.Drawing.Size(241, 229);
            this.imageBoxLeft2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxLeft2.TabIndex = 6;
            this.imageBoxLeft2.TabStop = false;
            // 
            // imageBoxRight1
            // 
            this.imageBoxRight1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRight1.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRight1.Location = new System.Drawing.Point(3, 3);
            this.imageBoxRight1.Name = "imageBoxRight1";
            this.imageBoxRight1.Size = new System.Drawing.Size(238, 229);
            this.imageBoxRight1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxRight1.TabIndex = 5;
            this.imageBoxRight1.TabStop = false;
            // 
            // imageBoxRight4
            // 
            this.imageBoxRight4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRight4.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRight4.Location = new System.Drawing.Point(247, 238);
            this.imageBoxRight4.Name = "imageBoxRight4";
            this.imageBoxRight4.Size = new System.Drawing.Size(238, 229);
            this.imageBoxRight4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxRight4.TabIndex = 6;
            this.imageBoxRight4.TabStop = false;
            // 
            // imageBoxRight2
            // 
            this.imageBoxRight2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRight2.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRight2.Location = new System.Drawing.Point(247, 3);
            this.imageBoxRight2.Name = "imageBoxRight2";
            this.imageBoxRight2.Size = new System.Drawing.Size(238, 229);
            this.imageBoxRight2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxRight2.TabIndex = 4;
            this.imageBoxRight2.TabStop = false;
            // 
            // imageBoxRight3
            // 
            this.imageBoxRight3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxRight3.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxRight3.Location = new System.Drawing.Point(3, 238);
            this.imageBoxRight3.Name = "imageBoxRight3";
            this.imageBoxRight3.Size = new System.Drawing.Size(238, 229);
            this.imageBoxRight3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxRight3.TabIndex = 7;
            this.imageBoxRight3.TabStop = false;
            // 
            // imageBoxLeft1
            // 
            this.imageBoxLeft1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeft1.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxLeft1.Location = new System.Drawing.Point(491, 3);
            this.imageBoxLeft1.Name = "imageBoxLeft1";
            this.imageBoxLeft1.Size = new System.Drawing.Size(238, 229);
            this.imageBoxLeft1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxLeft1.TabIndex = 8;
            this.imageBoxLeft1.TabStop = false;
            // 
            // imageBoxLeft3
            // 
            this.imageBoxLeft3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxLeft3.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.RightClickMenu;
            this.imageBoxLeft3.Location = new System.Drawing.Point(491, 238);
            this.imageBoxLeft3.Name = "imageBoxLeft3";
            this.imageBoxLeft3.Size = new System.Drawing.Size(238, 229);
            this.imageBoxLeft3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBoxLeft3.TabIndex = 7;
            this.imageBoxLeft3.TabStop = false;
            // 
            // chartBottom
            // 
            this.chartBottom.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            chartArea1.Name = "ChartArea1";
            this.chartBottom.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chartBottom.Legends.Add(legend1);
            this.chartBottom.Location = new System.Drawing.Point(0, 0);
            this.chartBottom.Name = "chartBottom";
            this.chartBottom.Size = new System.Drawing.Size(1115, 237);
            this.chartBottom.TabIndex = 8;
            this.chartBottom.Text = "chartCrossCorrelation";
            // 
            // DebugImagesPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer2);
            this.Name = "DebugImagesPanel";
            this.Size = new System.Drawing.Size(1115, 718);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxRight3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxLeft3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartBottom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private Emgu.CV.UI.ImageBox imageBoxLeft4;
        private Emgu.CV.UI.ImageBox imageBoxLeft2;
        private Emgu.CV.UI.ImageBox imageBoxRight1;
        private Emgu.CV.UI.ImageBox imageBoxRight4;
        private Emgu.CV.UI.ImageBox imageBoxRight2;
        private Emgu.CV.UI.ImageBox imageBoxRight3;
        private Emgu.CV.UI.ImageBox imageBoxLeft1;
        private Emgu.CV.UI.ImageBox imageBoxLeft3;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartBottom;
    }
}
