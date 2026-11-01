//-----------------------------------------------------------------------
// <copyright file="PipelineTuningTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Sweeps the JOM pipeline's thresholds, to choose the ones the tests use for the sample video
    /// and the synthetic eye. Prints a table for each.
    /// </summary>
    /// <remarks>
    /// Category Measurement: it only reports. Run it with --filter "TestCategory=Measurement"
    /// --logger "console;verbosity=detailed".
    /// </remarks>
    [TestClass]
    public class PipelineTuningTests
    {
        private const int VideoFrames = 1000;
        private const int SyntheticFrames = 100;

        public TestContext TestContext { get; set; } = null!;

        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SyntheticEyeThresholds(Eye eye)
        {
            TestContext.WriteLine($"Synthetic eye {eye}: dark bright | good | pupil error mean max (px) | reflection error mean max (px) | torsion RMS max (°)");
            foreach (var dark in new[] { 30, 40, 50, 60, 70, 80, 90 })
            {
                foreach (var bright in new[] { 235, 240, 245, 250 })
                {
                    var data = PipelineHarness.RunSyntheticEye("JOM", eye, SyntheticFrames, PipelineHarness.Thresholds(dark, bright));
                    var pupil = new List<double>();
                    var reflection = new List<double>();
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (data[i].ProcessFrameResult != ProcessFrameResult.Good) continue;
                        var truth = SyntheticEye.GetTruth(eye, i);
                        pupil.Add(Distance(data[i].Pupil.Center, truth.PupilCenter));
                        if (data[i].CornealReflections?.Length > 0) reflection.Add(Distance(data[i].CornealReflections![0].Center, truth.ReflectionCenter));
                    }

                    var torsion = Torsion(data).Where(t => !double.IsNaN(t)).ToArray();
                    TestContext.WriteLine(
                        $"  {dark,3} {bright,3} | {pupil.Count,3} | {Mean(pupil),6:0.000} {Max(pupil),6:0.000} | {Mean(reflection),6:0.000} {Max(reflection),6:0.000} | " +
                        $"{Rms(torsion),6:0.000} {Max(torsion.Select(Math.Abs)),6:0.000}");
                }
            }
        }

        /// <summary>
        /// Without ground truth, a better threshold tracks more frames and makes the pupil and the
        /// torsion jitter less from frame to frame. The pupil's jitter is a median, so saccades and
        /// blinks don't count.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SampleVideoThresholds(Eye eye)
        {
            TestContext.WriteLine($"Sample video {eye}: dark bright | good | pupil jitter center size (px) | torsion noise (°)");
            var settings = new[] { 30, 40, 50, 60, 70, 80, 90, 100 }.Select(d => (d, 250))
                .Concat(new[] { 200, 220, 230, 240, 245, 254 }.Select(b => (60, b)));
            foreach (var (dark, bright) in settings)
            {
                var data = PipelineHarness.RunSampleVideo("JOM", eye, VideoFrames, configure: PipelineHarness.Thresholds(dark, bright));
                TestContext.WriteLine($"  {dark,3} {bright,3} | {VideoSummary(data)}");
            }
        }

        private static string VideoSummary(List<EyeData> data)
        {
            var good = data.Count(d => d.ProcessFrameResult == ProcessFrameResult.Good);
            var center = new List<double>();
            var size = new List<double>();
            for (int i = 1; i < data.Count; i++)
            {
                if (data[i].ProcessFrameResult != ProcessFrameResult.Good || data[i - 1].ProcessFrameResult != ProcessFrameResult.Good) continue;
                center.Add(Distance(data[i].Pupil.Center, data[i - 1].Pupil.Center));
                size.Add(Math.Abs(data[i].Pupil.Size.Width - data[i - 1].Pupil.Size.Width));
            }

            var torsion = Torsion(data);
            var differences = torsion.Zip(torsion.Skip(1), (a, b) => b - a).Where(d => !double.IsNaN(d)).ToArray();

            // Divided by √2 because each difference has the noise of two frames. (Not a median like
            // the pupil: torsion comes in steps of 0.02°, so most differences are 0.)
            var noise = differences.Length == 0 ? double.NaN : Math.Sqrt(differences.Average(d => d * d) / 2);
            return $"{good,4} | {Median(center),6:0.000} {Median(size),6:0.000} | {noise,6:0.000}";
        }

        private static double[] Torsion(List<EyeData> data) =>
            data.Select(d => d.ProcessFrameResult == ProcessFrameResult.Good ? d.TorsionAngle : double.NaN).ToArray();

        private static double Distance(System.Drawing.PointF a, System.Drawing.PointF b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private static double Mean(IEnumerable<double> x) => x.Any() ? x.Average() : double.NaN;

        private static double Max(IEnumerable<double> x) => x.Any() ? x.Max() : double.NaN;

        private static double Rms(IEnumerable<double> x) => x.Any() ? Math.Sqrt(x.Average(v => v * v)) : double.NaN;

        private static double Median(IEnumerable<double> x)
        {
            var sorted = x.OrderBy(v => v).ToArray();
            return sorted.Length == 0 ? double.NaN : sorted[sorted.Length / 2];
        }
    }
}
