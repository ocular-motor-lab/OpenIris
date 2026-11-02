//-----------------------------------------------------------------------
// <copyright file="VideoWritingTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Writing EmguCV images to videos. FFmpeg reads 1024 bytes past the end of gray frames, and
    /// writing them crashed now and then (access violation, #32). These write thousands of new images
    /// while the garbage collector runs constantly.
    /// </summary>
    [TestClass]
    public class VideoWritingTests
    {
        public TestContext TestContext { get; set; } = null!;

        [DataTestMethod]
        [DataRow(false, DisplayName = "Gray, as the raw videos")]
        [DataRow(true, DisplayName = "Color, as the processed videos")]
        public void WritingImagesToVideosDoesNotCrash(bool color)
        {
            var stop = false;
            var collections = 0;
            var collector = new Thread(() =>
            {
                while (!Volatile.Read(ref stop))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    collections++;
                }
            })
            { IsBackground = true };
            collector.Start();

            var file = Path.Combine(Path.GetTempPath(), "OpenIrisVideoWritingTest-" + Guid.NewGuid().ToString("N") + ".avi");
            try
            {
                for (int video = 0; video < 4; video++)
                {
                    using var writer = new VideoWriter(file, 0, 100, new Size(320, 240), color);
                    using var buffer = new VideoFrameBuffer();
                    for (int i = 0; i < 500; i++)
                    {
                        if (color) VideoTools.WriteFrame(writer, new Image<Bgr, byte>(320, 240, new Bgr(i % 255, 0, 0)), buffer);
                        else VideoTools.WriteFrame(writer, new Image<Gray, byte>(320, 240, new Gray(i % 255)), buffer);
                    }
                }

                using var check = new VideoCapture(file);
                Assert.AreEqual(500, (int)check.Get(Emgu.CV.CvEnum.CapProp.FrameCount), "Frames in the last video.");
            }
            finally
            {
                Volatile.Write(ref stop, true);
                collector.Join();
                File.Delete(file);
            }

            TestContext.WriteLine($"2000 frames written while the garbage collector ran {collections} times.");
        }
        [DataTestMethod]
        [DataRow(320, 240, 1)]
        [DataRow(720, 540, 1)]
        [DataRow(400, 300, 1)]
        [DataRow(1440, 1080, 1)]
        [DataRow(642, 482, 3)]
        public void FramesHaveWhatFfmpegNeeds(int width, int height, int channels)
        {
            // Many frames, so some of them land next to a page boundary.
            for (int i = 0; i < 200; i++)
            {
                using var buffer = new VideoFrameBuffer();
                var frame = buffer.Frame(new Size(width, height), channels);

                // OpenCV gives FFmpeg the frame itself (not a copy without room after it) when the rows
                // are multiples of 32 bytes and the frame doesn't end within 32 bytes of a page boundary.
                var step = frame.Step;
                var start = frame.DataPointer.ToInt64();
                var end = start + (long)step * height;
                Assert.AreEqual(0, step % 32, "Bytes per row.");
                Assert.AreEqual((end - 32) >> 12, (end + 32) >> 12, "Frame next to a page boundary.");

                // FFmpeg reads 1024 bytes from the end of the image as if its rows had no padding.
                var memory = (Mat)typeof(VideoFrameBuffer).GetField("memory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(buffer)!;
                var memoryEnd = memory.DataPointer.ToInt64() + memory.Cols;
                Assert.IsTrue(start + (long)width * channels * height + 1024 <= memoryEnd, "Room after the frame.");
                Assert.IsTrue(end <= memoryEnd, "Frame inside the memory.");
            }
        }
    }
}
