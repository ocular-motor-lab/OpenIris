//-----------------------------------------------------------------------
// <copyright file="PipelinePerformanceTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Guards the processing speed that high frame rates depend on. Timing depends on the computer,
    /// so each machine keeps its own reference time, recorded the first time the test runs there
    /// (in Baselines\performance\, which is not committed). The test is skipped on build servers.
    /// </summary>
    [TestClass]
    public class PipelinePerformanceTests
    {
        private const int WarmupFrames = 20;
        private const int TimedFrames = 300;

        /// <summary>
        /// How much slower than the reference the pipeline may get before the test fails.
        /// </summary>
        private const double AllowedSlowdown = 1.25;

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [TestCategory("Performance")]
        public void JomIsNotSlowerThanBefore()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true") Assert.Inconclusive("Timing is not meaningful on shared build servers.");

            // A real eye is more representative than the synthetic one.
            var frames = PipelineHarness.ReadVideo(PipelineHarness.SampleVideo(Eye.Left), Eye.Left, WarmupFrames + TimedFrames).ToList();

            var calibration = new EyeCalibration(Eye.Left);
            var milliseconds = new double[frames.Count];
            using (var pipeline = PipelineHarness.CreatePipeline("JOM", Eye.Left, SyntheticEye.MmPerPix))
            {
                var stopwatch = new Stopwatch();
                for (int i = 0; i < frames.Count; i++)
                {
                    stopwatch.Restart();
                    var (_, imageTorsion) = pipeline.Process(frames[i], calibration);
                    stopwatch.Stop();
                    imageTorsion?.Dispose();
                    milliseconds[i] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            frames.ForEach(f => f.Dispose());

            var sorted = milliseconds.Skip(WarmupFrames).OrderBy(t => t).ToArray();
            double median = sorted[sorted.Length / 2];
            TestContext.WriteLine($"JOM pipeline, sample video: median {median:0.000} ms per frame ({1000 / median:0} frames/s per eye)");

            var config = typeof(PipelinePerformanceTests).Assembly.Location.Contains(@"\Release\") ? "Release" : "Debug";
            var referenceFile = Path.Combine(PipelineHarness.SourceFolder, "Baselines", "performance",
                $"{Environment.MachineName}-{config}-sample-video.txt");

            if (Baseline.UpdateRequested || !File.Exists(referenceFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(referenceFile));
                File.WriteAllText(referenceFile, median.ToString("0.000", CultureInfo.InvariantCulture));
                Assert.Inconclusive($"Reference time recorded in {referenceFile}.");
            }

            double reference = double.Parse(File.ReadAllText(referenceFile), CultureInfo.InvariantCulture);
            TestContext.WriteLine($"Reference: {reference:0.000} ms per frame ({referenceFile})");

            Assert.IsTrue(median <= reference * AllowedSlowdown,
                $"JOM pipeline takes {median:0.000} ms per frame, more than {AllowedSlowdown:0.##}x the reference {reference:0.000} ms. " +
                "If the slowdown is expected, delete the reference file to record a new one.");
        }
    }
}
