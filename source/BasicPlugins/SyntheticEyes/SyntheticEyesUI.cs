//-----------------------------------------------------------------------
// <copyright file="SyntheticEyesUI.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.EyeTrackingSystems
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;
    using OpenIris.UI;

    /// <summary>
    /// Window to set the orientation of the synthetic eyes. The same orientation shows in every
    /// coordinate system: moving the sliders of one updates the others. On the black pad, drag with
    /// the left button to point the eye (Listing's law, keeping the torsion), drag up and down with
    /// the right button for torsion, and double click to go back to the primary position.
    /// </summary>
    public partial class SyntheticEyesUI : Form
    {
        private Group[] groups = new Group[0];
        private EyeRotation rotation = EyeRotation.Primary;

        /// <summary>
        /// Initializes the window.
        /// </summary>
        public SyntheticEyesUI()
        {
            InitializeComponent();
            panel1.Paint += Pad_Paint;
            panel1.MouseDown += panel1_MouseMove;
            panel1.Resize += (_, __) => panel1.Invalidate();
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel1, true);
        }

        /// <summary>
        /// Current orientation of the eyes. Read from the camera threads: EyeRotation is immutable,
        /// so reading the reference is enough.
        /// </summary>
        public EyeRotation Rotation => rotation;

        /// <summary>
        /// The sliders of one coordinate system and its conversions.
        /// </summary>
        private sealed class Group
        {
            public Group(SliderTextControl[] sliders, Func<double, double, double, EyeRotation> from, Func<EyeRotation, (double, double, double)> to)
            {
                Sliders = sliders;
                From = from;
                To = to;
            }

            public SliderTextControl[] Sliders { get; }
            public Func<double, double, double, EyeRotation> From { get; }
            public Func<EyeRotation, (double, double, double)> To { get; }
        }

        private void SyntheticEyesUI_Load(object sender, EventArgs ea)
        {
            void Setup(SliderTextControl slider, string text, double min, double max)
            {
                slider.Text = text;
                slider.Range = new RangeDouble(min, max);
                slider.Value = 0;
                slider.Enabled = true;
            }

            Setup(ListingAngle, "Angle (0 left, 90 up)", -180, 180);
            Setup(ListingEccentricity, "Eccentricity", 0, 90);
            Setup(ListingTorsion, "Torsion", -90, 90);
            foreach (var (h, v, t) in new[] { (FickHorizontal, FickVertical, FickTorsion), (HelmholtzHorizontal, HelmholtzVertical, HelmholtzTorsion), (HessHorizontal, HessVertical, HessTorsion), (HarmsHorizontal, HarmsVertical, HarmsTorsion) })
            {
                Setup(h, "Horizontal (+ left)", -90, 90);
                Setup(v, "Vertical (+ up)", -90, 90);
                Setup(t, "Torsion", -90, 90);
            }
            Setup(RotationVectorX, "X (line of sight axis)", -1, 1);
            Setup(RotationVectorY, "Y (interaural axis)", -1, 1);
            Setup(RotationVectorZ, "Z (vertical axis)", -1, 1);

            groups = new[]
            {
                new Group(new[] { ListingAngle, ListingEccentricity, ListingTorsion }, EyeRotation.FromListing, r => r.ToListing()),
                new Group(new[] { FickHorizontal, FickVertical, FickTorsion }, EyeRotation.FromFick, r => r.ToFick()),
                new Group(new[] { HelmholtzHorizontal, HelmholtzVertical, HelmholtzTorsion }, EyeRotation.FromHelmholtz, r => r.ToHelmholtz()),
                new Group(new[] { HessHorizontal, HessVertical, HessTorsion }, EyeRotation.FromHess, r => r.ToHess()),
                new Group(new[] { HarmsHorizontal, HarmsVertical, HarmsTorsion }, EyeRotation.FromHarms, r => r.ToHarms()),
                new Group(new[] { RotationVectorX, RotationVectorY, RotationVectorZ }, EyeRotation.FromRotationVector, r => r.ToRotationVector()),
            };

            foreach (var group in groups)
            {
                foreach (var slider in group.Sliders)
                {
                    slider.ValueChanged += (_, __) =>
                        SetRotation(group.From(group.Sliders[0].Value, group.Sliders[1].Value, group.Sliders[2].Value), group);
                }
            }
        }

        /// <summary>
        /// Sets the orientation and shows it in the other coordinate systems (not in the one being
        /// moved, which would fight the slider with rounding).
        /// </summary>
        private void SetRotation(EyeRotation newRotation, Group? source = null)
        {
            rotation = newRotation;

            foreach (var group in groups)
            {
                if (group == source) continue;
                var (a, b, c) = group.To(rotation);
                var values = new[] { a, b, c };
                for (int i = 0; i < 3; i++)
                {
                    var slider = group.Sliders[i];
                    slider.EnableValueChangedEvent = false;
                    // NaN: no value in this system (a rotation vector at 180 degrees).
                    if (!double.IsNaN(values[i])) slider.Value = Math.Round(values[i], slider.Range.End <= 1 ? 3 : 1);
                    slider.EnableValueChangedEvent = true;
                }
            }

            panel1.Invalidate();
        }

        #region Pad

        // The pad is the eye seen from the front: a point at distance r from the center is a gaze
        // with eccentricity asin(r). Left is to the subject's left.
        private (double x, double y)? PadPoint(Point location)
        {
            var size = Math.Min(panel1.Width, panel1.Height) / 2.0;
            var x = -(location.X - panel1.Width / 2.0) / size;
            var y = -(location.Y - panel1.Height / 2.0) / size;
            return x * x + y * y < 1 ? (x, y) : ((double, double)?)null;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            var (_, _, torsion) = rotation.ToListing();

            if (e.Button == MouseButtons.Left && PadPoint(e.Location) is (double x, double y))
            {
                var angle = Math.Atan2(y, x) * 180 / Math.PI;
                var eccentricity = Math.Asin(Math.Sqrt(x * x + y * y)) * 180 / Math.PI;
                SetRotation(EyeRotation.FromListing(angle, eccentricity, torsion));
            }

            if (e.Button == MouseButtons.Right)
            {
                var (angle, eccentricity, _) = rotation.ToListing();
                var newTorsion = -(e.Y - panel1.Height / 2.0) / (panel1.Height / 2.0) * 45;
                SetRotation(EyeRotation.FromListing(angle, eccentricity, Math.Max(-90, Math.Min(90, newTorsion))));
            }
        }

        private void panel1_DoubleClick(object sender, EventArgs e) => SetRotation(EyeRotation.Primary);

        private void Pad_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var size = (float)Math.Min(panel1.Width, panel1.Height) / 2;
            var cx = panel1.Width / 2f;
            var cy = panel1.Height / 2f;

            using var grid = new Pen(Color.FromArgb(70, 70, 70));
            g.DrawEllipse(grid, cx - size, cy - size, 2 * size, 2 * size);
            g.DrawLine(grid, cx - size, cy, cx + size, cy);
            g.DrawLine(grid, cx, cy - size, cx, cy + size);

            // Where the line of sight points, and a tick for the torsion.
            var m = rotation.Matrix();
            var px = cx - (float)m[1, 0] * size;
            var py = cy - (float)m[2, 0] * size;
            var tickX = cx - (float)m[1, 2] * size;
            var tickY = cy - (float)m[2, 2] * size;
            using var pen = new Pen(Color.Lime, 2);
            g.DrawLine(pen, px, py, px + (tickX - cx) * 0.15f, py + (tickY - cy) * 0.15f);
            g.FillEllipse(Brushes.Lime, px - 5, py - 5, 10, 10);

            TextRenderer.DrawText(g, "Left drag: gaze   Right drag: torsion   Double click: center", Font, new Point(6, 6), Color.Gray);
        }

        #endregion
    }
}
