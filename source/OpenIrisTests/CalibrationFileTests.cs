//-----------------------------------------------------------------------
// <copyright file="CalibrationFileTests.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The torsion reference image is saved in calibration files as a BMP. OpenIris used to write and
    /// read it with System.Drawing (Windows only) and now uses OpenCV: files must work both ways.
    /// </summary>
    [TestClass]
    public class CalibrationFileTests
    {
        // An odd width, so the rows of the BMP need padding.
        private static Image<Gray, byte> TestImage()
        {
            var image = new Image<Gray, byte>(101, 37);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    image.Data[y, x, 0] = (byte)((x * 7 + y * 13) % 256);
                }
            }
            return image;
        }

        private static void AssertSameImage(Image<Gray, byte> expected, Image<Gray, byte>? actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Size, actual!.Size);
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    Assert.AreEqual(expected.Data[y, x, 0], actual.Data[y, x, 0], $"Pixel ({x}, {y})");
                }
            }
        }

        [TestMethod]
        public void ReadsTorsionReferenceWrittenTheOldWay()
        {
            using var image = TestImage();
            using var ms = new MemoryStream();
            using var bitmap = image.ToBitmap();
            bitmap.Save(ms, ImageFormat.Bmp);

            var calibration = new EyeCalibration { ImageTorsionReferenceSerialized = ms.ToArray() };

            AssertSameImage(image, calibration.ImageTorsionReference);
        }

        [TestMethod]
        public void OldWayReadsTorsionReference()
        {
            using var image = TestImage();
            var calibration = new EyeCalibration { ImageTorsionReference = image };

            using var ms = new MemoryStream(calibration.ImageTorsionReferenceSerialized!);
            using var bitmap = new Bitmap(ms);

            AssertSameImage(image, bitmap.ToImage<Gray, byte>());
        }

        [TestMethod]
        public void TorsionReferenceRoundTrip()
        {
            using var image = TestImage();
            var saved = new EyeCalibration { ImageTorsionReference = image }.ImageTorsionReferenceSerialized;

            var calibration = new EyeCalibration { ImageTorsionReferenceSerialized = saved };

            AssertSameImage(image, calibration.ImageTorsionReference);
        }
    }
}
