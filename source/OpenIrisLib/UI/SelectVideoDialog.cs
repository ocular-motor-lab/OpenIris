//-----------------------------------------------------------------------
// <copyright file="SelectVideoDialog.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Windows.Forms;
    using System.Linq;
    using OpenIris;

    /// <summary>
    /// Dialog used to select a video to play or process.
    /// </summary>
    public partial class SelectVideoDialog : Form
    {
        /// <summary>
        /// Modes for the dialog.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Simple, only filenames.
            /// </summary>
            Simple,

            /// <summary>
            /// Complete, with all elements.
            /// </summary>
            Complete,

            /// <summary>
            /// Complete, plus options for post processing.
            /// </summary>
            Processing
        }

        private readonly EyeTrackerSettings settings;
        private readonly Mode mode;

        /// <summary>
        /// Initializes a new instance of the SelectVideoDialog class.
        /// </summary>
        public SelectVideoDialog(EyeTrackerSettings settings, Mode mode)
        {
            this.mode = mode;
            this.settings = settings;

            InitializeComponent();
        }

        /// <summary>
        /// Shows a dialog to select a video.
        /// </summary>
        /// <param name="settings">Settings of the eye tracking.</param>
        /// <returns>The options selected in the dialog.</returns>
        public static ProcessVideoOptions? SelectVideoForProcessing(EyeTrackerSettings settings)
        {
            return SelectVideo(settings, Mode.Processing) as ProcessVideoOptions;
        }

        /// <summary>
        /// Shows a dialog to select a video.
        /// </summary>
        /// <param name="settings">Settings of the eye tracking.</param>
        /// <param name="mode">Level of detail of options to show.</param>
        /// <returns>The options selected in the dialog.</returns>
        public static PlayVideoOptions? SelectVideo(EyeTrackerSettings settings, Mode mode = Mode.Complete)
        {
            using var dialog = new SelectVideoDialog(settings, mode);

            switch (mode)
            {
                case Mode.Simple:
                    dialog.panelSystem.Visible = false;
                    dialog.panelCalibration.Visible = false;
                    dialog.panelCustomRange.Visible = false;
                    dialog.panelSaveProcessedVideo.Visible = false;
                    break;
                case Mode.Complete:
                    dialog.panelCustomRange.Visible = false;
                    dialog.panelSaveProcessedVideo.Visible = false;
                    break;
                case Mode.Processing:
                    break;
                default:
                    break;
            }

            var result = dialog.ShowDialog();

            if (result != DialogResult.OK) return null;

            PlayVideoOptions options;

            if (mode != Mode.Processing)
            {
                options = new PlayVideoOptions
                {
                    EyeTrackingSystem = dialog.GetEyeTrackingSystemMetadata().Name,
                    CalibrationFileName = dialog.GetCalibrationFileName()
                };
            }
            else
            {
                options = new ProcessVideoOptions
                {
                    EyeTrackingSystem = dialog.GetEyeTrackingSystemMetadata().Name,
                    CalibrationFileName = dialog.GetCalibrationFileName(),
                    SaveProcessedVideo = dialog.checkBoxSaveProcessedVideo.Checked,
                    CustomRange = dialog.GetCustomRange(),
                };
            }

            options.VideoFileNames = dialog.GetVideoFileNames();

            return options;
        }

        /// <summary>
        /// Gets the names of the video files.
        /// </summary>
        /// <returns>The names of the video files.</returns>
        private EyeCollection<string?> GetVideoFileNames()
        {
            switch (mode)
            {
                case Mode.Simple:
                    return new EyeCollection<string?>(file1TextBox.Text, file2TextBox.Text);
                case Mode.Complete:
                case Mode.Processing:
                    switch (GetEyeTrackingSystemMetadata().VideoConfiguration)
                    {
                        case VideoEyeConfiguration.TwoVideosTwoEyes:
                            var video1 = file1TextBox.Text.Length >0 ? file1TextBox.Text : null;
                            var video2 = file2TextBox.Text.Length >0 ? file2TextBox.Text : null;
                            return new EyeCollection<string?>(video1, video2);

                        case VideoEyeConfiguration.SingleVideoTwoEyes:
                        case VideoEyeConfiguration.SingleVideoOneEye:
                            return new EyeCollection<string?>(file1TextBox.Text);
                    }
                    break;
            }

            throw new InvalidOperationException("Error.");
        }

        /// <summary>
        /// Gets the names of the calibration files.
        /// </summary>
        /// <returns>The names of the calibration files.</returns>
        private string GetCalibrationFileName()
        {
            return textBoxCalibration.Text;
        }

        /// <summary>
        /// Gets the camera system.
        /// </summary>
        /// <returns>Camera system.</returns>
        private IEyeTrackingSystemMetadata GetEyeTrackingSystemMetadata()
        {
            if (EyeTrackerPluginManager.EyeTrackingsyStemFactory is null) throw new InvalidOperationException("No factory.");

            if (systemComboBox.SelectedValue is null) return EyeTrackerPluginManager.EyeTrackingsyStemFactory.ClassesAvaiable.First();

            var metadata = from plugin in EyeTrackerPluginManager.EyeTrackingsyStemFactory.ClassesAvaiable
                           where plugin.Name == (string)systemComboBox.SelectedValue
                           select plugin;

            return metadata.ElementAt(0);
        }

        /// <summary>
        /// Gets the custom range of frames.
        /// </summary>
        /// <returns>Range of frames.</returns>
        private Range GetCustomRange()
        {
            if (!checkBoxCustomRange.Checked)
            {
                return new Range(
                    int.Parse(textBoxFromFrame.Text),
                    int.Parse(textBoxToFrame.Text));
            }
            else
            {
                return new Range();
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void SelectVideoDialog_Load(object sender, EventArgs e)
        {
            var availableSystems = EyeTrackerPluginManager.EyeTrackingsyStemFactory.ClassesAvaiable.Select(x => x.Name).ToArray();
            Array.Sort(availableSystems);

            systemComboBox.DataSource = availableSystems;
            systemComboBox.SelectedIndex = systemComboBox.FindStringExact(settings.LastVideoEyeTrackerSystem);
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void File1Button_Click(object sender, EventArgs e)
        {
            using var openFileDialog1 = new OpenFileDialog();
            if (settings != null)
            {
                if (System.IO.Directory.Exists(settings.LastVideoFolder))
                {
                    openFileDialog1.InitialDirectory = settings.LastVideoFolder;
                }
            }

            openFileDialog1.Filter = "Video Files (*.avi;*.mpg;*.mp4)|*.avi;*.mpg;*.mp4|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;

            var result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var fileName = openFileDialog1.FileName;
                if (settings != null)
                {
                    settings.LastVideoFolder = System.IO.Path.GetDirectoryName(fileName);
                }

                if (fileName.Length == 0)
                {
                    throw new ArgumentException("File not valid");
                }
                string leftFileName = file1TextBox.Text;
                string rightFileName = file2TextBox.Text;

                switch (((string)((Button)sender).Tag))
                {
                    case "Left":
                        leftFileName = fileName;
                        break;
                    case "Right":
                        rightFileName = fileName;
                        break;
                }

                if (fileName.Contains("Left.") && !fileName.Contains("LeftRight."))
                {
                    leftFileName = fileName;
                    rightFileName = fileName.Replace("Left.", "Right.");
                }

                if (fileName.Contains("Right.") && !fileName.Contains("LeftRight."))
                {
                    rightFileName = fileName;
                    leftFileName = fileName.Replace("Right.", "Left.");
                }

                if (fileName.Contains("LeftRight."))
                {
                    leftFileName = fileName;
                    rightFileName = fileName;
                }

                file1TextBox.Text = leftFileName;
                file2TextBox.Text = rightFileName;
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void ButtonCalibrationLeftEye_Click(object sender, EventArgs e)
        {
            using var openFileDialog1 = new OpenFileDialog();
            if (System.IO.Directory.Exists(settings.LastVideoFolder))
            {
                openFileDialog1.InitialDirectory = settings.LastVideoFolder;
            }
            openFileDialog1.Filter = "Calibration Files (*.cal)|*.cal";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;

            var result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var fileName = openFileDialog1.FileName;
                settings.LastVideoFolder = System.IO.Path.GetDirectoryName(fileName);

                if (fileName.Length == 0)
                {
                    throw new ArgumentException("File not valid");
                }

                textBoxCalibration.Text = fileName;
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void CheckBoxCustomRange_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }
        
        private void acceptButton_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(file1TextBox.Text) && !System.IO.File.Exists(file2TextBox.Text))
            {
                MessageBox.Show("You must select at least one video file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        private void systemComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            panelRange.Visible = !checkBoxCustomRange.Checked;

            var eyeTrackingsystem = GetEyeTrackingSystemMetadata();

            if (eyeTrackingsystem != null)
            {
                switch (eyeTrackingsystem.VideoConfiguration)
                {
                    case VideoEyeConfiguration.TwoVideosTwoEyes:
                        panelVideo2.Visible = true;
                        labelVideo1.Text = "Left eye video file";
                        labelVideo2.Text = "Right eye video file";
                        break;
                    case VideoEyeConfiguration.SingleVideoOneEye:
                    case VideoEyeConfiguration.SingleVideoTwoEyes:
                        panelVideo2.Visible = false;
                        labelVideo1.Text = "Video video file";
                        break;
                }
            }
        }

        private void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (System.IO.Directory.Exists(settings.LastVideoFolder))
            {
                dialog.SelectedPath = settings.LastVideoFolder;
            }

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var folder = dialog.SelectedPath;

                //var fileName = openFileDialog1.FileName;
                //settings.LastVideoFolder = System.IO.Path.GetDirectoryName(fileName);

                //if (fileName.Length == 0)
                //{
                //    throw new ArgumentException("File not valid");
                //}
                //string leftFileName = file1TextBox.Text;
                //string rightFileName = file2TextBox.Text;

                //switch (((string)((Button)sender).Tag))
                //{
                //    case "Left":
                //        leftFileName = fileName;
                //        break;
                //    case "Right":
                //        rightFileName = fileName;
                //        break;
                //}

                //if (fileName.Contains("Left.") && !fileName.Contains("LeftRight."))
                //{
                //    leftFileName = fileName;
                //    rightFileName = fileName.Replace("Left.", "Right.");
                //}

                //if (fileName.Contains("Right.") && !fileName.Contains("LeftRight."))
                //{
                //    rightFileName = fileName;
                //    leftFileName = fileName.Replace("Right.", "Left.");
                //}

                //if (fileName.Contains("LeftRight."))
                //{
                //    leftFileName = fileName;
                //    rightFileName = fileName;
                //}

                //file1TextBox.Text = leftFileName;
                //file2TextBox.Text = rightFileName;
            }
        }
    }
}