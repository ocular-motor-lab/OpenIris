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
    /// Calibration files: they load with exactly the two eyes that were saved, and the torsion
    /// reference image, saved as a BMP, works both ways: OpenIris used to write and read it with
    /// System.Drawing (Windows only) and now uses OpenCV.
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

        /// <summary>
        /// The reference keeps the whole image of the frame (a copy, whatever its ROI), and it is saved
        /// and read back unchanged. Files without it read as null.
        /// </summary>
        [TestMethod]
        public void ReferenceImageIsKeptAndRoundTrips()
        {
            using var image = TestImage();
            using var frame = new ImageEye(image.Clone(), Eye.Left, ImageEyeTimestamp.Empty, null);
            frame.Image.ROI = new Rectangle(2, 2, 10, 10);

            var calibration = new EyeCalibration(Eye.Left);
            calibration.SetReference(frame);
            frame.Image.SetValue(new Gray(0));
            AssertSameImage(image, calibration.ReferenceImage);

            var read = new EyeCalibration { ReferenceImageSerialized = calibration.ReferenceImageSerialized };
            AssertSameImage(image, read.ReferenceImage);

            Assert.IsNull(new EyeCalibration().ReferenceImageSerialized);
            Assert.IsNull(new EyeCalibration { ReferenceImageSerialized = null }.ReferenceImage);
        }

        /// <summary>
        /// The head model and the depth of the eye model are saved and read back; a file without them
        /// (older) reads the defaults.
        /// </summary>
        [TestMethod]
        public void HeadModelAndEyeDepthRoundTrip()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var path = Path.Combine(Path.GetTempPath(), "OpenIrisCalibrationTest-" + System.Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                var saved = new CalibrationParameters();
                saved.EyeCalibrationParameters[Eye.Left].SetEyeModel(new EyePhysicalModel(new PointF(100, 110), 120) { CenterDepth = 900 });
                saved.HeadCalibrationParameters = new HeadCalibration
                {
                    ReferenceHeadData = new HeadData(),
                    HeadModel = new HeadModel { LeftEye = new System.Numerics.Vector3(-300, 2, 3), RightEye = new System.Numerics.Vector3(310, 4, 5), InterpupillaryDistance = 60, NeckToEyes = new System.Numerics.Vector3(70, 0, 80) },
                };
                saved.Save(path);

                var loaded = CalibrationParameters.Load(path);
                Assert.AreEqual(900, loaded.EyeCalibrationParameters[Eye.Left].EyePhysicalModel.CenterDepth);
                var model = loaded.HeadCalibrationParameters!.HeadModel;
                Assert.AreEqual(new System.Numerics.Vector3(-300, 2, 3), model.LeftEye);
                Assert.AreEqual(new System.Numerics.Vector3(310, 4, 5), model.RightEye);
                Assert.AreEqual(60, model.InterpupillaryDistance);
                Assert.AreEqual(new System.Numerics.Vector3(70, 0, 80), model.NeckToEyes);

                // An older file: no head model, no depth.
                var text = File.ReadAllText(path);
                var start = text.IndexOf("<HeadModel>", System.StringComparison.Ordinal);
                var end = text.IndexOf("</HeadModel>", System.StringComparison.Ordinal) + "</HeadModel>".Length;
                text = text.Remove(start, end - start).Replace("<CenterDepth>900</CenterDepth>", "");
                File.WriteAllText(path, text);
                var old = CalibrationParameters.Load(path);
                Assert.AreEqual(0, old.EyeCalibrationParameters[Eye.Left].EyePhysicalModel.CenterDepth);
                Assert.AreEqual(63, old.HeadCalibrationParameters!.HeadModel.InterpupillaryDistance);
                Assert.AreEqual(System.Numerics.Vector3.Zero, old.HeadCalibrationParameters.HeadModel.LeftEye);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Reading used to add the two eyes in the file to the two the constructor creates.
        /// </summary>
        [TestMethod]
        public void LoadedCalibrationHasTheTwoSavedEyes()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var path = Path.Combine(Path.GetTempPath(), "OpenIrisCalibrationTest-" + System.Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                var saved = new CalibrationParameters();
                saved.EyeCalibrationParameters[Eye.Left].SetEyeModel(new EyePhysicalModel(new PointF(100, 110), 120));
                saved.EyeCalibrationParameters[Eye.Right].SetEyeModel(new EyePhysicalModel(new PointF(200, 210), 130));
                saved.Save(path);

                var loaded = CalibrationParameters.Load(path);

                Assert.AreEqual(2, loaded.EyeCalibrationParameters.Count);
                Assert.AreEqual(Eye.Left, loaded.EyeCalibrationParameters[Eye.Left].WhichEye);
                Assert.AreEqual(new PointF(100, 110), loaded.EyeCalibrationParameters[Eye.Left].EyePhysicalModel.Center);
                Assert.AreEqual(Eye.Right, loaded.EyeCalibrationParameters[Eye.Right].WhichEye);
                Assert.AreEqual(new PointF(200, 210), loaded.EyeCalibrationParameters[Eye.Right].EyePhysicalModel.Center);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
