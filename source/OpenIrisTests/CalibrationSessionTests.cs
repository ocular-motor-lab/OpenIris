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

        /// <summary>
        /// The calibration keeps the extrinsics of the cameras and the lights of the system (from each
        /// eye), as they were when it was made.
        /// </summary>
        [TestMethod]
        public void KeepsTheExtrinsicsAndTheLights()
        {
            var extrinsics = new CameraExtrinsics(1.1f, 1.2f, -1.3f, 2, -3, 55);
            var system = new EyeTrackingSystemSettings
            {
                CameraExtrinsics = new EyeCollection<CameraExtrinsics>(extrinsics, default),
                Lights = new[] { new LightPosition(50, 0, -20) },
            };
            using var session = new CalibrationSession(Eye.Both, "Auto", new CalibrationSettings(), system);

            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () => Frame(Eye.Both, new CalibrationParameters(), new PointF(30, 20)));
            Assert.AreEqual(extrinsics, model!.EyeCalibrationParameters[Eye.Left].Extrinsics);
            Assert.AreEqual(CameraExtrinsics.Facing(CameraIntrinsics.AssumedDistanceToEyeMm), model.EyeCalibrationParameters[Eye.Right].Extrinsics, "Unknown: facing.");
            CollectionAssert.AreEqual(system.LightsFrom(Eye.Right), model.EyeCalibrationParameters[Eye.Right].Lights);
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

        /// <summary>
        /// The window of the camera moved 5 px right and 3 px down between Calibrate and Reset
        /// reference: the new calibration is in the pixels of the new window, with the eye model where
        /// it was on the sensor.
        /// </summary>
        [TestMethod]
        public void ResetReferenceAfterTheWindowMoved()
        {
            var sensor = new CameraIntrinsics(400, 400, 360, 270);
            using var session = new CalibrationSession(Eye.Both, "Auto", new CalibrationSettings());
            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () => Frame(Eye.Both, new CalibrationParameters(), new PointF(30, 20), intrinsics: sensor))!;
            Assert.AreEqual(sensor, model.EyeCalibrationParameters[Eye.Left].Intrinsics, "The intrinsics of the frames.");

            var moved = sensor.Cropped(new Point(5, 3));
            var calibration = Run(session, session.StartCalibratingZeroReference(model, TrackingSettings), () => Frame(Eye.Both, model, new PointF(25, 17), intrinsics: moved));

            var left = calibration.EyeCalibrationParameters[Eye.Left];
            Assert.AreEqual(moved, left.Intrinsics);
            Assert.AreEqual(new PointF(25, 17), left.EyePhysicalModel.Center);
            Assert.AreEqual(new PointF(25, 17), left.ReferenceData.Pupil.Center);
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
        /// N-point driven by messages, as the web client or a MATLAB script does it: the pupil
        /// moves 2 px per degree, and the map fitted for each eye gives back the targets.
        /// </summary>
        [DataTestMethod]
        [DataRow(Eye.Both)]
        [DataRow(Eye.Right)]
        public void RemoteLinearFitsTheTargetsSentInMessages(Eye eyes)
        {
            var settings = new CalibrationSettingsRemoteLinear { SignalSource = EyeSignalSource.Pupil, SignalReference = EyeSignalSource.None };
            using var session = new CalibrationSession(eyes, "N-point", settings);

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

        /// <summary>
        /// N-point with OpenIris showing the targets (Local and RemoteDisplay): the subject (the pupil)
        /// follows the target OpenIris draws, and the map fitted gives back the targets. The targets
        /// don't come in messages then.
        /// </summary>
        [DataTestMethod]
        [DataRow(NPointMode.Local)]
        [DataRow(NPointMode.RemoteDisplay)]
        public void NPointShowsTheTargetsItself(NPointMode mode)
        {
            var settings = new CalibrationSettingsRemoteLinear
            {
                Mode = mode, Targets = NPointTargets.Five, SettleSeconds = 0.2, CollectSeconds = 0.3,
                SignalSource = EyeSignalSource.Pupil, SignalReference = EyeSignalSource.None,
            };
            using var session = new CalibrationSession(Eye.Both, "N-point", settings);
            var pipeline = (CalibrationPipelineRemoteLinear)session.Pipeline;

            // The time advances 50 ms per frame.
            var frames = 0;
            pipeline.Clock = () => Volatile.Read(ref frames) * 0.05;
            var seen = new System.Collections.Generic.HashSet<PointF>();

            var model = Run(session, session.StartCalibratingModel(TrackingSettings), () =>
            {
                Interlocked.Increment(ref frames);
                var target = pipeline.Target?.Position ?? new PointF(0, 0);
                if (pipeline.Target?.Recording == true) seen.Add(target);
                return Frame(Eye.Both, new CalibrationParameters(), new PointF(100 + 2 * target.X, 80 - 2 * target.Y));
            });

            Assert.AreEqual(5, seen.Count, "Five targets shown.");
            StringAssert.StartsWith(session.HandleMessage("FixStart:1,2"), "ERROR", "No targets in messages.");

            var map = (LinearGazeMap)model!.EyeCalibrationParameters[Eye.Left].CalibrationData!;
            var (horizontal, vertical) = map.Apply(Frame(Eye.Both, model, new PointF(100 + 2 * 5, 80 - 2 * -3)).Images[Eye.Left]!.EyeData!)!.Value;
            Assert.AreEqual(5, horizontal, 0.05);
            Assert.AreEqual(-3, vertical, 0.05);
        }

        /// <summary>
        /// The sequence of targets: each one moves, settles shrinking, and then records; the center
        /// first.
        /// </summary>
        [TestMethod]
        public void NPointSequenceOfTargets()
        {
            var points = NPointSequence.Layout(9, 10, 8, new Random(1));
            Assert.AreEqual(9, points.Length);
            Assert.AreEqual(new PointF(0, 0), points[0], "The center first.");
            Assert.AreEqual(5, NPointSequence.Layout(5, 10, 8, new Random(1)).Length);
            Assert.AreEqual(13, NPointSequence.Layout(13, 10, 8, new Random(1)).Length);

            var sequence = new NPointSequence(points, 1, 2);
            var perTarget = NPointSequence.MoveSeconds + 1 + 2;
            Assert.AreEqual(9 * perTarget, sequence.Duration, 1e-9);

            var moving = sequence.TargetAt(perTarget + NPointSequence.MoveSeconds / 2)!;
            Assert.AreEqual(2, moving.Number);
            Assert.AreEqual(points[1].X / 2, moving.Position.X, 1e-4, "Halfway from the first target.");
            Assert.IsFalse(moving.Recording);

            var settling = sequence.TargetAt(perTarget + NPointSequence.MoveSeconds + 0.5)!;
            Assert.AreEqual(1.5, settling.Scale, 1e-9);
            Assert.IsFalse(settling.Recording);

            var recording = sequence.TargetAt(perTarget + NPointSequence.MoveSeconds + 1.5)!;
            Assert.IsTrue(recording.Recording);
            Assert.AreEqual(points[1], recording.Position);

            Assert.IsNull(sequence.TargetAt(sequence.Duration + 0.01), "Over.");
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
        private static EyeTrackerImagesAndData Frame(Eye eyes, CalibrationParameters calibration, PointF pupil, HeadData? head = null, CameraIntrinsics intrinsics = default)
        {
            ImageEye? Image(Eye eye) => !Has(eyes, eye) ? null : new ImageEye(new Image<Gray, byte>(64, 48), eye, ImageEyeTimestamp.Empty, null)
            {
                Intrinsics = intrinsics,
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
