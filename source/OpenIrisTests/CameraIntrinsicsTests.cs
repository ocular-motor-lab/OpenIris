//-----------------------------------------------------------------------
// <copyright file="CameraIntrinsicsTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris.ImageGrabbing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The intrinsics follow the image (window, crops, camera orientation), and the calibration follows
    /// them when the window of the camera moves (#61).
    /// </summary>
    [TestClass]
    public class CameraIntrinsicsTests
    {
        /// <summary>
        /// A pixel at the principal point stays at the principal point, whatever the orientation does to
        /// the image.
        /// </summary>
        [TestMethod]
        public void PrincipalPointFollowsTheOrientation()
        {
            foreach (CameraOrientation orientation in Enum.GetValues(typeof(CameraOrientation)))
            {
                var image = new ImageEye(new Image<Gray, byte>(40, 30), Eye.Left, ImageEyeTimestamp.Empty, null)
                {
                    Intrinsics = new CameraIntrinsics(100, 110, 7, 5),
                };
                image.Image.Data[5, 7, 0] = 255;

                image.CorrectOrientation(orientation);

                var (x, y) = BrightPixel(image.Image);
                Assert.AreEqual(new PointF(x, y), new PointF(image.Intrinsics.Cx, image.Intrinsics.Cy), orientation.ToString());
                var swapped = image.Intrinsics.Fx == 110;
                Assert.AreEqual(image.Size.Width == 30, swapped, $"{orientation}: the focal lengths swap with the axes.");
            }
        }

        [TestMethod]
        public void CropsMoveThePrincipalPoint()
        {
            var image = new ImageEye(new Image<Gray, byte>(40, 30), Eye.Both, ImageEyeTimestamp.Empty, null)
            {
                Intrinsics = new CameraIntrinsics(100, 100, 20, 15),
            };

            Assert.AreEqual(new CameraIntrinsics(100, 100, 0, 5), image.Copy(new Rectangle(20, 10, 20, 20)).Intrinsics);
            Assert.AreEqual(image.Intrinsics, image.Copy().Intrinsics);
            Assert.IsTrue(new ImageEye(new Image<Gray, byte>(4, 4), Eye.Left, ImageEyeTimestamp.Empty, null).Copy(new Rectangle(1, 1, 2, 2)).Intrinsics.IsEmpty, "Unknown stays unknown.");
        }

        [TestMethod]
        public void WithoutCalibrationFromThePixelScale()
        {
            var intrinsics = CameraIntrinsics.FromPixelScale(new Size(721, 541), 8, 50);
            Assert.AreEqual(new CameraIntrinsics(400, 400, 360, 270), intrinsics);
        }

        /// <summary>
        /// The window moves 30 px right and 20 px down on the sensor: the eye looks the same, so the eye
        /// appears 30 px left and 20 px up in the image, and the calibrated position doesn't change.
        /// </summary>
        [TestMethod]
        public void CalibratedDataDoesNotChangeWhenTheWindowMoves()
        {
            var sensor = new CameraIntrinsics(400, 400, 360, 270);
            var calibration = new CalibrationParameters();
            var eyeCalibration = calibration.EyeCalibrationParameters[Eye.Left];
            eyeCalibration.Intrinsics = sensor.Cropped(new Point(100, 50));
            eyeCalibration.SetEyeModel(new EyePhysicalModel(new PointF(200, 150), 150));
            eyeCalibration.ReferenceData = Data(new PointF(200, 150), eyeCalibration.Intrinsics);

            var before = calibration.GetCalibratedEyeData(Data(new PointF(230, 120), sensor.Cropped(new Point(100, 50))));
            var after = calibration.GetCalibratedEyeData(Data(new PointF(200, 100), sensor.Cropped(new Point(130, 70))));

            Assert.AreEqual(before.HorizontalPosition, after.HorizontalPosition, 1e-9);
            Assert.AreEqual(before.VerticalPosition, after.VerticalPosition, 1e-9);
            Assert.AreNotEqual(0, before.HorizontalPosition);

            Assert.AreEqual(new PointF(170, 130), eyeCalibration.EyeModelFor(sensor.Cropped(new Point(130, 70))).Center);
            Assert.AreEqual(new PointF(170, 130), eyeCalibration.ReferencePupilFor(sensor.Cropped(new Point(130, 70))).Center);

            var unknown = calibration.GetCalibratedEyeData(Data(new PointF(230, 120), default));
            Assert.AreEqual(before.HorizontalPosition, unknown.HorizontalPosition, 1e-9, "Unknown intrinsics: no shift.");
        }

        [TestMethod]
        public void IntrinsicsAreSavedWithTheCalibration()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var path = Path.Combine(Path.GetTempPath(), "OpenIrisCalibrationTest-" + Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                var saved = new CalibrationParameters();
                saved.EyeCalibrationParameters[Eye.Right].Intrinsics = new CameraIntrinsics(400, 401, 260.5f, 170);
                saved.Save(path);

                var loaded = CalibrationParameters.Load(path);
                Assert.AreEqual(new CameraIntrinsics(400, 401, 260.5f, 170), loaded.EyeCalibrationParameters[Eye.Right].Intrinsics);
                Assert.IsTrue(loaded.EyeCalibrationParameters[Eye.Left].Intrinsics.IsEmpty);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static EyeData Data(PointF pupil, CameraIntrinsics intrinsics) => new EyeData
        {
            WhichEye = Eye.Left,
            ProcessFrameResult = ProcessFrameResult.Good,
            Pupil = new PupilData(pupil, new SizeF(20, 20), 0),
            Iris = new IrisData(pupil, 60),
            DataQuality = 100,
            Intrinsics = intrinsics,
        };

        private static (int x, int y) BrightPixel(Image<Gray, byte> image)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image.Data[y, x, 0] == 255) return (x, y);
                }
            }

            throw new AssertFailedException("No bright pixel.");
        }
    }
}
