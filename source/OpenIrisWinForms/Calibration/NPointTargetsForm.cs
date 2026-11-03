//-----------------------------------------------------------------------
// <copyright file="NPointTargetsForm.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Calibration
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// The targets of the N-point calibration when OpenIris shows them itself (Local mode): full screen
    /// on the eye tracking system's StimulusDisplay, drawing the calibration's current target. Esc
    /// stops the calibration.
    /// </summary>
    internal sealed class NPointTargetsForm : Form
    {
        private readonly CalibrationPipelineRemoteLinear pipeline;
        private readonly Timer timer = new Timer { Interval = 15 };
        private NPointTarget? drawn;

        public NPointTargetsForm(CalibrationPipelineRemoteLinear pipeline)
        {
            this.pipeline = pipeline;

            Text = "OpenIris calibration targets";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = StimulusScreen(pipeline.SystemSettings?.StimulusDisplay ?? 0).Bounds;
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;

            // Mid gray, as is usual for stimuli.
            BackColor = Color.FromArgb(128, 128, 128);
            DoubleBuffered = true;
            Cursor.Hide();

            timer.Tick += (_, __) =>
            {
                if (!ReferenceEquals(pipeline.Target, drawn)) Invalidate();
            };
            timer.Start();
        }

        /// <summary>
        /// The display with the given number in Windows' settings (\\.\DISPLAYn), or the main display.
        /// </summary>
        private static Screen StimulusScreen(int number) =>
            Screen.AllScreens.FirstOrDefault(screen => number > 0 && screen.DeviceName.TrimEnd('\0').EndsWith("DISPLAY" + number, StringComparison.OrdinalIgnoreCase))
            ?? Screen.PrimaryScreen;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            drawn = pipeline.Target;
            if (drawn is null) return;

            var settings = (CalibrationSettingsRemoteLinear)pipeline.Settings;
            var screenWidthMm = pipeline.SystemSettings?.ScreenWidthMm ?? 530;
            var distanceMm = pipeline.SystemSettings?.ViewingDistanceMm ?? 570;
            float Px(double degrees) => (float)(Math.Tan(degrees * Math.PI / 180) * distanceMm * ClientSize.Width / screenWidthMm);

            var x = ClientSize.Width / 2f + Px(drawn.Position.X);
            var y = ClientSize.Height / 2f - Px(drawn.Position.Y);
            var r = Math.Max(4, Px(settings.TargetSize / 2)) * (float)drawn.Scale;
            var dot = Math.Max(2, r * 0.18f);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var disc = new SolidBrush(drawn.Recording ? Color.White : Color.FromArgb(232, 232, 232));
            e.Graphics.FillEllipse(disc, x - r, y - r, 2 * r, 2 * r);
            e.Graphics.FillEllipse(Brushes.Black, x - dot, y - dot, 2 * dot, 2 * dot);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                pipeline.Cancel();
                Close();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Dispose();
            Cursor.Show();
            base.OnFormClosed(e);
        }
    }
}
