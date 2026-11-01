//-----------------------------------------------------------------------
// <copyright file="VideoReadingTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Reading videos with OpenCV: things that went wrong with older versions of OpenCV.
    /// </summary>
    [TestClass]
    public class VideoReadingTests
    {
        /// <summary>
        /// Scrolling to a frame of an MP4 gives that frame (OpenCV 4.1 landed on a nearby one).
        /// </summary>
        [TestMethod]
        public void ScrollingInMp4GivesTheRightFrame()
        {
            var file = PipelineHarness.SampleVideo(Eye.Left);

            Image<Gray, byte> sequential;
            using (var video = new VideoEye(Eye.Left, file))
            {
                for (int i = 0; i < 300; i++) video.GrabImageEyeFromVideo();
                sequential = video.GrabImageEyeFromVideo().Image.Copy();
            }

            using (var video = new VideoEye(Eye.Left, file))
            {
                video.Scroll(300);
                var scrolled = video.GrabImageEyeFromVideo();

                Assert.AreEqual(300UL, scrolled.TimeStamp.FrameNumber);
                using var difference = sequential.AbsDiff(scrolled.Image);
                Assert.AreEqual(0, CvInvoke.CountNonZero(difference), "The scrolled frame is a different frame.");
            }
        }

        /// <summary>
        /// Videos filmed with a phone held upright come out upright: OpenCV applies the rotation
        /// saved in the file.
        /// </summary>
        [TestMethod]
        public void PhoneVideosAreUpright()
        {
            var file = Path.Combine(PipelineHarness.SourceFolder, "..", "..", "sample-videos", "Jorge-iPhone.MOV");
            using var video = new VideoCapture(file);
            using var frame = video.QueryFrame();

            Assert.IsNotNull(frame);
            Assert.IsTrue(frame.Height > frame.Width, $"The frame is {frame.Width}x{frame.Height}, not portrait.");
        }
    }
}
