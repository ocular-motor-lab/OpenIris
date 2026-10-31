//-----------------------------------------------------------------------
// <copyright file="FaceDetector.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// A face found by the detector, in image coordinates.
    /// </summary>
    internal sealed class Face
    {
        public float Score { get; set; }

        /// <summary>
        /// Center of the box around the face.
        /// </summary>
        public PointF Center { get; set; }

        /// <summary>
        /// Width or height of the box around the face, whichever is larger.
        /// </summary>
        public float Size { get; set; }

        /// <summary>
        /// Center of the eye on the left of the image (the subject's right eye, if the image is not
        /// mirrored).
        /// </summary>
        public PointF EyeImageLeft { get; set; }

        /// <summary>
        /// Center of the eye on the right of the image (the subject's left eye).
        /// </summary>
        public PointF EyeImageRight { get; set; }
    }

    /// <summary>
    /// MediaPipe's BlazeFace short range face detector. Finds the most likely face in an image.
    /// </summary>
    internal sealed class FaceDetector : IDisposable
    {
        private const int InputSize = 128;
        private const float MinScore = 0.5f;

        private readonly NeuralNetwork network = new NeuralNetwork("face_detection_short_range.onnx", InputSize, "regressors", "classificators");
        private readonly PointF[] anchors = CreateAnchors();
        private readonly Image<Gray, byte> input = new Image<Gray, byte>(InputSize, InputSize);

        /// <summary>
        /// Finds the face with the highest score.
        /// </summary>
        /// <param name="image">The whole camera image.</param>
        /// <returns>The face, or null if none.</returns>
        public Face? Detect(Image<Gray, byte> image)
        {
            // Scale the image to fit the 128x128 input, keeping its proportions, padded with black at
            // the bottom or right.
            var scale = (float)InputSize / Math.Max(image.Width, image.Height);
            var scaledSize = new Size(
                Math.Max(1, (int)Math.Round(image.Width * scale)),
                Math.Max(1, (int)Math.Round(image.Height * scale)));
            input.SetZero();
            input.ROI = new Rectangle(Point.Empty, scaledSize);
            CvInvoke.Resize(image, input, scaledSize, 0, 0, Inter.Area);
            input.ROI = Rectangle.Empty;

            var outputs = network.Run(input, 2f / 255f, -1f);
            var regressors = outputs[0];
            var scores = outputs[1];

            var best = -1;
            for (int i = 0; i < anchors.Length; i++)
            {
                if (best < 0 || scores[i] > scores[best]) best = i;
            }

            var score = 1 / (1 + Math.Exp(-Math.Max(-100, Math.Min(100, scores[best]))));
            if (score < MinScore) return null;

            // Values are in pixels of the 128x128 input, relative to the anchor. Keypoints 0 and 1 are
            // the eyes.
            PointF Keypoint(int k) => new PointF(
                (regressors[best * 16 + 4 + 2 * k] / InputSize + anchors[best].X) * InputSize / scale,
                (regressors[best * 16 + 5 + 2 * k] / InputSize + anchors[best].Y) * InputSize / scale);

            var r = best * 16;
            return new Face
            {
                Score = (float)score,
                Center = new PointF(
                    (regressors[r] / InputSize + anchors[best].X) * InputSize / scale,
                    (regressors[r + 1] / InputSize + anchors[best].Y) * InputSize / scale),
                Size = Math.Max(regressors[r + 2], regressors[r + 3]) / scale,
                EyeImageLeft = Keypoint(0),
                EyeImageRight = Keypoint(1),
            };
        }

        /// <summary>
        /// Anchor centers of BlazeFace short range, in fractions of the image: 2 anchors per cell of a
        /// 16x16 grid, then 6 per cell of an 8x8 grid.
        /// </summary>
        private static PointF[] CreateAnchors()
        {
            var anchors = new List<PointF>();
            foreach (var (grid, perCell) in new[] { (16, 2), (8, 6) })
            {
                for (int y = 0; y < grid; y++)
                {
                    for (int x = 0; x < grid; x++)
                    {
                        for (int a = 0; a < perCell; a++)
                        {
                            anchors.Add(new PointF((x + 0.5f) / grid, (y + 0.5f) / grid));
                        }
                    }
                }
            }

            return anchors.ToArray();
        }

        public void Dispose()
        {
            network.Dispose();
            input.Dispose();
        }
    }
}
