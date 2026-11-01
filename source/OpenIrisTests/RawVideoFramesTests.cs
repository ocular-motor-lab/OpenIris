//-----------------------------------------------------------------------
// <copyright file="RawVideoFramesTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The -videoframes.txt file of a recording says where each frame is in the raw videos, so a
    /// video frame can be matched with its line of data even when frames are missing from a video
    /// (GitHub issue #68).
    /// </summary>
    [TestClass]
    public class RawVideoFramesTests
    {
        [TestMethod]
        [Timeout(60000)]
        public async Task FramesMissingFromAVideoAreMarked()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisVideoFramesTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                using var session = new RecordingSession(new RecordingOptions { SessionName = "Test", DataFolder = folder, SaveRawVideo = true });
                var recording = session.Start(new CalibrationParameters(), new EyeTrackerSettings());

                // Frame 3 has no right image (as when the slave camera misses a trigger), and the left
                // image of frame 4 has a different size than the video.
                Assert.IsTrue(session.TryRecordImages(Frame(1, Image(Eye.Left), Image(Eye.Right)), 100));
                Assert.IsTrue(session.TryRecordImages(Frame(2, Image(Eye.Left), Image(Eye.Right)), 100));
                Assert.IsTrue(session.TryRecordImages(Frame(3, Image(Eye.Left), null), 100));
                Assert.IsTrue(session.TryRecordImages(Frame(4, Image(Eye.Left, width: 80), Image(Eye.Right)), 100));
                Assert.IsTrue(session.TryRecordImages(Frame(5, Image(Eye.Left), Image(Eye.Right)), 100));
                session.Stop(5);
                await recording;

                var framesFile = session.DataFileName.Replace(".txt", "-videoframes.txt");
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "FrameNumber LeftVideoFrame RightVideoFrame",
                        "1 0 0",
                        "2 1 1",
                        "3 2 NaN",
                        "4 NaN 2",
                        "5 3 3",
                    },
                    File.ReadAllLines(framesFile));

                Assert.AreEqual(4, FrameCount(session.DataFileName.Replace(".txt", "-Left.avi")));
                Assert.AreEqual(4, FrameCount(session.DataFileName.Replace(".txt", "-Right.avi")));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        private static ImageEye Image(Eye whichEye, int width = 64) =>
            new ImageEye(new Image<Gray, byte>(width, 48), whichEye, default, null);

        private static EyeCollection<ImageEye?> Frame(ulong frameNumber, ImageEye? left, ImageEye? right)
        {
            var timestamp = new ImageEyeTimestamp(frameNumber / 100.0, frameNumber, frameNumber);
            foreach (var image in new[] { left, right }) if (image is not null) image.TimeStamp = timestamp;
            return new EyeCollection<ImageEye?>(left, right);
        }

        private static int FrameCount(string file)
        {
            using var video = new VideoCapture(file);
            return (int)video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
        }
    }
}
