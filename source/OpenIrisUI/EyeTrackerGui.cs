//-----------------------------------------------------------------------
// <copyright file="EyeTrackerGui.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using Emgu.CV.Structure;
    using Emgu.CV.UI;
    using OpenIris;
    using OpenIris.ImageGrabbing;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Main graphical user interface of the eye tracker
    /// </summary>
    public partial class EyeTrackerGui : Form
    {
        private static object _locker = new object();

        // private EyeTracker eyeTracker;
        private readonly EyeTrackerGuiViewModel eyeTrackerViewModel;
        private readonly EyeTracker eyeTracker;
        private readonly Timer timerRefreshUI;
        private readonly LogTraceListener log;

        private ICalibrationUI calibrationUI;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerGui class
        /// </summary>
        public EyeTrackerGui()
        {
            InitializeComponent();

            log = new LogTraceListener(richTextBox1, richTextBoxLogLarge);
            eyeTrackerViewModel = new EyeTrackerGuiViewModel();
            eyeTracker = eyeTrackerViewModel.EyeTracker;

            timerRefreshUI = new Timer(components)
            {
                Interval = 100,
            };
            timerRefreshUI.Tick += TimerRefreshUI_Tick;

            Initialize();
        }

        /// <summary>
        /// Initializes the Eye tracker interface.
        /// </summary>
        /// <returns></returns>
        private void Initialize()
        {
            var settings = eyeTrackerViewModel.Settings;
            if (settings is null) return;

            Hide();

            try
            {
                buttonMoveRightEyeUp.Tag = (Eye.Right, MovementDirection.Up);
                buttonMoveRightEyeDown.Tag = (Eye.Right, MovementDirection.Down);
                buttonMoveRightEyeRight.Tag = (Eye.Right, MovementDirection.Right);
                buttonMoveRightEyeLeft.Tag = (Eye.Right, MovementDirection.Left);

                buttonMoveLeftEyeUp.Tag = (Eye.Left, MovementDirection.Up);
                buttonMoveLeftEyeDown.Tag = (Eye.Left, MovementDirection.Down);
                buttonMoveLeftEyeRight.Tag = (Eye.Left, MovementDirection.Right);
                buttonMoveLeftEyeLeft.Tag = (Eye.Left, MovementDirection.Left);

                //
                // Bind commands with UI items
                //

                eyeTrackerViewModel.PlayVideoCommand.Bind(playVideoToolStripMenuItem);
                eyeTrackerViewModel.PlayVideoCommand.Bind(buttonPlayVideo);
                eyeTrackerViewModel.ProcessVideoCommand.Bind(processVideoToolStripMenuItem);
                eyeTrackerViewModel.ProcessVideoCommand.Bind(buttonProcessVideo);
                eyeTrackerViewModel.BatchProcessVideoCommand.Bind(batchAnalysisToolStripMenuItem);

                eyeTrackerViewModel.StopCommand.Bind(stopToolStripMenuItem);

                eyeTrackerViewModel.StartCalibrationCommand.Bind(calibrateToolStripMenuItem);
                eyeTrackerViewModel.CancelCalibrationCommand.Bind(cancelCalibrationToolStripMenuItem);
                eyeTrackerViewModel.StartCancelCalibrationCommand.Bind(buttonCalibrate);
                eyeTrackerViewModel.LoadCalibrationCommand.Bind(loadCalibrationToolStripMenuItem);
                eyeTrackerViewModel.SaveCalibrationCommand.Bind(saveCalibrationToolStripMenuItem);
                eyeTrackerViewModel.ResetReferenceCommand.Bind(buttonResetReference);
                eyeTrackerViewModel.ResetReferenceCommand.Bind(resetReferenceToolStripMenuItem);
                eyeTrackerViewModel.ResetCalibrationCommand.Bind(resetCalibrationToolStripMenuItem);

                eyeTrackerViewModel.StartStopRecordingCommand.Bind(startStopRecordingToolStripMenuItem);
                eyeTrackerViewModel.StartStopRecordingCommand.Bind(buttonRecord);

                eyeTrackerViewModel.StartTrackingCommand.Bind(startTrackingToolStripMenuItem);
                eyeTrackerViewModel.StartTrackingCommand.Bind(buttonStartTracking);

                eyeTrackerViewModel.EditSettingsCommand.Bind(configurationToolStripMenuItem);
                eyeTrackerViewModel.EditSettingsCommand.Bind(buttonEditSettings);

                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveRightEyeUp);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveRightEyeDown);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveRightEyeRight);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveRightEyeLeft);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveLeftEyeUp);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveLeftEyeDown);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveLeftEyeRight);
                eyeTrackerViewModel.MoveCamerasCommand.Bind(buttonMoveLeftEyeLeft);

                eyeTrackerViewModel.CenterCamerasCommand.Bind(buttonCenterEyes);

                eyeTrackerViewModel.ChangeDataFolderCommand.Bind(buttonSelectFolder);
                eyeTrackerViewModel.ChangeDataFolderCommand.Bind(buttonSelectFolder2);

                eyeTrackerViewModel.TrimVideosCommand.Bind(trimVideosToolStripMenuItem);
                eyeTrackerViewModel.ConvertVideoToRGBCommand.Bind(convertVideoToRGBToolStripMenuItem);

                //
                // Data Bindings with settings
                //
                labelEyeTrackingSystem.DataBindings.Add(nameof(labelEyeTrackingSystem.Text), eyeTrackerViewModel.Settings, nameof(eyeTrackerViewModel.Settings.EyeTrackerSystem));
                textBoxSession.DataBindings.Add(nameof(textBoxSession.Text), eyeTrackerViewModel.Settings, nameof(eyeTrackerViewModel.Settings.SessionName));
                linkLabelDataFolder.DataBindings.Add(nameof(linkLabelDataFolder.Text), eyeTrackerViewModel.Settings, nameof(eyeTrackerViewModel.Settings.DataFolder));
                linkLabelDataFolder2.DataBindings.Add(nameof(linkLabelDataFolder2.Text), eyeTrackerViewModel.Settings, nameof(eyeTrackerViewModel.Settings.DataFolder));

                //
                // Other event handlers not worth a command
                //
                exitToolStripMenuItem.Click += (o, e) => Close();
                linkLabelDataFolder.Click += (o, e) => Process.Start("explorer.exe", settings.DataFolder);
                linkLabelDataFolder2.Click += (o, e) => Process.Start("explorer.exe", settings.DataFolder);

                openSoundRecorderToolStripMenuItem.Click += (o, e) => Process.Start("SoundRecorder.exe");
                openLogFolderToolStripMenuItem.Click += (o, e) => Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                showIPNetworkInfoToolStripMenuItem.Click += (o, e) => MessageBox.Show(EyeTrackerRemoteService.GetIPAddresses());
                aboutToolStripMenuItem.Click += (o, e) => new EyeTrackerAboutBox().ShowDialog();

                KeyPreview = true;
                KeyPress += (o, e) => eyeTracker.RecordEvent("KEYPRESS", e.KeyChar);

                // Enable UI timer for updates
                timerRefreshUI.Enabled = true;
            }
            catch (Exception ex)
            {
                timerRefreshUI.Enabled = false;
                MessageBox.Show("GUI:Load: " + ex.ToString());
            }
        }

        private void UpdateUI()
        {
            var settings = eyeTrackerViewModel.Settings;
            if (settings is null) return;

            try
            {
                tabPages.SuspendLayout();

                if (eyeTracker.NotStarted)
                {
                    if (tabPages.TabPages.Contains(tabSetup))
                    {
                        tabPages.TabPages.Clear();

                        tabPages.TabPages.Add(tabPageStart);
                        tabPages.TabPages.Add(tabViewer);
                        tabPages.TabPages.Add(tabPageLog);

                        tabPages.SelectedTab = tabPageStart;
                    }
                }
                else
                {
                    if (tabPages.TabPages.Contains(tabPageStart))
                    {
                        tabPages.TabPages.Clear();

                        tabPages.TabPages.Add(tabSetup);
                        tabPages.TabPages.Add(tabViewer);
                        tabPages.TabPages.Add(tabPageLog);

                        tabPages.SelectedTab = tabSetup;
                    }

                    if (settings.Debug && !tabPages.TabPages.Contains(tabDebug))
                    {
                        tabPages.TabPages.Add(tabDebug);
                        tabPages.TabPages.Add(tabTiming);
                    }
                    if (!settings.Debug && tabPages.TabPages.Contains(tabDebug))
                    {
                        tabPages.TabPages.Remove(tabDebug);
                        tabPages.TabPages.Remove(tabTiming);
                    }
                }
            }
            finally
            {
                tabPages.ResumeLayout();
            }


            if (!eyeTrackerTrace.Initialized)
            {
                eyeTrackerTrace.Init(settings);
            }

            // Eye tracking pipeline settings
            tabSetup.Enabled = eyeTracker.Tracking;

            // Player
            panelPlayer.Visible = eyeTracker.PlayingVideo;
            panelPlayer.Enabled = eyeTracker.PlayingVideo && !eyeTracker.PostProcessing;

            // Menu Configuration
            configurationToolStripMenuItem.Enabled = eyeTracker.Tracking || eyeTracker.NotStarted;

            // Record buttons
            if (eyeTracker.RecordingSession?.Stopping ?? false)
            {
                startStopRecordingToolStripMenuItem.Text = "Stop recording";
                buttonRecord.Text = "WAIT, closing files";
                buttonRecord.BackColor = Color.Salmon;
            }
            else if (eyeTracker.Recording)
            {
                startStopRecordingToolStripMenuItem.Text = "Stop recording";
                buttonRecord.Text = "Stop recording";
                buttonRecord.BackColor = Color.Salmon;
            }
            else
            {
                startStopRecordingToolStripMenuItem.Text = "Start recording";
                buttonRecord.Text = "Start recording";
                buttonRecord.BackColor = SystemColors.Control;
            }

            // Calibration

            if (eyeTrackerViewModel.CancelCalibrationCommand.CanExecute())
            {
                buttonCalibrate.Text = "Cancel calibration";
                buttonCalibrate.BackColor = Color.LightYellow;
            }
            else
            {
                buttonCalibrate.Text = "Calibrate";
                buttonCalibrate.BackColor = SystemColors.Control;
            }

            // Update calibration UI
            if (eyeTracker.Calibrating && !tabPages.Contains(tabCalibration))
            {
                if (eyeTracker.CalibrationSession?.CalibrationUI is UserControl calibrationControl)
                {
                    calibrationControl.Dock = DockStyle.Fill;
                    calibrationControl.Location = new Point(0, 0);
                    calibrationControl.Size = tabCalibration.ClientSize;
                    tabCalibration.Controls.Add(calibrationControl);
                    calibrationUI = eyeTracker.CalibrationSession?.CalibrationUI;

                    tabPages.TabPages.Add(tabCalibration);
                    tabPages.SelectTab(tabCalibration);
                }
            }

            if (!eyeTracker.Calibrating && tabPages.Contains(tabCalibration))
            {
                tabCalibration.Controls.Clear();
                tabPages.TabPages.Remove(tabCalibration);
                tabPages.SelectTab(0);
                calibrationUI = null;
            }

            if (eyeTracker.VideoPlayer != null)
            {
                videoPlayerUI.Update(eyeTracker.VideoPlayer);
            }

            if (eyeTracker.EyeTrackingSystem != null)
            {
                if (systemToolStripMenuItem.Tag == null || systemToolStripMenuItem.Tag != eyeTracker.EyeTrackingSystem)
                {
                    systemToolStripMenuItem.DropDownItems.Clear();
                    var items = eyeTracker.EyeTrackingSystem.GetToolStripMenuItems();
                    if (items != null)
                    {
                        systemToolStripMenuItem.DropDownItems.AddRange(items);
                        systemToolStripMenuItem.Visible = true;
                    }
                    else
                    {
                        systemToolStripMenuItem.Visible = false;
                    }
                    systemToolStripMenuItem.Tag = eyeTracker.EyeTrackingSystem;
                }
            }
            else
            {
                systemToolStripMenuItem.Visible = false;
            }
        }

        private void UpdateTabSetup()
        {
            var panels = new EyeCollection<Panel>(panelPipelineQuickSettingsLeftEye, panelPipelineQuickSettingsRightEye);

            try
            {
                tabSetup.SuspendLayout();

                panels[Eye.Left].SuspendLayout();
                panels[Eye.Right].SuspendLayout();

                if (!eyeTracker.Tracking) return;

                var settings = eyeTrackerViewModel.Settings;
                if (settings is null) return;

                var eyes = new Eye[] { Eye.Left, Eye.Right };

                // Update pipeline UIs
                foreach (var eye in eyes)
                {
                    var pipelineUI = eyeTracker.ImageProcessor?.PipelineUI?[eye];

                    if (pipelineUI is null) continue;

                    if (panels[eye].Controls.Contains(pipelineUI))
                    {
                        pipelineUI.UpdatePipelineUI(eyeTrackerViewModel.LastDataAndImages);
                        continue;
                    }

                    panels[eye].Controls.Clear();

                    pipelineUI.Dock = DockStyle.Fill;
                    pipelineUI.Location = new Point(0, 0);
                    pipelineUI.Size = panels[eye].ClientSize;
                    panels[eye].Controls.Add(pipelineUI);
                }

                // Update Eye Images
                var data = eyeTrackerViewModel.LastDataAndImages;
                foreach (var eye in eyes)
                {
                    if (data != null)
                    {
                        var imageBoxes = new EyeCollection<ImageBox>(imageBoxLeftEye, imageBoxRightEye);

                        var ui = eyeTracker.ImageProcessor?.PipelineUI?[eye];

                        if (ui is null)
                        {
                            EyeTrackingPipelineUI.UpdatePipelineEyeImage(eye, imageBoxes[eye], eyeTrackerViewModel.LastDataAndImages);
                        }
                        else
                        {
                            ui.UpdatePipelineEyeImage(imageBoxes[eye], eyeTrackerViewModel.LastDataAndImages);
                        }
                    }
                }

            }
            finally
            {
                tabSetup.ResumeLayout();
                panels[Eye.Left].ResumeLayout();
                panels[Eye.Right].ResumeLayout();
            }
        }

        private void UpdateTabCalibration()
        {
            calibrationUI?.UpdateUI();
        }

        private void UpdateTabViewer()
        {
            if (eyeTrackerViewModel.LastDataAndImages != null && eyeTrackerViewModel.LastDataAndImages.Images != null)
            {
                if (eyeTrackerViewModel.LastDataAndImages.Images[Eye.Left] != null)
                {
                    var imageLeft = eyeTrackerViewModel.LastDataAndImages.Images[Eye.Left]?.Image.Convert<Bgr, byte>();
                    eyeTrackerImageEyeBoxLeftEyeSmall.Image = imageLeft;
                }

                if (eyeTrackerViewModel.LastDataAndImages.Images[Eye.Right] != null)
                {
                    var imageRight = eyeTrackerViewModel.LastDataAndImages.Images[Eye.Right]?.Image.Convert<Bgr, byte>();
                    eyeTrackerImageEyeBoxRightEyeSmall.Image = imageRight;
                }
            }

            if (eyeTrackerViewModel.LastDataAndImages != null && eyeTrackerViewModel.LastDataAndImages.Data != null)
            {
                var headData = eyeTrackerViewModel.LastDataAndImages?.Data?.HeadDataCalibrated ?? new CalibratedHeadData();
                var headDataRaw = eyeTrackerViewModel.LastDataAndImages?.Data?.HeadDataRaw ?? new HeadData();
                labelHeadData.Text =
                      $"Roll:  {headData.Roll,-6:F1}\n" +
                      $"Pitch: {headData.Pitch,-6:F1}\n" +
                      $"Yaw:   {headData.Yaw,-6:F1}\n" +
                      $"MagX:  {headDataRaw.MagnetometerX:F2}\n" +
                      $"MagY:  {headDataRaw.MagnetometerY:F2}\n" +
                      $"MagZ:  {headDataRaw.MagnetometerZ:F2}\n";
                //$"X ACC: {headData.XAcceleration}\n" +
                //$"Y ACC: {headData.YAcceleration}\n" +
                //$"Z ACC: {headData.ZAcceleration}\n" +
                //$"Vel yaw: {headData.YawVelocity}\n" +
                //$"Vel pitchL: {headData.PitchVelocity}\n" +
                //$"Vel roll: {headData.RollVelocity}\n";
            }

            // Update Traces
            eyeTrackerTrace.Update(eyeTracker.DataBuffer);
        }

        private void UpdateTabDebug()
        {
            if (eyeTrackerViewModel.Settings?.Debug==true)
            {
                panel6.UpdateUI();
            }
        }

        private void UpdateTabTiming()
        {
            labelTiming.Text = EyeTrackerDebug.GetDeltaTimesText();
            labelDiagnosticsGrabbing.Text = eyeTracker.ImageGrabber?.CameraDebugInfo ?? string.Empty;
        }

        private void UpdateStatusBar()
        {
            toolStripStatusLabelImageGrabbingStatus.Text = (eyeTracker.ImageGrabber?.GrabbingStatus ?? "Not tracking") + " "
                + eyeTracker.HeadTracker?.Status ?? "[No head tracking]";

            var percentDropsInCamera = eyeTracker.ImageGrabber?.NumberFramesDropped / (eyeTracker.ImageGrabber?.NumberFramesGrabbed + eyeTracker.ImageGrabber?.NumberFramesDropped + 1.0) * 100.0 ?? 0.0;

            if (percentDropsInCamera < 5)
                toolStripStatusLabelImageGrabbingStatus.BackColor = SystemColors.Control;
            else if (percentDropsInCamera < 10)
                toolStripStatusLabelImageGrabbingStatus.BackColor = Color.Yellow;
            else if (percentDropsInCamera < 20)
                toolStripStatusLabelImageGrabbingStatus.BackColor = Color.Orange;
            else
                toolStripStatusLabelImageGrabbingStatus.BackColor = Color.Red;

            toolStripStatusLabelProcessingTimeLeftEye.Text = eyeTracker.ImageProcessor?.ProcessingStatus + string.Format(" {0:0.0}", eyeTracker.AverageFrameProcessingTime * 1000) ?? "Not tracking";
            var processingBuffersize = eyeTracker.ImageProcessor?.NumberFramesInBuffer ?? 0;
            if (processingBuffersize < 10)
                toolStripStatusLabelProcessingTimeLeftEye.BackColor = SystemColors.Control;
            else if (processingBuffersize < 30)
                toolStripStatusLabelProcessingTimeLeftEye.BackColor = Color.Yellow;
            else if (processingBuffersize < 50)
                toolStripStatusLabelProcessingTimeLeftEye.BackColor = Color.Orange;
            else
                toolStripStatusLabelProcessingTimeLeftEye.BackColor = Color.Red;


            var recording = eyeTracker.RecordingSession;
            var percentDropsInRecording = (recording?.NumberFramesDroppedInDataFile + recording?.NumberFramesDroppedInVideo) / (
                recording?.NumberFramesRecordedInDataFile +
                recording?.NumberFramesRecordedInVideo +
                recording?.NumberFramesDroppedInDataFile +
                recording?.NumberFramesDroppedInVideo + 1.0) * 100.0 ?? 0.0;
            toolStripStatusLabelRecording.Text = recording?.RecordingStatus ?? "Not recording";

            if (percentDropsInRecording < 1)
                toolStripStatusLabelRecording.BackColor = SystemColors.Control;
            else if (percentDropsInRecording < 2)
                toolStripStatusLabelRecording.BackColor = Color.Yellow;
            else if (percentDropsInRecording < 3)
                toolStripStatusLabelRecording.BackColor = Color.Orange;
            else
                toolStripStatusLabelRecording.BackColor = Color.Red;

            statusStrip1.Refresh();
        }

        private void TimerRefreshUI_Tick(object sender, EventArgs e)
        {
            var hasLock = false;

            try
            {
                // This will force the UI to wait until this drawing is done before closing
                System.Threading.Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    return;
                }

                SuspendLayout();

                // Update the enabled and disabled elements of the UI
                UpdateUI();
                
                UpdateStatusBar();

                switch (tabPages.SelectedTab.Name)
                {
                    case ("tabSetup"):
                        UpdateTabSetup();
                        break;

                    case ("tabCalibration"):
                        UpdateTabCalibration();
                        break;

                    case ("tabViewer"):
                        UpdateTabViewer();
                        break;

                    case ("tabDebug"):
                        UpdateTabDebug();
                        break;

                    case "tabTiming":
                        UpdateTabTiming();
                        break;

                    case "tabPageLog":
                        richTextBoxLogLarge.SuspendLayout();
                        richTextBoxLogLarge.ResumeLayout();
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error updating UI." + ex);
            }
            finally
            {
                ResumeLayout();

                if (hasLock)
                {
                    System.Threading.Monitor.Exit(_locker);
                }
            }
        }

        private void EyeTrackerGUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            var stillRecording = eyeTrackerViewModel.StopRecordingCommand.CanExecute();
            bool CanFinish = !stillRecording;

            try
            {
                if (!CanFinish)
                {
                    e.Cancel = true;
                    return;
                }

                Dispose(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GUI:Clossing: " + ex.ToString());
            }
        }
    }
}