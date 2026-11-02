// TEMPORARY (#77).
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageProcessing;

    [TestClass]
    public class EllipseArcExperimentTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [TestCategory("Experiment")]
        public void Arcs()
        {
            var random = new Random(1);
            foreach (var cut in new[] { 40.0, 25.0, 10.0, 0.0, -15.0 })
            {
                // Round pupil (radius 50 at 100, 100) with the eyelid covering it above y = 100 - cut:
                // the contour is the arc below the eyelid plus the eyelid's straight edge.
                var points = new System.Collections.Generic.List<PointF>();
                for (int i = 0; i < 360; i += 4)
                {
                    var t = i * Math.PI / 180;
                    var y = 100 + 50 * Math.Sin(t);
                    if (y >= 100 - cut) points.Add(new PointF((float)(100 + 50 * Math.Cos(t) + random.NextDouble() - 0.5), (float)(y + random.NextDouble() - 0.5)));
                }
                var half = Math.Sqrt(50 * 50 - cut * cut);
                for (double x = -half; x <= half; x += 3.5) points.Add(new PointF((float)(100 + x), (float)(100 - cut + random.NextDouble() - 0.5)));
                using var vector = new VectorOfPointF(points.ToArray());
                var opencv = CvInvoke.FitEllipse(vector);
                var line = $"eyelid {cut,4} px above center: OpenCV {Describe(opencv)}";
                foreach (var (prior, free, name) in new[] { (0.0, 1.0, "no prior"), (1.0, 1.3, "free 1.3"), (1.0, 1.5, "free 1.5") })
                {
                    RobustEllipseFit.LossFunction = RobustEllipseFit.Loss.Huber;
                    RobustEllipseFit.PriorPoints = prior;
                    RobustEllipseFit.PriorFreeRatio = free;
                    line += $" | {name} {Describe(RobustEllipseFit.Fit(vector))}";
                }
                TestContext.WriteLine(line);
            }
        }

        private static string Describe(RotatedRect e) =>
            $"center ({e.Center.X - 100:0.0}, {e.Center.Y - 100:0.0}) axes {e.Size.Width / 2:0} {e.Size.Height / 2:0}";
    }
}
