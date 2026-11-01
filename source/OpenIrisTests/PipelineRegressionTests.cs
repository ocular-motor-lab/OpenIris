//-----------------------------------------------------------------------
// <copyright file="PipelineRegressionTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
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
        [DataRow("SimpleCentroid", Eye.Left)]
        [DataRow("SimpleCentroid", Eye.Right)]
        public void SyntheticEyeMatchesBaseline(string pipeline, Eye eye)
        {
            var data = PipelineHarness.RunSyntheticEye(pipeline, eye, SyntheticFrames);

            Baseline.Check($"synthetic-{pipeline}-{eye}", data);
        }

        /// <summary>
        /// Runs on the sample videos played by the Simulation eye tracking system.
        /// </summary>
        [DataTestMethod]
        [DataRow("JOM", Eye.Left)]
        [DataRow("JOM", Eye.Right)]
        [DataRow("SimpleCentroid", Eye.Left)]
        [DataRow("SimpleCentroid", Eye.Right)]
        public void SampleVideoMatchesBaseline(string pipeline, Eye eye)
        {
            var data = PipelineHarness.RunSampleVideo(pipeline, eye, VideoFrames);

            Baseline.Check($"video-{pipeline}-{eye}", data);
        }
    }
}
