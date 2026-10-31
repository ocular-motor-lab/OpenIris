//-----------------------------------------------------------------------
// <copyright file="IrisLandmarks.cs">
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
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// The eye found in an eye image, in its pixel coordinates.
    /// </summary>
    internal sealed class IrisLandmarksResult
    {
        public PointF IrisCenter { get; set; }

        public float IrisRadius { get; set; }

        /// <summary>
        /// Points along the upper eyelid, left to right.
        /// </summary>
        public PointF[] UpperEyelid { get; set; } = Array.Empty<PointF>();

        /// <summary>
        /// Points along the lower eyelid, left to right.
        /// </summary>
        public PointF[] LowerEyelid { get; set; } = Array.Empty<PointF>();
    }

    /// <summary>
    /// MediaPipe's iris landmark network: finds the iris and the outline of the eye in an image
    /// of one eye. Not thread safe: use one object per thread.
    /// </summary>
    internal sealed class IrisLandmarks : IDisposable
    {
        private const int InputSize = 64;

        // The first 16 of the 71 contour points outline the eye opening: a corner (0), the lower
        // eyelid (1-7), the other corner (8), and the upper eyelid back from corner 0 (9-15).
        private static readonly int[] UpperEyelid = { 10, 11, 13, 14 };
        private static readonly int[] LowerEyelid = { 2, 3, 5, 6 };

        private readonly NeuralNetwork network = new NeuralNetwork("iris_landmark.onnx", InputSize, "output_eyes_contours_and_brows", "output_iris");
        private readonly Image<Gray, byte> input = new Image<Gray, byte>(InputSize, InputSize);

        /// <summary>
        /// Finds the eye in an image.
        /// </summary>
        /// <param name="eyeImage">Square image of the eye, centered on the eye corners and 2.3
        /// times wider than the distance between them.</param>
        /// <param name="whichEye">Which eye it is.</param>
        public IrisLandmarksResult Find(Image<Gray, byte> eyeImage, Eye whichEye)
        {
            CvInvoke.Resize(eyeImage, input, input.Size, 0, 0, eyeImage.Width > InputSize ? Inter.Area : Inter.Linear);

            // The network was trained on left eyes: mirror right eyes.
            var mirror = whichEye == Eye.Right;
            var outputs = network.Run(input, 1f / 255f, 0f, mirror);
            var contour = outputs[0];
            var iris = outputs[1];

            // The network gives points in the 64x64 input, with 0 at the edge of the first pixel.
            // OpenIris puts 0 at the center of the first pixel.
            var scaleX = eyeImage.Width / (float)InputSize;
            var scaleY = eyeImage.Height / (float)InputSize;
            PointF Point(float[] points, int i) => new PointF(
                (mirror ? InputSize - points[3 * i] : points[3 * i]) * scaleX - 0.5f,
                points[3 * i + 1] * scaleY - 0.5f);

            var irisCenter = Point(iris, 0);
            return new IrisLandmarksResult
            {
                IrisCenter = irisCenter,
                IrisRadius = (float)Enumerable.Range(1, 4).Average(i => Distance(Point(iris, i), irisCenter)),
                UpperEyelid = UpperEyelid.Select(i => Point(contour, i)).OrderBy(p => p.X).ToArray(),
                LowerEyelid = LowerEyelid.Select(i => Point(contour, i)).OrderBy(p => p.X).ToArray(),
            };
        }

        private static float Distance(PointF a, PointF b) =>
            (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        public void Dispose()
        {
            network.Dispose();
            input.Dispose();
        }
    }
}
