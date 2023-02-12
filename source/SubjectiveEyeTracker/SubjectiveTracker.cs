//-----------------------------------------------------------------------
// <copyright file="SubjectiveTracker.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace SubjectiveTracker
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using OpenIris;
    using OpenIris.UI;

    /// <summary>
    /// 
    /// </summary>
    public partial class SubjectiveTracker : Form
    {
        /// <summary>
        /// Default colors for the markers
        /// </summary>
        private Color[] markerColors;

        /// <summary>
        /// Data structure with all the markers.
        /// </summary>
        private SubjectiveTrackerData data;

        /// <summary>
        /// Video player to control the image acquisition.
        /// </summary>
        private VideoPlayer videoPlayer;

        /// <summary>
        /// Path of the data file.
        /// </summary>
        private string dataFile;

        /// <summary>
        /// Path of the video file
        /// </summary>
        private EyeCollection<string> videoFiles;

        /// <summary>
        /// Value indicating wether the UI is currently updating. Used to avoid recurrent events while changing
        /// the data in the controls.
        /// </summary>
        private bool updating;

        private EyeTrackerSettings settings;

        /// <summary>
        /// Initializes a new instance of the SubjectiveTracker class.
        /// </summary>
        public SubjectiveTracker()
        {
            InitializeComponent();
            EyeTrackerPluginManager.Init(true);
            settings = EyeTrackerSettings.Load();
        }

        /// <summary>
        /// Initializes the data and the UI.
        /// </summary>
        private void Init()
        {
            // Default colors
            markerColors = new Color[]
            {
                Color.Blue,
                Color.Red,
                Color.Green,
                Color.Yellow,
                Color.Purple,
                Color.Brown,
            };


            if (Properties.Settings.Default.LastDataFile.Length > 0)
            {
                var result = MessageBox.Show(
                    "Do you want to load the last data file? " + Properties.Settings.Default.LastDataFile,
                    "Load file?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    dataFile = Properties.Settings.Default.LastDataFile;
                }
            }

            LoadDataFile();

            UpdateUI();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadDataFile()
        {
            if (dataFile != null)
            {
                if (!System.IO.File.Exists(dataFile))
                {
                    var result = MessageBox.Show(
                        "The data file does not exist, do you want to select another one?",
                        "Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        SelectDataFile();

                        if (dataFile != null)
                        {
                            LoadDataFile();
                        }

                        return;
                    }
                }

                data = SubjectiveTrackerData.Load(dataFile);

                if (data.VideoFiles != null)
                {
                    videoFiles = new EyeCollection<string>(data.VideoFiles);
                    LoadVideoFiles();
                }
                else
                {
                    SelectVideoFile();
                    if (videoFiles != null)
                    {
                        LoadVideoFiles();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SelectDataFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Data Files (*.dat)|*.dat";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select data file.";

            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            Properties.Settings.Default.LastDataFile = openFileDialog1.FileName;
            Properties.Settings.Default.Save();
            dataFile = Properties.Settings.Default.LastDataFile;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateNewDataFile()
        {
            // The video file has to be selected first
            if (videoFiles is null)
            {
                return;
            }
            else
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Data Files (*.dat)|*.dat";
                saveFileDialog1.OverwritePrompt = true;

                if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                Properties.Settings.Default.LastDataFile = saveFileDialog1.FileName;
                Properties.Settings.Default.Save();
                dataFile = Properties.Settings.Default.LastDataFile;

                data = new SubjectiveTrackerData();
                data.VideoFiles = videoFiles;
                data.Save(dataFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadVideoFiles()
        {
            if (videoFiles != null)
            {
                //if (!System.IO.File.Exists(videoFiles))
                //{
                //    var result = MessageBox.Show(
                //        "The video file does not exist at the saved location. Select again.",
                //        "Error",
                //        MessageBoxButtons.OK,
                //        MessageBoxIcon.Error);

                //    SelectVideoFile();
                //    return;
                //}

                // Initialize the video grabbing
                videoPlayer = new OpenIris.VideoPlayer(videoFiles[Eye.Left], videoFiles[Eye.Right]);
                videoPlayer.ImagesGrabbed += videoPlayer_ImagesGrabbed;
                videoPlayer.Scroll(data.CurrentFrameNumber);

                videoPlayer.Play();
                videoPlayer.Pause();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SelectVideoFile()
        {
            var options = SelectVideoDialog.SelectVideo(settings,  SelectVideoDialog.Mode.Simple);
          
            videoFiles = options.VideoFileNames;

            if (dataFile is null)
            {
                CreateNewDataFile();
            }

            if (dataFile != null)
            {
                data.VideoFiles = options.VideoFileNames;
            }
        }

        private void UpdateUI()
        {
            if (updating)
            {
                return;
            }

            updating = true;

            try
            {
                if (data != null)
                {
                    listBoxMarkers.DataSource = null;
                    listBoxMarkers.DataSource = data.MarkerNames;
                    listBoxMarkers.SelectedIndex = listBoxMarkers.FindStringExact(data.SelectedMarker);

                    comboBoxLeftClick.Items.Clear();
                    comboBoxLeftClick.Items.Add("None");
                    comboBoxLeftClick.Items.AddRange(data.MarkerNames.ToArray());
                    if (data.Options.LeftClickAction is null)
                        data.Options.LeftClickAction = comboBoxLeftClick.Items[0].ToString();
                    if (data.Options.LeftClickAction != null)
                        comboBoxLeftClick.SelectedItem = data.Options.LeftClickAction;

                    comboBoxRightClick.Items.Clear();
                    comboBoxRightClick.Items.Add("None");
                    comboBoxRightClick.Items.AddRange(data.MarkerNames.ToArray());
                    if (data.Options.RightClickAction is null)
                        data.Options.RightClickAction = comboBoxRightClick.Items[0].ToString();
                    if (data.Options.RightClickAction != null)
                        comboBoxRightClick.SelectedItem = data.Options.RightClickAction;

                    comboBoxCtrlLeftClick.Items.Clear();
                    comboBoxCtrlLeftClick.Items.Add("None");
                    comboBoxCtrlLeftClick.Items.AddRange(data.MarkerNames.ToArray());
                    if (data.Options.CtrlLeftClickAction is null)
                        data.Options.CtrlLeftClickAction = comboBoxCtrlLeftClick.Items[0].ToString();
                    if (data.Options.CtrlLeftClickAction != null)
                        comboBoxCtrlLeftClick.SelectedItem = data.Options.CtrlLeftClickAction;

                    comboBoxCtrlRightClick.Items.Clear();
                    comboBoxCtrlRightClick.Items.Add("None");
                    comboBoxCtrlRightClick.Items.AddRange(data.MarkerNames.ToArray());
                    if (data.Options.CtrlRightClickAction is null)
                        data.Options.CtrlRightClickAction = comboBoxCtrlRightClick.Items[0].ToString();
                    if (data.Options.CtrlRightClickAction != null)
                        comboBoxCtrlRightClick.SelectedItem = data.Options.CtrlRightClickAction;


                    if (data.Options.AutoAdvanceLeftClick)
                        radioButtonLeftClick.Checked = true;
                    if (data.Options.AutoAdvanceRightClick)
                        radioButtonRightClick.Checked = true;
                    if (data.Options.AutoAdvanceCtrlLeftClick)
                        radioButtonCtrlLeftClick.Checked = true;
                    if (data.Options.AutoAdvanceCtrlRightClick)
                        radioButtonCtrlRightClick.Checked = true;
                    if (!data.Options.AutoAdvanceLeftClick && !data.Options.AutoAdvanceRightClick &&
                        !data.Options.AutoAdvanceCtrlLeftClick && !data.Options.AutoAdvanceCtrlRightClick)
                        radioButtonNoAutoAdvance.Checked = true;

                    numericUpDownSkipFrames.Value = data.Options.EveryOtherFrame;

                    if (videoPlayer != null)
                    {
                        videoPlayerUI1.Update(videoPlayer);
                    }
                }

                if (dataFile != null)
                {
                    toolStripStatusLabel1.Text = "DATA FILE : " + dataFile;
                }
                if (videoFiles != null)
                {
                    toolStripStatusLabel2.Text = "VIDEO FILE : " + videoFiles;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                updating = false;
            }
        }
    
        private Image<Bgr, byte> DrawMarkers(Image<Bgr, byte> image, LeftRight leftRight)
        {
            var frameMarkers =
                from item in data.MarkerData
                where item.FrameNumber == data.CurrentFrameNumber
                select item;

            var color = Color.Black;

            foreach (var marker in frameMarkers)
            {
                for (int i = 0; i < data.MarkerNames.Count; i++)
                {
                    if (data.MarkerNames[i].Equals(marker.MarkerName))
                    {
                        color = markerColors[i];
                        break;
                    }
                }

                if (leftRight == marker.LeftRight)
                {
                    image.Draw(new CircleF(marker.Location, 5), new Bgr(color), 2);
                }
            }

            return image;
        }

        private void Advance()
        {
            videoPlayer.Scroll(data.CurrentFrameNumber + 1);
            UpdateUI();
        }

        /// <summary>
        /// From: http://www.codeproject.com/script/Articles/View.aspx?aid=859100
        /// </summary>
        /// <param name="p">The point.</param>
        private Point ConvertCoordinates(Point p)
        {
            var pic = imageBox;

            int pic_hgt = pic.ClientSize.Height;
            int pic_wid = pic.ClientSize.Width;
            int img_hgt = pic.Image.GetInputArray().GetSize().Height;
            int img_wid = pic.Image.GetInputArray().GetSize().Width;

            var x = p.X;
            var y = p.Y;

            var X0 = x;
            var Y0 = y;
            switch (pic.SizeMode)
            {
                case PictureBoxSizeMode.AutoSize:
                case PictureBoxSizeMode.Normal:
                    // These are okay. Leave them alone.
                    break;
                case PictureBoxSizeMode.CenterImage:
                    X0 = x - (pic_wid - img_wid) / 2;
                    Y0 = y - (pic_hgt - img_hgt) / 2;
                    break;
                case PictureBoxSizeMode.StretchImage:
                    X0 = (int)(img_wid * x / (float)pic_wid);
                    Y0 = (int)(img_hgt * y / (float)pic_hgt);
                    break;
                case PictureBoxSizeMode.Zoom:
                    float pic_aspect = pic_wid / (float)pic_hgt;
                    float img_aspect = img_wid / (float)img_wid;
                    if (pic_aspect > img_aspect)
                    {
                        // The PictureBox is wider/shorter than the image.
                        Y0 = (int)(img_hgt * y / (float)pic_hgt);

                        // The image fills the height of the PictureBox.
                        // Get its width.
                        float scaled_width = img_wid * pic_hgt / img_hgt;
                        float dx = (pic_wid - scaled_width) / 2;
                        X0 = (int)((x - dx) * img_hgt / (float)pic_hgt);
                    }
                    else
                    {
                        // The PictureBox is taller/thinner than the image.
                        X0 = (int)(img_wid * x / (float)pic_wid);

                        // The image fills the height of the PictureBox.
                        // Get its height.
                        float scaled_height = img_hgt * pic_wid / img_wid;
                        float dy = (pic_hgt - scaled_height) / 2;
                        Y0 = (int)((y - dy) * img_wid / pic_wid);
                    }
                    break;
            }

            return new Point(X0, Y0);
        }

        private long Mod(long a, long b)
        {
            long result;
            Math.DivRem(a, b, out result);

            return result;
        }

        private void videoPlayer_ImagesGrabbed(object sender, EyeCollection<ImageEye?> grabbedImages)
        {
            if (data.CurrentFrameNumber <= videoPlayer.CurrentFrameNumber)
            {
                data.CurrentFrameNumber = videoPlayer.CurrentFrameNumber;

                if (Mod((int)data.CurrentFrameNumber, data.Options.EveryOtherFrame) != 0)
                {
                    var newFrameNumber = Math.Ceiling(data.CurrentFrameNumber / (double)data.Options.EveryOtherFrame) * data.Options.EveryOtherFrame;
                    videoPlayer.Scroll((ulong)newFrameNumber);
                    return;
                }
            }
            else
            {
                data.CurrentFrameNumber = videoPlayer.CurrentFrameNumber;

                if (Mod((long)data.CurrentFrameNumber, data.Options.EveryOtherFrame) != 0)
                {
                    var newFrameNumber = Math.Floor(data.CurrentFrameNumber / (double)data.Options.EveryOtherFrame) * data.Options.EveryOtherFrame;
                    videoPlayer.Scroll((ulong)newFrameNumber);
                    return;
                }
            }

            videoPlayerUI1.Update(videoPlayer);

            var imageLeft = grabbedImages[Eye.Left].Image.Resize(3.0f, Inter.Cubic).Convert<Bgr, byte>();
            var imageRight = grabbedImages[Eye.Right].Image.Resize(3.0f, Inter.Cubic).Convert<Bgr, byte>();

            imageLeft = DrawMarkers(imageLeft, LeftRight.Left);
            imageRight= DrawMarkers(imageRight, LeftRight.Right);
            imageBox.Image = imageRight;
            imageBox1.Image = imageLeft;
        }

        private void SubjectiveTracker_Load(object sender, EventArgs e)
        {
            Init();
        }
        

        private void imageBox_MouseDown(object sender, MouseEventArgs e)
        {
            var leftRight = LeftRight.Left;
            if ( sender == imageBox)
            {
                leftRight = LeftRight.Right;
            }

            var mousePosition = ConvertCoordinates(e.Location);
            var action = string.Empty;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (Control.ModifierKeys != Keys.Control)
                    {
                        action = data.Options.LeftClickAction;
                    }
                    else
                    {
                        action = data.Options.CtrlLeftClickAction;
                    }
                    break;
                case MouseButtons.Right:
                    if (Control.ModifierKeys != Keys.Control)
                    {
                        action = data.Options.RightClickAction;
                    }
                    else
                    {
                        action = data.Options.CtrlRightClickAction;
                    }
                    break;
                default:
                    break;
            }

            if (action.Length > 0 && !action.ToUpper().Equals("NONE"))
            {
                var marker = new MarkerData();
                marker.Location = mousePosition;
                marker.FrameNumber = data.CurrentFrameNumber;
                marker.MarkerName = action;
                marker.LeftRight = leftRight;

                data.MarkerData.Add(marker);

                videoPlayer.Scroll(data.CurrentFrameNumber);
            }

            data.Save(dataFile);
            UpdateUI();
        }
        
        private void imageBox_MouseUp(object sender, MouseEventArgs e)
        {
            var autoadvance = false;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (Control.ModifierKeys != Keys.Control &&  radioButtonLeftClick.Checked)
                    {
                        autoadvance = true;
                    }
                    if (Control.ModifierKeys == Keys.Control &&  radioButtonCtrlLeftClick.Checked)
                    {
                        autoadvance = true;
                    }
                    break;
                case MouseButtons.Right:
                    if (Control.ModifierKeys != Keys.Control &&  radioButtonRightClick.Checked)
                    {
                        autoadvance = true;
                    }
                    if (Control.ModifierKeys == Keys.Control && radioButtonCtrlRightClick.Checked)
                    {
                        autoadvance = true;
                    }
                    break;
                default:
                    break;
            }

            if (autoadvance)
            {
                Advance();
            }
        }


        private void loadVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataFile = null;
            data = null;

            SelectVideoFile();

            if (videoFiles != null)
            {
                LoadVideoFiles();
            }

            data.Save(dataFile);
            UpdateUI();
        }

        private void loadDataFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectDataFile();

            if (dataFile != null)
            {
                LoadDataFile();
            }
            data.Save(dataFile);
            UpdateUI();
        }



        private void comboBoxLeftClick_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating && comboBoxLeftClick.SelectedItem != null)
            {
                data.Options.LeftClickAction = comboBoxLeftClick.SelectedItem.ToString();
                data.Save(dataFile);
                UpdateUI();
            }
        }

        private void comboBoxRightClick_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating && comboBoxRightClick.SelectedItem != null)
            {
                data.Options.RightClickAction = comboBoxRightClick.SelectedItem.ToString();
                data.Save(dataFile);
                UpdateUI();
            }
        }

        private void comboBoxCtrlLeftClick_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating && comboBoxCtrlLeftClick.SelectedItem != null)
            {
                data.Options.CtrlLeftClickAction = comboBoxCtrlLeftClick.SelectedItem.ToString();
                data.Save(dataFile);
                UpdateUI();
            }
        }

        private void comboBoxCtrlRightClick_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating && comboBoxCtrlRightClick.SelectedItem != null)
            {
                data.Options.CtrlRightClickAction = comboBoxCtrlRightClick.SelectedItem.ToString();
                data.Save(dataFile);
                UpdateUI();
            }
        }
        

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            data.MarkerNames.Add(textBoxNewMarkerName.Text);
            data.SelectedMarker = textBoxNewMarkerName.Text;
            textBoxNewMarkerName.Text = string.Empty;
            data.Save(dataFile);
            UpdateUI();
        }

        private void listBoxMarkers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                data.SelectedMarker = listBoxMarkers.SelectedItem.ToString();
                data.Save(dataFile);
                UpdateUI();
            }
        }


        private void radioButtonAutoadvance_CheckedChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                data.Options.AutoAdvanceLeftClick = radioButtonLeftClick.Checked;
                data.Options.AutoAdvanceRightClick = radioButtonRightClick.Checked;
                data.Options.AutoAdvanceCtrlLeftClick = radioButtonCtrlLeftClick.Checked;
                data.Options.AutoAdvanceCtrlRightClick = radioButtonCtrlRightClick.Checked;
                data.Save(dataFile);
                UpdateUI();
            }
        }

        private void SubjectiveTracker_FormClosing(object sender, FormClosingEventArgs e)
        {
            data.Save(dataFile);
            UpdateUI();
        }

        private void numericUpDownSkipFrames_ValueChanged(object sender, EventArgs e)
        {
            data.Options.EveryOtherFrame = (int)numericUpDownSkipFrames.Value;
            data.Save(dataFile);
            UpdateUI();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (data.MarkerData.Count > 0)
            {
                data.MarkerData.RemoveAt(data.MarkerData.Count - 1);
                videoPlayer.Scroll(data.CurrentFrameNumber);
                data.Save(dataFile);
                UpdateUI();
            }
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Advance();
        }

        private void exportDataToCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //before your loop
            var csv = new StringBuilder();

            csv.AppendLine("MARKER,FRAME NUMBER,EYE,X,Y");

            foreach (var marker in data.MarkerData)
            {
                csv.AppendLine(marker.ToCSVString());
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog1.OverwritePrompt = true;

            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            
            File.WriteAllText(saveFileDialog1.FileName, csv.ToString());
        }
    }
}
