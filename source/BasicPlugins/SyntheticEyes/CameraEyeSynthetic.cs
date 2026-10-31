//-----------------------------------------------------------------------
// <copyright file="CameraEyeSynthetic.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris.EyeTrackingSystems;

    /// <summary>
    /// Camera that shows a synthetic eye: a sphere with an iris and a pupil, rotated to the
    /// orientation set in the Synthetic eyes window.
    /// </summary>
    public class CameraEyeSynthetic : CameraEye
    {
        private readonly SyntheticEyeRenderer renderer;
        private readonly Func<EyeRotation> rotation;
        private readonly Stopwatch clock = new Stopwatch();
        private long numberFramesGrabbed;

        /// <summary>
        /// Initializes an instance of the CameraEyeSynthetic class.
        /// </summary>
        /// <param name="whichEye">Left or right eye.</param>
        /// <param name="renderer">Renderer, shared by both eyes: they look the same way.</param>
        /// <param name="rotation">Current orientation of the eye.</param>
        public CameraEyeSynthetic(Eye whichEye, SyntheticEyeRenderer renderer, Func<EyeRotation> rotation)
        {
            WhichEye = whichEye;
            FrameSize = renderer.FrameSize;
            FrameRate = 50;
            this.renderer = renderer;
            this.rotation = rotation;
        }

        protected override ImageEye GrabImageFromCamera()
        {
            // One frame every 1/FrameRate seconds, as a real camera.
            var due = TimeSpan.FromSeconds(numberFramesGrabbed / FrameRate);
            var wait = due - clock.Elapsed;
            if (wait > TimeSpan.Zero) Thread.Sleep(wait);

            var timestamp = new ImageEyeTimestamp(
                seconds: clock.Elapsed.TotalSeconds,
                frameNumber: (ulong)numberFramesGrabbed,
                frameNumberRaw: (ulong)numberFramesGrabbed);
            numberFramesGrabbed++;

            return new ImageEye(renderer.Render(rotation()), WhichEye, timestamp, null);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            clock.Restart();
        }

        /// <inheritdoc/>
        public override void Stop()
        {
            clock.Stop();
        }
    }

    /// <summary>
    /// Draws the eye: the image of the front of an eyeball, remapped as if rotated. The rotation of
    /// each pixel of the sphere uses only a 3x3 matrix, and an image is drawn again only when the
    /// orientation changes.
    /// </summary>
    public sealed class SyntheticEyeRenderer : IDisposable
    {
        private readonly object sync = new object();
        private readonly Image<Gray, byte> baseImage;
        private readonly Image<Gray, float> mapX;
        private readonly Image<Gray, float> mapY;
        private readonly int[] sphereX, sphereY;
        private readonly float[] pointX, pointY, pointZ;
        private readonly PointF center = new PointF(200, 200);
        private readonly float radius = 160;
        private Image<Gray, byte>? lastImage;
        private EyeRotation? lastRotation;
        private bool disposed;

        /// <summary>
        /// Initializes the renderer: draws the eye at the primary position and precomputes the
        /// points of the sphere.
        /// </summary>
        public SyntheticEyeRenderer()
        {
            FrameSize = new Size(400, 400);
            baseImage = DrawEye(FrameSize, center, radius);

            // Points of the front of the sphere seen by each pixel, relative to its center.
            var count = 0;
            var xs = new int[FrameSize.Width * FrameSize.Height];
            var ys = new int[xs.Length];
            for (int y = 0; y < FrameSize.Height; y++)
            {
                for (int x = 0; x < FrameSize.Width; x++)
                {
                    var dx = x - center.X;
                    var dy = y - center.Y;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        xs[count] = x;
                        ys[count] = y;
                        count++;
                    }
                }
            }
            sphereX = new int[count];
            sphereY = new int[count];
            pointX = new float[count];
            pointY = new float[count];
            pointZ = new float[count];
            for (int i = 0; i < count; i++)
            {
                sphereX[i] = xs[i];
                sphereY[i] = ys[i];
                pointX[i] = xs[i] - center.X;
                pointY[i] = ys[i] - center.Y;
                pointZ[i] = (float)Math.Sqrt(Math.Max(0, radius * radius - pointX[i] * pointX[i] - pointY[i] * pointY[i]));
            }

            // Outside the sphere the maps point outside the image: white, as the border.
            mapX = new Image<Gray, float>(FrameSize);
            mapY = new Image<Gray, float>(FrameSize);
            mapX.SetValue(new Gray(-1));
            mapY.SetValue(new Gray(-1));
        }

        /// <summary>Size of the images.</summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Image of the eye in this orientation. A new copy each time: the caller owns it.
        /// </summary>
        public Image<Gray, byte> Render(EyeRotation rotation)
        {
            lock (sync)
            {
                // A last frame asked for while stopping.
                if (disposed) return new Image<Gray, byte>(FrameSize);

                if (lastImage is null || !ReferenceEquals(rotation, lastRotation))
                {
                    lastImage?.Dispose();
                    lastImage = Draw(rotation);
                    lastRotation = rotation;
                }
                return lastImage.Copy();
            }
        }

        private Image<Gray, byte> Draw(EyeRotation rotation)
        {
            // Image axes: x right, y down, z away from the camera. The eye shows as OpenIris expects
            // it (see the data file docs): looking left (+horizontal) moves the pupil to the left of
            // the image and looking up moves it up, as in a camera behind a hot mirror. So from head
            // axes (X line of sight, Y left, Z up): x = -Y, y = -Z, z = -X.
            //
            // Each pixel of the new image shows the point of the eye at the primary position that
            // the rotation brings there: source = P * R^T * P^T * pixel.
            var r = rotation.Matrix();
            var p = new double[,] { { 0, -1, 0 }, { 0, 0, -1 }, { -1, 0, 0 } };
            var a = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        for (int l = 0; l < 3; l++)
                            a[i, j] += p[i, k] * r[l, k] * p[j, l];

            float a00 = (float)a[0, 0], a01 = (float)a[0, 1], a02 = (float)a[0, 2];
            float a10 = (float)a[1, 0], a11 = (float)a[1, 1], a12 = (float)a[1, 2];

            var dataX = mapX.Data;
            var dataY = mapY.Data;
            for (int i = 0; i < pointX.Length; i++)
            {
                // The front of the sphere faces the camera: negative z.
                float x = pointX[i], y = pointY[i], z = -pointZ[i];
                dataX[sphereY[i], sphereX[i], 0] = a00 * x + a01 * y + a02 * z + center.X;
                dataY[sphereY[i], sphereX[i], 0] = a10 * x + a11 * y + a12 * z + center.Y;
            }

            var image = new Image<Gray, byte>(FrameSize);
            CvInvoke.Remap(baseImage, image, mapX, mapY, Emgu.CV.CvEnum.Inter.Linear, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(255));

            // Eyelids.
            image.Draw(new LineSegment2D(new Point(0, (int)center.Y + 200), new Point(400, (int)center.Y + 200)), new Gray(80), 40);
            image.Draw(new LineSegment2D(new Point(0, (int)center.Y - 200), new Point(400, (int)center.Y - 200)), new Gray(80), 40);
            return image;
        }

        /// <summary>
        /// The eye at the primary position: sclera, iris with a pattern of radial lines (so torsion
        /// can be seen and measured) and pupil.
        /// </summary>
        private static Image<Gray, byte> DrawEye(Size size, PointF center, float radius)
        {
            const float irisRadius = 80;
            const float pupilRadius = 20;

            var image = new Image<Gray, byte>(size);
            image.SetValue(new Gray(128));
            image.Draw(new CircleF(center, radius), new Gray(230), 0);
            image.Draw(new CircleF(center, irisRadius), new Gray(100), 0);

            const int segments = 500;
            for (int i = 0; i < segments; i++)
            {
                var theta = 2 * Math.PI / segments * i + 4 * Math.Round(30.0 * Math.Sin(7069 * (double)i / segments * 2.0 * Math.PI));
                Math.DivRem(7069 * i, 10, out var offset);
                var a = 0.2f + offset / 12f;
                var b = offset / 12f;

                image.Draw(
                    new LineSegment2D(
                        new Point((int)(Math.Cos(theta) * irisRadius * a + center.X), (int)(Math.Sin(theta) * irisRadius * a + center.Y)),
                        new Point((int)(Math.Cos(theta) * irisRadius * b + center.X), (int)(Math.Sin(theta) * irisRadius * b + center.Y))),
                    new Gray(120 + 50 * Math.Sin(7069 * (double)i / segments * 2.0 * Math.PI)), 4);
            }

            image.Draw(new CircleF(center, pupilRadius), new Gray(10), 0);
            return image;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                lastImage?.Dispose();
                baseImage.Dispose();
                mapX.Dispose();
                mapY.Dispose();
            }
        }
    }
}
