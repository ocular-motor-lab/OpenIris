//-----------------------------------------------------------------------
// <copyright file="LinearGazeMapTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.Calibration;

    /// <summary>
    /// The map of the Remote Linear calibration: applied to the data, saved in the calibration file,
    /// and with the signals for the pupil alone (webcam), the pupil minus a reflection, and dual Purkinje.
    /// </summary>
    [TestClass]
    public class LinearGazeMapTests
    {
        [TestInitialize]
        public void LoadPlugins() => PipelineHarness.EnsurePluginsLoaded();

        private static readonly LinearGazeMap Map = new LinearGazeMap { Gxx = 0.5, Gxy = 0.1, Gyx = -0.2, Gyy = -0.4, XOffset = 3, YOffset = -1, Source = EyeSignalSource.Pupil, Reference = EyeSignalSource.CR1 };

        private static EyeData Eye(float pupilX, float pupilY, params PointF[] reflections) => new EyeData
        {
            WhichEye = OpenIris.Eye.Left,
            ProcessFrameResult = ProcessFrameResult.Good,
            Pupil = new PupilData(new PointF(pupilX, pupilY), new SizeF(20, 20), 0),
            CornealReflections = Array.ConvertAll(reflections, r => new CornealReflectionData(r, new SizeF(4, 4), 0)),
        };

        [TestMethod]
        public void MapIsAppliedAsTheWebClientDoes()
        {
            // Signal = pupil - CR1 = (10, -6): [10 -6 1] * [[0.5 0.1] [-0.2 -0.4] [3 -1]].
            var gaze = Map.Apply(Eye(110, 94, new PointF(100, 100)));
            Assert.AreEqual(10 * 0.5 + -6 * -0.2 + 3, gaze!.Value.horizontal, 1e-6);
            Assert.AreEqual(10 * 0.1 + -6 * -0.4 - 1, gaze!.Value.vertical, 1e-6);
        }

        [TestMethod]
        public void SignalsForEachKindOfSetup()
        {
            var eye = Eye(110, 94, new PointF(100, 100), new PointF(130, 90), new PointF(0, 0), new PointF(0, 0), new PointF(120, 96));

            Assert.AreEqual(new PointF(110, 94), LinearGazeMap.Signal(eye, EyeSignalSource.Pupil, EyeSignalSource.None), "Pupil alone (webcam).");
            Assert.AreEqual(new PointF(10, -6), LinearGazeMap.Signal(eye, EyeSignalSource.Pupil, EyeSignalSource.CR1), "Pupil minus the first reflection.");
            Assert.AreEqual(new PointF(-20, 4), LinearGazeMap.Signal(eye, EyeSignalSource.CR1, EyeSignalSource.CR5), "Dual Purkinje: first reflection minus the fifth.");
            Assert.IsNull(LinearGazeMap.Signal(eye, EyeSignalSource.Pupil, EyeSignalSource.CR3), "A reflection at 0, 0 wasn't found.");
            Assert.IsNull(LinearGazeMap.Signal(Eye(110, 94), EyeSignalSource.Pupil, EyeSignalSource.CR1), "No reflections.");
        }

        [TestMethod]
        public void CalibratedPositionsComeFromTheMap()
        {
            var calibration = new CalibrationParameters();
            var eye = Eye(110, 94, new PointF(100, 100));
            var withModel = calibration.GetCalibratedEyeData(eye);

            calibration.EyeCalibrationParameters[OpenIris.Eye.Left].CalibrationData = Map;
            var withMap = calibration.GetCalibratedEyeData(eye);
            Assert.AreEqual(Map.Apply(eye)!.Value.horizontal, withMap.HorizontalPosition, 1e-6);
            Assert.AreEqual(Map.Apply(eye)!.Value.vertical, withMap.VerticalPosition, 1e-6);
            Assert.AreNotEqual(withModel.HorizontalPosition, withMap.HorizontalPosition);

            Assert.IsTrue(double.IsNaN(calibration.GetCalibratedEyeData(Eye(110, 94)).HorizontalPosition), "No reflection, no position.");
        }

        [TestMethod]
        public void MapIsSavedInTheCalibrationFile()
        {
            var file = Path.Combine(Path.GetTempPath(), "OpenIrisLinearMapTest-" + Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                var calibration = new CalibrationParameters();
                calibration.EyeCalibrationParameters[OpenIris.Eye.Right].CalibrationData = Map;
                calibration.Save(file);

                var loaded = CalibrationParameters.Load(file);
                Assert.IsNull(loaded.EyeCalibrationParameters[OpenIris.Eye.Left].CalibrationData);
                var map = loaded.EyeCalibrationParameters[OpenIris.Eye.Right].CalibrationData as LinearGazeMap ?? throw new AssertFailedException("No map after loading.");
                Assert.AreEqual(Map.Gyx, map.Gyx);
                Assert.AreEqual(Map.YOffset, map.YOffset);
                Assert.AreEqual(EyeSignalSource.CR1, map.Reference);
                Assert.IsNotNull(loaded.Copy().EyeCalibrationParameters[OpenIris.Eye.Right].CalibrationData, "Copies keep the map.");
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
