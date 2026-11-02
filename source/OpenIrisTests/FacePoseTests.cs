//-----------------------------------------------------------------------
// <copyright file="FacePoseTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks the head pose from points of the face: a face with a known pose is projected into a
    /// camera, and the pose found from its points must have the angles and position given, with the
    /// signs of CalibratedHeadData (positive to the subject's right, chin up, right ear down).
    /// </summary>
    [TestClass]
    public class FacePoseTests
    {
        // The generic face of FacePose (mm; X to the subject's left, Y up, Z out of the face).
        private static readonly MCvPoint3D32f[] Face =
        {
            new MCvPoint3D32f(0, 0, 0), new MCvPoint3D32f(0, -66, -13),
            new MCvPoint3D32f(-45, 34, -27), new MCvPoint3D32f(45, 34, -27),
            new MCvPoint3D32f(-30, -30, -25), new MCvPoint3D32f(30, -30, -25),
        };

        private static readonly Size ImageSize = new Size(1280, 720);
        private const double FieldOfView = 65;

        [DataTestMethod]
        [DataRow(20, 0, 0, DisplayName = "Head turned 20° to the subject's right")]
        [DataRow(-20, 0, 0, DisplayName = "Head turned 20° to the subject's left")]
        [DataRow(0, 15, 0, DisplayName = "Chin 15° up")]
        [DataRow(0, 0, 10, DisplayName = "Right ear 10° down")]
        [DataRow(15, -10, 5, DisplayName = "All three")]
        public void AnglesHaveTheConventionsSigns(double yaw, double pitch, double roll)
        {
            var pose = FacePose.Estimate(Project(HeadRotation(yaw, pitch, roll), 0, 0, 500), ImageSize, FieldOfView);
            var head = FacePose.Calibrated(pose, null);

            Assert.AreEqual(yaw, head.Yaw, 0.5, "Yaw");
            Assert.AreEqual(pitch, head.Pitch, 0.5, "Pitch");
            Assert.AreEqual(roll, head.Roll, 0.5, "Roll");
            Assert.AreEqual(500, head.Z, 1, "Distance from the camera");
        }

        [TestMethod]
        public void PositionIsFromTheCamera()
        {
            // 50 mm to the subject's left is to the right in the camera's image (+x), 30 mm up is -y.
            var pose = FacePose.Estimate(Project(HeadRotation(0, 0, 0), 50, -30, 600), ImageSize, FieldOfView);
            var head = FacePose.Calibrated(pose, null);

            Assert.AreEqual(-50, head.X, 1, "To the subject's right");
            Assert.AreEqual(30, head.Y, 1, "Up");
            Assert.AreEqual(600, head.Z, 1, "Distance");
        }

        [TestMethod]
        public void AnglesAreRelativeToTheReference()
        {
            var reference = FacePose.Estimate(Project(HeadRotation(20, 0, 0), 0, 0, 500), ImageSize, FieldOfView);
            var pose = FacePose.Estimate(Project(HeadRotation(30, 0, 0), 0, 0, 500), ImageSize, FieldOfView);

            Assert.AreEqual(10, FacePose.Calibrated(pose, reference).Yaw, 0.5);
            Assert.AreEqual(0, FacePose.Calibrated(reference, reference).Yaw, 0.5);
            Assert.IsTrue(double.IsNaN(FacePose.Calibrated(null, reference).Yaw), "No face, no angles.");
        }

        /// <summary>
        /// Rotation of the head from facing the camera, in the axes of the face (X to the subject's
        /// left), in Fick order, built from rotations about each axis with the signs worked out by hand:
        /// yaw to the subject's right turns the face towards -X, chin up turns it towards +Y, and right
        /// ear down turns its right side (-X) towards -Y.
        /// </summary>
        private static double[,] HeadRotation(double yaw, double pitch, double roll)
        {
            var ry = Rotation(1, -yaw);
            var rx = Rotation(0, -pitch);
            var rz = Rotation(2, roll);
            return Multiply(Multiply(ry, rx), rz);
        }

        private static double[,] Rotation(int axis, double degrees)
        {
            var c = Math.Cos(degrees * Math.PI / 180);
            var s = Math.Sin(degrees * Math.PI / 180);
            return axis switch
            {
                0 => new double[,] { { 1, 0, 0 }, { 0, c, -s }, { 0, s, c } },
                1 => new double[,] { { c, 0, s }, { 0, 1, 0 }, { -s, 0, c } },
                _ => new double[,] { { c, -s, 0 }, { s, c, 0 }, { 0, 0, 1 } },
            };
        }

        private static double[,] Multiply(double[,] a, double[,] b)
        {
            var result = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        result[i, j] += a[i, k] * b[k, j];
            return result;
        }

        /// <summary>
        /// The points of the face in the image, with the head rotated and at (x, y, z) mm in the
        /// camera's axes (x right in the image, y down, z away from the camera).
        /// </summary>
        private static PointF[] Project(double[,] headRotation, double x, double y, double z)
        {
            // Facing the camera, the face's axes are the camera's with y and z flipped.
            var cameraRotation = Multiply(new double[,] { { 1, 0, 0 }, { 0, -1, 0 }, { 0, 0, -1 } }, headRotation);
            using var rotation = new Matrix<double>(cameraRotation);
            using var rotationVector = new Matrix<double>(3, 1);
            CvInvoke.Rodrigues(rotation, rotationVector);
            using var translation = new Matrix<double>(new double[,] { { x }, { y }, { z } });

            var focal = ImageSize.Width / 2.0 / Math.Tan(FieldOfView * Math.PI / 360);
            using var camera = new Matrix<double>(new double[,] { { focal, 0, ImageSize.Width / 2.0 }, { 0, focal, ImageSize.Height / 2.0 }, { 0, 0, 1 } });
            using var distortion = new Matrix<double>(4, 1);
            return CvInvoke.ProjectPoints(Face, rotationVector, translation, camera, distortion);
        }
    }
}
