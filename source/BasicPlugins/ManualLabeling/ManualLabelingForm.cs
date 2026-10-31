//-----------------------------------------------------------------------
// <copyright file="ManualLabelingForm.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;
    using Emgu.CV;

    /// <summary>
    /// Window to label the videos: the images of both eyes, the markers, and the controls to move
    /// through the frames. Click places the selected marker, right click removes the closest one.
    /// </summary>
    internal sealed class ManualLabelingForm : Form
    {
        private static readonly Color[] Colors =
        {
            Color.Lime, Color.Cyan, Color.Magenta, Color.Yellow, Color.OrangeRed, Color.DeepSkyBlue, Color.White, Color.Orange, Color.SpringGreen,
        };

        private readonly EyeTrackingSystemManualLabeling system;
        private readonly ManualLabels labels;
        private readonly EyeView leftView;
        private readonly EyeView rightView;
        private readonly DataGridView markersGrid;
        private readonly NumericUpDown frameBox;
        private readonly NumericUpDown stepBox;
        private readonly CheckBox advanceBox;
        private readonly Button playButton;
        private readonly ToolStripStatusLabel frameLabel;
        private readonly ToolStripStatusLabel labeledLabel;
        private readonly Timer saveTimer;

        // The frame on screen, and the images that arrived for it (from the processing thread).
        private readonly object sync = new object();
        private ulong shownFrame;
        private Bitmap? newLeft, newRight;
        private ulong newFrame;
        private bool updatePending;
        private bool hasImage;
        private bool updatingGrid;

        public ManualLabelingForm(EyeTrackingSystemManualLabeling system, ManualLabels labels)
        {
            this.system = system;
            this.labels = labels;

            Text = "Manual labeling - " + System.IO.Path.GetFileName(labels.FileName);
            Size = new Size(1300, 700);
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterScreen;

            // Frame controls.
            var controls = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4), WrapContents = true };
            Button AddButton(string text, string tip, Action action)
            {
                var button = new Button { Text = text, AutoSize = true, Margin = new Padding(2) };
                button.Click += (_, __) => action();
                new ToolTip().SetToolTip(button, tip);
                controls.Controls.Add(button);
                return button;
            }
            AddButton("|◀", "First frame", () => GoTo(Player?.FrameRange.Begin ?? 0));
            AddButton("◀ labeled", "Previous labeled frame", () => GoToLabeled(-1));
            AddButton("◀", "Back one step (Left arrow)", () => Step(-1));
            playButton = AddButton("Play", "Play or pause (Space)", PlayPause);
            AddButton("▶", "Forward one step (Right arrow)", () => Step(1));
            AddButton("labeled ▶", "Next labeled frame", () => GoToLabeled(1));
            AddButton("▶|", "Last frame", () => GoTo(Player?.FrameRange.End ?? 0));

            controls.Controls.Add(new Label { Text = "Frame", AutoSize = true, Margin = new Padding(12, 8, 2, 0) });
            frameBox = new NumericUpDown { Maximum = decimal.MaxValue, Width = 90 };
            frameBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { GoTo((long)frameBox.Value); e.SuppressKeyPress = true; } };
            controls.Controls.Add(frameBox);
            AddButton("Go", "Go to the frame", () => GoTo((long)frameBox.Value));

            controls.Controls.Add(new Label { Text = "Step", AutoSize = true, Margin = new Padding(12, 8, 2, 0) });
            stepBox = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = 1, Width = 60 };
            new ToolTip().SetToolTip(stepBox, "Frames to move with each step: 2 labels every other frame.");
            controls.Controls.Add(stepBox);

            advanceBox = new CheckBox { Text = "Step after each click", AutoSize = true, Margin = new Padding(12, 6, 2, 0) };
            controls.Controls.Add(advanceBox);
            AddButton("Clear frame", "Remove the labels of this frame (Delete)", () => labels.RemoveFrame(shownFrame));

            // Markers.
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
            markersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Marker", FillWeight = 40 });
            foreach (var name in new[] { "Left eye", "Right eye" })
            {
                var column = new DataGridViewComboBoxColumn { Name = name, HeaderText = name + " goes to", FillWeight = 30, FlatStyle = FlatStyle.Flat };
                column.Items.AddRange(Enum.GetNames(typeof(ManualLabelingMarkerTarget)));
                markersGrid.Columns.Add(column);
            }
            markersGrid.CellValueChanged += (_, __) => SaveMarkers();
            markersGrid.UserDeletedRow += (_, __) => SaveMarkers();
            markersGrid.CurrentCellDirtyStateChanged += (_, __) => { if (markersGrid.IsCurrentCellDirty) markersGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            markersGrid.SelectionChanged += (_, __) => { leftView.Invalidate(); rightView.Invalidate(); };
            markersGrid.RowPrePaint += (_, e) => markersGrid.Rows[e.RowIndex].HeaderCell.Style.BackColor = Colors[e.RowIndex % Colors.Length];
            markersGrid.DataError += (_, e) => e.ThrowException = false;

            var help = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 150,
                Padding = new Padding(4),
                Text = "Click: place the selected marker (it replaces the same marker on that eye and frame).\n" +
                       "Right click: remove the closest marker.\n1-9: select a marker.  Left/Right arrows: step.\n" +
                       "Space: play/pause.  Delete: clear the frame.\n\n" +
                       "\"Goes to\": where the marker is saved in the data. Pupil and CR replace what the tracking found.",
            };
            var markersPanel = new Panel { Dock = DockStyle.Fill };
            markersPanel.Controls.Add(markersGrid);
            markersPanel.Controls.Add(help);

            // Eye images.
            leftView = new EyeView(Eye.Left, this);
            rightView = new EyeView(Eye.Right, this);
            var images = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Black };
            images.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            images.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            images.Controls.Add(rightView, 0, 0); // The right eye is on the left of the screen, as in OpenIris.
            images.Controls.Add(leftView, 1, 0);

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            // Only once the window has its size: before, the splitter can't go there.
            Shown += (_, __) => split.SplitterDistance = 380;
            split.Panel1.Controls.Add(markersPanel);
            split.Panel2.Controls.Add(images);

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
            labels.Changed += (_, __) => BeginInvokeIfOpen(() =>
            {
                saveTimer.Stop();
                saveTimer.Start();
                leftView.Invalidate();
                rightView.Invalidate();
                UpdateStatus();
            });
            FormClosed += (_, __) => { saveTimer.Stop(); SaveLabels(); };

            UpdateStatus();
        }

        private VideoPlayer? Player => EyeTracker.Create().VideoPlayer;

        #region Frames

        /// <summary>
        /// Called by the system with every processed frame (on the processing thread).
        /// </summary>
        public void ShowFrame(EyeTrackerImagesAndData imagesAndData)
        {
            var frame = (imagesAndData.Images[Eye.Left] ?? imagesAndData.Images[Eye.Right])?.TimeStamp.FrameNumber ?? 0;

            lock (sync)
            {
                // While paused the same frame comes again and again: only redraw for a new frame.
                if (updatePending || (hasImage && frame == shownFrame)) return;
                updatePending = true;

                newFrame = frame;
                newLeft?.Dispose();
                newRight?.Dispose();
                newLeft = imagesAndData.Images[Eye.Left]?.Image.ToBitmap();
                newRight = imagesAndData.Images[Eye.Right]?.Image.ToBitmap();
            }

            BeginInvokeIfOpen(() =>
            {
                lock (sync)
                {
                    shownFrame = newFrame;
                    leftView.SetImage(newLeft);
                    rightView.SetImage(newRight);
                    newLeft = newRight = null;
                    updatePending = false;
                    hasImage = true;
                }

                UpdateStatus();
            });
        }

        private void GoTo(long frame)
        {
            var player = Player;
            if (player is null) return;
            frame = Math.Max(player.FrameRange.Begin, Math.Min(player.FrameRange.End, frame));
            player.Pause();
            player.Scroll((ulong)frame);
            UpdateStatus();
        }

        private void Step(int direction) => GoTo((long)shownFrame + direction * (long)stepBox.Value);

        private void GoToLabeled(int direction)
        {
            var frames = labels.LabeledFrames();
            var target = direction > 0 ? frames.Where(f => f > shownFrame).DefaultIfEmpty(shownFrame).First() : frames.Where(f => f < shownFrame).DefaultIfEmpty(shownFrame).Last();
            GoTo((long)target);
        }

        private void PlayPause()
        {
            var player = Player;
            if (player is null) return;
            if (player.Playing) player.Pause(); else player.Play();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var player = Player;
            playButton.Text = player?.Playing == true ? "Pause" : "Play";
            frameLabel.Text = $"Frame {shownFrame}" + (player is null ? "" : $" of {player.FrameRange.End}");
            labeledLabel.Text = $"{labels.LabeledFrames().Length} frames labeled";
            if (!frameBox.Focused) frameBox.Value = shownFrame;
        }

        #endregion

        #region Markers

        private void LoadMarkers()
        {
            updatingGrid = true;
            markersGrid.Rows.Clear();
            foreach (var marker in system.LabelingSettings.Markers ?? new ManualLabelingMarker[0])
            {
                markersGrid.Rows.Add(marker.Name, marker.LeftEye.ToString(), marker.RightEye.ToString());
            }
            updatingGrid = false;
        }

        private void SaveMarkers()
        {
            if (updatingGrid) return;
            system.LabelingSettings.Markers = markersGrid.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && !string.IsNullOrWhiteSpace(r.Cells[0].Value as string))
                .Select(r => new ManualLabelingMarker
                {
                    Name = ((string)r.Cells[0].Value).Trim(),
                    LeftEye = ParseTarget(r.Cells[1].Value),
                    RightEye = ParseTarget(r.Cells[2].Value),
                })
                .ToArray();
            leftView.Invalidate();
            rightView.Invalidate();
        }

        private static ManualLabelingMarkerTarget ParseTarget(object? value) =>
            Enum.TryParse<ManualLabelingMarkerTarget>(value as string, out var target) ? target : ManualLabelingMarkerTarget.None;

        private string? SelectedMarker =>
            markersGrid.CurrentRow is DataGridViewRow row && !row.IsNewRow ? (row.Cells[0].Value as string)?.Trim() : null;

        private Color ColorOf(string marker)
        {
            var names = (system.LabelingSettings.Markers ?? new ManualLabelingMarker[0]).Select(m => m.Name).ToList();
            var index = names.IndexOf(marker);
            return index < 0 ? Color.Gray : Colors[index % Colors.Length];
        }

        #endregion

        #region Clicks and keys

        private void OnClick(Eye eye, PointF location, MouseButtons button)
        {
            if (button == MouseButtons.Right)
            {
                labels.RemoveClosest(shownFrame, eye, location);
                return;
            }

            var marker = SelectedMarker;
            if (string.IsNullOrEmpty(marker))
            {
                MessageBox.Show(this, "Select a marker first, or add one in the list.", "Manual labeling");
                return;
            }

            labels.Set(new ManualLabel(shownFrame, eye, marker!, location));
            if (advanceBox.Checked) Step(1);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Not while typing in the list or the frame box.
            if (markersGrid.IsCurrentCellInEditMode || frameBox.Focused || stepBox.Focused) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Left: Step(-1); return true;
                case Keys.Right: Step(1); return true;
                case Keys.Space: PlayPause(); return true;
                case Keys.Delete: labels.RemoveFrame(shownFrame); return true;
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
            }
        }

        private void BeginInvokeIfOpen(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // Closing.
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

            public EyeView(Eye eye, ManualLabelingForm form)
            {
                this.eye = eye;
                this.form = form;
                Dock = DockStyle.Fill;
                DoubleBuffered = true;
                BackColor = Color.Black;
                Cursor = Cursors.Cross;
                Margin = new Padding(2);
                Resize += (_, __) => Invalidate();
            }

            public void SetImage(Bitmap? newImage)
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

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                if (image is null)
                {
                    TextRenderer.DrawText(g, (eye == Eye.Left ? "Left" : "Right") + " eye: no image", Font, ClientRectangle, Color.Gray);
                    return;
                }

                var rect = ImageRectangle();
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, rect);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var scale = rect.Width / image.Width;
                foreach (var label in form.labels.Get(form.shownFrame, eye))
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

                TextRenderer.DrawText(g, eye == Eye.Left ? "Left eye" : "Right eye", Font, new Point(6, 6), Color.White);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                var rect = ImageRectangle();
                if (image is null || !rect.Contains(e.Location)) return;

                // Pixel coordinates of the image, as the tracking uses them.
                var scale = rect.Width / image.Width;
                var location = new PointF((e.X - rect.X) / scale - 0.5f, (e.Y - rect.Y) / scale - 0.5f);
                form.OnClick(eye, location, e.Button);
            }
        }
    }
}
