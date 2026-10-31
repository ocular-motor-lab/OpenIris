//-----------------------------------------------------------------------
// <copyright file="EyeCrop.cs">
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
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// A square region of the webcam image, possibly rotated, that is cut out and scaled to a
    /// square image for a neural network or for the eye images.
    /// </summary>
    internal readonly struct RotatedSquare
    {
        public RotatedSquare(PointF center, float size, float angle)
        {
            Center = center;
            Size = size;
            Angle = angle;
        }

        public PointF Center { get; }

        /// <summary>
        /// Side of the square, in webcam image pixels.
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// Rotation in radians, positive clockwise in the image.
        /// </summary>
        public float Angle { get; }

        /// <summary>
        /// Cuts the region out of an image.
        /// </summary>
        /// <param name="image">The webcam image.</param>
        /// <param name="pixels">Width and height of the result.</param>
        public Image<Gray, byte> CutOut(Image<Gray, byte> image, int pixels)
        {
            var result = new Image<Gray, byte>(pixels, pixels);
            using var transform = GetTransform(pixels);
            CvInvoke.WarpAffine(image, result, transform, result.Size, Inter.Linear, Warp.InverseMap, BorderType.Replicate);
            return result;
        }

        /// <summary>
        /// Converts a point of the cut-out image (0 at the center of the first pixel) to webcam
        /// image coordinates.
        /// </summary>
        public PointF[] ToImage(PointF[] points, int pixels)
        {
            using var t = GetTransform(pixels);
            return Array.ConvertAll(points, p => new PointF(
                (float)(t[0, 0] * p.X + t[0, 1] * p.Y + t[0, 2]),
                (float)(t[1, 0] * p.X + t[1, 1] * p.Y + t[1, 2])));
        }

        /// <summary>
        /// The 2x3 affine transform from cut-out image to webcam image coordinates.
        /// </summary>
        private Matrix<double> GetTransform(int pixels)
        {
            var k = Size / pixels;
            var cos = k * Math.Cos(Angle);
            var sin = k * Math.Sin(Angle);
            var half = (pixels - 1) / 2.0;

            var transform = new Matrix<double>(2, 3);
            transform[0, 0] = cos;
            transform[0, 1] = -sin;
            transform[0, 2] = Center.X - cos * half + sin * half;
            transform[1, 0] = sin;
            transform[1, 1] = cos;
            transform[1, 2] = Center.Y - sin * half - cos * half;
            return transform;
        }
    }

    /// <summary>
    /// How an eye image was cut out of the webcam image. Travels with the eye image as its
    /// ImageSourceData.
    /// </summary>
    internal sealed class EyeCrop
    {
        /// <summary>
        /// Side of the eye image, in multiples of the distance between the eye corners. The iris
        /// network was trained with 2.3.
        /// </summary>
        public const float SizeInEyeWidths = 2.3f;

        public EyeCrop(bool found, RotatedSquare region)
        {
            Found = found;
            Region = region;
        }

        /// <summary>
        /// False if the face was not found: the eye image is then black.
        /// </summary>
        public bool Found { get; }

        /// <summary>
        /// Region of the webcam image the eye image shows.
        /// </summary>
        public RotatedSquare Region { get; }
    }
}
