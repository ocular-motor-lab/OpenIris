//-----------------------------------------------------------------------
// <copyright file="AppearanceStrip.cs">
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
    /// The image of both eyes that the eye-embedding network of saccade.js takes
    /// (https://saccade.jspsych.org): a level box around both eyes from points of MediaPipe's face
    /// mesh, gray, stretched to 144 × 36 pixels and with its contrast equalized (CLAHE). It must be
    /// made exactly as the network was trained, or the network "will load, run, and produce
    /// nonsense": this is saccade.js's extractEyeCrop (packages/core/src/crop.ts, MIT license), which
    /// reproduces OpenCV's functions bit for bit, so here it calls them. The tests check it against
    /// saccade.js's own test images.
    /// </summary>
    public static class AppearanceStrip
    {
        /// <summary>Width of the strip, in pixels.</summary>
        public const int Width = 144;

        /// <summary>Height of the strip, in pixels.</summary>
        public const int Height = 36;

        // Points of the face mesh around the eyes: the outer corners (beyond them, on the left and right of
        // the image) and the brows and lower lids of each eye.
        private const int LeftOfImage = 226;
        private const int RightOfImage = 446;
        private const int Top1 = 27;
        private const int Top2 = 257;
        private const int Bottom1 = 23;
        private const int Bottom2 = 253;

        // Margin around the points, in fractions of the image's width (horizontal) and height (vertical).
        private const double Padding = 0.01;

        /// <summary>
        /// The box around the eyes in the image, or null if it is empty (no face, or out of the image).
        /// </summary>
        /// <param name="mesh">The 468 (or 478) points of the face mesh, in pixels, with 0 at the center
        /// of the first pixel.</param>
        /// <param name="imageSize">Size of the image.</param>
        public static Rectangle? Region(PointF[] mesh, Size imageSize)
        {
            if (mesh is null) throw new ArgumentNullException(nameof(mesh));

            // saccade.js has MediaPipe's coordinates, fractions of the image with 0 at the edge of the
            // first pixel: x * W is the pixel coordinate here plus 0.5.
            double w = imageSize.Width;
            double h = imageSize.Height;
            var left = (int)Math.Truncate(mesh[LeftOfImage].X + 0.5 - Padding * w);
            var right = (int)Math.Truncate(mesh[RightOfImage].X + 0.5 + Padding * w);
            var top = (int)Math.Truncate(Math.Min(mesh[Top1].Y, mesh[Top2].Y) + 0.5 - Padding * h);
            var bottom = (int)Math.Truncate(Math.Max(mesh[Bottom1].Y, mesh[Bottom2].Y) + 0.5 + Padding * h);
            if (right - left <= 0 || bottom - top <= 0) return null;

            left = Math.Max(0, left);
            top = Math.Max(0, top);
            right = Math.Min(imageSize.Width, right);
            bottom = Math.Min(imageSize.Height, bottom);
            if (right - left <= 0 || bottom - top <= 0) return null;

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        /// <summary>
        /// Cuts the strip out of a gray image (gray as OpenCV's BGR to gray, BT.601).
        /// </summary>
        /// <param name="image">The gray image of the face.</param>
        /// <param name="mesh">The points of the face mesh, in the image's pixels.</param>
        /// <returns>The strip, or null if the box around the eyes is empty.</returns>
        public static Image<Gray, byte>? Cut(Image<Gray, byte> image, PointF[] mesh)
        {
            if (image is null) throw new ArgumentNullException(nameof(image));
            if (!(Region(mesh, image.Size) is Rectangle region)) return null;

            using var crop = new Mat(image.Mat, region);
            return Prepare(crop);
        }

        /// <summary>
        /// Stretches a gray crop of both eyes to the strip and equalizes its contrast.
        /// </summary>
        /// <param name="crop">The crop.</param>
        public static Image<Gray, byte> Prepare(IInputArray crop)
        {
            using var resized = new Mat();
            CvInvoke.Resize(crop, resized, new Size(Width, Height), 0, 0, Inter.Linear);
            var strip = new Image<Gray, byte>(Width, Height);
            CvInvoke.CLAHE(resized, 2.0, new Size(8, 8), strip);
            return strip;
        }
    }
}
