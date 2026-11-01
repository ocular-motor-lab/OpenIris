//-----------------------------------------------------------------------
// <copyright file="TrackingMeasurementTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Measures the accuracy and precision of the JOM pipeline, to compare image processing changes
    /// before and after: pupil, reflection, pupil minus reflection, and torsion. The torsion
    /// reference is a fixed frame (see PipelineHarness), so every version measures against the same
    /// frame.
    /// </summary>
    /// <remarks>
    /// Category Measurement: it reports numbers and only fails if nothing can be measured.
    /// Run it with --filter "TestCategory=Measurement" --logger "console;verbosity=detailed".
    /// </remarks>
    [TestClass]
    public class TrackingMeasurementTests
    {
        private const int VideoFrames = 1000;
        private const int SyntheticFrames = 100;

        public TestContext TestContext { get; set; } = null!;

        /// <summary>
        /// On the sample video the truth isn't known, so precision is the frame-to-frame noise:
        /// differences between consecutive frames. For positions it is robust (a median), so
        /// saccades and blinks don't count. Torsion comes in steps of 0.02°, which makes most
        /// differences 0, so its noise is an RMS.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SampleVideo(Eye eye)
        {
            var data = PipelineHarness.RunSampleVideo("JOM", eye, VideoFrames);
            var good = data.Select(d => d.ProcessFrameResult == ProcessFrameResult.Good ? d : null).ToArray();
            var withReflection = good.Select(d => d?.CornealReflections?.Length > 0 ? d : null).ToArray();

            TestContext.WriteLine($"Sample video {eye}: {good.Count(d => d != null)}/{data.Count} frames tracked, {withReflection.Count(d => d != null)} with a reflection");
            TestContext.WriteLine($"  Precision (frame-to-frame noise, px): " +
                $"pupil {Noise(good, d => d.Pupil.Center.X):0.000} {Noise(good, d => d.Pupil.Center.Y):0.000}, " +
                $"reflection {Noise(withReflection, d => d.CornealReflections[0].Center.X):0.000} {Noise(withReflection, d => d.CornealReflections[0].Center.Y):0.000}, " +
                $"pupil - reflection {Noise(withReflection, d => d.Pupil.Center.X - d.CornealReflections[0].Center.X):0.000} {Noise(withReflection, d => d.Pupil.Center.Y - d.CornealReflections[0].Center.Y):0.000}");
            ReportTorsion(good, truthIsZero: false);
        }

        /// <summary>
        /// The synthetic eye's truth is known: accuracy is the mean error, precision its standard
        /// deviation. Its iris never rotates, so all torsion is error.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SyntheticEye(Eye eye)
        {
            var data = PipelineHarness.RunSyntheticEye("JOM", eye, SyntheticFrames);
            var good = data.Select(d => d.ProcessFrameResult == ProcessFrameResult.Good ? d : null).ToArray();
            var truth = Enumerable.Range(0, data.Count).Select(i => Tests.SyntheticEye.GetTruth(eye, i)).ToArray();

            var pupil = Errors(good, (d, i) => Subtract(d.Pupil.Center, truth[i].PupilCenter));
            var reflection = Errors(good, (d, i) => d.CornealReflections?.Length > 0 ? Subtract(d.CornealReflections[0].Center, truth[i].ReflectionCenter) : (PointF?)null);

            TestContext.WriteLine($"Synthetic eye {eye}: {good.Count(d => d != null)}/{data.Count} frames tracked, {reflection.Count} with a reflection");
            TestContext.WriteLine($"  Pupil (px): accuracy {Mean(pupil, p => p.X):0.000} {Mean(pupil, p => p.Y):0.000}, precision {Sd(pupil, p => p.X):0.000} {Sd(pupil, p => p.Y):0.000}");
            TestContext.WriteLine($"  Reflection (px): accuracy {Mean(reflection, p => p.X):0.000} {Mean(reflection, p => p.Y):0.000}, precision {Sd(reflection, p => p.X):0.000} {Sd(reflection, p => p.Y):0.000}");
            ReportTorsion(good, truthIsZero: true);
        }

        private void ReportTorsion(EyeData?[] good, bool truthIsZero)
        {
            var torsion = good.Select(d => d?.TorsionAngle ?? double.NaN).ToArray();
            var valid = torsion.Where(t => !double.IsNaN(t)).ToArray();
            Assert.IsTrue(valid.Length > 1, "No torsion measured.");

            // Divided by √2 because each difference has the noise of two frames.
            var differences = torsion.Zip(torsion.Skip(1), (a, b) => b - a).Where(d => !double.IsNaN(d)).ToArray();
            var noise = Math.Sqrt(differences.Average(d => d * d) / 2);
            var mean = valid.Average();
            var sd = Math.Sqrt(valid.Average(t => (t - mean) * (t - mean)));
            TestContext.WriteLine(truthIsZero
                ? $"  Torsion (°): accuracy {mean:0.000}, precision {sd:0.000}, frame-to-frame noise {noise:0.000}, max |error| {valid.Max(Math.Abs):0.000}"
                : $"  Torsion (°): frame-to-frame noise {noise:0.000}, SD {sd:0.000} (includes real torsion)");
        }

        private static double Noise(EyeData?[] data, Func<EyeData, double> value)
        {
            var differences = new List<double>();
            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] is null || data[i - 1] is null) continue;
                differences.Add(Math.Abs(value(data[i]!) - value(data[i - 1]!)));
            }

            if (differences.Count == 0) return double.NaN;

            // Median absolute difference, scaled to a standard deviation for normal noise (1.4826),
            // divided by √2 because each difference has the noise of two frames.
            differences.Sort();
            return differences[differences.Count / 2] * 1.4826 / Math.Sqrt(2);
        }

        private static List<PointF> Errors(EyeData?[] data, Func<EyeData, int, PointF?> error) =>
            data.Select((d, i) => d is null ? null : error(d, i)).Where(e => e.HasValue).Select(e => e!.Value).ToList();

        private static PointF Subtract(PointF a, PointF b) => new PointF(a.X - b.X, a.Y - b.Y);

        private static double Mean(List<PointF> points, Func<PointF, double> value) => points.Count == 0 ? double.NaN : points.Average(value);

        private static double Sd(List<PointF> points, Func<PointF, double> value)
        {
            if (points.Count == 0) return double.NaN;
            var mean = points.Average(value);
            return Math.Sqrt(points.Average(p => Math.Pow(value(p) - mean, 2)));
        }
    }
}
