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
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Camera calibration tool (Tools menu): the printed page of dot grids held where the eyes are,
    /// seen by both cameras while tracking, at one or more known positions along the head's forward
    /// axis, gives the intrinsics, the extrinsics and the pixels per mm of each camera, and a report.
    /// Each grid says which eye it is for, so swapped cameras are noticed. See CameraCalibrator and
    /// docs/user-manual/camera-calibration.md.
    /// </summary>
    public sealed class CameraCalibrationForm : Form
    {
        private const string TargetResource = "OpenIris.Resources.OpenIris-calibration-targets.pdf";

        private const string Instructions =
            "1. Print the target. Save the PDF and print it at 100% (Actual size, no fit to page) on a laser printer, on matte paper. " +
            "Inkjet ink can be invisible in infrared: check that the dots look black in the camera. Glue it flat on something rigid.\r\n\r\n" +
            "2. Measure the two 150 mm rulers of the print with calipers and enter what they measure below: printers scale a little, " +
            "and differently in x and y.\r\n\r\n" +
            "3. Hold the page where the eyes go: upright in front of the face, facing the cameras, level, the midline on the nose. " +
            "Each eye has its grid, 63 mm apart, with the center of the missing dots where the pupil would be (cut the grids apart " +
            "for another eye distance). Use one size for both eyes: A, or B if the image is blurry. The tool recognizes which grid it sees. " +
            "Enter how far behind the page the eye's center of rotation is (about 10 mm with the page where the pupils are).\r\n\r\n" +
            "4. Start tracking. Focus as for recordings. Found dots are green and the center of each grid is a red cross: it must be in view.\r\n\r\n" +
            "5. Capture at position 0. To measure the distance and the angles of the cameras, also move the page forward and back in known " +
            "steps (for example -1.5 and +1.5 mm, with a micrometer stage or spacers) while it stays in focus, entering each position " +
            "before capturing. Forward is away from the face, toward the cameras. With one position only, enter the distance from the page " +
            "to the lens: measure to its aperture (the iris ring), not to the front of the lens or the sensor.\r\n\r\n" +
            "6. Calculate. The errors should be a small fraction of a pixel. Apply the results to the settings or save the report, " +
            "then restart tracking and calibrate the eyes again.";

        private readonly EyeTracker eyeTracker;
        private readonly NumericUpDown rulerXBox;
        private readonly NumericUpDown rulerYBox;
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
        private readonly List<View> views = new List<View>();
        private readonly Dictionary<Eye, CameraCalibrationResult> results = new Dictionary<Eye, CameraCalibrationResult>();
        private readonly object sync = new object();

        // While capturing: the sum of the frames of each eye (null when not capturing).
        private Dictionary<Eye, Average>? averages;
        private int framesToAverage;
        private volatile bool previewBusy;
        private DateTime lastPreview;

        /// <summary>
        /// Opens the tool.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker: its images while tracking, and its settings.</param>
        public CameraCalibrationForm(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;

            // In pixels at 100% scaling (WindowsStyle.ScaleToScreen at the end).
            Text = "Camera calibration";
            Size = new Size(1200, 820);
            MinimumSize = new Size(900, 680);
            StartPosition = FormStartPosition.CenterScreen;

            // One column: the instructions take the height left (and scroll), everything else is
            // as tall as it needs, so it is always all visible.
            var left = new TableLayoutPanel { Dock = DockStyle.Left, Width = 440, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
            left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int i = 0; i < 3; i++) left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var instructions = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = Instructions,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(0, 60),
                BackColor = SystemColors.Window,
            };
            var savePdfButton = new Button { Text = "Save printable target (PDF)...", Dock = DockStyle.Fill };
            savePdfButton.Click += (o, e) => SaveTarget();
            var helpButton = new Button { Text = "More in the user manual", Dock = DockStyle.Fill };
            helpButton.Click += (o, e) => OpenManual();
            var topButtons = new TableLayoutPanel { ColumnCount = 2, Height = 36, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 3) };
            topButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topButtons.Controls.AddRange(new Control[] { savePdfButton, helpButton });

            rulerXBox = Number((decimal)CameraCalibrationTarget.RulerMm, 100, 200, 2, 0.05m);
            rulerYBox = Number((decimal)CameraCalibrationTarget.RulerMm, 100, 200, 2, 0.05m);
            eyeDepthBox = Number(10, 0, 30, 1, 0.5m);
            assumedDistanceBox = Number((decimal)CameraIntrinsics.AssumedDistanceToEyeMm, 10, 500, 0, 5);
            distortionBox = new CheckBox { Text = "Estimate radial distortion (k1)", Checked = true, AutoSize = true };

            var targetGroup = Group("Target and eyes", new (string, Control)[]
            {
                ("Horizontal 150 mm ruler measures (mm)", rulerXBox),
                ("Vertical 150 mm ruler measures (mm)", rulerYBox),
                ("Eye's center behind the page (mm)", eyeDepthBox),
                ("Distance if one position only (mm)", assumedDistanceBox),
                (string.Empty, distortionBox),
            });

            positionBox = Number(0, -20, 20, 2, 0.5m);
            framesBox = Number(20, 1, 500, 0, 5);
            captureButton = new Button { Text = "Capture", AutoSize = true };
            captureButton.Click += (o, e) => StartCapture();
            var removeButton = new Button { Text = "Remove", AutoSize = true };
            removeButton.Click += (o, e) => RemoveView();
            var clearButton = new Button { Text = "Clear", AutoSize = true };
            clearButton.Click += (o, e) => { views.Clear(); UpdateViews(); };
            var captureButtons = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            captureButtons.Controls.AddRange(new Control[] { captureButton, removeButton, clearButton });
            capturesList = new ListBox { Height = 100, IntegralHeight = false, Anchor = AnchorStyles.Left | AnchorStyles.Right, HorizontalScrollbar = true };

            var captureGroup = Group("Views (both cameras at once)", new (string, Control)[]
            {
                ("Page position, forward (mm)", positionBox),
                ("Frames to average", framesBox),
                (string.Empty, captureButtons),
                (string.Empty, capturesList),
            });

            calculateButton = new Button { Text = "Calculate", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Enabled = false };
            calculateButton.Click += (o, e) => Calculate();
            applyButton = new Button { Text = "Apply to settings...", AutoSize = true, Enabled = false };
            applyButton.Click += (o, e) => Apply();
            saveReportButton = new Button { Text = "Save report...", AutoSize = true, Enabled = false };
            saveReportButton.Click += (o, e) => SaveReport();
            var resultButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 0) };
            resultButtons.Controls.AddRange(new Control[] { calculateButton, applyButton, saveReportButton });

            left.Controls.Add(instructions, 0, 0);
            left.Controls.Add(topButtons, 0, 1);
            left.Controls.Add(targetGroup, 0, 2);
            left.Controls.Add(captureGroup, 0, 3);
            left.Controls.Add(resultButtons, 0, 4);

            imageView = new ImageView { Dock = DockStyle.Fill, BackColor = Color.Black };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 44, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
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
            Load += (o, e) =>
            {
                // Not bigger than the screen, at any scaling.
                var screen = Screen.FromControl(this).WorkingArea;
                Size = new Size(Math.Min(Width, screen.Width), Math.Min(Height, screen.Height));
                MinimumSize = new Size(Math.Min(MinimumSize.Width, screen.Width), Math.Min(MinimumSize.Height, screen.Height));
                CenterToScreen();
                right.SplitterDistance = (int)(right.Height * 0.62);
            };

            statusLabel.Text = eyeTracker.Tracking ? "Looking for the target..." : "Start tracking to see the cameras.";
            eyeTracker.NewDataAndImagesAvailable += EyeTracker_NewDataAndImagesAvailable;
            FormClosed += (o, e) =>
            {
                eyeTracker.NewDataAndImagesAvailable -= EyeTracker_NewDataAndImagesAvailable;
                lock (sync) DisposeAverages();
            };

            WindowsStyle.ScaleToScreen(this);
        }

        private static NumericUpDown Number(decimal value, decimal min, decimal max, int decimals, decimal increment) =>
            new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals, Increment = increment, Value = value, Width = 90 };

        private static GroupBox Group(string title, (string Label, Control Control)[] rows)
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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

            var group = new GroupBox { Text = title, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6, 3, 6, 3) };
            group.Controls.Add(table);
            return group;
        }

        private CameraCalibrationOptions Options() => new CameraCalibrationOptions
        {
            RulerXMm = (double)rulerXBox.Value,
            RulerYMm = (double)rulerYBox.Value,
            EyeCenterBehindTargetMm = (double)eyeDepthBox.Value,
            AssumedDistanceMm = (double)assumedDistanceBox.Value,
            EstimateDistortion = distortionBox.Checked,
        };

        // The images of the frame by eye, in the order they are shown: as seen facing the subject,
        // the right eye on the left.
        private static List<(Eye Eye, ImageEye Image)> ImagesOf(EyeTrackerImagesAndData imagesAndData)
        {
            var eyes = imagesAndData.Images.Count == 1 ? new[] { Eye.Both } : new[] { Eye.Right, Eye.Left };
            return eyes
                .Select(eye => (eye, imagesAndData.Images[eye]))
                .Where(x => x.Item2?.Image is not null)
                .Select(x => (x.eye, x.Item2!))
                .ToList();
        }

        // On the processing thread: add the frames to the averages while capturing, and copy them for
        // the preview a few times per second.
        private void EyeTracker_NewDataAndImagesAvailable(object? sender, EyeTrackerImagesAndData imagesAndData)
        {
            var images = ImagesOf(imagesAndData);
            if (images.Count == 0) return;

            lock (sync)
            {
                if (averages is not null)
                {
                    foreach (var (eye, image) in images)
                    {
                        if (!averages.TryGetValue(eye, out var average)) averages[eye] = average = new Average();
                        average.Add(image);
                    }

                    if (averages.Values.All(a => a.Frames >= framesToAverage))
                    {
                        var done = averages.ToDictionary(x => x.Key, x => x.Value.Result());
                        DisposeAverages();
                        BeginInvokeSafe(() => FinishCapture(done));
                    }
                }
            }

            if (previewBusy || DateTime.Now - lastPreview < TimeSpan.FromMilliseconds(200)) return;
            previewBusy = true;
            lastPreview = DateTime.Now;

            var copies = images.Select(x => (x.Eye, Image: x.Image.Image.Copy())).ToList();
            Task.Run(() =>
            {
                var found = copies.Select(x => (x.Eye, x.Image, Grid: DotGrid.Find(x.Image))).ToList();
                var overlay = SideBySide(found.Select(x => Overlay(x.Image, x.Grid)).ToList());
                var status = string.Join("\r\n", found.Select(x => Describe(x.Eye, x.Grid)));
                foreach (var x in copies) x.Image.Dispose();
                BeginInvokeSafe(() =>
                {
                    imageView.Image = overlay;
                    overlay.Dispose();
                    statusLabel.Text = status;
                });
                previewBusy = false;
            });
        }

        private void DisposeAverages()
        {
            foreach (var average in averages?.Values ?? Enumerable.Empty<Average>()) average.Dispose();
            averages = null;
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

        private static string EyeName(Eye eye) => eye == Eye.Both ? "Camera" : $"{eye} eye";

        // What's wrong with the grid an eye's camera sees, or null if it can be captured.
        private static string? Problem(Eye eye, DotGrid? grid)
        {
            if (grid is null) return "no target in view.";
            if (!grid.FiducialFound) return "the center of the grid (the missing dots) is not in view or not clear.";
            if (grid.Mirrored) return "the grid looks MIRRORED: check the camera orientation in the settings.";
            if (eye != Eye.Both && grid.Eye != eye) return $"it sees the grid of the {grid.Eye.ToString().ToLowerInvariant()} eye: are the cameras swapped?";
            return null;
        }

        private static string Describe(Eye eye, DotGrid? grid)
        {
            var text = EyeName(eye) + ": ";
            if (grid is not null) text += string.Format(CultureInfo.InvariantCulture, "{0} dots, {1:0.00} px between dots, ", grid.Count, grid.PitchPixels);
            if (grid?.Target is not null) text += $"grid {grid.Target.Name}, ";
            return text + (Problem(eye, grid) ?? "ready to capture.");
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

        // The images of the eyes next to each other in one image (disposes them).
        private static Image<Bgr, byte> SideBySide(List<Image<Bgr, byte>> images)
        {
            if (images.Count == 1) return images[0];

            const int Gap = 8;
            var result = new Image<Bgr, byte>(images.Sum(i => i.Width) + Gap * (images.Count - 1), images.Max(i => i.Height), new Bgr(Color.DimGray));
            var x = 0;
            foreach (var image in images)
            {
                result.ROI = new Rectangle(x, 0, image.Width, image.Height);
                image.CopyTo(result);
                x += image.Width + Gap;
                image.Dispose();
            }

            result.ROI = Rectangle.Empty;
            return result;
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
                DisposeAverages();
                averages = new Dictionary<Eye, Average>();
                framesToAverage = (int)framesBox.Value;
            }

            captureButton.Enabled = false;
            captureButton.Text = "Capturing...";
        }

        private void FinishCapture(Dictionary<Eye, (Image<Gray, byte> Image, CameraIntrinsics Intrinsics, int Frames)> done)
        {
            captureButton.Enabled = true;
            captureButton.Text = "Capture";

            var position = (double)positionBox.Value;
            var view = new View(position);
            var overlays = new List<Image<Bgr, byte>>();
            var eyes = done.Keys.OrderBy(e => e == Eye.Left).ToList(); // right eye first, as shown
            foreach (var eye in eyes)
            {
                var (image, intrinsics, frames) = done[eye];
                var grid = DotGrid.Find(image);
                overlays.Add(Overlay(image, grid));

                var problem = Problem(eye, grid);
                if (problem is null)
                {
                    view.Captures[eye] = new CameraCalibrationCapture(position, grid!, image.Size, intrinsics, frames);
                }
                else
                {
                    view.Problems[eye] = problem;
                }

                image.Dispose();
            }

            using (var overlay = SideBySide(overlays)) imageView.Image = overlay;

            if (view.Captures.Count == 0)
            {
                MessageBox.Show(this, "Not captured:\r\n" + string.Join("\r\n", view.Problems.Select(p => $"{EyeName(p.Key)}: {p.Value}")), Text);
                return;
            }

            views.Add(view);
            UpdateViews();
        }

        private void RemoveView()
        {
            if (capturesList.SelectedIndex < 0) return;
            views.RemoveAt(capturesList.SelectedIndex);
            UpdateViews();
        }

        private void UpdateViews()
        {
            capturesList.Items.Clear();
            foreach (var view in views)
            {
                var eyes = view.Captures.Keys.Concat(view.Problems.Keys).OrderBy(e => e == Eye.Left).Select(eye =>
                    view.Captures.TryGetValue(eye, out var c)
                        ? string.Format(CultureInfo.InvariantCulture, "{0} {1} dots", EyeName(eye).ToLowerInvariant(), c.Grid.Count)
                        : $"{EyeName(eye).ToLowerInvariant()} not found");
                capturesList.Items.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.00} mm: {1}", view.Position, string.Join(", ", eyes)));
            }

            calculateButton.Enabled = views.Count > 0;
        }

        private void Calculate()
        {
            var sources = eyeTracker.EyeTrackingSystem?.imageSources;
            if (sources is null)
            {
                MessageBox.Show(this, "Calculate while tracking with the cameras: the tool needs the intrinsics they have now.", Text);
                return;
            }

            results.Clear();
            var report = new StringBuilder();
            var eyes = views.SelectMany(v => v.Captures.Keys).Distinct().OrderBy(e => e == Eye.Left).ToList();
            foreach (var eye in eyes)
            {
                var source = sources.Count == 1 ? sources[Eye.Both] : sources[eye];
                if (source is not CameraEye camera)
                {
                    report.AppendLine($"{EyeName(eye)}: not a camera (a video?): the tool needs the intrinsics of the camera.").AppendLine();
                    continue;
                }

                try
                {
                    var title = $"{eyeTracker.Settings.EyeTrackerSystem}, {EyeName(eye).ToLowerInvariant()}" +
                        $"{(camera.Description.Length > 0 ? ", " + camera.Description : string.Empty)}, " +
                        $"sensor {camera.SensorSize.Width} x {camera.SensorSize.Height}, {camera.CameraOrientation}";
                    var captures = views.Where(v => v.Captures.ContainsKey(eye)).Select(v => v.Captures[eye]).ToList();
                    var result = CameraCalibrator.Calibrate(captures, camera.Intrinsics, Options(), eye, title);
                    results[eye] = result;
                    report.AppendLine(result.Report);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is CvException)
                {
                    report.AppendLine($"{EyeName(eye)}: could not calibrate: {ex.Message}").AppendLine();
                }
            }

            reportBox.Text = report.ToString();
            applyButton.Enabled = results.Count > 0;
            saveReportButton.Enabled = report.Length > 0;
        }

        private void Apply()
        {
            if (results.Count == 0) return;

            var settings = eyeTracker.Settings.EyeTrackingSystemSettings;
            // Empty in the settings (<CameraIntrinsics />) until set: count the cameras of the system.
            var cameras = new[] { eyeTracker.EyeTrackingSystem?.imageSources?.Count, settings.CameraIntrinsics?.Count, settings.CameraExtrinsics?.Count }
                .FirstOrDefault(n => n is 1 or 2) ?? (results.ContainsKey(Eye.Both) ? 1 : 2);
            var pixPerMm = results.Values.Average(r => r.PixPerMm);

            var message = new StringBuilder($"Set in the settings of {eyeTracker.Settings.EyeTrackerSystem}:\r\n\r\n");
            foreach (var x in results)
            {
                message.AppendLine($"{EyeName(x.Key)}:").AppendLine($"  CameraIntrinsics: {x.Value.Intrinsics}").AppendLine($"  CameraExtrinsics: {x.Value.Extrinsics}");
            }

            message.AppendLine(string.Format(CultureInfo.InvariantCulture, "PixPerMm: {0:0.000} ({1}, now {2:0.000})",
                pixPerMm, results.Count > 1 ? "the mean of the eyes, shared by both" : "shared by both eyes", settings.PixPerMm));
            message.AppendLine().Append("Restart tracking afterwards and calibrate the eyes again.");
            if (MessageBox.Show(this, message.ToString(), Text, MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            var intrinsics = settings.CameraIntrinsics;
            var extrinsics = settings.CameraExtrinsics;
            foreach (var x in results)
            {
                var key = cameras == 1 ? Eye.Both : x.Key;
                intrinsics = With(intrinsics, cameras, key, x.Value.Intrinsics);
                extrinsics = With(extrinsics, cameras, key, x.Value.Extrinsics);
            }

            settings.CameraIntrinsics = intrinsics;
            settings.CameraExtrinsics = extrinsics;
            settings.PixPerMm = pixPerMm;
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
            using var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = $"camera-calibration-{DateTime.Now:yyyyMMdd-HHmm}.txt",
                InitialDirectory = Directory.Exists(eyeTracker.Settings.DataFolder) ? eyeTracker.Settings.DataFolder : string.Empty,
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllText(dialog.FileName, reportBox.Text);
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

        /// <summary>
        /// One placement of the page: what each camera saw, or why it couldn't be used.
        /// </summary>
        private sealed class View
        {
            public View(double position) => Position = position;

            public double Position { get; }

            public Dictionary<Eye, CameraCalibrationCapture> Captures { get; } = new Dictionary<Eye, CameraCalibrationCapture>();

            public Dictionary<Eye, string> Problems { get; } = new Dictionary<Eye, string>();
        }

        /// <summary>
        /// The average of the frames of one eye while capturing. A new window of the camera (size or
        /// intrinsics) starts it again.
        /// </summary>
        private sealed class Average : IDisposable
        {
            private Image<Gray, float>? sum;
            private CameraIntrinsics intrinsics;

            public int Frames { get; private set; }

            public void Add(ImageEye image)
            {
                if (sum is null || sum.Size != image.Size || intrinsics != image.Intrinsics)
                {
                    sum?.Dispose();
                    sum = new Image<Gray, float>(image.Size);
                    intrinsics = image.Intrinsics;
                    Frames = 0;
                }

                CvInvoke.Accumulate(image.Image, sum);
                Frames++;
            }

            public (Image<Gray, byte> Image, CameraIntrinsics Intrinsics, int Frames) Result() =>
                (sum!.ConvertScale<byte>(1.0 / Frames, 0), intrinsics, Frames);

            public void Dispose() => sum?.Dispose();
        }
    }
}
