//-----------------------------------------------------------------------
// <copyright file="VideoConversionTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tools > Convert video to MP4 / to RGB: the new video has all the frames, at the same size and
    /// frame rate (GitHub issue #74: converting to MP4 used to fail).
    /// </summary>
    [TestClass]
    public class VideoConversionTests
    {
        private const int Frames = 30;
        private const double Fps = 50;
        private static readonly Size FrameSize = new Size(160, 120);

        [DataTestMethod]
        [DataRow("mp4")]
        [DataRow("rgb")]
        public void ConvertedVideoHasAllTheFrames(string to)
        {
            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisVideoConversionTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var input = Path.Combine(folder, "input.avi");
                using (var writer = new VideoWriter(input, 0, Fps, FrameSize, isColor: false))
                using (var buffer = new Mat())
                {
                    for (int i = 0; i < Frames; i++)
                    {
                        using var image = new Image<Gray, byte>(FrameSize);
                        image.Draw(new CircleF(new PointF(20 + i * 4, 60), 15), new Gray(255), -1);
                        VideoTools.WriteFrame(writer, image, buffer);
                    }
                }

                string output;
                if (to == "mp4")
                {
                    VideoTools.ConvertToMP4(input);
                    output = input + "out2.mp4";
                }
                else
                {
                    VideoTools.ConvertVideoToRGB(input);
                    output = input + "out2.avi";
                }

                using var video = new VideoCapture(output);
                Assert.IsTrue(video.IsOpened, $"Can't open {output}.");
                Assert.AreEqual(FrameSize.Width, video.Width);
                Assert.AreEqual(FrameSize.Height, video.Height);
                Assert.AreEqual(Fps, video.Get(CapProp.Fps), 0.01);

                var count = 0;
                using var frame = new Mat();
                while (video.Read(frame) && !frame.IsEmpty) count++;
                Assert.AreEqual(Frames, count);
            }
            finally
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                }
                catch (IOException)
                {
                    // A file may still be open. It is in the temp folder anyway.
                }
            }
        }
    }
}
