namespace SubjectiveTracker
{
    partial class SubjectiveTracker
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadVideoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadDataFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.comboBoxLeftClick = new System.Windows.Forms.ComboBox();
            this.comboBoxRightClick = new System.Windows.Forms.ComboBox();
            this.listBoxMarkers = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBoxAutoAdvance = new System.Windows.Forms.GroupBox();
            this.radioButtonNoAutoAdvance = new System.Windows.Forms.RadioButton();
            this.radioButtonCtrlRightClick = new System.Windows.Forms.RadioButton();
            this.radioButtonRightClick = new System.Windows.Forms.RadioButton();
            this.radioButtonCtrlLeftClick = new System.Windows.Forms.RadioButton();
            this.radioButtonLeftClick = new System.Windows.Forms.RadioButton();
            this.comboBoxCtrlLeftClick = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxCtrlRightClick = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.textBoxNewMarkerName = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.imageBox1 = new Emgu.CV.UI.ImageBox();
            this.imageBox = new Emgu.CV.UI.ImageBox();
            this.numericUpDownSkipFrames = new System.Windows.Forms.NumericUpDown();
            this.videoPlayerUI1 = new OpenIris.UI.VideoPlayerUI();
            this.label1 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.exportDataToCsvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.groupBoxAutoAdvance.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSkipFrames)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1295, 25);
            this.menuStrip1.TabIndex = 6;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadVideoToolStripMenuItem,
            this.loadDataFileToolStripMenuItem,
            this.exportDataToCsvToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(39, 21);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadVideoToolStripMenuItem
            // 
            this.loadVideoToolStripMenuItem.Name = "loadVideoToolStripMenuItem";
            this.loadVideoToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.loadVideoToolStripMenuItem.Text = "Load video ...";
            this.loadVideoToolStripMenuItem.Click += new System.EventHandler(this.loadVideoToolStripMenuItem_Click);
            // 
            // loadDataFileToolStripMenuItem
            // 
            this.loadDataFileToolStripMenuItem.Name = "loadDataFileToolStripMenuItem";
            this.loadDataFileToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.loadDataFileToolStripMenuItem.Text = "Load data file ...";
            this.loadDataFileToolStripMenuItem.Click += new System.EventHandler(this.loadDataFileToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.nextToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(42, 21);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.undoToolStripMenuItem.Text = "Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // nextToolStripMenuItem
            // 
            this.nextToolStripMenuItem.Name = "nextToolStripMenuItem";
            this.nextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Space)));
            this.nextToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.nextToolStripMenuItem.Text = "Next";
            this.nextToolStripMenuItem.Click += new System.EventHandler(this.nextToolStripMenuItem_Click);
            // 
            // comboBoxLeftClick
            // 
            this.comboBoxLeftClick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLeftClick.FormattingEnabled = true;
            this.comboBoxLeftClick.Location = new System.Drawing.Point(112, 19);
            this.comboBoxLeftClick.Name = "comboBoxLeftClick";
            this.comboBoxLeftClick.Size = new System.Drawing.Size(150, 21);
            this.comboBoxLeftClick.TabIndex = 8;
            this.comboBoxLeftClick.SelectedIndexChanged += new System.EventHandler(this.comboBoxLeftClick_SelectedIndexChanged);
            // 
            // comboBoxRightClick
            // 
            this.comboBoxRightClick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRightClick.FormattingEnabled = true;
            this.comboBoxRightClick.Location = new System.Drawing.Point(112, 49);
            this.comboBoxRightClick.Name = "comboBoxRightClick";
            this.comboBoxRightClick.Size = new System.Drawing.Size(150, 21);
            this.comboBoxRightClick.TabIndex = 8;
            this.comboBoxRightClick.SelectedIndexChanged += new System.EventHandler(this.comboBoxRightClick_SelectedIndexChanged);
            // 
            // listBoxMarkers
            // 
            this.listBoxMarkers.FormattingEnabled = true;
            this.listBoxMarkers.Location = new System.Drawing.Point(6, 45);
            this.listBoxMarkers.Name = "listBoxMarkers";
            this.listBoxMarkers.Size = new System.Drawing.Size(262, 186);
            this.listBoxMarkers.TabIndex = 9;
            this.listBoxMarkers.SelectedIndexChanged += new System.EventHandler(this.listBoxMarkers_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(50, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "left click";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(43, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "right click";
            // 
            // groupBoxAutoAdvance
            // 
            this.groupBoxAutoAdvance.Controls.Add(this.radioButtonNoAutoAdvance);
            this.groupBoxAutoAdvance.Controls.Add(this.radioButtonCtrlRightClick);
            this.groupBoxAutoAdvance.Controls.Add(this.radioButtonRightClick);
            this.groupBoxAutoAdvance.Controls.Add(this.radioButtonCtrlLeftClick);
            this.groupBoxAutoAdvance.Controls.Add(this.radioButtonLeftClick);
            this.groupBoxAutoAdvance.Location = new System.Drawing.Point(5, 405);
            this.groupBoxAutoAdvance.Name = "groupBoxAutoAdvance";
            this.groupBoxAutoAdvance.Size = new System.Drawing.Size(288, 148);
            this.groupBoxAutoAdvance.TabIndex = 11;
            this.groupBoxAutoAdvance.TabStop = false;
            this.groupBoxAutoAdvance.Text = "Auto advance";
            // 
            // radioButtonNoAutoAdvance
            // 
            this.radioButtonNoAutoAdvance.AutoSize = true;
            this.radioButtonNoAutoAdvance.Checked = true;
            this.radioButtonNoAutoAdvance.Location = new System.Drawing.Point(8, 111);
            this.radioButtonNoAutoAdvance.Name = "radioButtonNoAutoAdvance";
            this.radioButtonNoAutoAdvance.Size = new System.Drawing.Size(108, 17);
            this.radioButtonNoAutoAdvance.TabIndex = 0;
            this.radioButtonNoAutoAdvance.TabStop = true;
            this.radioButtonNoAutoAdvance.Text = "No auto advance";
            this.radioButtonNoAutoAdvance.UseVisualStyleBackColor = true;
            this.radioButtonNoAutoAdvance.CheckedChanged += new System.EventHandler(this.radioButtonAutoadvance_CheckedChanged);
            // 
            // radioButtonCtrlRightClick
            // 
            this.radioButtonCtrlRightClick.AutoSize = true;
            this.radioButtonCtrlRightClick.Location = new System.Drawing.Point(8, 88);
            this.radioButtonCtrlRightClick.Name = "radioButtonCtrlRightClick";
            this.radioButtonCtrlRightClick.Size = new System.Drawing.Size(111, 17);
            this.radioButtonCtrlRightClick.TabIndex = 0;
            this.radioButtonCtrlRightClick.Text = "On ctr + right click";
            this.radioButtonCtrlRightClick.UseVisualStyleBackColor = true;
            this.radioButtonCtrlRightClick.CheckedChanged += new System.EventHandler(this.radioButtonAutoadvance_CheckedChanged);
            // 
            // radioButtonRightClick
            // 
            this.radioButtonRightClick.AutoSize = true;
            this.radioButtonRightClick.Location = new System.Drawing.Point(7, 42);
            this.radioButtonRightClick.Name = "radioButtonRightClick";
            this.radioButtonRightClick.Size = new System.Drawing.Size(87, 17);
            this.radioButtonRightClick.TabIndex = 0;
            this.radioButtonRightClick.Text = "On right click";
            this.radioButtonRightClick.UseVisualStyleBackColor = true;
            this.radioButtonRightClick.CheckedChanged += new System.EventHandler(this.radioButtonAutoadvance_CheckedChanged);
            // 
            // radioButtonCtrlLeftClick
            // 
            this.radioButtonCtrlLeftClick.AutoSize = true;
            this.radioButtonCtrlLeftClick.Location = new System.Drawing.Point(7, 65);
            this.radioButtonCtrlLeftClick.Name = "radioButtonCtrlLeftClick";
            this.radioButtonCtrlLeftClick.Size = new System.Drawing.Size(105, 17);
            this.radioButtonCtrlLeftClick.TabIndex = 0;
            this.radioButtonCtrlLeftClick.Text = "On ctr + left click";
            this.radioButtonCtrlLeftClick.UseVisualStyleBackColor = true;
            this.radioButtonCtrlLeftClick.CheckedChanged += new System.EventHandler(this.radioButtonAutoadvance_CheckedChanged);
            // 
            // radioButtonLeftClick
            // 
            this.radioButtonLeftClick.AutoSize = true;
            this.radioButtonLeftClick.Location = new System.Drawing.Point(6, 19);
            this.radioButtonLeftClick.Name = "radioButtonLeftClick";
            this.radioButtonLeftClick.Size = new System.Drawing.Size(81, 17);
            this.radioButtonLeftClick.TabIndex = 0;
            this.radioButtonLeftClick.Text = "On left click";
            this.radioButtonLeftClick.UseVisualStyleBackColor = true;
            this.radioButtonLeftClick.CheckedChanged += new System.EventHandler(this.radioButtonAutoadvance_CheckedChanged);
            // 
            // comboBoxCtrlLeftClick
            // 
            this.comboBoxCtrlLeftClick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCtrlLeftClick.FormattingEnabled = true;
            this.comboBoxCtrlLeftClick.Location = new System.Drawing.Point(112, 76);
            this.comboBoxCtrlLeftClick.Name = "comboBoxCtrlLeftClick";
            this.comboBoxCtrlLeftClick.Size = new System.Drawing.Size(150, 21);
            this.comboBoxCtrlLeftClick.TabIndex = 8;
            this.comboBoxCtrlLeftClick.SelectedIndexChanged += new System.EventHandler(this.comboBoxCtrlLeftClick_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 76);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "ctrl + left click";
            // 
            // comboBoxCtrlRightClick
            // 
            this.comboBoxCtrlRightClick.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCtrlRightClick.FormattingEnabled = true;
            this.comboBoxCtrlRightClick.Location = new System.Drawing.Point(112, 102);
            this.comboBoxCtrlRightClick.Name = "comboBoxCtrlRightClick";
            this.comboBoxCtrlRightClick.Size = new System.Drawing.Size(150, 21);
            this.comboBoxCtrlRightClick.TabIndex = 8;
            this.comboBoxCtrlRightClick.SelectedIndexChanged += new System.EventHandler(this.comboBoxCtrlRightClick_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 102);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "ctrl + right click";
            // 
            // buttonAdd
            // 
            this.buttonAdd.Location = new System.Drawing.Point(194, 16);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonAdd.TabIndex = 13;
            this.buttonAdd.Text = "Add";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // textBoxNewMarkerName
            // 
            this.textBoxNewMarkerName.Location = new System.Drawing.Point(6, 19);
            this.textBoxNewMarkerName.Name = "textBoxNewMarkerName";
            this.textBoxNewMarkerName.Size = new System.Drawing.Size(181, 20);
            this.textBoxNewMarkerName.TabIndex = 14;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listBoxMarkers);
            this.groupBox1.Controls.Add(this.textBoxNewMarkerName);
            this.groupBox1.Controls.Add(this.buttonAdd);
            this.groupBox1.Location = new System.Drawing.Point(5, 9);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(288, 243);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Markers";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.comboBoxLeftClick);
            this.groupBox2.Controls.Add(this.comboBoxRightClick);
            this.groupBox2.Controls.Add(this.comboBoxCtrlLeftClick);
            this.groupBox2.Controls.Add(this.comboBoxCtrlRightClick);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Location = new System.Drawing.Point(5, 258);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(288, 141);
            this.groupBox2.TabIndex = 16;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Mouse actions";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(0, 26);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1295, 701);
            this.tabControl1.TabIndex = 15;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tableLayoutPanel1);
            this.tabPage1.Controls.Add(this.numericUpDownSkipFrames);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.videoPlayerUI1);
            this.tabPage1.Controls.Add(this.groupBoxAutoAdvance);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage1.Size = new System.Drawing.Size(1287, 675);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Image";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.imageBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.imageBox, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(299, 9);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(980, 603);
            this.tableLayoutPanel1.TabIndex = 18;
            // 
            // imageBox1
            // 
            this.imageBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.imageBox1.BackColor = System.Drawing.Color.Black;
            this.imageBox1.Cursor = System.Windows.Forms.Cursors.Cross;
            this.imageBox1.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.Minimum;
            this.imageBox1.Location = new System.Drawing.Point(493, 3);
            this.imageBox1.Name = "imageBox1";
            this.imageBox1.Size = new System.Drawing.Size(484, 597);
            this.imageBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBox1.TabIndex = 4;
            this.imageBox1.TabStop = false;
            this.imageBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBox_MouseDown);
            this.imageBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.imageBox_MouseUp);
            // 
            // imageBox
            // 
            this.imageBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.imageBox.BackColor = System.Drawing.Color.Black;
            this.imageBox.Cursor = System.Windows.Forms.Cursors.Cross;
            this.imageBox.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.Minimum;
            this.imageBox.Location = new System.Drawing.Point(3, 3);
            this.imageBox.Name = "imageBox";
            this.imageBox.Size = new System.Drawing.Size(484, 597);
            this.imageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.imageBox.TabIndex = 3;
            this.imageBox.TabStop = false;
            this.imageBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.imageBox_MouseDown);
            this.imageBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.imageBox_MouseUp);
            // 
            // numericUpDownSkipFrames
            // 
            this.numericUpDownSkipFrames.Location = new System.Drawing.Point(104, 559);
            this.numericUpDownSkipFrames.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSkipFrames.Name = "numericUpDownSkipFrames";
            this.numericUpDownSkipFrames.Size = new System.Drawing.Size(170, 20);
            this.numericUpDownSkipFrames.TabIndex = 17;
            this.numericUpDownSkipFrames.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSkipFrames.ValueChanged += new System.EventHandler(this.numericUpDownSkipFrames_ValueChanged);
            // 
            // videoPlayerUI1
            // 
            this.videoPlayerUI1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.videoPlayerUI1.Location = new System.Drawing.Point(298, 618);
            this.videoPlayerUI1.Name = "videoPlayerUI1";
            this.videoPlayerUI1.Size = new System.Drawing.Size(981, 44);
            this.videoPlayerUI1.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 561);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Every other frame";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 725);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1295, 22);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
            // 
            // exportDataToCsvToolStripMenuItem
            // 
            this.exportDataToCsvToolStripMenuItem.Name = "exportDataToCsvToolStripMenuItem";
            this.exportDataToCsvToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.exportDataToCsvToolStripMenuItem.Text = "Export data to csv ...";
            this.exportDataToCsvToolStripMenuItem.Click += new System.EventHandler(this.exportDataToCsvToolStripMenuItem_Click);
            // 
            // SubjectiveTracker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1295, 747);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SubjectiveTracker";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Subjective video tracker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SubjectiveTracker_FormClosing);
            this.Load += new System.EventHandler(this.SubjectiveTracker_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBoxAutoAdvance.ResumeLayout(false);
            this.groupBoxAutoAdvance.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imageBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSkipFrames)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadVideoToolStripMenuItem;
        private System.Windows.Forms.ComboBox comboBoxLeftClick;
        private System.Windows.Forms.ComboBox comboBoxRightClick;
        private System.Windows.Forms.ListBox listBoxMarkers;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBoxAutoAdvance;
        private System.Windows.Forms.RadioButton radioButtonNoAutoAdvance;
        private System.Windows.Forms.RadioButton radioButtonRightClick;
        private System.Windows.Forms.RadioButton radioButtonLeftClick;
        private System.Windows.Forms.ComboBox comboBoxCtrlLeftClick;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxCtrlRightClick;
        private System.Windows.Forms.Label label5;
        private OpenIris.UI.VideoPlayerUI videoPlayerUI1;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.TextBox textBoxNewMarkerName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ToolStripMenuItem loadDataFileToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.NumericUpDown numericUpDownSkipFrames;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonCtrlRightClick;
        private System.Windows.Forms.RadioButton radioButtonCtrlLeftClick;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Emgu.CV.UI.ImageBox imageBox1;
        private Emgu.CV.UI.ImageBox imageBox;
        private System.Windows.Forms.ToolStripMenuItem nextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportDataToCsvToolStripMenuItem;
    }
}

