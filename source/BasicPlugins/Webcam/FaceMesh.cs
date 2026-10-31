//-----------------------------------------------------------------------
// <copyright file="FaceMesh.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// MediaPipe's face landmark network: 468 points on the face (the face mesh), from an image of
    /// the face upright, about 1.5 times larger than the face.
    /// </summary>
    internal sealed class FaceMesh : IDisposable
    {
        private const int InputSize = 192;

        // Corners of the eyes in the mesh. The subject's right eye is on the left of the image.
        public const int RightEyeOuterCorner = 33;
        public const int RightEyeInnerCorner = 133;
        public const int LeftEyeInnerCorner = 362;
        public const int LeftEyeOuterCorner = 263;

        private readonly NeuralNetwork network = new NeuralNetwork("face_landmark.onnx", InputSize, "landmarks", "scores");

        /// <summary>
        /// Finds the face mesh in a region of the image.
        /// </summary>
        /// <param name="image">The webcam image.</param>
        /// <param name="region">Region around the face.</param>
        /// <returns>How likely there is a face (0 to 1), and the points in webcam image coordinates.</returns>
        public (float score, PointF[] points) Find(Image<Gray, byte> image, RotatedSquare region)
        {
            using var face = region.CutOut(image, InputSize);
            var outputs = network.Run(face, 1f / 255f, 0f);
            var landmarks = outputs[0];

            // In fractions of the input, with 0 at the edge of the first pixel.
            var points = new PointF[landmarks.Length / 3];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new PointF(landmarks[3 * i] * InputSize - 0.5f, landmarks[3 * i + 1] * InputSize - 0.5f);
            }

            return (outputs[1][0], region.ToImage(points, InputSize));
        }

        public void Dispose()
        {
            network.Dispose();
        }
    }
}
