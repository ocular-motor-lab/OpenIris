// TEMPORARY: compares pupil fits (#77). Not to commit.
namespace OpenIris.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageProcessing;

    [TestClass]
    public class EllipseExperimentTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [TestCategory("Experiment")]
        public void Variants()
        {
            var R = PositionTrackerEllipseFitting.PositionTrackingMethod.RobustEllipseFitting;
            var variants = new (PositionTrackerEllipseFitting.PositionTrackingMethod method, RobustEllipseFit.Loss loss, double prior, double free, string name)[]
            {
                (R, RobustEllipseFit.Loss.Huber, 0, 1, "Robust Huber, no prior"),
                (R, RobustEllipseFit.Loss.Huber, 0.01, 1, "Robust Huber, prior 0.01"),
                (R, RobustEllipseFit.Loss.Huber, 0.03, 1, "Robust Huber, prior 0.03"),
                (R, RobustEllipseFit.Loss.Huber, 1, 1.3, "Robust Huber, prior 1 beyond 1.3:1"),
                (R, RobustEllipseFit.Loss.Huber, 1, 1.5, "Robust Huber, prior 1 beyond 1.5:1"),
                (R, RobustEllipseFit.Loss.Tukey, 1, 1.3, "Robust Tukey, prior 1 beyond 1.3:1"),
            };
            foreach (var (method, loss, prior, free, name) in variants)
            {
                RobustEllipseFit.LossFunction = loss;
                RobustEllipseFit.PriorPoints = prior;
                RobustEllipseFit.PriorFreeRatio = free;
                PipelineHarness.ExperimentConfigure = s => { if (s is EyeTrackingPipelineJOMSettings jom) jom.PositionTrackingMethod = method; };
                TestContext.WriteLine("=== " + name);
                foreach (var eye in new[] { Eye.Left, Eye.Right })
                {
                    foreach (var threshold in new[] { 40, 60 })
                    {
                        var data = PipelineHarness.RunSyntheticEye("JOM", eye, 100, PipelineHarness.Thresholds(threshold));
                        var center = data.Select((d, i) => d.ProcessFrameResult == ProcessFrameResult.Good
                            ? Math.Sqrt(Math.Pow(d.Pupil.Center.X - SyntheticEye.GetTruth(eye, i).PupilCenter.X, 2) + Math.Pow(d.Pupil.Center.Y - SyntheticEye.GetTruth(eye, i).PupilCenter.Y, 2)) : double.NaN).Where(e => !double.IsNaN(e)).ToArray();
                        var size = data.Select((d, i) => d.ProcessFrameResult == ProcessFrameResult.Good
                            ? Math.Max(Math.Abs(d.Pupil.Size.Width / 2 - SyntheticEye.GetTruth(eye, i).PupilRadius), Math.Abs(d.Pupil.Size.Height / 2 - SyntheticEye.GetTruth(eye, i).PupilRadius)) : double.NaN).Where(e => !double.IsNaN(e)).ToArray();
                        TestContext.WriteLine($"  synthetic {eye} threshold {threshold}: {center.Length}/100 good, center error mean {center.Average():0.000} max {center.Max():0.000}, radius error mean {size.Average():0.000} max {size.Max():0.000} px");
                    }

                    var watch = Stopwatch.StartNew();
                    var m = new TrackingMeasurementTests { TestContext = TestContext };
                    m.SampleVideo(eye);
                    TestContext.WriteLine($"  ({watch.ElapsedMilliseconds} ms for the sample video test)");
                    m.SampleVideoRotated(eye);
                }
            }
        }
    }
}
