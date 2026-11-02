// TEMPORARY (#77): where the current and robust fits disagree over the whole sample video.
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageProcessing;

    [TestClass]
    public class EllipseFramesExperimentTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [TestCategory("Experiment")]
        public void Disagreements()
        {
            const int frames = 3960;
            RobustEllipseFit.LossFunction = RobustEllipseFit.Loss.Huber;
            RobustEllipseFit.PriorPoints = 1;
            RobustEllipseFit.PriorFreeRatio = 1.5;
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                PipelineHarness.ExperimentConfigure = s => { if (s is EyeTrackingPipelineJOMSettings jom) jom.PositionTrackingMethod = PositionTrackerEllipseFitting.PositionTrackingMethod.EllipseFitting; };
                var current = PipelineHarness.RunSampleVideo("JOM", eye, frames);
                PipelineHarness.ExperimentConfigure = s => { if (s is EyeTrackingPipelineJOMSettings jom) jom.PositionTrackingMethod = PositionTrackerEllipseFitting.PositionTrackingMethod.RobustEllipseFitting; };
                var robust = PipelineHarness.RunSampleVideo("JOM", eye, frames);
                PipelineHarness.ExperimentConfigure = null;

                var n = Math.Min(current.Count, robust.Count);
                var diff = Enumerable.Range(0, n).Select(i => (i, d: Distance(current[i].Pupil.Center, robust[i].Pupil.Center))).Where(t => !double.IsNaN(t.d)).ToArray();
                TestContext.WriteLine($"{eye}: {n} frames; center difference > 0.5 px in {diff.Count(t => t.d > 0.5)}, > 1 px in {diff.Count(t => t.d > 1)}, > 3 px in {diff.Count(t => t.d > 3)}");
                foreach (var segment in new[] { (0, 1000), (1000, 2500), (2500, 3500), (3500, n) })
                {
                    TestContext.WriteLine($"  frames {segment.Item1}-{segment.Item2}: jitter current {Jitter(current, segment):0.000}, robust {Jitter(robust, segment):0.000}; differences > 1 px {diff.Count(t => t.i >= segment.Item1 && t.i < segment.Item2 && t.d > 1)}");
                }

                // Draw the worst frames: current in red, robust in green.
                foreach (var (i, d) in diff.OrderByDescending(t => t.d).Take(6))
                {
                    using var frame = PipelineHarness.ReadFrame(PipelineHarness.SampleVideo(eye), eye, i);
                    using var image = frame.Image.Convert<Bgr, byte>();
                    Draw(image, current[i].Pupil, new Bgr(0, 0, 255));
                    Draw(image, robust[i].Pupil, new Bgr(0, 255, 0));
                    var file = $@"D:\OpenIrisDev\vw\{eye}-frame{i}-{d:0.0}px.png";
                    image.Resize(2, Emgu.CV.CvEnum.Inter.Linear).Save(file);
                    TestContext.WriteLine($"  frame {i}: {d:0.00} px apart, current {Describe(current[i].Pupil)}, robust {Describe(robust[i].Pupil)}");
                }
            }
        }

        private static double Distance(PointF a, PointF b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private static double Jitter(System.Collections.Generic.List<EyeData> data, (int, int) segment)
        {
            var d = Enumerable.Range(Math.Max(1, segment.Item1), segment.Item2 - Math.Max(1, segment.Item1))
                .Where(i => i < data.Count && data[i].ProcessFrameResult == ProcessFrameResult.Good && data[i - 1].ProcessFrameResult == ProcessFrameResult.Good)
                .Select(i => Distance(data[i].Pupil.Center, data[i - 1].Pupil.Center)).OrderBy(v => v).ToArray();
            return d.Length == 0 ? double.NaN : d[d.Length / 2] * 1.4826 / Math.Sqrt(2);
        }

        private static void Draw(Image<Bgr, byte> image, PupilData pupil, Bgr color) =>
            image.Draw(new Ellipse(pupil.Center, pupil.Size, pupil.Angle), color, 1);

        private static string Describe(PupilData p) => $"({p.Center.X:0.0}, {p.Center.Y:0.0}) {p.Size.Width:0}x{p.Size.Height:0}";
    }
}
