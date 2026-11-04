//-----------------------------------------------------------------------
// <copyright file="CameraCalibratorTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The camera calibration tool with images of the dot grid rendered through a known camera: a
    /// window of the sensor, tilted below the eye and rolled, a bit out of focus and noisy.
    /// </summary>
    [TestClass]
    public class CameraCalibratorTests
    {
        private static readonly Size Sensor = new Size(1440, 1080);
        private static readonly Rectangle Window = new Rectangle(360, 300, 720, 450);
        private const float F = 1400; // 20 px/mm at 70 mm

        private static readonly CameraIntrinsics CameraK = new CameraIntrinsics(F, F, (Sensor.Width - 1) / 2f, (Sensor.Height - 1) / 2f);

        // The camera 15 deg below straight ahead and rolled 5 deg, 70 mm from the eye.
        private static readonly CameraExtrinsics TrueExtrinsics = Turned(
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, 15 * (float)Math.PI / 180) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, 5 * (float)Math.PI / 180), 70);

        private static CameraExtrinsics Turned(Quaternion headTurn, float distanceMm)
        {
            var q = CameraExtrinsics.Facing(distanceMm).Rotation * Quaternion.Inverse(headTurn);
            if (q.W < 0) q = Quaternion.Negate(q);
            var angle = 2 * Math.Acos(Math.Min(1, q.W));
            var axis = Vector3.Normalize(new Vector3(q.X, q.Y, q.Z));
            var rvec = axis * (float)angle;
            return new CameraExtrinsics(rvec.X, rvec.Y, rvec.Z, 0, 0, distanceMm);
        }

        // The grid A at a position along the head's forward axis, as the camera sees it in its window.
        private static CameraCalibrationCapture Render(double positionMm, CameraCalibrationOptions options)
        {
            var imageK = CameraK.Cropped(Window.Location);
            const int Super = 4;
            using var big = new Image<Gray, byte>(Window.Width * Super, Window.Height * Super, new Gray(200));

            var target = options.Target;
            var half = target.DotsPerSide / 2;
            var points = new List<MCvPoint3D32f>();
            for (int row = -half; row <= half; row++)
            {
                for (int column = -half; column <= half; column++)
                {
                    if (CameraCalibrationTarget.Fiducial.Contains((column, row))) continue;
                    points.Add(new MCvPoint3D32f((float)(options.EyeCenterBehindTargetMm + positionMm), (float)(column * options.PitchXMm), (float)(-row * options.PitchYMm)));
                }
            }

            using var rvec = new Matrix<double>(new double[] { TrueExtrinsics.Rx, TrueExtrinsics.Ry, TrueExtrinsics.Rz });
            using var tvec = new Matrix<double>(new double[] { TrueExtrinsics.Tx, TrueExtrinsics.Ty, TrueExtrinsics.Tz });
            using var k = new Matrix<double>(new double[,] { { imageK.Fx, 0, imageK.Cx }, { 0, imageK.Fy, imageK.Cy }, { 0, 0, 1 } });
            using var noDistortion = new Matrix<double>(1, 5);
            var projected = CvInvoke.ProjectPoints(points.ToArray(), rvec, tvec, k, noDistortion, null, 0);

            var radius = target.DotDiameterMm / 2 * F / 70 * Super;
            foreach (var p in projected)
            {
                // In 1/16 of a pixel (shift 4), so the centers aren't rounded.
                var center = new Point((int)Math.Round(((p.X + 0.5) * Super - 0.5) * 16), (int)Math.Round(((p.Y + 0.5) * Super - 0.5) * 16));
                CvInvoke.Circle(big, center, (int)Math.Round(radius * 16), new MCvScalar(40), -1, LineType.AntiAlias, 4);
            }

            var image = new Image<Gray, byte>(Window.Width, Window.Height);
            CvInvoke.Resize(big, image, image.Size, 0, 0, Inter.Area);
            image = image.SmoothGaussian(0, 0, 1.5, 1.5);
            using var noise = new Image<Gray, byte>(image.Size);
            CvInvoke.Randn(noise, new MCvScalar(0), new MCvScalar(4));
            image = image.Add(noise);

            var grid = DotGrid.Find(image);
            Assert.IsNotNull(grid, "Dots found");
            Assert.IsTrue(grid!.FiducialFound, "Fiducial found");
            Assert.IsFalse(grid.Mirrored, "Not mirrored");
            return new CameraCalibrationCapture(positionMm, grid, image.Size, imageK, 1);
        }

        private static double AngleBetween(Quaternion a, Quaternion b) =>
            2 * Math.Acos(Math.Min(1, Math.Abs(Quaternion.Dot(a, b)))) * 180 / Math.PI;

        /// <summary>
        /// The dots are numbered as printed: the center of the fiducial is where the eye is.
        /// </summary>
        [TestMethod]
        public void FindsAndNumbersTheDots()
        {
            var options = new CameraCalibrationOptions();
            var capture = Render(0, options);

            // The window sees about 31 x 19 mm of the 30 mm grid.
            Assert.IsTrue(capture.Grid.Count > 400, $"Dots found: {capture.Grid.Count}");
            Assert.AreEqual(capture.Grid.Count, capture.Grid.Indices.Distinct().Count(), "Each dot numbered once");
            Assert.IsFalse(capture.Grid.Indices.Any(i => CameraCalibrationTarget.Fiducial.Contains(i)), "No dot where the fiducial is");

            // The dot to the left of the center is where the camera puts it.
            var eye = new MCvPoint3D32f((float)options.EyeCenterBehindTargetMm, (float)-options.PitchXMm, 0);
            var imageK = CameraK.Cropped(Window.Location);
            using var rvec = new Matrix<double>(new double[] { TrueExtrinsics.Rx, TrueExtrinsics.Ry, TrueExtrinsics.Rz });
            using var tvec = new Matrix<double>(new double[] { TrueExtrinsics.Tx, TrueExtrinsics.Ty, TrueExtrinsics.Tz });
            using var k = new Matrix<double>(new double[,] { { imageK.Fx, 0, imageK.Cx }, { 0, imageK.Fy, imageK.Cy }, { 0, 0, 1 } });
            using var noDistortion = new Matrix<double>(1, 5);
            var expected = CvInvoke.ProjectPoints(new[] { eye }, rvec, tvec, k, noDistortion, null, 0)[0];
            var found = capture.Grid.Positions[Array.IndexOf(capture.Grid.Indices, (-1, 0))];
            Assert.IsTrue(Math.Abs(found.X - expected.X) < 0.2 && Math.Abs(found.Y - expected.Y) < 0.2, $"Dot left of center at {found}, expected {expected}");
        }

        /// <summary>
        /// With the target moved in steps of 1.5 mm, the focal length, the pose of the camera and the
        /// pixels per mm come back.
        /// </summary>
        [TestMethod]
        public void RecoversTheCameraFromDepthSteps()
        {
            var options = new CameraCalibrationOptions();
            var captures = new[] { -1.5, 0, 1.5 }.Select(z => Render(z, options)).ToArray();

            var result = CameraCalibrator.Calibrate(captures, CameraK, options, "Test camera");
            Console.WriteLine(result.Report);

            Assert.IsTrue(result.DistanceMeasured);
            Assert.IsTrue(result.RmsPixels < 0.3, $"Error of the fit {result.RmsPixels} px");
            Assert.AreEqual(F, result.Intrinsics.Fx, F * 0.02, "Focal length");
            Assert.AreEqual(CameraK.Cx, result.Intrinsics.Cx, "Principal point kept");
            Assert.AreEqual(CameraK.Cy, result.Intrinsics.Cy, "Principal point kept");
            var angle = AngleBetween(TrueExtrinsics.Rotation, result.Extrinsics.Rotation);
            Assert.IsTrue(angle < 0.5, $"Rotation off by {angle} deg");
            Assert.AreEqual(70, result.Extrinsics.Tz, 1.5, "Distance");

            // The target is 10 mm in front of the eye center, which the camera sees 15 deg from straight ahead.
            var expectedPixPerMm = F / (70 - options.EyeCenterBehindTargetMm * Math.Cos(15 * Math.PI / 180));
            Assert.AreEqual(expectedPixPerMm, result.PixPerMm, expectedPixPerMm * 0.01, "Pixels per mm");
            StringAssert.Contains(result.Report, "below straight ahead");
            StringAssert.Contains(result.Report, "CameraIntrinsics: Fx");
        }

        /// <summary>
        /// With every view at the same position, the distance is assumed, and the report says so; the
        /// pixels per mm are still measured.
        /// </summary>
        [TestMethod]
        public void OnePositionAssumesTheDistance()
        {
            var options = new CameraCalibrationOptions { AssumedDistanceMm = 60 };
            var captures = new[] { Render(0, options), Render(0, options) };

            var result = CameraCalibrator.Calibrate(captures, CameraK, options, "Test camera");

            Assert.IsFalse(result.DistanceMeasured);
            var expectedPixPerMm = F / (70 - options.EyeCenterBehindTargetMm * Math.Cos(15 * Math.PI / 180));
            Assert.AreEqual(expectedPixPerMm, result.PixPerMm, expectedPixPerMm * 0.02, "Pixels per mm");
            StringAssert.Contains(result.Report, "ASSUMED");
        }

        /// <summary>
        /// A mirrored image (wrong camera orientation) is noticed.
        /// </summary>
        [TestMethod]
        public void NoticesAMirroredImage()
        {
            var options = new CameraCalibrationOptions();
            var capture = Render(0, options);
            var mirrored = DotGrid.Find(RenderMirrored(options));
            Assert.IsNotNull(mirrored);
            Assert.IsTrue(mirrored!.FiducialFound);
            Assert.IsTrue(mirrored.Mirrored);
            Assert.ThrowsException<InvalidOperationException>(() => CameraCalibrator.Calibrate(
                new[] { new CameraCalibrationCapture(0, mirrored, capture.ImageSize, capture.ImageIntrinsics, 1) }, CameraK, options, "Test camera"));
        }

        private static Image<Gray, byte> RenderMirrored(CameraCalibrationOptions options)
        {
            var image = new Image<Gray, byte>(Window.Width, Window.Height, new Gray(200));
            var half = options.Target.DotsPerSide / 2;
            for (int row = -half; row <= half; row++)
            {
                for (int column = -half; column <= half; column++)
                {
                    if (CameraCalibrationTarget.Fiducial.Contains((column, row))) continue;
                    var center = new Point(Window.Width / 2 - 20 * column, Window.Height / 2 + 20 * row);
                    CvInvoke.Circle(image, center, 5, new MCvScalar(40), -1, LineType.AntiAlias);
                }
            }

            return image.SmoothGaussian(3);
        }
    }
}
