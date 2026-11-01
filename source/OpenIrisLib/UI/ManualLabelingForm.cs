//-----------------------------------------------------------------------
// <copyright file="ManualLabelingForm.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;
    using Emgu.CV;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Tool to label videos by hand (Tools menu): the images of each eye, a list of markers, and
    /// controls to move through the frames. Click places the selected marker, right click removes the
    /// closest one. The labels are saved as you go next to the first video, VIDEO-labels.csv. It
    /// reads the videos itself: no tracking or recording is involved. It replaces the old
    /// SubjectiveTracker program.
    /// </summary>
    public sealed class ManualLabelingForm : Form
    {
        private static readonly Color[] Colors =
        {
            Color.Lime, Color.Cyan, Color.Magenta, Color.Yellow, Color.OrangeRed, Color.DeepSkyBlue, Color.White, Color.Orange, Color.SpringGreen,
        };

        private readonly EyeTrackerSettings settings;
        private readonly ManualLabels labels;
        private readonly EyeView[] views;
        private readonly long lastFrame;
        private readonly DataGridView markersGrid;
        private readonly NumericUpDown frameBox;
        private readonly NumericUpDown stepBox;
        private readonly CheckBox advanceBox;
        private readonly Button playButton;
        private readonly ToolStripStatusLabel frameLabel;
        private readonly ToolStripStatusLabel labeledLabel;
        private readonly Timer saveTimer;
        private readonly Timer playTimer;
        private long frame = -1;
        private bool updatingGrid;

        /// <summary>
        /// Opens the labeling window for the videos: one per eye (left and right), or one with both eyes.
        /// </summary>
        /// <param name="videoFiles">Video files: left and right (either can be missing), or one.</param>
        /// <param name="settings">Settings of OpenIris, where the markers are remembered.</param>
        public ManualLabelingForm(EyeCollection<string?> videoFiles, EyeTrackerSettings settings)
        {
            this.settings = settings;

            var files = videoFiles.Count == 1
                ? new[] { (Eye.Both, videoFiles[Eye.Both]) }
                : new[] { (Eye.Right, videoFiles[Eye.Right]), (Eye.Left, videoFiles[Eye.Left]) }; // Right eye on the left of the screen, as in OpenIris.
            views = files.Where(f => !string.IsNullOrEmpty(f.Item2)).Select(f => new EyeView(f.Item1, new VideoEye(f.Item1, f.Item2!), this)).ToArray();
            if (views.Length == 0) throw new ArgumentException("No video to label.");
            lastFrame = Math.Max(0, views.Min(v => v.Video.NumberOfFrames) - 1);

            var labelsVideo = videoFiles.Count == 1 ? videoFiles[Eye.Both] : videoFiles[Eye.Left] ?? videoFiles[Eye.Right];
            labels = ManualLabels.LoadForVideo(labelsVideo!);

            Text = "Label videos by hand - " + System.IO.Path.GetFileName(labels.FileName);
            Size = new Size(1300, 720);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            // Frame controls.
            var controls = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4), WrapContents = true };
            var tips = new ToolTip();
            Button AddButton(string text, string tip, Action action)
            {
                var button = new Button { Text = text, AutoSize = true, Margin = new Padding(2) };
                button.Click += (_, __) => action();
                tips.SetToolTip(button, tip);
                controls.Controls.Add(button);
                return button;
            }
            AddButton("|◀", "First frame", () => GoTo(0));
            AddButton("◀ labeled", "Previous labeled frame", () => GoToLabeled(-1));
            AddButton("◀", "Back one step (Left arrow)", () => Step(-1));
            playButton = AddButton("Play", "Play or pause (Space)", PlayPause);
            AddButton("▶", "Forward one step (Right arrow)", () => Step(1));
            AddButton("labeled ▶", "Next labeled frame", () => GoToLabeled(1));
            AddButton("▶|", "Last frame", () => GoTo(lastFrame));

            controls.Controls.Add(new Label { Text = "Frame", AutoSize = true, Margin = new Padding(12, 8, 2, 0) });
            frameBox = new NumericUpDown { Maximum = lastFrame, Width = 90 };
            frameBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { GoTo((long)frameBox.Value); e.SuppressKeyPress = true; } };
            controls.Controls.Add(frameBox);
            AddButton("Go", "Go to the frame", () => GoTo((long)frameBox.Value));

            controls.Controls.Add(new Label { Text = "Step", AutoSize = true, Margin = new Padding(12, 8, 2, 0) });
            stepBox = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = 1, Width = 60 };
            tips.SetToolTip(stepBox, "Frames to move with each step: 2 labels every other frame.");
            controls.Controls.Add(stepBox);

            advanceBox = new CheckBox { Text = "Step after each click", AutoSize = true, Margin = new Padding(12, 6, 2, 0) };
            controls.Controls.Add(advanceBox);
            AddButton("Clear frame", "Remove the labels of this frame (Delete)", () => labels.RemoveFrame((ulong)frame));

            // Markers: free names, remembered in the settings.
            markersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersWidth = 28,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            markersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Markers (type to add)" });
            markersGrid.CellValueChanged += (_, __) => SaveMarkers();
            markersGrid.UserDeletedRow += (_, __) => SaveMarkers();
            markersGrid.SelectionChanged += (_, __) => InvalidateViews();
            markersGrid.RowPrePaint += (_, e) => markersGrid.Rows[e.RowIndex].HeaderCell.Style.BackColor = Colors[e.RowIndex % Colors.Length];

            var help = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(4),
                Text = "Click: place the selected marker (it replaces the same marker on that eye and frame).\n" +
                       "Right click: remove the closest marker.\n1-9: select a marker.  Left/Right arrows: step.\n" +
                       "Space: play/pause.  Delete: clear the frame.",
            };
            var markersPanel = new Panel { Dock = DockStyle.Fill };
            markersPanel.Controls.Add(markersGrid);
            markersPanel.Controls.Add(help);

            // Eye images.
            var images = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = views.Length, RowCount = 1, BackColor = Color.Black };
            foreach (var view in views)
            {
                images.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / views.Length));
                images.Controls.Add(view);
            }

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(markersPanel);
            split.Panel2.Controls.Add(images);
            // Only once the window has its size: before, the splitter can't go there.
            Shown += (_, __) => split.SplitterDistance = 300;

            var status = new StatusStrip();
            frameLabel = new ToolStripStatusLabel();
            labeledLabel = new ToolStripStatusLabel();
            status.Items.Add(frameLabel);
            status.Items.Add(labeledLabel);
            status.Items.Add(new ToolStripStatusLabel("Labels: " + labels.FileName) { Spring = true, TextAlign = ContentAlignment.MiddleRight });

            Controls.Add(split);
            Controls.Add(controls);
            Controls.Add(status);

            LoadMarkers();

            // Save the labels a moment after they change, and when closing.
            saveTimer = new Timer { Interval = 1000 };
            saveTimer.Tick += (_, __) => { saveTimer.Stop(); SaveLabels(); };
            labels.Changed += (_, __) =>
            {
                saveTimer.Stop();
                saveTimer.Start();
                InvalidateViews();
                UpdateStatus();
            };

            var frameRate = views[0].Video.FrameRate;
            playTimer = new Timer { Interval = (int)Math.Max(15, 1000 / (frameRate > 0 ? frameRate : 30)) };
            playTimer.Tick += (_, __) => { if (frame >= lastFrame) PlayPause(); else ShowFrame(frame + 1); };

            FormClosed += (_, __) =>
            {
                playTimer.Stop();
                saveTimer.Stop();
                SaveLabels();
                foreach (var view in views) view.Video.Dispose();
            };

            ShowFrame(0);
        }

        #region Frames

        private void ShowFrame(long newFrame)
        {
            newFrame = Math.Max(0, Math.Min(lastFrame, newFrame));
            if (newFrame == frame) return;

            try
            {
                foreach (var view in views)
                {
                    view.Video.Scroll((ulong)newFrame);
                    using var image = view.Video.GrabImageEyeFromVideo();
                    view.SetImage(image.Image.ToBitmap());
                }
                frame = newFrame;
            }
            catch (InvalidOperationException ex)
            {
                // A frame that can't be read (the end of a damaged video).
                playTimer.Stop();
                Trace.WriteLine("ERROR reading the frame to label: " + ex.Message);
            }

            UpdateStatus();
        }

        private void GoTo(long newFrame)
        {
            playTimer.Stop();
            ShowFrame(newFrame);
        }

        private void Step(int direction) => GoTo(frame + direction * (long)stepBox.Value);

        private void GoToLabeled(int direction)
        {
            var frames = labels.LabeledFrames();
            var current = (ulong)frame;
            var target = direction > 0 ? frames.Where(f => f > current).DefaultIfEmpty(current).First() : frames.Where(f => f < current).DefaultIfEmpty(current).Last();
            GoTo((long)target);
        }

        private void PlayPause()
        {
            playTimer.Enabled = !playTimer.Enabled;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            playButton.Text = playTimer.Enabled ? "Pause" : "Play";
            frameLabel.Text = $"Frame {frame} of {lastFrame}";
            labeledLabel.Text = $"{labels.LabeledFrames().Length} frames labeled";
            if (!frameBox.Focused && frame >= 0) frameBox.Value = frame;
        }

        private void InvalidateViews()
        {
            foreach (var view in views) view.Invalidate();
        }

        #endregion

        #region Markers

        private void LoadMarkers()
        {
            // The markers of the settings, and any other in the labels file.
            var names = (settings.LabelingMarkers ?? new string[0])
                .Concat(labels.LabeledFrames().SelectMany(f => labels.Get(f, Eye.Left).Concat(labels.Get(f, Eye.Right)).Concat(labels.Get(f, Eye.Both))).Select(l => l.Marker))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            updatingGrid = true;
            markersGrid.Rows.Clear();
            foreach (var name in names) markersGrid.Rows.Add(name);
            updatingGrid = false;
            SaveMarkers();
        }

        private void SaveMarkers()
        {
            if (updatingGrid) return;
            settings.LabelingMarkers = MarkerNames();
            InvalidateViews();
        }

        private string[] MarkerNames() => markersGrid.Rows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow && !string.IsNullOrWhiteSpace(r.Cells[0].Value as string))
            .Select(r => ((string)r.Cells[0].Value).Trim())
            .ToArray();

        private string? SelectedMarker =>
            markersGrid.CurrentRow is DataGridViewRow row && !row.IsNewRow ? (row.Cells[0].Value as string)?.Trim() : null;

        private Color ColorOf(string marker)
        {
            var index = Array.IndexOf(MarkerNames(), marker);
            return index < 0 ? Color.Gray : Colors[index % Colors.Length];
        }

        #endregion

        #region Clicks and keys

        private void OnClick(Eye eye, PointF location, MouseButtons button)
        {
            if (button == MouseButtons.Right)
            {
                labels.RemoveClosest((ulong)frame, eye, location);
                return;
            }

            var marker = SelectedMarker;
            if (string.IsNullOrEmpty(marker))
            {
                MessageBox.Show(this, "Select a marker first, or type one in the list.", "Label videos by hand");
                return;
            }

            labels.Set(new ManualLabel((ulong)frame, eye, marker!, location));
            if (advanceBox.Checked) Step(1);
        }

        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Not while typing in the list or the frame boxes.
            if (markersGrid.IsCurrentCellInEditMode || frameBox.Focused || stepBox.Focused) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Left: Step(-1); return true;
                case Keys.Right: Step(1); return true;
                case Keys.Space: PlayPause(); return true;
                case Keys.Delete: labels.RemoveFrame((ulong)frame); return true;
            }

            if (keyData >= Keys.D1 && keyData <= Keys.D9 || keyData >= Keys.NumPad1 && keyData <= Keys.NumPad9)
            {
                var index = keyData >= Keys.NumPad1 ? keyData - Keys.NumPad1 : keyData - Keys.D1;
                if (index < markersGrid.Rows.Count && !markersGrid.Rows[index].IsNewRow)
                {
                    markersGrid.CurrentCell = markersGrid.Rows[index].Cells[0];
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        private void SaveLabels()
        {
            try
            {
                labels.Save();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR saving the labels: " + ex.Message);
                MessageBox.Show(this, "The labels could not be saved: " + ex.Message, "Label videos by hand");
            }
        }

        /// <summary>
        /// Image of one eye, scaled to fit, with the labels of the frame drawn on top.
        /// </summary>
        private sealed class EyeView : Panel
        {
            private readonly Eye eye;
            private readonly ManualLabelingForm form;
            private Bitmap? image;

            public EyeView(Eye eye, VideoEye video, ManualLabelingForm form)
            {
                this.eye = eye;
                this.form = form;
                Video = video;
                Dock = DockStyle.Fill;
                DoubleBuffered = true;
                BackColor = Color.Black;
                Cursor = Cursors.Cross;
                Margin = new Padding(2);
                Resize += (_, __) => Invalidate();
            }

            public VideoEye Video { get; }

            public void SetImage(Bitmap newImage)
            {
                image?.Dispose();
                image = newImage;
                Invalidate();
            }

            // Where the image is drawn: as large as fits, keeping its proportions.
            private RectangleF ImageRectangle()
            {
                if (image is null) return RectangleF.Empty;
                var scale = Math.Min((float)ClientSize.Width / image.Width, (float)ClientSize.Height / image.Height);
                var width = image.Width * scale;
                var height = image.Height * scale;
                return new RectangleF((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
            }

            private string EyeName => eye switch { Eye.Left => "Left eye", Eye.Right => "Right eye", _ => "Both eyes" };

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                if (image is null) return;

                var rect = ImageRectangle();
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, rect);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var scale = rect.Width / image.Width;
                foreach (var label in form.labels.Get((ulong)form.frame, eye))
                {
                    var x = rect.X + (label.Location.X + 0.5f) * scale;
                    var y = rect.Y + (label.Location.Y + 0.5f) * scale;
                    using var pen = new Pen(form.ColorOf(label.Marker), 1.5f);
                    g.DrawLine(pen, x - 8, y, x - 2, y);
                    g.DrawLine(pen, x + 2, y, x + 8, y);
                    g.DrawLine(pen, x, y - 8, x, y - 2);
                    g.DrawLine(pen, x, y + 2, x, y + 8);
                    using var brush = new SolidBrush(pen.Color);
                    g.DrawString(label.Marker, Font, brush, x + 6, y + 4);
                }

                TextRenderer.DrawText(g, EyeName, Font, new Point(6, 6), Color.White);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                var rect = ImageRectangle();
                if (image is null || !rect.Contains(e.Location)) return;

                // Pixel coordinates of the image, as the tracking uses them.
                var scale = rect.Width / image.Width;
                form.OnClick(eye, new PointF((e.X - rect.X) / scale - 0.5f, (e.Y - rect.Y) / scale - 0.5f), e.Button);
            }
        }
    }
}
