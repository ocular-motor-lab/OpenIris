//-----------------------------------------------------------------------
// <copyright file="CalibratedDataConventionsTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Linq;
    using Emgu.CV;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The signs of the calibrated eye data (see CalibratedEyeData): positive is rightward, upward
    /// and clockwise, from the subject's point of view. The eye images show the eye as seen by
    /// someone facing the subject, so the subject's right is the left of the image.
    /// </summary>
    [TestClass]
    public class CalibratedDataConventionsTests
    {
        private static readonly PointF Center = new PointF(200, 200);
        private const float GlobeRadius = 120;

        [TestMethod]
        public void LookingToTheSubjectsRightIsPositiveHorizontal()
        {
            // 10 px towards the left of the image.
            var data = Calibrated(new PointF(Center.X - 10, Center.Y));
            Assert.AreEqual(Math.Asin(10 / GlobeRadius) * 180 / Math.PI, data.HorizontalPosition, 1e-4);
            Assert.AreEqual(0, data.VerticalPosition, 1e-4);
        }

        [TestMethod]
        public void LookingUpIsPositiveVertical()
        {
            // 10 px towards the top of the image (y grows down).
            var data = Calibrated(new PointF(Center.X, Center.Y - 10));
            Assert.AreEqual(0, data.HorizontalPosition, 1e-4);
            Assert.AreEqual(Math.Asin(10 / GlobeRadius) * 180 / Math.PI, data.VerticalPosition, 1e-4);
        }

        /// <summary>
        /// Clockwise from the subject's point of view is counterclockwise in the image. OpenCV's
        /// rotation is counterclockwise on screen for positive angles.
        /// </summary>
        [DataTestMethod]
        [DataRow(5.0)]
        [DataRow(-5.0)]
        public void ClockwiseFromTheSubjectsPointOfViewIsPositiveTorsion(double counterclockwiseInImage)
        {
            var eye = Eye.Left;
            var pupil = SyntheticEye.GetTruth(eye, 0).PupilCenter;
            ImageEye Rotated()
            {
                var image = SyntheticEye.Draw(eye, 0);
                using var rotation = new Mat();
                CvInvoke.GetRotationMatrix2D(pupil, counterclockwiseInImage, 1, rotation);
                CvInvoke.WarpAffine(image.Image, image.Image, rotation, image.Image.Size, Emgu.CV.CvEnum.Inter.Linear);
                return image;
            }

            var data = PipelineHarness.Run("JOM", eye, Enumerable.Range(0, 3).Select(_ => Rotated()), SyntheticEye.MmPerPix,
                reference: SyntheticEye.Draw(eye, 0), configure: PipelineHarness.Thresholds(PipelineHarness.SyntheticEyeDarkThreshold));

            foreach (var d in data) Assert.AreEqual(counterclockwiseInImage, d.TorsionAngle, 0.2);
        }

        /// <summary>
        /// Calibrated data for a pupil, with the reference pupil and the eye globe at the center.
        /// </summary>
        private static CalibratedEyeData Calibrated(PointF pupil)
        {
            EyeData Data(PointF center) => new EyeData
            {
                WhichEye = Eye.Left,
                Pupil = new PupilData(center, new SizeF(40, 40), 0),
                Iris = new IrisData(center, 80),
                ProcessFrameResult = ProcessFrameResult.Good,
                DataQuality = 100,
            };

            var calibration = new CalibrationParameters();
            calibration.EyeCalibrationParameters[Eye.Left].SetEyeModel(new EyePhysicalModel(Center, GlobeRadius));
            calibration.EyeCalibrationParameters[Eye.Left].ReferenceData = Data(Center);
            return calibration.GetCalibratedEyeData(Data(pupil));
        }
    }
}
