//-----------------------------------------------------------------------
// <copyright file="CalibrationSessionTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris.Calibration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The calibration session with the calibrations included in OpenIris: the two stages (model, zero
    /// reference) with binocular and monocular systems, the references only from frames processed with
    /// the new eye models, and the messages from clients (#61).
    /// </summary>
    [TestClass]
    public class CalibrationSessionTests
    {
        private static readonly EyeTrackingPipelineSettings TrackingSettings = new EyeTrackingPipelineJOMSettings();

        [ClassInitialize]
        public static void Initialize(TestContext _) => PipelineHarness.EnsurePluginsLoaded();

        [DataTestMethod]
        [DataRow(Eye.Both)]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void AutoGivesModelsAndReferencesForTheEyesOfTheSystem(Eye eyes)
        {
            using var session = new CalibrationSession(eyes, "Auto", new CalibrationSettings());

            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () => Frame(eyes, new CalibrationParameters(), new PointF(30, 20)));
            Assert.IsNotNull(model, "Model");
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                Assert.AreEqual(Has(eyes, eye), model!.EyeCalibrationParameters[eye].HasEyeModel, $"{eye} eye model");
            }

            Assert.IsFalse(session.HasReferences(model!), "Auto takes the references in the second stage.");

            var head = new HeadData { FaceRotation = new double[] { 1, 2, 3 } };
            var calibration = Run(session, session.StartCalibratingZeroReference(model!, TrackingSettings), () => Frame(eyes, model!, new PointF(31, 21), head));
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                Assert.AreEqual(Has(eyes, eye), calibration.EyeCalibrationParameters[eye].HasReference, $"{eye} eye reference");
                Assert.AreEqual(Has(eyes, eye), calibration.EyeCalibrationParameters[eye].ReferenceImage is object, $"{eye} eye reference image");
            }

            Assert.AreSame(head, calibration.HeadCalibrationParameters!.ReferenceHeadData, "The head's reference is from the same frame.");
            Assert.IsTrue(session.HasReferences(calibration));
        }

        [TestMethod]
        public void ReferencesOnlyFromFramesProcessedWithTheNewModels()
        {
            using var session = new CalibrationSession(Eye.Both, "Auto", new CalibrationSettings());
            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () => Frame(Eye.Both, new CalibrationParameters(), new PointF(30, 20)))!;

            // The first frames were processed with the previous calibration, then with the new one.
            var frames = 0;
            var calibration = Run(session, session.StartCalibratingZeroReference(model, TrackingSettings), () =>
                ++frames <= 5
                    ? Frame(Eye.Both, new CalibrationParameters(), new PointF(10, 10))
                    : Frame(Eye.Both, model, new PointF(32, 22)));

            Assert.AreEqual(new PointF(32, 22), calibration.EyeCalibrationParameters[Eye.Left].ReferenceData.Pupil.Center);
            Assert.AreEqual(new PointF(32, 22), calibration.EyeCalibrationParameters[Eye.Right].ReferenceData.Pupil.Center);
        }

        [TestMethod]
        public void ManualUsesTheModelsTheUserSets()
        {
            using var session = new CalibrationSession(Eye.Both, "Manual Calibration", new CalibrationSettings());
            var manual = (CalibrationPipelineManual)session.Pipeline;
            var left = new EyePhysicalModel(new PointF(40, 30), 50);
            var right = new EyePhysicalModel(new PointF(41, 31), 51);

            var frames = 0;
            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () =>
            {
                if (++frames == 5) manual.EyeModels = new EyeCollection<EyePhysicalModel>(left, right);
                return Frame(Eye.Both, new CalibrationParameters(), new PointF(30, 20));
            })!;

            Assert.AreEqual(left, model.EyeCalibrationParameters[Eye.Left].EyePhysicalModel);
            Assert.AreEqual(right, model.EyeCalibrationParameters[Eye.Right].EyePhysicalModel);
            Assert.IsNotNull(manual.LastImages[Eye.Left], "The user interface gets the images.");
        }

        /// <summary>
        /// Remote Linear driven by messages, as the web client or a MATLAB script does it: the pupil
        /// moves 2 px per degree, and the map fitted for each eye gives back the targets.
        /// </summary>
        [DataTestMethod]
        [DataRow(Eye.Both)]
        [DataRow(Eye.Right)]
        public void RemoteLinearFitsTheTargetsSentInMessages(Eye eyes)
        {
            var settings = new CalibrationSettingsRemoteLinear { SignalSource = EyeSignalSource.Pupil, SignalReference = EyeSignalSource.None };
            using var session = new CalibrationSession(eyes, "Remote Linear", settings);

            var target = new PointF(0, 0);
            PointF Pupil() => new PointF(100 + 2 * target.X, 80 - 2 * target.Y);

            using var stop = new CancellationTokenSource();
            var modelTask = session.StartCalibratingModel(TrackingSettings);
            var feeder = Task.Run(() =>
            {
                while (!stop.IsCancellationRequested && !modelTask.IsCompleted)
                {
                    session.ProcessNewDataAndImages(Frame(eyes, new CalibrationParameters(), Pupil()));
                    Thread.Sleep(2);
                }
            });

            foreach (var point in new[] { new PointF(0, 0), new PointF(-10, 0), new PointF(10, 0), new PointF(0, -8), new PointF(0, 8) })
            {
                target = point;
                Thread.Sleep(20);
                Assert.AreEqual("ack", session.HandleMessage($"FixStart:{point.X},{point.Y}"));
                Thread.Sleep(40);
                Assert.AreEqual("ack", session.HandleMessage("FixStop"));
            }

            Assert.AreEqual("ack", session.HandleMessage("Complete"));
            Assert.IsTrue(modelTask.Wait(TimeSpan.FromSeconds(5)), "The model stage ends with Complete.");
            stop.Cancel();
            feeder.Wait();

            var model = modelTask.Result!;
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                var map = model.EyeCalibrationParameters[eye].CalibrationData as LinearGazeMap;
                Assert.AreEqual(Has(eyes, eye), map is object, $"{eye} eye map");
                if (map is null) continue;

                var (horizontal, vertical) = map.Apply(Frame(eyes, model, new PointF(100 + 2 * 5, 80 - 2 * -3)).Images[eye]!.EyeData!)!.Value;
                Assert.AreEqual(5, horizontal, 0.05, $"{eye} eye horizontal");
                Assert.AreEqual(-3, vertical, 0.05, $"{eye} eye vertical");
            }
        }

        [TestMethod]
        public void MessageWithoutFramesGetsAnError()
        {
            using var session = new CalibrationSession(Eye.Both, "Auto", new CalibrationSettings());
            StringAssert.StartsWith(session.HandleMessage("anything"), "ERROR");
        }

        private static bool Has(Eye eyes, Eye eye) => eyes == Eye.Both || eyes == eye;

        /// <summary>
        /// A processed frame with every eye of the system tracked, its pupil at the given point.
        /// </summary>
        private static EyeTrackerImagesAndData Frame(Eye eyes, CalibrationParameters calibration, PointF pupil, HeadData? head = null)
        {
            ImageEye? Image(Eye eye) => !Has(eyes, eye) ? null : new ImageEye(new Image<Gray, byte>(64, 48), eye, ImageEyeTimestamp.Empty, null)
            {
                EyeData = new EyeData
                {
                    WhichEye = eye,
                    ProcessFrameResult = ProcessFrameResult.Good,
                    Pupil = new PupilData(pupil, new SizeF(10, 10), 0),
                    Iris = new IrisData(pupil, 20),
                },
            };

            return new EyeTrackerImagesAndData(new EyeCollection<ImageEye?>(Image(Eye.Left), Image(Eye.Right)), calibration, "JOM", TrackingSettings)
            {
                Data = new EyeTrackerData { HeadDataRaw = head ?? new HeadData() },
            };
        }

        /// <summary>
        /// Feeds frames to the session until the stage ends.
        /// </summary>
        private static T Run<T>(CalibrationSession session, Task<T> stage, Func<EyeTrackerImagesAndData> frame)
        {
            var watch = Stopwatch.StartNew();
            while (!stage.IsCompleted)
            {
                Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(5), "The stage didn't end.");
                session.ProcessNewDataAndImages(frame());
                Thread.Sleep(2);
            }

            return stage.Result;
        }
    }
}
