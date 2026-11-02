//-----------------------------------------------------------------------
// <copyright file="OrientationTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Checks that each camera orientation turns the image as documented: rotated clockwise by the
    /// angle, then mirrored horizontally.
    /// </summary>
    [TestClass]
    public class OrientationTests
    {
        [DataTestMethod]
        [DataRow(CameraOrientation.Upright, 0, false)]
        [DataRow(CameraOrientation.Rotated90, 90, false)]
        [DataRow(CameraOrientation.Rotated180, 180, false)]
        [DataRow(CameraOrientation.Rotated270, 270, false)]
        [DataRow(CameraOrientation.UprightMirrored, 0, true)]
        [DataRow(CameraOrientation.Rotated90Mirrored, 90, true)]
        [DataRow(CameraOrientation.Rotated180Mirrored, 180, true)]
        [DataRow(CameraOrientation.Rotated270Mirrored, 270, true)]
        public void ImageIsRotatedClockwiseThenMirrored(CameraOrientation orientation, int angle, bool mirrored)
        {
            const int width = 4, height = 3;
            var original = new Image<Gray, byte>(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    original.Data[y, x, 0] = (byte)(10 * y + x);

            var image = new ImageEye(original.Copy(), Eye.Left, new ImageEyeTimestamp(0, 0), null);
            image.CorrectOrientation(orientation);

            var rotatedSize = angle % 180 == 0 ? new System.Drawing.Size(width, height) : new System.Drawing.Size(height, width);
            Assert.AreEqual(rotatedSize, image.Size);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Where the pixel goes when rotating clockwise, then mirroring.
                    var (newX, newY) = angle switch
                    {
                        0 => (x, y),
                        90 => (height - 1 - y, x),
                        180 => (width - 1 - x, height - 1 - y),
                        270 => (y, width - 1 - x),
                        _ => throw new ArgumentException(nameof(angle)),
                    };
                    if (mirrored) newX = rotatedSize.Width - 1 - newX;

                    Assert.AreEqual(original.Data[y, x, 0], image.Image.Data[newY, newX, 0], $"Pixel ({x}, {y}) should be at ({newX}, {newY}).");
                }
            }
        }
    }
}
