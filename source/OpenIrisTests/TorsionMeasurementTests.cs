//-----------------------------------------------------------------------
// <copyright file="TorsionMeasurementTests.cs">
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
    /// Measures how noisy the JOM pipeline's torsion is, to compare image processing changes before
    /// and after. The torsion reference is a fixed frame (see PipelineHarness), so every version
    /// measures against the same frame.
    /// </summary>
    /// <remarks>
    /// Category Measurement: it reports numbers and only fails if torsion can't be measured at all.
    /// Run it with --filter "TestCategory=Measurement" --logger "console;verbosity=detailed".
    /// </remarks>
    [TestClass]
    public class TorsionMeasurementTests
    {
        private const int VideoFrames = 1000;
        private const int SyntheticFrames = 100;

        public TestContext TestContext { get; set; } = null!;

        /// <summary>
        /// On the sample video the true torsion isn't known, so the noise is estimated from the
        /// differences between consecutive frames.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SampleVideoTorsion(Eye eye)
        {
            Report($"sample video {eye}", PipelineHarness.RunSampleVideo("JOM", eye, VideoFrames));
        }

        /// <summary>
        /// The synthetic eye's iris never rotates, so all torsion is error.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Measurement")]
        [DataRow(Eye.Left)]
        [DataRow(Eye.Right)]
        public void SyntheticEyeTorsion(Eye eye)
        {
            Report($"synthetic eye {eye}", PipelineHarness.RunSyntheticEye("JOM", eye, SyntheticFrames));
        }

        private void Report(string what, List<EyeData> data)
        {
            var torsion = data.Select(d => d.ProcessFrameResult == ProcessFrameResult.Good ? d.TorsionAngle : double.NaN).ToArray();
            var valid = torsion.Where(t => !double.IsNaN(t)).ToArray();
            Assert.IsTrue(valid.Length > 1, $"No torsion measured on the {what}.");

            var mean = valid.Average();
            var rms = Math.Sqrt(valid.Average(t => t * t));
            var sd = Math.Sqrt(valid.Average(t => (t - mean) * (t - mean)));

            // Noise: differences between consecutive frames, both measured. Divided by √2 because each
            // difference has the noise of two frames.
            var differences = torsion.Zip(torsion.Skip(1), (a, b) => b - a).Where(d => !double.IsNaN(d)).ToArray();
            var noise = Math.Sqrt(differences.Average(d => d * d) / 2);

            TestContext.WriteLine(
                $"Torsion, {what}: {valid.Length}/{torsion.Length} frames, RMS {rms:0.000}°, mean {mean:0.000}°, SD {sd:0.000}°, " +
                $"frame-to-frame noise {noise:0.000}°, max |torsion| {valid.Max(Math.Abs):0.000}°");
        }
    }
}
