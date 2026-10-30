//-----------------------------------------------------------------------
// <copyright file="SyntheticEye.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// Draws a deterministic sequence of simple infrared-like eye images (sclera, textured iris, dark
    /// pupil, one bright corneal reflection, mild noise) where the true pupil and reflection centers
    /// are known. The same frame number always produces the same pixels on any machine.
    /// </summary>
    internal static class SyntheticEye
    {
        public static readonly Size FrameSize = new Size(400, 400);

        /// <summary>
        /// Pixel scale that matches the default eye tracking system settings (6 pixels per mm).
        /// </summary>
        public const double MmPerPix = 1.0 / 6.0;

        private static readonly PointF EyeCenter = new PointF(200, 200);
        private const float ScleraRadius = 150;
        private const float IrisRadius = 80;

        /// <summary>
        /// Ground truth for one frame.
        /// </summary>
        public readonly struct Truth
        {
            public Truth(PointF pupilCenter, float pupilRadius, PointF reflectionCenter)
            {
                PupilCenter = pupilCenter;
                PupilRadius = pupilRadius;
                ReflectionCenter = reflectionCenter;
            }

            public PointF PupilCenter { get; }
            public float PupilRadius { get; }
            public PointF ReflectionCenter { get; }
        }

        /// <summary>
        /// Gets the ground truth of a frame. The pupil follows a smooth path with sub-pixel positions,
        /// its size changes slowly, and the reflection moves less than the pupil, as a real one does.
        /// </summary>
        public static Truth GetTruth(Eye whichEye, int frame)
        {
            double phase = whichEye == Eye.Left ? 0 : 0.7;
            double t = 2 * Math.PI * frame;
            var pupil = new PointF(
                EyeCenter.X + (float)(40 * Math.Sin(t / 50 + phase)),
                EyeCenter.Y + (float)(25 * Math.Sin(t / 37 + phase)));
            var pupilRadius = (float)(22 + 3 * Math.Sin(t / 80 + phase));
            var reflection = new PointF(
                EyeCenter.X + 0.3f * (pupil.X - EyeCenter.X) + 30,
                EyeCenter.Y + 0.3f * (pupil.Y - EyeCenter.Y) + 22);
            return new Truth(pupil, pupilRadius, reflection);
        }

        /// <summary>
        /// Draws frames 0 to count-1.
        /// </summary>
        public static IEnumerable<ImageEye> Frames(Eye whichEye, int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                yield return Draw(whichEye, frame);
            }
        }

        /// <summary>
        /// Gets one frame. Frames are drawn once and then copied, since several tests use the same ones.
        /// </summary>
        public static ImageEye Draw(Eye whichEye, int frame)
        {
            Image<Gray, byte> image;
            lock (Cache)
            {
                if (!Cache.TryGetValue((whichEye, frame), out image))
                {
                    image = DrawImage(whichEye, frame);
                    Cache.Add((whichEye, frame), image);
                }
            }

            var timestamp = new ImageEyeTimestamp(seconds: frame / 100.0, frameNumber: (ulong)frame, frameNumberRaw: (ulong)frame);
            return new ImageEye(image.Copy(), whichEye, timestamp, null);
        }

        private static readonly Dictionary<(Eye, int), Image<Gray, byte>> Cache = new Dictionary<(Eye, int), Image<Gray, byte>>();

        private static Image<Gray, byte> DrawImage(Eye whichEye, int frame)
        {
            var truth = GetTruth(whichEye, frame);
            var image = new Image<Gray, byte>(FrameSize);
            image.SetValue(new Gray(150));

            FillCircle(image, EyeCenter, ScleraRadius, 210);
            FillCircle(image, truth.PupilCenter, IrisRadius, 105);

            // Radial iris texture so the torsion tracker has a pattern to work with.
            const int spokes = 90;
            for (int i = 0; i < spokes; i++)
            {
                double theta = 2 * Math.PI * i / spokes;
                double gray = 105 + 35 * Math.Sin(7 * theta) * Math.Cos(3 * theta);
                var inner = new PointF(
                    truth.PupilCenter.X + (float)(Math.Cos(theta) * truth.PupilRadius),
                    truth.PupilCenter.Y + (float)(Math.Sin(theta) * truth.PupilRadius));
                var outer = new PointF(
                    truth.PupilCenter.X + (float)(Math.Cos(theta) * IrisRadius * 0.95),
                    truth.PupilCenter.Y + (float)(Math.Sin(theta) * IrisRadius * 0.95));
                CvInvoke.Line(image, ToFixed(inner), ToFixed(outer), new MCvScalar(gray), 3, LineType.AntiAlias, FixedShift);
            }

            FillCircle(image, truth.PupilCenter, truth.PupilRadius, 20);
            // Big enough to survive the JOM reflection tracker's blur, half-size resize and threshold of 250.
            FillCircle(image, truth.ReflectionCenter, 5, 255);

            AddNoise(image, seed: (whichEye == Eye.Left ? 0 : 100000) + frame, amplitude: 6);
            return image;
        }

        // Sub-pixel drawing: OpenCV takes coordinates as fixed point with this many fractional bits.
        private const int FixedShift = 4;

        private static Point ToFixed(PointF p) =>
            new Point((int)Math.Round(p.X * (1 << FixedShift)), (int)Math.Round(p.Y * (1 << FixedShift)));

        private static void FillCircle(Image<Gray, byte> image, PointF center, float radius, double gray)
        {
            CvInvoke.Circle(image, ToFixed(center), (int)Math.Round(radius * (1 << FixedShift)), new MCvScalar(gray), -1, LineType.AntiAlias, FixedShift);
        }

        private static void AddNoise(Image<Gray, byte> image, int seed, int amplitude)
        {
            // System.Random with a fixed seed gives the same sequence on every .NET Framework machine.
            var random = new Random(seed);
            var data = image.Data;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int value = data[y, x, 0] + random.Next(-amplitude, amplitude + 1);
                    data[y, x, 0] = (byte)Math.Max(0, Math.Min(255, value));
                }
            }
        }
    }
}
