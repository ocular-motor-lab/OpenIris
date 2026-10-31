//-----------------------------------------------------------------------
// <copyright file="WebcamFaceView.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// Window with the whole webcam image, the face mesh and the regions cut out as eye images, to
    /// see what the webcam system is doing.
    /// </summary>
    /// <remarks>
    /// The processing thread offers every frame, but only copies one when the window has drawn the
    /// previous one, so the window costs the tracking almost nothing.
    /// </remarks>
    internal sealed class WebcamFaceView : Form
    {
        private static readonly Bgr MeshColor = new Bgr(80, 220, 80);
        private static readonly Bgr FaceRegionColor = new Bgr(220, 140, 40);
        private static readonly Bgr EyeColor = new Bgr(40, 200, 255);

        private readonly PictureBox pictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
        private readonly Label status = new Label { Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Timer timer = new Timer { Interval = 66 };
        private readonly object frameLock = new object();
        private volatile bool wantsFrame = true;
        private Frame? frame;
        private Image<Bgr, byte>? shown;

        public WebcamFaceView()
        {
            Text = "Webcam face";
            ClientSize = new Size(640, 382);
            Controls.Add(pictureBox);
            Controls.Add(status);
            timer.Tick += (_, __) => DrawLastFrame();
            timer.Start();
        }

        /// <summary>
        /// Gives the window the latest frame. Called from the processing thread.
        /// </summary>
        public void Offer(Image<Gray, byte> image, WebcamEyeLocator locator, EyeCollection<ImageEye?> eyes)
        {
            if (!wantsFrame) return;
            wantsFrame = false;

            var regions = eyes
                .Select(eye => eye?.ImageSourceData as EyeCrop)
                .Where(crop => crop?.Found == true)
                .Select(crop => crop!.Region)
                .ToArray();
            var newFrame = new Frame(image.Copy(), locator.LastMesh, locator.LastScore, locator.LastFaceRegion, regions);

            lock (frameLock)
            {
                frame?.Image.Dispose();
                frame = newFrame;
            }
        }

        private void DrawLastFrame()
        {
            Frame? toDraw;
            lock (frameLock)
            {
                toDraw = frame;
                frame = null;
            }

            if (toDraw is null) return;

            using (var image = toDraw.Image)
            {
                var color = image.Convert<Bgr, byte>();

                // Lines and dots of the same size on the screen whatever the resolution.
                var thickness = Math.Max(1, image.Width / 640);

                if (toDraw.FaceRegion is RotatedSquare faceRegion) DrawSquare(color, faceRegion, FaceRegionColor, thickness);
                if (toDraw.Mesh != null)
                {
                    foreach (var point in toDraw.Mesh)
                    {
                        CvInvoke.Circle(color, Point.Round(point), thickness, MeshColor.MCvScalar, -1, LineType.AntiAlias);
                    }
                }

                foreach (var region in toDraw.EyeRegions) DrawSquare(color, region, EyeColor, thickness * 2);

                pictureBox.Image = color.Bitmap;
                shown?.Dispose();
                shown = color;
            }

            status.Text = toDraw.Mesh != null
                ? $"Face found, score {toDraw.Score:0.00}. Green: face mesh, yellow: eye images, blue: region searched for the face."
                : "Face not found.";
            wantsFrame = true;
        }

        private static void DrawSquare(Image<Bgr, byte> image, RotatedSquare square, Bgr color, int thickness)
        {
            var corners = square.Corners().Select(Point.Round).ToArray();
            CvInvoke.Polylines(image, corners, true, color.MCvScalar, thickness, LineType.AntiAlias);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                pictureBox.Image = null;
                shown?.Dispose();
                lock (frameLock)
                {
                    frame?.Image.Dispose();
                    frame = null;
                }
            }

            base.Dispose(disposing);
        }

        private sealed class Frame
        {
            public Frame(Image<Gray, byte> image, PointF[]? mesh, float score, RotatedSquare? faceRegion, RotatedSquare[] eyeRegions)
            {
                Image = image;
                Mesh = mesh;
                Score = score;
                FaceRegion = faceRegion;
                EyeRegions = eyeRegions;
            }

            public Image<Gray, byte> Image { get; }
            public PointF[]? Mesh { get; }
            public float Score { get; }
            public RotatedSquare? FaceRegion { get; }
            public RotatedSquare[] EyeRegions { get; }
        }
    }
}
