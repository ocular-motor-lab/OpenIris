//-----------------------------------------------------------------------
// <copyright file="CameraCalibrationForm.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Camera calibration tool (Tools menu): the printed dot grid where the eye sits, seen by the
    /// camera while tracking, at one or more known positions along the head's forward axis, gives the
    /// intrinsics, the extrinsics and the pixels per mm of the camera, and a report. See
    /// CameraCalibrator and docs/user-manual/camera-calibration.md.
    /// </summary>
    public sealed class CameraCalibrationForm : Form
    {
        private const string TargetResource = "OpenIris.Resources.OpenIris-calibration-targets.pdf";

        private const string Instructions =
            "1. Print the target. Save the PDF and print it at 100% (Actual size, no fit to page) on a laser printer, on matte paper. " +
            "Inkjet ink can be invisible in infrared: check that the dots look black in the camera. Glue the grid flat on something rigid.\r\n\r\n" +
            "2. Measure it. With calipers, measure the distance between the outer dots of the grid you use (30 mm as designed), " +
            "horizontally and vertically, and enter it below: printers scale a little, and differently in x and y.\r\n\r\n" +
            "3. Put it where the eye goes: upright in front of the face, printed side facing forward (toward the camera), rows level, " +
            "with the center of the missing dots where the pupil would be. Enter how far behind the target the eye's center of rotation " +
            "is (about 10 mm with the target where the pupil is).\r\n\r\n" +
            "4. Start tracking and pick the eye. Focus as for recordings. Found dots are green, the center of the target a red cross: " +
            "it must be in view.\r\n\r\n" +
            "5. Capture at position 0. To measure the distance and the angles of the camera, also move the target forward and back in known " +
            "steps (for example -1.5 and +1.5 mm, with a micrometer stage or spacers) while it stays in focus, entering each position " +
            "before capturing. Forward is away from the face, toward the camera.\r\n\r\n" +
            "6. Calculate. The error should be a small fraction of a pixel. Apply the results to the settings or save the report, " +
            "restart tracking and calibrate the eye again. Then the other eye.";

        private readonly EyeTracker eyeTracker;
        private readonly ComboBox eyeBox;
        private readonly ComboBox gridBox;
        private readonly NumericUpDown spanXBox;
        private readonly NumericUpDown spanYBox;
        private readonly NumericUpDown eyeDepthBox;
        private readonly NumericUpDown assumedDistanceBox;
        private readonly CheckBox distortionBox;
        private readonly NumericUpDown positionBox;
        private readonly NumericUpDown framesBox;
        private readonly Button captureButton;
        private readonly ListBox capturesList;
        private readonly Button calculateButton;
        private readonly Button applyButton;
        private readonly Button saveReportButton;
        private readonly ImageView imageView;
        private readonly Label statusLabel;
        private readonly TextBox reportBox;
        private readonly List<CameraCalibrationCapture> captures = new List<CameraCalibrationCapture>();
        private readonly object sync = new object();

        private Eye eye = Eye.Left;
        private volatile bool previewBusy;
        private DateTime lastPreview;
        private Image<Gray, float>? accumulator;
        private int accumulated;
        private int framesToAccumulate;
        private CameraIntrinsics accumulatorIntrinsics;
        private CameraCalibrationResult? result;
        private Eye resultEye;

        /// <summary>
        /// Opens the tool.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker: its images while tracking, and its settings.</param>
        public CameraCalibrationForm(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;

            Text = "Camera calibration";
            Size = new Size(1250, 900);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9);

            const int ColumnWidth = 420;
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, Width = ColumnWidth + 30, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8), AutoScroll = true };

            var instructions = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = Instructions,
                Height = 280,
                Width = ColumnWidth,
                BackColor = SystemColors.Window,
            };
            var savePdfButton = new Button { Text = "Save printable target (PDF)...", AutoSize = true };
            savePdfButton.Click += (o, e) => SaveTarget();
            var helpButton = new Button { Text = "More in the user manual", AutoSize = true };
            helpButton.Click += (o, e) => OpenManual();
            var topButtons = new FlowLayoutPanel { AutoSize = true };
            topButtons.Controls.AddRange(new Control[] { savePdfButton, helpButton });

            eyeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            eyeBox.Items.AddRange(new object[] { Eye.Left, Eye.Right });
            eyeBox.SelectedIndex = 0;
            eyeBox.SelectedIndexChanged += (o, e) => ChangeEye((Eye)eyeBox.SelectedItem);

            gridBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
            gridBox.Items.AddRange(CameraCalibrationTarget.Printed.Cast<object>().ToArray());
            gridBox.SelectedIndex = 0;

            spanXBox = Number(30, 1, 100, 2, 0.01m);
            spanYBox = Number(30, 1, 100, 2, 0.01m);
            eyeDepthBox = Number(10, 0, 30, 1, 0.5m);
            assumedDistanceBox = Number((decimal)CameraIntrinsics.AssumedDistanceToEyeMm, 10, 500, 0, 5);
            distortionBox = new CheckBox { Text = "Estimate radial distortion (k1)", Checked = true, AutoSize = true };

            var targetGroup = Group("Target and eye", ColumnWidth, new (string, Control)[]
            {
                ("Eye (camera)", eyeBox),
                ("Grid", gridBox),
                ("Measured outer dots, horizontal (mm)", spanXBox),
                ("Measured outer dots, vertical (mm)", spanYBox),
                ("Eye's center behind the target (mm)", eyeDepthBox),
                ("Distance if one position only (mm)", assumedDistanceBox),
                (string.Empty, distortionBox),
            });

            positionBox = Number(0, -20, 20, 2, 0.5m);
            framesBox = Number(20, 1, 500, 0, 5);
            captureButton = new Button { Text = "Capture", AutoSize = true };
            captureButton.Click += (o, e) => StartCapture();
            var removeButton = new Button { Text = "Remove", AutoSize = true };
            removeButton.Click += (o, e) => RemoveCapture();
            var clearButton = new Button { Text = "Clear", AutoSize = true };
            clearButton.Click += (o, e) => { captures.Clear(); UpdateCaptures(); };
            var captureButtons = new FlowLayoutPanel { AutoSize = true };
            captureButtons.Controls.AddRange(new Control[] { captureButton, removeButton, clearButton });
            capturesList = new ListBox { Height = 110, Width = ColumnWidth - 30, HorizontalScrollbar = true };

            var captureGroup = Group("Views", ColumnWidth, new (string, Control)[]
            {
                ("Target position, forward (mm)", positionBox),
                ("Frames to average", framesBox),
                (string.Empty, captureButtons),
                (string.Empty, capturesList),
            });

            calculateButton = new Button { Text = "Calculate", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            calculateButton.Click += (o, e) => Calculate();
            applyButton = new Button { Text = "Apply to settings...", AutoSize = true, Enabled = false };
            applyButton.Click += (o, e) => Apply();
            saveReportButton = new Button { Text = "Save report...", AutoSize = true, Enabled = false };
            saveReportButton.Click += (o, e) => SaveReport();
            var resultButtons = new FlowLayoutPanel { AutoSize = true };
            resultButtons.Controls.AddRange(new Control[] { calculateButton, applyButton, saveReportButton });

            left.Controls.Add(instructions);
            left.Controls.Add(topButtons);
            left.Controls.Add(targetGroup);
            left.Controls.Add(captureGroup);
            left.Controls.Add(resultButtons);

            imageView = new ImageView { Dock = DockStyle.Fill, BackColor = Color.Black };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            reportBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9),
                BackColor = SystemColors.Window,
            };

            var right = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            right.Panel1.Controls.Add(imageView);
            right.Panel1.Controls.Add(statusLabel);
            right.Panel2.Controls.Add(reportBox);

            Controls.Add(right);
            Controls.Add(left);
            Load += (o, e) => right.SplitterDistance = (int)(right.Height * 0.62);

            statusLabel.Text = eyeTracker.Tracking ? "Looking for the target..." : "Start tracking to see the cameras.";
            eyeTracker.NewDataAndImagesAvailable += EyeTracker_NewDataAndImagesAvailable;
            FormClosed += (o, e) =>
            {
                eyeTracker.NewDataAndImagesAvailable -= EyeTracker_NewDataAndImagesAvailable;
                lock (sync) accumulator?.Dispose();
            };
        }

        private static NumericUpDown Number(decimal value, decimal min, decimal max, int decimals, decimal increment) =>
            new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals, Increment = increment, Value = value, Width = 90 };

        private static GroupBox Group(string title, int width, (string Label, Control Control)[] rows)
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            foreach (var (label, control) in rows)
            {
                if (label.Length == 0)
                {
                    table.Controls.Add(control);
                    table.SetColumnSpan(control, 2);
                }
                else
                {
                    table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) });
                    table.Controls.Add(control);
                }
            }

            var group = new GroupBox { Text = title, Width = width, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, Padding = new Padding(6) };
            group.Controls.Add(table);
            return group;
        }

        private CameraCalibrationOptions Options() => new CameraCalibrationOptions
        {
            Target = (CameraCalibrationTarget)gridBox.SelectedItem,
            MeasuredSpanXMm = (double)spanXBox.Value,
            MeasuredSpanYMm = (double)spanYBox.Value,
            EyeCenterBehindTargetMm = (double)eyeDepthBox.Value,
            AssumedDistanceMm = (double)assumedDistanceBox.Value,
            EstimateDistortion = distortionBox.Checked,
        };

        private void ChangeEye(Eye newEye)
        {
            if (newEye == eye) return;
            if (captures.Count > 0 &&
                MessageBox.Show(this, "Changing the eye clears the views captured. Continue?", Text, MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                eyeBox.SelectedItem = eye;
                return;
            }

            lock (sync)
            {
                eye = newEye;
                accumulator?.Dispose();
                accumulator = null;
            }

            captures.Clear();
            UpdateCaptures();
        }

        // On the processing thread: copy the image of the eye for the preview (a few per second) or the capture.
        private void EyeTracker_NewDataAndImagesAvailable(object? sender, EyeTrackerImagesAndData imagesAndData)
        {
            Eye whichEye;
            lock (sync) whichEye = eye;

            var image = imagesAndData.Images.Count == 1 ? imagesAndData.Images[Eye.Both] : imagesAndData.Images[whichEye];
            if (image?.Image is null) return;

            lock (sync)
            {
                if (accumulator is not null)
                {
                    // A new window of the camera restarts the average.
                    if (accumulator.Size != image.Size || accumulatorIntrinsics != image.Intrinsics)
                    {
                        accumulator.Dispose();
                        accumulator = new Image<Gray, float>(image.Size);
                        accumulated = 0;
                        accumulatorIntrinsics = image.Intrinsics;
                    }

                    CvInvoke.Accumulate(image.Image, accumulator);
                    accumulated++;
                    if (accumulated >= framesToAccumulate)
                    {
                        var average = accumulator.ConvertScale<byte>(1.0 / accumulated, 0);
                        var frames = accumulated;
                        var intrinsics = accumulatorIntrinsics;
                        accumulator.Dispose();
                        accumulator = null;
                        BeginInvokeSafe(() => FinishCapture(average, intrinsics, frames));
                    }
                }
            }

            if (previewBusy || DateTime.Now - lastPreview < TimeSpan.FromMilliseconds(200)) return;
            previewBusy = true;
            lastPreview = DateTime.Now;

            var copy = image.Image.Copy();
            Task.Run(() =>
            {
                var grid = DotGrid.Find(copy);
                var overlay = Overlay(copy, grid);
                copy.Dispose();
                BeginInvokeSafe(() =>
                {
                    imageView.Image = overlay;
                    overlay.Dispose();
                    statusLabel.Text = Describe(grid);
                });
                previewBusy = false;
            });
        }

        private void BeginInvokeSafe(Action action)
        {
            try
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // Closing.
            }
        }

        private static string Describe(DotGrid? grid)
        {
            if (grid is null) return "No target in view.";
            var pixPerMm = grid.PitchPixels;
            var text = string.Format(CultureInfo.InvariantCulture, "{0} dots, {1:0.00} px between dots. ", grid.Count, pixPerMm);
            if (!grid.FiducialFound) return text + "The center of the target (the missing dots) is not in view or not clear.";
            if (grid.Mirrored) return text + "The target looks MIRRORED: check the camera orientation in the settings.";
            return text + "Ready to capture.";
        }

        private static Image<Bgr, byte> Overlay(Image<Gray, byte> image, DotGrid? grid)
        {
            var overlay = image.Convert<Bgr, byte>();
            if (grid is null) return overlay;

            var radius = Math.Max(2, (int)(grid.PitchPixels / 5));
            var color = grid.FiducialFound && !grid.Mirrored ? new Bgr(Color.LimeGreen) : new Bgr(Color.Orange);
            foreach (var p in grid.Positions)
            {
                overlay.Draw(new CircleF(p, radius), color, 1);
            }

            if (grid.FiducialFound)
            {
                var c = grid.Center;
                var size = (float)(grid.PitchPixels * 1.5);
                overlay.Draw(new LineSegment2DF(new PointF(c.X - size, c.Y), new PointF(c.X + size, c.Y)), new Bgr(Color.Red), 2);
                overlay.Draw(new LineSegment2DF(new PointF(c.X, c.Y - size), new PointF(c.X, c.Y + size)), new Bgr(Color.Red), 2);
            }

            return overlay;
        }

        private void StartCapture()
        {
            if (!eyeTracker.Tracking)
            {
                MessageBox.Show(this, "Start tracking first: the tool uses the images of the cameras.", Text);
                return;
            }

            lock (sync)
            {
                // Any size: the first frame sets the right one (a new window restarts the average).
                accumulator?.Dispose();
                accumulator = new Image<Gray, float>(1, 1);
                accumulated = 0;
                framesToAccumulate = (int)framesBox.Value;
            }

            captureButton.Enabled = false;
            captureButton.Text = "Capturing...";
        }

        private void FinishCapture(Image<Gray, byte> average, CameraIntrinsics intrinsics, int frames)
        {
            captureButton.Enabled = true;
            captureButton.Text = "Capture";

            var grid = DotGrid.Find(average);
            using (var overlay = Overlay(average, grid)) imageView.Image = overlay;
            var size = average.Size;
            average.Dispose();

            if (grid is null || !grid.FiducialFound || grid.Mirrored)
            {
                MessageBox.Show(this, "Not captured: " + Describe(grid), Text);
                return;
            }

            captures.Add(new CameraCalibrationCapture((double)positionBox.Value, grid, size, intrinsics, frames));
            UpdateCaptures();
        }

        private void RemoveCapture()
        {
            if (capturesList.SelectedIndex < 0) return;
            captures.RemoveAt(capturesList.SelectedIndex);
            UpdateCaptures();
        }

        private void UpdateCaptures()
        {
            capturesList.Items.Clear();
            foreach (var c in captures)
            {
                capturesList.Items.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.00} mm: {1} dots, {2:0.00} px between dots, {3} frames",
                    c.TargetPositionMm, c.Grid.Count, c.Grid.PitchPixels, c.Frames));
            }

            calculateButton.Enabled = captures.Count > 0;
        }

        private void Calculate()
        {
            var system = eyeTracker.EyeTrackingSystem;
            var sources = system?.imageSources;
            var source = sources is null ? null : sources.Count == 1 ? sources[Eye.Both] : sources[eye];
            if (source is not CameraEye camera)
            {
                MessageBox.Show(this, "Calculate while tracking with the cameras: the tool needs the intrinsics they have now.", Text);
                return;
            }

            try
            {
                var title = $"{eyeTracker.Settings.EyeTrackerSystem}, {eye} eye{(camera.Description.Length > 0 ? ", " + camera.Description : string.Empty)}, " +
                    $"sensor {camera.SensorSize.Width} x {camera.SensorSize.Height}, {camera.CameraOrientation}";
                result = CameraCalibrator.Calibrate(captures, camera.Intrinsics, Options(), title);
                resultEye = eye;
                reportBox.Text = result.Report;
                applyButton.Enabled = true;
                saveReportButton.Enabled = true;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is CvException)
            {
                MessageBox.Show(this, "Could not calibrate: " + ex.Message, Text);
            }
        }

        private void Apply()
        {
            if (result is null) return;

            var settings = eyeTracker.Settings.EyeTrackingSystemSettings;
            var cameras = settings.CameraIntrinsics?.Count ?? settings.CameraExtrinsics?.Count ?? (eyeTracker.EyeTrackingSystem?.imageSources?.Count ?? 2);
            var key = cameras == 1 ? Eye.Both : resultEye;

            var message = string.Format(CultureInfo.InvariantCulture,
                "Set, for the {0} camera of {1}:\r\n\r\nCameraIntrinsics: {2}\r\nCameraExtrinsics: {3}\r\nPixPerMm: {4:0.000} (shared by both eyes, now {5:0.000})\r\n\r\n" +
                "Restart tracking afterwards and calibrate the eye again.",
                key == Eye.Both ? "only" : resultEye.ToString().ToLowerInvariant(), eyeTracker.Settings.EyeTrackerSystem,
                result.Intrinsics, result.Extrinsics, result.PixPerMm, settings.PixPerMm);
            if (MessageBox.Show(this, message, Text, MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            settings.CameraIntrinsics = With(settings.CameraIntrinsics, cameras, key, result.Intrinsics);
            settings.CameraExtrinsics = With(settings.CameraExtrinsics, cameras, key, result.Extrinsics);
            settings.PixPerMm = result.PixPerMm;
            eyeTracker.Settings.Save();
        }

        // A new collection, so the settings notice the change.
        private static EyeCollection<T> With<T>(EyeCollection<T>? old, int cameras, Eye key, T value) where T : struct
        {
            var values = old is not null && old.Count == cameras ? old.ToArray() : new T[cameras];
            var copy = new EyeCollection<T>(values);
            copy[key] = value;
            return copy;
        }

        private void SaveReport()
        {
            if (result is null) return;

            using var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = $"camera-calibration-{resultEye.ToString().ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmm}.txt",
                InitialDirectory = Directory.Exists(eyeTracker.Settings.DataFolder) ? eyeTracker.Settings.DataFolder : string.Empty,
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllText(dialog.FileName, result.Report);
        }

        private void SaveTarget()
        {
            using var dialog = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = "OpenIris-calibration-targets.pdf" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            using (var resource = typeof(CameraCalibrationForm).Assembly.GetManifestResourceStream(TargetResource)
                ?? throw new InvalidOperationException("The target PDF is missing from OpenIrisWinForms."))
            using (var file = File.Create(dialog.FileName))
            {
                resource.CopyTo(file);
            }

            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }

        private static void OpenManual() =>
            Process.Start(new ProcessStartInfo("https://omlab.berkeley.edu/OpenIris/user-manual/camera-calibration/") { UseShellExecute = true });
    }
}
