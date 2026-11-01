//-----------------------------------------------------------------------
// <copyright file="PipelineAccuracyTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks that pipelines find the pupil (and reflection) where the synthetic eye drew them.
    /// Unlike the baselines, these limits say how accurate tracking must be, not that it is unchanged.
    /// The limits were set from the accuracy measured when the tests were written, plus a margin:
    /// - JOM is within about 0.2 px for both pupil and reflection. (It used to read about 0.6 px
    ///   right of and below the true center, from even-sized blurs and resized coordinates.)
    /// - SimpleCentroid is unbiased but drifts up to about 1.1 px when the reflection covers part of
    ///   the pupil, since it has no reflection mask.
    /// - SimplePupilCRBlobs is not tested here: it does not find the pupil in these images.
    /// </summary>
    [TestClass]
    public class PipelineAccuracyTests
    {
        private const int Frames = 100;

        public TestContext TestContext { get; set; } = null!;

        [DataTestMethod]
        [DataRow("JOM", Eye.Left, MaxPupilErrorPixels.Jom)]
        [DataRow("JOM", Eye.Right, MaxPupilErrorPixels.Jom)]
        [DataRow("SimpleCentroid", Eye.Left, MaxPupilErrorPixels.SimpleCentroid)]
        [DataRow("SimpleCentroid", Eye.Right, MaxPupilErrorPixels.SimpleCentroid)]
        public void FindsPupilCenter(string pipeline, Eye eye, double maxError)
        {
            var data = PipelineHarness.RunSyntheticEye(pipeline, eye, Frames);

            var truths = data.Select((d, frame) => SyntheticEye.GetTruth(eye, frame).PupilCenter).ToList();
            var errors = data.Select((d, frame) => Distance(d.Pupil.Center, truths[frame])).ToList();
            TestContext.WriteLine($"Pupil center error: mean {errors.Average():0.000} px, max {errors.Max():0.000} px, " +
                $"mean offset ({data.Select((d, i) => d.Pupil.Center.X - truths[i].X).Average():0.000}, {data.Select((d, i) => d.Pupil.Center.Y - truths[i].Y).Average():0.000}) px");

            Assert.IsTrue(data.All(d => d.ProcessFrameResult == ProcessFrameResult.Good), "Some frames were not tracked.");
            Assert.IsTrue(errors.Max() <= maxError, $"Pupil center off by up to {errors.Max():0.000} px (limit {maxError} px).");
        }

        [DataTestMethod]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void JomFindsCornealReflection(Eye eye)
        {
            var data = PipelineHarness.RunSyntheticEye("JOM", eye, Frames);

            var errors = data.Select((d, frame) =>
            {
                var truth = SyntheticEye.GetTruth(eye, frame).ReflectionCenter;
                var found = d.CornealReflections?.Where(cr => !cr.IsEmpty).ToList();
                return found is null || found.Count == 0 ? double.PositiveInfinity : found.Min(cr => Distance(cr.Center, truth));
            }).ToList();
            var offsets = data.Select((d, frame) => (truth: SyntheticEye.GetTruth(eye, frame).ReflectionCenter, d.CornealReflections))
                .Where(x => x.CornealReflections?.Length > 0)
                .Select(x => (x: x.CornealReflections![0].Center.X - x.truth.X, y: x.CornealReflections[0].Center.Y - x.truth.Y)).ToList();
            TestContext.WriteLine($"Reflection center error: mean {errors.Average():0.000} px, max {errors.Max():0.000} px, " +
                $"mean offset ({offsets.Average(o => o.x):0.000}, {offsets.Average(o => o.y):0.000}) px, " +
                $"found in {errors.Count(e => !double.IsInfinity(e))} of {errors.Count} frames");

            Assert.IsTrue(errors.Max() <= MaxReflectionErrorPixels, $"Reflection center off by up to {errors.Max():0.000} px (limit {MaxReflectionErrorPixels} px).");
        }

        private const double MaxReflectionErrorPixels = 0.3;

        private static class MaxPupilErrorPixels
        {
            public const double Jom = 0.3;
            public const double SimpleCentroid = 1.5;
        }

        private static double Distance(PointF a, PointF b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}
