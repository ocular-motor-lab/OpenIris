//-----------------------------------------------------------------------
// <copyright file="EyeTrackerGui.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using Emgu.CV;
#nullable enable

    using Emgu.CV.Structure;
    using Emgu.CV.UI;
    using OpenIris;
    using OpenIris.ImageGrabbing;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    /// Main graphical user interface of the eye tracker
    /// </summary>
    public partial class EyeTrackerGui : Form
    {
        private static object _locker = new object();

        private readonly EyeTracker eyeTracker;
        private readonly EyeTrackerUICommands eyeTrackerUICommands;
        private readonly Timer timerRefreshUI;
        private readonly LogTraceListener log;

        private readonly EyeCollection<ImageBox> imageBoxes;

        private (string name, ICalibrationUIControl? control)? calibrationUI;
        private (string name, EyeCollection<IEyeTrackingPipeline?>? pipelines)? pipelineUI;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerGui class
        /// </summary>
        public EyeTrackerGui()
        {
            InitializeComponent();

            imageBoxes = new EyeCollection<ImageBox>(imageBoxLeftEye, imageBoxRightEye);

            log = new LogTraceListener(richTextBox1, richTextBoxLogLarge);

            Exception? ex;

            (eyeTracker, ex) = EyeTracker.Start();

            if (ex is PluginManagerException) MessageBox.Show("Error initializing, trying safe mode (no plugins). " + ex.Message);

            // Check if settings changed need restarting
            eyeTracker.Settings.PropertyChanged += (o, e) =>
                {
                    var needsRestartingAttributes = o.GetType().GetProperty(e.PropertyName)?.
                        GetCustomAttributes(typeof(NeedsRestartingAttribute), false) as NeedsRestartingAttribute[];

                    bool needsRestarting = needsRestartingAttributes?.Select(x => x.Value).SingleOrDefault() ?? false;

                    if (needsRestarting && !eyeTracker.NotStarted)
                    {
                        DialogResult result = MessageBox.Show(
                              "Changing the setting " + e.PropertyName + " requires to stop the tracking. Do you want to stop?",
                              "Do you want to stop?",
                              MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            eyeTracker.StopTracking();
                            return;
                        }
                    }
                };

            eyeTrackerUICommands = new EyeTrackerUICommands(eyeTracker);

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
            var settings = eyeTracker.Settings;
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

                eyeTrackerUICommands.PlayVideoCommand.Bind(playVideoToolStripMenuItem);
                eyeTrackerUICommands.PlayVideoCommand.Bind(buttonPlayVideo);
                eyeTrackerUICommands.ProcessVideoCommand.Bind(processVideoToolStripMenuItem);
                eyeTrackerUICommands.ProcessVideoCommand.Bind(buttonProcessVideo);
                eyeTrackerUICommands.BatchProcessVideoCommand.Bind(batchAnalysisToolStripMenuItem);

                eyeTrackerUICommands.StopCommand.Bind(stopToolStripMenuItem);

                eyeTrackerUICommands.StartCalibrationCommand.Bind(calibrateToolStripMenuItem);
                eyeTrackerUICommands.CancelCalibrationCommand.Bind(cancelCalibrationToolStripMenuItem);
                eyeTrackerUICommands.StartCancelCalibrationCommand.Bind(buttonCalibrate);
                eyeTrackerUICommands.LoadCalibrationCommand.Bind(loadCalibrationToolStripMenuItem);
                eyeTrackerUICommands.SaveCalibrationCommand.Bind(saveCalibrationToolStripMenuItem);
                eyeTrackerUICommands.ResetReferenceCommand.Bind(buttonResetReference);
                eyeTrackerUICommands.ResetReferenceCommand.Bind(resetReferenceToolStripMenuItem);
                eyeTrackerUICommands.ResetCalibrationCommand.Bind(resetCalibrationToolStripMenuItem);

                eyeTrackerUICommands.StartStopRecordingCommand.Bind(startStopRecordingToolStripMenuItem);
                eyeTrackerUICommands.StartStopRecordingCommand.Bind(buttonRecord);

                eyeTrackerUICommands.StartTrackingCommand.Bind(startTrackingToolStripMenuItem);
                eyeTrackerUICommands.StartTrackingCommand.Bind(buttonStartTracking);

                eyeTrackerUICommands.EditSettingsCommand.Bind(configurationToolStripMenuItem);

                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeUp);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeDown);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeRight);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeLeft);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeUp);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeDown);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeRight);
                eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeLeft);

                eyeTrackerUICommands.CenterCamerasCommand.Bind(buttonCenterEyes);

                eyeTrackerUICommands.ChangeDataFolderCommand.Bind(buttonSelectFolder);

                eyeTrackerUICommands.TrimVideosCommand.Bind(trimVideosToolStripMenuItem);
                eyeTrackerUICommands.ConvertVideoToRGBCommand.Bind(convertVideoToRGBToolStripMenuItem);

                //
                // Data Bindings with settings
                //
                textBoxSession.DataBindings.Add(nameof(textBoxSession.Text), eyeTracker.Settings, nameof(eyeTracker.Settings.SessionName));
                linkLabelDataFolder.DataBindings.Add(nameof(linkLabelDataFolder.Text), eyeTracker.Settings, nameof(eyeTracker.Settings.DataFolder));
                linkLabelDataFolder2.DataBindings.Add(nameof(linkLabelDataFolder2.Text), eyeTracker.Settings, nameof(eyeTracker.Settings.DataFolder));

                //
                // Other event handlers not worth a command
                //
                exitToolStripMenuItem.Click += (o, e) => Close();
                linkLabelDataFolder.Click += (o, e) => Process.Start("explorer.exe", settings.DataFolder);
                linkLabelDataFolder2.Click += (o, e) => Process.Start("explorer.exe", settings.DataFolder);

                openSoundRecorderToolStripMenuItem.Click += (o, e) => Process.Start("SoundRecorder.exe");
                openLogFolderToolStripMenuItem.Click += (o, e) => Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                showIPNetworkInfoToolStripMenuItem.Click += (o, e) => MessageBox.Show(EyeTrackerRemoteServices.GetIPAddresses());
                aboutToolStripMenuItem.Click += (o, e) => new EyeTrackerAboutBox().ShowDialog();

                KeyPreview = true;
                KeyPress += (o, e) => eyeTracker.RecordEvent("KEYPRESS", e.KeyChar);

                //
                // Update system combo box
                //
                var availableSystems = EyeTrackerPluginManager.EyeTrackingsyStemFactory.ClassesAvaiable.Select(x => x.Name).ToArray();
                Array.Sort(availableSystems);

                systemComboBox.DataSource = availableSystems;
                systemComboBox.SelectedIndex = systemComboBox.FindStringExact(settings.EyeTrackerSystem);
                settings.PropertyChanged += (o, e) => { 
                    if (e.PropertyName == nameof(settings.EyeTrackerSystem))
                        systemComboBox.SelectedIndex = systemComboBox.FindStringExact(settings.EyeTrackerSystem); };
                systemComboBox.SelectedIndexChanged += (o, e) => { settings.EyeTrackerSystem = availableSystems[systemComboBox.SelectedIndex]; };


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
            var settings = eyeTracker.Settings;
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
                        systemComboBox.SelectedIndex = systemComboBox.FindStringExact(settings.EyeTrackerSystem);

                        pipelineUI = null;
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

            if (eyeTrackerUICommands.CancelCalibrationCommand.CanExecute())
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
                // Calibration just started

                calibrationUI = (eyeTracker.Settings.CalibrationMethod, eyeTracker.CalibrationSession?.GetCalibrationUI());

                if (calibrationUI?.control is UserControl calibrationControl)
                {
                    calibrationControl.Dock = DockStyle.Fill;
                    calibrationControl.Location = new Point(0, 0);
                    calibrationControl.Size = tabCalibration.ClientSize;
                    tabCalibration.Controls.Add(calibrationControl);

                    tabPages.TabPages.Add(tabCalibration);
                    tabPages.SelectTab(tabCalibration);
                }
            }

            if (!eyeTracker.Calibrating && tabPages.Contains(tabCalibration))
            {
                tabCalibration.Controls.Clear();
                tabPages.TabPages.Remove(tabCalibration);
                tabPages.SelectTab(0);
                calibrationUI = (string.Empty, null);
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

                var settings = eyeTracker.Settings.TrackingPipelineSettings;
                if (settings is null) return;

                var eyes = new Eye[] { Eye.Left, Eye.Right };

                // Change the pipeline UIs if necessary
                if (pipelineUI?.name != eyeTracker.Settings.EyeTrackingPipeline)
                {
                    // Clear the two panels for quick settings
                    foreach (var c in panels[Eye.Left].Controls )
                    {
                        var table = c as TableLayoutPanel;
                        table?.Dispose();
                    }
                    foreach (var c in panels[Eye.Right].Controls)
                    {
                        var table = c as TableLayoutPanel;
                        table?.Dispose();
                    }
                    panels[Eye.Left].Controls.Clear();
                    panels[Eye.Right].Controls.Clear();
                    // end clear the quick settings

                    pipelineUI = eyeTracker.ImageProcessor?.PipelineForUI;

                    foreach (var eye in eyes)
                    {
                        // Check if we are doing this eye
                        if (eye != eyeTracker.Settings.EyeTrackingSystemSettings.Eye &&
                            Eye.Both != eyeTracker.Settings.EyeTrackingSystemSettings.Eye)
                            continue;

                        var settingsList = pipelineUI?.pipelines?[eye]?.GetQuickSettingsList(eye, settings);
                        if (settingsList is null) continue;

                        var table = new TableLayoutPanel
                        {
                            RowCount = settingsList.Count,
                            ColumnCount = 1,
                            Height = settingsList.Count * 55,
                            Dock = DockStyle.Fill
                        };
                        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                        for (int i = 0; i < settingsList.Count; i++)
                        {
                            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

                            var sliderPupil = new SliderTextControl
                            {
                                Text = settingsList[i].text,
                                Range = settingsList[i].range,
                                Dock = DockStyle.Fill,
                            };
                            sliderPupil.Bind(settings, settingsList[i].settingName);

                            table.Controls.Add(sliderPupil, i, 0);
                        }

                        panels[eye].Controls.Add(table);
                    }
                }

                // Update Torsion images
                var imageBoxIris = new EyeCollection<ImageBox>(imageBoxIrisLeft, imageBoxIrisRight);
                var imageBoxIrisReference = new EyeCollection<ImageBox>(imageBoxIrisRefeferenceLeft, imageBoxIrisRefeferenceRight);

                foreach (var eye in eyes)
                {
                    Image<Gray, byte>? imageTorsion = null;
                    Image<Gray, byte>? imageTorsionRef = null;

                    // Torsion image
                    var torsionImage = eyeTracker.LastImagesAndData?.Images[eye]?.ImageTorsion;
                    if (torsionImage?.Size.Width > 4)
                    {
                        imageTorsion = new Image<Gray, byte>(torsionImage.Size.Height, torsionImage.Size.Width);
                        CvInvoke.Transpose(torsionImage, imageTorsion);
                    }

                    // Torsion reference
                    var torsionRef = eyeTracker.LastImagesAndData?.Calibration.EyeCalibrationParameters[eye]?.ImageTorsionReference;
                    if (torsionRef?.Size.Width > 4)
                    {
                        imageTorsionRef = new Image<Gray, byte>(torsionRef.Size.Height, torsionRef.Size.Width);
                        CvInvoke.Transpose(torsionRef, imageTorsionRef);
                    }

                    imageBoxIris[eye].Image = imageTorsion;
                    imageBoxIrisReference[eye].Image = imageTorsionRef;
                }

                // Update pipeline UIs
                foreach (var eye in eyes)
                {
                    if (eyeTracker.LastImagesAndData != null)
                    {
                        imageBoxes[eye].Image = pipelineUI?.pipelines?[eye]?.UpdatePipelineEyeImage(eye, eyeTracker.LastImagesAndData);
                    }
                    else
                    {
                      //  imageBoxes[eye].Image = null;
                    }
                }
            }
            finally
            {
                tabSetup.ResumeLayout();
                foreach (var panel in panels)
                {
                    panel.ResumeLayout();
                }
            }
        }

        private void UpdateTabCalibration()
        {
            calibrationUI?.control?.UpdateUI();
        }

        private void UpdateTabViewer()
        {
            // Update images
            var imageLeft = eyeTracker.LastImagesAndData?.Images[Eye.Left]?.Image.Convert<Bgr, byte>();
            var imageRight = eyeTracker.LastImagesAndData?.Images[Eye.Left]?.Image.Convert<Bgr, byte>();
            if (imageLeft != null) eyeTrackerImageEyeBoxLeftEyeSmall.Image = imageLeft;
            if (imageRight != null) eyeTrackerImageEyeBoxRightEyeSmall.Image = imageRight;

            // Update Traces
            eyeTrackerTrace.Update(eyeTracker.DataBuffer);

            // Update Head text
            if (eyeTracker.LastImagesAndData?.Data != null)
            {
                var headData = eyeTracker.LastImagesAndData?.Data?.HeadDataCalibrated ?? new CalibratedHeadData();
                var headDataRaw = eyeTracker.LastImagesAndData?.Data?.HeadDataRaw ?? new HeadData();
                var headText =
                      $"Roll:  {headData.Roll,-6:F1}\n" +
                      $"Pitch: {headData.Pitch,-6:F1}\n" +
                      $"Yaw:   {headData.Yaw,-6:F1}\n" +
                      $"MagX:  {headDataRaw.MagnetometerX:F2}\n" +
                      $"MagY:  {headDataRaw.MagnetometerY:F2}\n" +
                      $"MagZ:  {headDataRaw.MagnetometerZ:F2}\n";

                labelHeadData.Text = (eyeTracker.HeadTracker is null) ? "" : headText;
            }
        }

        private void UpdateTabDebug()
        {
            if (eyeTracker.Settings.Debug==true)
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

            toolStripStatusLabelProcessingTimeLeftEye.Text = eyeTracker.ImageProcessor?.ProcessingStatus + string.Format(" {0:0.0}ms", eyeTracker.AverageFrameProcessingTime*1000) ?? "Not tracking";
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
            var stillRecording = eyeTrackerUICommands.StopRecordingCommand.CanExecute();
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