//-----------------------------------------------------------------------
// <copyright file="EyeRotationMathTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Numerics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The calibrated angles from rotations (#61): Fick angles of Listing's rotation from the
    /// reference, plus the torsion.
    /// </summary>
    [TestClass]
    public class EyeRotationMathTests
    {
        private const double ToRadians = Math.PI / 180;
        private static readonly EyePhysicalModel Model = new EyePhysicalModel(new PointF(200, 150), 100);

        /// <summary>
        /// Where the pupil is in the image for an eye at the given Fick angles (OpenIris's signs), the
        /// reference being the eye looking at the camera: the line of sight is (cos h cos v, sin h cos v,
        /// -sin v) in the head frame (x forward, y left, z up) with h, v right-handed (left, down).
        /// </summary>
        private static PointF Pupil(double horizontal, double vertical, EyePhysicalModel model)
        {
            var h = -horizontal * ToRadians;
            var v = -vertical * ToRadians;
            var left = Math.Sin(h) * Math.Cos(v);
            var up = -Math.Sin(v);

            // The image: the subject's left is to the right (+x), up is up (-y).
            return new PointF((float)(model.Center.X + model.Radius * left), (float)(model.Center.Y - model.Radius * up));
        }

        private static (double h, double v, double t) Angles(PointF reference, PointF pupil, double torsion, EyePhysicalModel model, Eye eye = Eye.Left) =>
            EyeRotationMath.FickAngles(EyeRotationMath.FromLinesOfSight(
                EyeRotationMath.LineOfSight(reference, model, eye),
                EyeRotationMath.LineOfSight(pupil, model, eye),
                torsion));

        [DataTestMethod]
        [DataRow(20.0, 0.0)]
        [DataRow(0.0, -25.0)]
        [DataRow(20.0, 15.0)]
        [DataRow(-30.0, 25.0)]
        public void FickAnglesComeBack(double horizontal, double vertical)
        {
            var (h, v, t) = Angles(Model.Center, Pupil(horizontal, vertical, Model), double.NaN, Model);
            Assert.AreEqual(horizontal, h, 1e-3);
            Assert.AreEqual(vertical, v, 1e-3);
        }

        /// <summary>
        /// Pure horizontal and vertical are the same as the arcsines used before, and oblique positions
        /// differ: the old horizontal was the arcsine, Fick's is the angle in the horizontal plane.
        /// </summary>
        [TestMethod]
        public void SameAsBeforeOnTheAxes()
        {
            var x = 0.4;
            var (h, _, _) = Angles(Model.Center, new PointF((float)(Model.Center.X - x * Model.Radius), Model.Center.Y), double.NaN, Model);
            Assert.AreEqual(Math.Asin(x) / ToRadians, h, 1e-3);
        }

        [TestMethod]
        public void TorsionAddsToTheRotationButNotToTheDirection()
        {
            var (h0, v0, t0) = Angles(Model.Center, Model.Center, 5, Model);
            Assert.AreEqual(0, h0, 1e-4);
            Assert.AreEqual(0, v0, 1e-4);
            Assert.AreEqual(5, t0, 1e-4, "Looking at the reference, the torsion is the measured one.");

            var pupil = Pupil(20, 15, Model);
            var (h, v, t) = Angles(Model.Center, pupil, 5, Model);
            var (hL, vL, tL) = Angles(Model.Center, pupil, 0, Model);
            Assert.AreEqual(hL, h, 1e-4);
            Assert.AreEqual(vL, v, 1e-4);
            Assert.AreEqual(tL + 5, t, 0.05, "Measured torsion adds to Listing's (Fick) torsion.");
        }

        /// <summary>
        /// Listing's law: the axis of the rotation is perpendicular to the line of sight at the
        /// reference, so it has no forward component. In Fick coordinates that rotation has some
        /// torsion at oblique positions ("false torsion"): tan(t/2) = tan(h/2)·tan(v/2).
        /// </summary>
        [TestMethod]
        public void ListingsLawWithoutTorsion()
        {
            var rotation = EyeRotationMath.FromLinesOfSight(
                EyeRotationMath.LineOfSight(Model.Center, Model, Eye.Left),
                EyeRotationMath.LineOfSight(Pupil(20, 15, Model), Model, Eye.Left),
                double.NaN);
            Assert.AreEqual(0, rotation.X, 1e-6);

            var (_, _, t) = EyeRotationMath.FickAngles(rotation);
            var expected = 2 * Math.Atan(Math.Tan(10 * ToRadians) * Math.Tan(7.5 * ToRadians)) / ToRadians;
            Assert.AreEqual(expected, t, 0.05);
        }

        /// <summary>
        /// The reference doesn't need to be on the camera's axis: at the reference all is 0.
        /// </summary>
        [TestMethod]
        public void ReferenceAwayFromTheCamerasAxis()
        {
            var reference = new PointF(230, 120);
            var (h, v, t) = Angles(reference, reference, double.NaN, Model);
            Assert.AreEqual(0, h, 1e-4);
            Assert.AreEqual(0, v, 1e-4);
            Assert.AreEqual(0, t, 1e-4);

            var (h2, _, _) = Angles(reference, new PointF(220, 120), double.NaN, Model);
            Assert.IsTrue(h2 > 0, "The pupil moved to the left of the image: the eye looks to the subject's right.");
        }

        /// <summary>
        /// Kappa turns the line of sight from the pupillary axis towards the nose (the subject's left
        /// for the right eye, the image's +x) and up.
        /// </summary>
        [TestMethod]
        public void KappaTurnsTheLineOfSight()
        {
            var model = Model;
            model.KappaAngleH = 5;
            model.KappaAngleV = 2;

            var right = EyeRotationMath.LineOfSight(model.Center, model, Eye.Right);
            Assert.AreEqual(Math.Sin(5 * ToRadians) * Math.Cos(2 * ToRadians), right.X, 1e-4, "Right eye: towards the subject's left.");
            Assert.AreEqual(-Math.Sin(2 * ToRadians), right.Y, 1e-4, "Up.");

            var left = EyeRotationMath.LineOfSight(model.Center, model, Eye.Left);
            Assert.AreEqual(-right.X, left.X, 1e-6, "Left eye: towards the subject's right.");

            var (h, v, t) = Angles(model.Center, model.Center, double.NaN, model, Eye.Right);
            Assert.AreEqual(0, h, 1e-4, "Still 0 at the reference.");
        }

        [DataTestMethod]
        [DataRow(20.0, 15.0, 5.0)]
        [DataRow(-30.0, 25.0, -8.0)]
        [DataRow(10.0, -20.0, double.NaN)]
        public void GazeAndUpAgreeWithTheAngles(double horizontal, double vertical, double torsion)
        {
            var data = new CalibratedEyeData { HorizontalPosition = horizontal, VerticalPosition = vertical, TorsionalPosition = torsion };
            var h = horizontal * ToRadians;
            var v = vertical * ToRadians;

            // Right, up, forward: Fick's horizontal turns about up, then the vertical.
            Assert.AreEqual(Math.Sin(h) * Math.Cos(v), data.Gaze.X, 1e-5);
            Assert.AreEqual(Math.Sin(v), data.Gaze.Y, 1e-5);
            Assert.AreEqual(Math.Cos(h) * Math.Cos(v), data.Gaze.Z, 1e-5);
            Assert.AreEqual(0, Vector3.Dot(data.Gaze, data.Up), 1e-5);
            Assert.AreEqual(1, data.Up.Length(), 1e-5);

            // The angles come back from the rotation of the vectors.
            var (h2, v2, t2) = EyeRotationMath.FickAngles(EyeRotationMath.FromFickAngles(horizontal, vertical, torsion));
            Assert.AreEqual(horizontal, h2, 1e-3);
            Assert.AreEqual(vertical, v2, 1e-3);
            if (!double.IsNaN(torsion)) Assert.AreEqual(torsion, t2, 1e-3);
        }

        [TestMethod]
        public void GazeAndUpAtTheReference()
        {
            var data = new CalibratedEyeData { TorsionalPosition = double.NaN };
            Assert.AreEqual(new Vector3(0, 0, 1), data.Gaze);
            Assert.AreEqual(new Vector3(0, 1, 0), data.Up);

            // Clockwise as the subject sees it: the top of the eye to the subject's right.
            data.TorsionalPosition = 10;
            Assert.IsTrue(data.Up.X > 0);

            Assert.IsTrue(float.IsNaN(new CalibratedEyeData { HorizontalPosition = double.NaN }.Gaze.X), "Not found.");
        }

        /// <summary>
        /// Without measured torsion, the up vector is the one of Listing's law: the same as the
        /// rotation from the lines of sight.
        /// </summary>
        [TestMethod]
        public void UpFollowsListingsLawWithoutTorsion()
        {
            var rotation = EyeRotationMath.FromLinesOfSight(
                EyeRotationMath.LineOfSight(Model.Center, Model, Eye.Left),
                EyeRotationMath.LineOfSight(Pupil(20, 15, Model), Model, Eye.Left),
                double.NaN);
            var (_, expectedUp) = EyeRotationMath.GazeAndUp(rotation);

            var up = new CalibratedEyeData { HorizontalPosition = 20, VerticalPosition = 15, TorsionalPosition = double.NaN }.Up;
            Assert.AreEqual(expectedUp.X, up.X, 1e-4);
            Assert.AreEqual(expectedUp.Y, up.Y, 1e-4);
            Assert.AreEqual(expectedUp.Z, up.Z, 1e-4);
        }

        [TestMethod]
        public void PupilAreaWithThePixelScale()
        {
            var calibration = new CalibrationParameters();
            var data = new EyeData
            {
                WhichEye = Eye.Left,
                ProcessFrameResult = ProcessFrameResult.Good,
                Pupil = new PupilData(new PointF(100, 100), new SizeF(20, 30), 0),
                Iris = new IrisData(new PointF(100, 100), 60),
            };

            Assert.AreEqual(Math.PI / 4 * 2 * 3, calibration.GetCalibratedEyeData(data, 0.1).PupilArea, 1e-9);
            Assert.IsTrue(double.IsNaN(calibration.GetCalibratedEyeData(data).PupilArea), "Unknown pixel scale.");
        }
    }
}
