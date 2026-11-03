//-----------------------------------------------------------------------
// <copyright file="CameraExtrinsicsTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.IO;
    using System.Numerics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The extrinsics of the cameras and the lights of the system: in the settings, copied into the
    /// calibration, and the head's up of the calibrated angles from the extrinsics (#61).
    /// </summary>
    [TestClass]
    public class CameraExtrinsicsTests
    {
        private const double ToRadians = Math.PI / 180;

        private static void AreEqual(Vector3 expected, Vector3 actual, string message) =>
            Assert.IsTrue(Vector3.Distance(expected, actual) < 1e-5, $"{message}: expected {expected}, got {actual}.");

        // OpenCV's rvec of a rotation.
        private static CameraExtrinsics FromRotation(Quaternion rotation, float distanceMm)
        {
            var angle = 2 * Math.Acos(Math.Min(1, rotation.W));
            var axis = Vector3.Normalize(new Vector3(rotation.X, rotation.Y, rotation.Z));
            var rvec = axis * (float)angle;
            return new CameraExtrinsics(rvec.X, rvec.Y, rvec.Z, 0, 0, distanceMm);
        }

        /// <summary>
        /// A camera facing the eye sees the head's forward towards it, the subject's left to the right
        /// of the image and up up: the image OpenIris expects.
        /// </summary>
        [TestMethod]
        public void FacingCameraSeesTheEyeAsOpenIrisExpects()
        {
            var facing = CameraExtrinsics.Facing(50);
            AreEqual(-Vector3.UnitZ, Vector3.Transform(Vector3.UnitX, facing.Rotation), "Forward points at the camera");
            AreEqual(Vector3.UnitX, Vector3.Transform(Vector3.UnitY, facing.Rotation), "Left is to the right of the image");
            AreEqual(-Vector3.UnitY, facing.HeadUp, "Up is up in the image");
            Assert.AreEqual(50, facing.Tz);
            Assert.IsFalse(facing.IsEmpty);
            Assert.IsTrue(default(CameraExtrinsics).IsEmpty);
        }

        /// <summary>
        /// With a camera rolled about its axis, a purely vertical eye movement shows tilted in the
        /// image. With the camera's extrinsics it is vertical again; without them, it isn't.
        /// </summary>
        [TestMethod]
        public void RolledCameraGivesTheRightAngles()
        {
            var roll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(15 * ToRadians));
            var rolled = FromRotation(roll * CameraExtrinsics.Facing(50).Rotation, 50);

            // The eye looks 10° up, in camera coordinates, from looking at the camera.
            var reference = Vector3.Transform(Vector3.UnitX, rolled.Rotation);
            var up = Vector3.Transform(new Vector3((float)Math.Cos(10 * ToRadians), 0, (float)Math.Sin(10 * ToRadians)), rolled.Rotation);

            var (h, v, _) = EyeRotationMath.FickAngles(EyeRotationMath.FromLinesOfSight(reference, up, double.NaN, rolled.HeadUp));
            Assert.AreEqual(0, h, 1e-3, "No horizontal with the extrinsics.");
            Assert.AreEqual(10, v, 1e-3);

            var (hWithout, _, _) = EyeRotationMath.FickAngles(EyeRotationMath.FromLinesOfSight(reference, up, double.NaN));
            Assert.IsTrue(Math.Abs(hWithout) > 2, "Without the extrinsics the movement looks oblique.");
        }

        /// <summary>
        /// The lights are from the point between the eyes in the settings, and from each eye in the
        /// calibration. Unknown lights stay unknown.
        /// </summary>
        [TestMethod]
        public void LightsFromEachEye()
        {
            var system = new EyeTrackingSystemSettings
            {
                DistanceBetweenEyesMm = 60,
                Lights = new[] { new LightPosition(50, 0, -20), new LightPosition() },
            };

            Assert.AreEqual(new LightPosition(50, -30, -20), system.LightsFrom(Eye.Left)[0], "The left eye is 30 mm to the left.");
            Assert.AreEqual(new LightPosition(50, 30, -20), system.LightsFrom(Eye.Right)[0]);
            Assert.IsTrue(system.LightsFrom(Eye.Left)[1].IsEmpty);
        }

        /// <summary>
        /// The extrinsics and the lights are saved with the settings and with the calibration.
        /// </summary>
        [TestMethod]
        public void SavedWithTheSettingsAndTheCalibration()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var extrinsics = new CameraExtrinsics(1.1f, 1.2f, -1.3f, 2, -3, 55);
            var light = new LightPosition(50, 5, -20);

            var settings = new EyeTrackerSettings { EyeTrackerSystem = "Simulation" };
            var system = settings.EyeTrackingSystemSettings;
            Assert.AreEqual(2, system.CameraExtrinsics!.Count, "One unknown extrinsics per camera, as the system's attribute says.");
            Assert.AreEqual(CameraExtrinsics.Facing(CameraIntrinsics.AssumedDistanceToEyeMm), system.ExtrinsicsOf(Eye.Left), "Unknown: facing the eye.");

            system.CameraExtrinsics = new EyeCollection<CameraExtrinsics>(default, extrinsics);
            system.Lights = new[] { light };

            var settingsPath = Path.Combine(Path.GetTempPath(), "OpenIrisSettingsTest-" + Guid.NewGuid().ToString("N") + ".xml");
            var calibrationPath = Path.Combine(Path.GetTempPath(), "OpenIrisCalibrationTest-" + Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                settings.Save(settingsPath);
                var loaded = EyeTrackerSettings.Load(settingsPath).EyeTrackingSystemSettings;
                Assert.AreEqual(extrinsics, loaded.ExtrinsicsOf(Eye.Right));
                CollectionAssert.AreEqual(new[] { light }, loaded.Lights);

                var calibration = new CalibrationParameters();
                calibration.EyeCalibrationParameters[Eye.Right].Extrinsics = extrinsics;
                calibration.EyeCalibrationParameters[Eye.Right].Lights = system.LightsFrom(Eye.Right);
                calibration.Save(calibrationPath);

                var right = CalibrationParameters.Load(calibrationPath).EyeCalibrationParameters[Eye.Right];
                Assert.AreEqual(extrinsics, right.Extrinsics);
                CollectionAssert.AreEqual(system.LightsFrom(Eye.Right), right.Lights);
                Assert.AreEqual(extrinsics, right.Copy().Extrinsics);
            }
            finally
            {
                File.Delete(settingsPath);
                File.Delete(calibrationPath);
            }
        }
    }
}
