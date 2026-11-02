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
            ReportTorsionSteady(data, data[PipelineHarness.SampleVideoReference].Pupil.Center);
        }

        /// <summary>
        /// Torsion noise where it can be trusted without knowing the truth: frame-to-frame
        /// differences, leaving out frames near a blink (3 frames around one not tracked), with the
        /// pupil more than 2 mm from the reference, and differences over 1° (saccades).
        /// </summary>
        private void ReportTorsionSteady(List<EyeData> data, PointF reference)
        {
            var maxDistance = 2 / PipelineHarness.SampleVideoMmPerPix;
            bool Steady(int i) =>
                Enumerable.Range(i - 3, 7).All(j => j < 0 || j >= data.Count || data[j].ProcessFrameResult == ProcessFrameResult.Good) &&
                Math.Sqrt(Math.Pow(data[i].Pupil.Center.X - reference.X, 2) + Math.Pow(data[i].Pupil.Center.Y - reference.Y, 2)) < maxDistance &&
                !double.IsNaN(data[i].TorsionAngle);

            var differences = Enumerable.Range(1, data.Count - 1)
                .Where(i => Steady(i) && Steady(i - 1))
                .Select(i => data[i].TorsionAngle - data[i - 1].TorsionAngle)
                .Where(d => Math.Abs(d) < 1)
                .ToArray();
            if (differences.Length == 0) return;
            var noise = Math.Sqrt(differences.Average(d => d * d) / 2);
            TestContext.WriteLine($"  Torsion steady (°): frame-to-frame noise {noise:0.000} ({differences.Length} pairs: no blinks, pupil within 2 mm of the reference, jumps under 1°)");
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

        /// <summary>
        /// Torsion against known rotations, with the real iris of the sample video: each frame is
        /// also rotated around its pupil by a known angle (up to ±5°), and the difference between
        /// the two measurements should be that angle. The eye's own torsion is in both, so it
        /// cancels. Gain is the slope of measured against applied (1 is perfect).
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SampleVideoRotated(Eye eye)
        {
            const int frames = 300;
            var plain = PipelineHarness.RunSampleVideo("JOM", eye, frames);
            var random = new Random(1);
            var angles = Enumerable.Range(0, plain.Count).Select(_ => random.NextDouble() * 10 - 5).ToArray();

            var rotatedFrames = PipelineHarness.ReadVideo(PipelineHarness.SampleVideo(eye), eye, plain.Count)
                .Select((frame, i) => Rotate(frame, plain[i].Pupil.Center, angles[i]));
            var rotated = PipelineHarness.Run("JOM", eye, rotatedFrames, PipelineHarness.SampleVideoMmPerPix, eyeModelFromReference: true,
                reference: PipelineHarness.ReadFrame(PipelineHarness.SampleVideo(eye), eye, PipelineHarness.SampleVideoReference),
                configure: PipelineHarness.Thresholds(PipelineHarness.SampleVideoDarkThreshold));

            var pairs = Enumerable.Range(0, plain.Count)
                .Where(i => plain[i].ProcessFrameResult == ProcessFrameResult.Good && rotated[i].ProcessFrameResult == ProcessFrameResult.Good)
                .Select(i => (applied: angles[i], measured: rotated[i].TorsionAngle - plain[i].TorsionAngle))
                .Where(p => !double.IsNaN(p.measured))
                .ToArray();
            Assert.IsTrue(pairs.Length > 1, "No torsion measured.");

            // Torsion is positive clockwise from the subject, and rotating the image counterclockwise
            // (OpenCV's positive angles) is... whichever sign the gain says.
            var gain = pairs.Sum(p => p.applied * p.measured) / pairs.Sum(p => p.applied * p.applied);
            var errors = pairs.Select(p => p.measured - Math.Sign(gain) * p.applied).ToArray();
            var mean = errors.Average();
            var sd = Math.Sqrt(errors.Average(e => (e - mean) * (e - mean)));
            TestContext.WriteLine($"Sample video {eye} rotated: {pairs.Length}/{plain.Count} frames measured");
            TestContext.WriteLine($"  Torsion (°): gain {gain:0.000}, accuracy {mean:0.000}, precision {sd:0.000}, " +
                $"max |error| {errors.Max(Math.Abs):0.000}, errors over 0.5° {errors.Count(e => Math.Abs(e) > 0.5)}");
        }

        /// <summary>
        /// Rotates a frame around a point (OpenCV's positive angles: counterclockwise in the image).
        /// </summary>
        private static ImageEye Rotate(ImageEye frame, PointF center, double angle)
        {
            using (frame)
            {
                using var matrix = new Emgu.CV.Mat();
                Emgu.CV.CvInvoke.GetRotationMatrix2D(center, angle, 1, matrix);
                var image = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(frame.Image.Size);
                Emgu.CV.CvInvoke.WarpAffine(frame.Image, image, matrix, frame.Image.Size, Emgu.CV.CvEnum.Inter.Cubic, Emgu.CV.CvEnum.Warp.Default, Emgu.CV.CvEnum.BorderType.Replicate);
                return new ImageEye(image, frame.WhichEye, frame.TimeStamp, null) { Roi = frame.Roi };
            }
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
