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
    /// Writing EmguCV images to videos. Their pixels are in .NET arrays, aligned to 8 bytes only, and
    /// FFmpeg needs more: writing them directly crashed now and then (access violation). These write
    /// thousands of new images while the garbage collector runs constantly.
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
                    using var buffer = new Mat();
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
    }
}
