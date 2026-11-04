//-----------------------------------------------------------------------
// <copyright file="EyeTrackerGui.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using Emgu.CV;
#nullable enable

    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.UI;
    using OpenIris;
    using OpenIris.ImageGrabbing;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime;
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
        private readonly Timer resizeLayoutTimer;
        private double imagesAspect;
        private readonly LogTraceListener log;

        private readonly EyeCollection<ImageView> imageBoxes;

        // Variables for keeping track of held buttons
        private readonly Timer moveCommandTimer;
        private readonly Timer holdTimer;
        private Button? pressedMoveButton;

        private (string name, ICalibrationUIControl? control)? calibrationUI;
        private (string name, EyeCollection<EyeTrackingPipelineBase?>? pipelines)? pipelineUI;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerGui class
        /// </summary>
        /// <param name="profile">Settings profile to start with. If null, the one used last.</param>
        public EyeTrackerGui(string? profile = null)
        {
            InitializeComponent();

            // The version, without the build metadata after the +: 0.2.0-alpha.4, for example.
            var version = (Attribute.GetCustomAttribute(typeof(EyeTrackerGui).Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute)) as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion;
            labelOpenIrisVersion.Text = "OpenIris " + version?.Split('+')[0];

            // Use the program's icon (OpenIris.ico) for the window and the taskbar too.
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);

            // Initialize button hold timers
            moveCommandTimer = new Timer { Interval = 100 };
            moveCommandTimer.Tick += MoveCommandTimer_Tick;

            holdTimer = new Timer { Interval = 500 };
            holdTimer.Tick += HoldTimer_Tick;

            imageBoxes = new EyeCollection<ImageView>(imageBoxLeftEye, imageBoxRightEye);
            splitContainer2.Resize += (o, e) => FitImagesHeight();
            imageBoxRightEye.SizeChanged += (o, e) => FitImagesHeight();

            log = new LogTraceListener(richTextBox1, richTextBoxLogLarge);

            try
            {
                eyeTracker = EyeTracker.Create(profile);

            }
            catch(Exception ex)
            {
                MessageBox.Show(
                              "Error:" + ex.Message,
                              "Error starting the system. See log file.",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            // Check if settings changed need restarting
            eyeTracker.Settings.PropertyChangingNeedsRestart += (o, e) =>
                {
                    if (!eyeTracker.NotStarted)
                    {
                        DialogResult result = MessageBox.Show(
                              "Changing the setting " + e.PropertyName + " requires to stop the tracking.",
                              "Stopping tracking.",
                              MessageBoxButtons.OK);
                        
                        eyeTracker.StopTracking();
                    }
                };

            eyeTracker.Settings.PropertyChanged += EyeTracker_Settings_PropertyChanged;

            eyeTrackerUICommands = new EyeTrackerUICommands(eyeTracker);

            timerRefreshUI = new Timer(components)
            {
                Interval = 100,
            };
            timerRefreshUI.Tick += TimerRefreshUI_Tick;

            // While the window is being resized, lay it out now and then, not at every mouse move:
            // each layout moves ~130 controls and can take 0.2-0.8 s, and the window did not follow
            // the mouse. After each layout, wait three times as long as it took: the window frame
            // always gets most of the time.
            resizeLayoutTimer = new Timer(components) { Interval = 150 };
            resizeLayoutTimer.Tick += (o, e) =>
            {
                var layoutTime = Stopwatch.StartNew();
                ResumeLayout(true);
                SuspendLayout();
                resizeLayoutTimer.Interval = Math.Max(150, (int)(3 * layoutTime.ElapsedMilliseconds));
            };

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

            UpdateTitle();
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
                eyeTrackerUICommands.EditSettingsCommand.Bind(buttonManageProfiles);

                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeUp);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeDown);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeRight);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveRightEyeLeft);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeUp);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeDown);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeRight);
                //eyeTrackerUICommands.MoveCamerasCommand.Bind(buttonMoveLeftEyeLeft);

                var moveButtons = new[]
                {
                    buttonMoveRightEyeUp, buttonMoveRightEyeDown, buttonMoveRightEyeRight, buttonMoveRightEyeLeft,
                    buttonMoveLeftEyeUp, buttonMoveLeftEyeDown, buttonMoveLeftEyeRight, buttonMoveLeftEyeLeft
                };

                foreach (var button in moveButtons)
                {
                    button.MouseDown += MoveButton_MouseDown;
                    button.MouseUp += MoveButton_MouseUp;
                }

                eyeTrackerUICommands.IncreaseExposureCommand.Bind(buttonIncreaseExposure);
                eyeTrackerUICommands.ReduceExposureCommand.Bind(buttonReduceExposure);


                eyeTrackerUICommands.CenterCamerasCommand.Bind(buttonCenterEyes);

                eyeTrackerUICommands.ChangeDataFolderCommand.Bind(buttonSelectFolder);

                eyeTrackerUICommands.TrimVideosCommand.Bind(trimVideosToolStripMenuItem);
                eyeTrackerUICommands.LabelVideosCommand.Bind(labelVideosToolStripMenuItem);
                eyeTrackerUICommands.CameraCalibrationCommand.Bind(cameraCalibrationToolStripMenuItem);
                eyeTrackerUICommands.ConvertVideoToRGBCommand.Bind(convertVideoToRGBToolStripMenuItem);
                eyeTrackerUICommands.ConvertVideoToMp4.Bind(convertVideoToMP4ToolStripMenuItem); 

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

                openLogFolderToolStripMenuItem.Click += (o, e) => Process.Start("explorer.exe", OpenIrisFolders.Logs);
                showIPNetworkInfoToolStripMenuItem.Click += (o, e) => MessageBox.Show(EyeTrackerRemoteServices.GetIPAddresses());
                aboutToolStripMenuItem.Click += (o, e) => new EyeTrackerAboutBox().ShowDialog();

                KeyPreview = true;
                KeyPress += (o, e) => eyeTracker.RecordEvent("KEYPRESS", e.KeyChar);

                //
                // Profile combo box: switches between the settings profiles, each with its eye
                // tracking system, pipeline and settings (managed in the configuration window).
                //
                UpdateProfileComboBox();
                profileComboBox.SelectionChangeCommitted += async (o, e) =>
                {
                    if (!(profileComboBox.SelectedItem is string profile) || profile == eyeTracker.Settings.Profile) return;

                    // While tracking, it asks to stop first.
                    await ProfileSwitcher.SwitchAsync(this, eyeTracker, profile);
                    UpdateProfileComboBox();
                };


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
                // Update what tabs are visible

                tabPages.SuspendLayout();

                if (eyeTracker.Tracking is false)
                {
                    if (tabPages.TabPages.Contains(tabSetup))
                    {
                        tabPages.TabPages.Clear();

                        tabPages.TabPages.Add(tabPageStart);
                        tabPages.TabPages.Add(tabViewer);
                        tabPages.TabPages.Add(tabPageLog);

                        tabPages.SelectedTab = tabPageStart;
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

            // Menu Configuration
            configurationToolStripMenuItem.Enabled = eyeTracker.Tracking || eyeTracker.NotStarted;

            // Player
            panelPlayer.Visible = eyeTracker.PlayingVideo;
            panelPlayer.Enabled = eyeTracker.PlayingVideo && !eyeTracker.PostProcessing;
            if (eyeTracker.VideoPlayer != null)
            {
                videoPlayerUI.Update(eyeTracker.VideoPlayer);
            }

            // Eye tracking system menu items
            if (eyeTracker.EyeTrackingSystem != null)
            {
                if (systemToolStripMenuItem.Tag == null || systemToolStripMenuItem.Tag != eyeTracker.EyeTrackingSystem)
                {
                    systemToolStripMenuItem.DropDownItems.Clear();
                    var items = PluginViews.CreateMenuItems(eyeTracker.EyeTrackingSystem);
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

        /// <summary>
        /// Shows the settings profile in the title of the window.
        /// </summary>
        private void UpdateTitle() => Text = $"OpenIris - {eyeTracker.Settings.Profile}";

        /// <summary>
        /// Shows the profiles, with the active one selected, and the eye tracking system of the active one.
        /// </summary>
        private void UpdateProfileComboBox()
        {
            var settings = eyeTracker.Settings;
            var profiles = settings.Profiles;
            if (!profileComboBox.Items.Cast<string>().SequenceEqual(profiles))
            {
                profileComboBox.Items.Clear();
                profileComboBox.Items.AddRange(profiles);
            }

            profileComboBox.SelectedItem = settings.Profile;
            labelProfileSystem.Text = $"System: {settings.EyeTrackerSystem}\nCalibration: {settings.CalibrationMethod}\nPipeline: {settings.EyeTrackingPipeline}";
        }

        private void EyeTracker_Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EyeTrackerSettings.Profile))
            {
                BeginInvoke(new Action(UpdateTitle));
            }

            if (e.PropertyName == nameof(EyeTrackerSettings.Profile) || e.PropertyName == nameof(EyeTrackerSettings.Profiles)
                || e.PropertyName == nameof(EyeTrackerSettings.EyeTrackerSystem))
            {
                BeginInvoke(new Action(UpdateProfileComboBox));
            }

            // Check if the property that changed was SessionName
            if (e.PropertyName == nameof(EyeTrackerSettings.SessionName))
            {
                // Use Invoke to safely update the UI from a different thread
                if (textBoxSession.InvokeRequired)
                {
                    textBoxSession.Invoke(new Action(() =>
                    {
                        textBoxSession.Text = eyeTracker.Settings.SessionName;
                    }));
                }
                else
                {
                    textBoxSession.Text = eyeTracker.Settings.SessionName;
                }
            }
        }

        private void UpdateTabStart()
        {
            if (eyeTracker.Tracking is false)
            {
                pipelineUI = null;
            }
        }

        private void UpdateTabSetup()
        {
            var panels = new EyeCollection<Panel>(panelPipelineQuickSettingsLeftEye, panelPipelineQuickSettingsRightEye);

            try
            {
                tabSetup.SuspendLayout();

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

                // Pipeline quick settings
                panels[Eye.Left].SuspendLayout();
                panels[Eye.Right].SuspendLayout();

                if (!eyeTracker.Tracking) return;

                var settings = eyeTracker.Settings.TrackingPipelineSettings;
                if (settings is null) return;

                var eyes = new Eye[] { Eye.Left, Eye.Right };

                // Change the pipeline UIs if necessary. The processor creates the pipelines with the
                // first frame: wait for them, or the quick settings would never show.
                var pipelinesForUI = eyeTracker.ImageProcessor?.PipelineForUI;
                if (pipelinesForUI != null &&
                    (pipelineUI?.name != eyeTracker.Settings.EyeTrackingPipeline || pipelineUI?.pipelines != pipelinesForUI))
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

                    pipelineUI = (eyeTracker.Settings.EyeTrackingPipeline, pipelinesForUI);

                    foreach (var eye in eyes)
                    {
                        // Check if we are doing this eye
                        if (eye != eyeTracker.Settings.EyeTrackingSystemSettings.Eye &&
                            Eye.Both != eyeTracker.Settings.EyeTrackingSystemSettings.Eye)
                            continue;

                        var settingsList = pipelineUI?.pipelines?[eye]?.GetQuickSettingsList();
                        if (settingsList is null) continue;

                        var table = new TableLayoutPanel
                        {
                            RowCount = settingsList.Count,
                            ColumnCount = 1,
                            Height = settingsList.Count * WindowsStyle.Scale(40),
                            Dock = DockStyle.Top
                        };
                        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                        for (int i = 0; i < settingsList.Count; i++)
                        {
                            table.RowStyles.Add(new RowStyle(SizeType.Absolute, WindowsStyle.Scale(40)));

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

                // Eye images, with the iris images below (current, then reference): one image that
                // scales together.
                foreach (var eye in eyes)
                {
                    if (eyeTracker.LastImagesAndData is null) continue;

                    var eyeImage = pipelineUI?.pipelines?[eye]?.UpdatePipelineEyeImage(eye, eyeTracker.LastImagesAndData);
                    using var irisReference = Transposed(eyeTracker.LastImagesAndData.Calibration.EyeCalibrationParameters[eye]?.ImageTorsionReference);
                    using var iris = Transposed(eyeTracker.LastImagesAndData.Images[eye]?.ImageTorsion);
                    using var combined = StackImages(eyeImage, iris, irisReference);
                    imageBoxes[eye].Image = combined;

                    if (combined != null && Math.Abs((double)combined.Height / combined.Width - imagesAspect) > 0.005)
                    {
                        imagesAspect = (double)combined.Height / combined.Width;
                        FitImagesHeight();
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

        /// <summary>
        /// The iris image, horizontal, or null if there is none.
        /// </summary>
        private static Image<Gray, byte>? Transposed(Image<Gray, byte>? image)
        {
            if (image is null || image.Size.Width <= 4) return null;

            var transposed = new Image<Gray, byte>(image.Size.Height, image.Size.Width);
            CvInvoke.Transpose(image, transposed);
            return transposed;
        }

        /// <summary>
        /// The eye image with the other images below it, at its width, separated by a thin line of
        /// the window color. Each image below gets 1/9 of the width in height (half the proportions
        /// of the default iris image, 360x80), and the space stays empty when it is missing: the
        /// layout does not change when, for example, the iris reference appears.
        /// </summary>
        private static Mat? StackImages(IInputArray? top, params IInputArray?[] below)
        {
            if (top is null) return null;

            using var parts = new Emgu.CV.Util.VectorOfMat();
            var temporary = new List<Mat>();
            try
            {
                Mat ToBgr(Mat image)
                {
                    if (image.NumberOfChannels == 3) return image;
                    var color = new Mat();
                    CvInvoke.CvtColor(image, color, image.NumberOfChannels == 1 ? ColorConversion.Gray2Bgr : ColorConversion.Bgra2Bgr);
                    temporary.Add(color);
                    return color;
                }

                using var topArray = top.GetInputArray();
                var first = ToBgr(topArray.GetMat());
                var width = first.Width;
                var background = SystemColors.Control;
                var gapColor = new MCvScalar(background.B, background.G, background.R);
                var gapHeight = Math.Max(2, width / 100);
                var height = Math.Max(1, width / 9);
                parts.Push(first);

                Mat Blank(int rows)
                {
                    var blank = new Mat(rows, width, DepthType.Cv8U, 3);
                    blank.SetTo(gapColor);
                    temporary.Add(blank);
                    return blank;
                }

                foreach (var image in below)
                {
                    parts.Push(Blank(gapHeight));

                    using var array = image?.GetInputArray();
                    var source = array?.GetMat();
                    if (source is null || source.IsEmpty)
                    {
                        parts.Push(Blank(height));
                        continue;
                    }

                    var resized = new Mat();
                    temporary.Add(resized);
                    CvInvoke.Resize(source, resized, new Size(width, height), 0, 0, Inter.Linear);
                    parts.Push(ToBgr(resized));
                }

                var stacked = new Mat();
                CvInvoke.VConcat(parts, stacked);
                return stacked;
            }
            finally
            {
                foreach (var mat in temporary) mat.Dispose();
            }
        }

        /// <summary>
        /// Gives the eye images (with the iris images below) the height they need at their width; the
        /// quick settings get the rest of the window.
        /// </summary>
        private void FitImagesHeight()
        {
            if (imagesAspect <= 0) return;

            var box = imageBoxRightEye; // Both columns are the same width.
            var around = splitContainer2.Panel1.Height - box.Height; // Margins.
            var wanted = (int)Math.Ceiling(box.Width * imagesAspect) + around;
            var largest = splitContainer2.Height - splitContainer2.Panel2MinSize - splitContainer2.SplitterWidth;
            var distance = Math.Max(splitContainer2.Panel1MinSize, Math.Min(largest, wanted));
            if (distance > 0 && distance != splitContainer2.SplitterDistance) splitContainer2.SplitterDistance = distance;
        }

        private void UpdateTabCalibration()
        {
            // Update calibration UI
            if (eyeTracker.Calibrating && !tabPages.Contains(tabCalibration))
            {
                // Calibration just started

                calibrationUI = (eyeTracker.Settings.CalibrationMethod, PluginViews.CreateCalibrationPanel(eyeTracker.CalibrationSession));

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
                // Disposed, so what the panel opened (the targets of N-point's Local mode) closes too.
                foreach (var control in tabCalibration.Controls.Cast<Control>().ToArray()) control.Dispose();
                tabCalibration.Controls.Clear();
                tabPages.TabPages.Remove(tabCalibration);
                tabPages.SelectTab(0);
                calibrationUI = (string.Empty, null);
            }

            calibrationUI?.control?.UpdateUI();
        }

        private void UpdateTabViewer()
        {
            // Update images
            using var imageLeft = eyeTracker.LastImagesAndData?.Images[Eye.Left]?.Image.Convert<Bgr, byte>();
            using var imageRight = eyeTracker.LastImagesAndData?.Images[Eye.Right]?.Image.Convert<Bgr, byte>();
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
            toolStripStatusLabelImageGrabbingStatus.Text = (eyeTracker.ImageGrabber?.GrabbingStatus() ?? "Not tracking") + " "
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

        }
        private void MoveButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                pressedMoveButton = button;
                eyeTrackerUICommands.MoveCamerasCommand.Execute(pressedMoveButton);
                holdTimer.Start();
            }
        }

        private void MoveButton_MouseUp(object sender, MouseEventArgs e)
        {
            holdTimer.Stop();
            moveCommandTimer.Stop();
            pressedMoveButton = null;
        }

        private void HoldTimer_Tick(object sender, EventArgs e)
        {
            holdTimer.Stop();
            if (pressedMoveButton != null)
            {
                moveCommandTimer.Start();
            }
        }

        private void MoveCommandTimer_Tick(object sender, EventArgs e)
        {
            if (pressedMoveButton != null && eyeTrackerUICommands.MoveCamerasCommand.CanExecute(null))
            {
                eyeTrackerUICommands.MoveCamerasCommand.Execute(pressedMoveButton);
            }
            else
            {
                moveCommandTimer.Stop();
            }
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

                UpdateTabCalibration();

                switch (tabPages.SelectedTab.Name)
                {
                    case "tabStart":
                        UpdateTabStart();
                        break;

                    case ("tabSetup"):
                        UpdateTabSetup();
                        break;

                    case ("tabCalibration"):
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

        /// <inheritdoc/>
        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            SuspendLayout();
            resizeLayoutTimer.Interval = 150;
            resizeLayoutTimer.Start();
        }

        /// <inheritdoc/>
        protected override void OnResizeEnd(EventArgs e)
        {
            resizeLayoutTimer.Stop();
            ResumeLayout(true);
            base.OnResizeEnd(e);
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

