using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenIris
{
#nullable enable
    
    /// <summary>
    /// 
    /// </summary>
    public partial class Form1 : Form
    {
        private OpenIrisClient? eyeTracker;
        private readonly Timer updateTimer;

        /// <summary>
        /// 
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            updateTimer = new Timer();
            updateTimer.Tick += updateTimer_Tick;
            updateTimer.Interval = 300;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            updateTimer.Enabled = true;

            textBoxIP.Text = EyeTrackerRemoteGUI.Properties.Settings.Default.LastIP;
        }

        void updateTimer_Tick(object sender, EventArgs e)
        {
            if (eyeTracker != null)
            {
                try
                {
                    // Get the status of the tracker
                    var eyeTrackerStatus = eyeTracker.Status;

                    toolStripStatusLabelImageGrabbingStatus.Text = eyeTrackerStatus.GrabberStatus;
                    toolStripStatusLabelProcessingTimeLeftEye.Text = eyeTrackerStatus.ProcessorStatus;
                    toolStripStatusLabelRecording.Text = eyeTrackerStatus.RecorderStatus;

                    if (eyeTrackerStatus.Tracking)
                    {
                        var settings = eyeTracker.Settings as EyeTrackingPipelinePupilCRSettings ?? 
                            throw new InvalidOperationException("Wrong settings.");

                        var calibrationParameters = eyeTracker.GetCalibrationParameters();
                        var imagesAndData = eyeTracker.GetCurrentImagesAndData();


                        labelDataLeft.Text = "DATA LEFT EYE: " +
                            "H: " + Math.Round(imagesAndData.CalibratedData[Eye.Left].HorizontalPosition) + " " +
                            "V: " + Math.Round(imagesAndData.CalibratedData[Eye.Left].VerticalPosition) + " " +
                            "T: " + Math.Round(imagesAndData.CalibratedData[Eye.Left].TorsionalPosition);

                        labelDataRight.Text = "DATA RIGHT EYE: " +
                            "H: " + Math.Round(imagesAndData.CalibratedData[Eye.Right].HorizontalPosition) + " " +
                            "V: " + Math.Round(imagesAndData.CalibratedData[Eye.Right].VerticalPosition) + " " +
                            "T: " + Math.Round(imagesAndData.CalibratedData[Eye.Right].TorsionalPosition);

                        var bitmap = imagesAndData.Image[Eye.Left];
                        var data = imagesAndData.RawData[Eye.Left];
                        if (bitmap != null && data != null)
                        {
                            var imLeft = new ImageEye(bitmap, Eye.Left, data.Timestamp);
                            imLeft.EyeData = data;
                            eyeTrackerImageEyeBoxLeft.UpdateImageEyeBox(imLeft,
                                calibrationParameters.PhysicalModel[Eye.Left],
                                settings.DarkThresholdLeftEye,
                                settings.BrightThresholdLeftEye,
                                settings.CroppingLeftEye, 
                                settings.MmPerPix);
                        }

                        bitmap = imagesAndData.Image[Eye.Right];
                        data = imagesAndData.RawData[Eye.Right];
                        if (bitmap != null && data != null)
                        {
                            var imRight = new ImageEye(bitmap, Eye.Right, data.Timestamp);
                            imRight.EyeData = data;
                            eyeTrackerImageEyeBoxRight.UpdateImageEyeBox(imRight,
                                calibrationParameters.PhysicalModel[Eye.Right],
                                settings.DarkThresholdRightEye,
                                settings.BrightThresholdRightEye,
                                settings.CroppingRightEye,
                                settings.MmPerPix);
                        }

                        labelDarkThresholdLeft.Text = "Pupil threshokld: " + settings.DarkThresholdLeftEye;
                        labelDarkThresholdRight.Text = "Pupil threshokld: " + settings.DarkThresholdRightEye;
                    }

                    buttonConnect.Enabled = false;
                    textBoxIP.Enabled = true;

                    buttonStartRecording.Enabled = !eyeTrackerStatus.Recording && eyeTrackerStatus.Tracking;
                    buttonStopRecording.Enabled = eyeTrackerStatus.Recording && eyeTrackerStatus.Tracking;
                    buttonResetReference.Enabled = eyeTrackerStatus.Tracking;

                    buttonDecreaseDarkThresholdLeft.Enabled = eyeTrackerStatus.Tracking;
                    buttonDecreaseDarkThresholdRight.Enabled = eyeTrackerStatus.Tracking;
                    buttonIncreaseDarkThresholdLeft.Enabled = eyeTrackerStatus.Tracking;
                    buttonIncreaseDarkThresholdRight.Enabled = eyeTrackerStatus.Tracking;
                }
                catch (Exception ex)
                {
                    labelError.Text = ex.Message;
                    labelError.ForeColor = Color.Red;

                    eyeTracker = null;
                }
            }
            else
            {
                buttonConnect.Enabled = true;
                textBoxIP.Enabled = true;

                buttonStartRecording.Enabled = false;
                buttonStopRecording.Enabled = false;
                buttonResetReference.Enabled = false;

                buttonDecreaseDarkThresholdLeft.Enabled = false;
                buttonDecreaseDarkThresholdRight.Enabled = false;
                buttonIncreaseDarkThresholdLeft.Enabled = false;
                buttonIncreaseDarkThresholdRight.Enabled = false;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                EyeTrackerRemoteGUI.Properties.Settings.Default.LastIP = textBoxIP.Text;
                EyeTrackerRemoteGUI.Properties.Settings.Default.Save();

                string hostname = textBoxIP.Text;
                int port = 9000;
                eyeTracker = new OpenIrisClient(hostname, port);

                labelError.Text = string.Empty;
            }
            catch (Exception ex)
            {
                labelError.Text = ex.Message;
                labelError.ForeColor = Color.Red;
            }
        }

        private void buttonStartRecording_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");

            eyeTracker.StartRecording();
        }

        private void buttonStopRecording_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to stop the recording? If the recording was started by another program, stopping may cause that other program to crash.",
                "Stop recording?", MessageBoxButtons.OKCancel);
            

            if (result == DialogResult.OK)
            {
                if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
                eyeTracker.StopRecording();
            }
        }

        private void buttonResetReference_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
            eyeTracker.ResetReference();
        }

        private void buttonDecreaseDarkThresholdLeft_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
            eyeTracker.ChangeThreshold(false, true, Eye.Left);
        }

        private void buttonDecreaseDarkThresholdRight_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
            eyeTracker.ChangeThreshold(false, true, Eye.Right);
        }

        private void buttonIncreaseDarkThresholdLeft_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
            eyeTracker.ChangeThreshold(true, true, Eye.Left);
        }

        private void buttonIncreaseDarkThresholdRight_Click(object sender, EventArgs e)
        {
            if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
            eyeTracker.ChangeThreshold(true, true, Eye.Right);
        }

        private void buttonDowndloadFile_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Don't keep going if you are not sure. This will stop an ongoing recording and cause some problems if not used properly.",
                "Download file?", MessageBoxButtons.OKCancel);

            if (result == DialogResult.OK)
            {
                if (eyeTracker == null) throw new NullReferenceException("Eyetracker is null.");
                eyeTracker.StopRecording();
                eyeTracker.DownloadFile();
            }
        }
    }
}
