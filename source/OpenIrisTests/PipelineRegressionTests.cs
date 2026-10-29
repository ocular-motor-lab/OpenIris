//-----------------------------------------------------------------------
// <copyright file="PipelineRegressionTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks that every pipeline still produces the same data, frame by frame, as when its baseline
    /// was recorded. A failure means some change altered tracking results, even slightly.
    /// </summary>
    [TestClass]
    public class PipelineRegressionTests
    {
        private const int SyntheticFrames = 100;
        private const int VideoFrames = 300;

        [DataTestMethod]
        [DataRow("JOM", Eye.Left)]
        [DataRow("JOM", Eye.Right)]
        [DataRow("SimplePupilCRBlobs", Eye.Left)]
        [DataRow("SimplePupilCRBlobs", Eye.Right)]
        [DataRow("SimpleCentroid", Eye.Left)]
        [DataRow("SimpleCentroid", Eye.Right)]
        public void SyntheticEyeMatchesBaseline(string pipeline, Eye eye)
        {
            var data = PipelineHarness.Run(pipeline, eye, SyntheticEye.Frames(eye, SyntheticFrames), SyntheticEye.MmPerPix);

            Baseline.Check($"synthetic-{pipeline}-{eye}", data);
        }

        /// <summary>
        /// Runs on the recordings used by the Simulation eye tracking system. They are not in git, so
        /// this test is skipped where the VideoSamples folder is missing (for example on GitHub).
        /// </summary>
        [DataTestMethod]
        [DataRow("JOM", Eye.Left)]
        [DataRow("JOM", Eye.Right)]
        [DataRow("SimplePupilCRBlobs", Eye.Left)]
        [DataRow("SimplePupilCRBlobs", Eye.Right)]
        [DataRow("SimpleCentroid", Eye.Left)]
        [DataRow("SimpleCentroid", Eye.Right)]
        public void SampleVideoMatchesBaseline(string pipeline, Eye eye)
        {
            var video = Path.Combine(PipelineHarness.VideoSamplesFolder, eye == Eye.Left ? "JorgeLeft.avi" : "JorgeRight.avi");
            if (!File.Exists(video)) Assert.Inconclusive($"Sample video not found: {video}");

            // Same pixel scale as the Simulation system's default settings.
            var data = PipelineHarness.Run(pipeline, eye, PipelineHarness.ReadVideo(video, eye, VideoFrames), SyntheticEye.MmPerPix);

            Baseline.Check($"video-{pipeline}-{eye}", data);
        }
    }
}
