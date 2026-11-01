//-----------------------------------------------------------------------
// <copyright file="VideoTools.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Threading;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// Tools to cut and convert videos. The Tools menu of the user interface uses them.
    /// </summary>
    public static class VideoTools
    {
        /// <summary>
        /// Extensions of the usual video files, which OpenCV reads through ffmpeg. Others may work too.
        /// </summary>
        public static readonly string[] VideoExtensions =
        {
            ".avi", ".mp4", ".m4v", ".mov", ".mkv", ".wmv", ".mpg", ".mpeg", ".webm", ".flv", ".3gp", ".mts", ".m2ts", ".ts",
        };

        /// <summary>
        /// Writes an EmguCV image to a video. Use it instead of writer.Write(image) or
        /// writer.Write(image.Mat): FFmpeg, which writes the videos, reads frames with instructions that
        /// need memory aligned to 16-64 bytes, and EmguCV images keep their pixels in .NET arrays,
        /// aligned only to 8 bytes. Writing them directly crashes (access violation) now and then. The
        /// frame is copied into a Mat that OpenCV allocated.
        /// </summary>
        /// <param name="writer">The video.</param>
        /// <param name="image">The frame.</param>
        /// <param name="buffer">Mat for the copy. Keep it from frame to frame, and dispose it with the video.</param>
        public static void WriteFrame<TColor>(VideoWriter writer, Image<TColor, byte> image, Mat buffer)
            where TColor : struct, IColor
        {
            image.Mat.CopyTo(buffer);
            GC.KeepAlive(image);
            writer.Write(buffer);
        }

        /// <summary>
        /// Copies a range of frames of the videos of each eye to new videos, named like the
        /// originals plus "trim.avi".
        /// </summary>
        /// <param name="options">Videos and range of frames. An empty range copies everything.</param>
        /// <param name="progress">Receives the percentage done.</param>
        /// <param name="cancellationToken">Stops the copy.</param>
        public static async Task TrimVideos(ProcessVideoOptions options, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            if (options?.VideoFileNames is null) return;

            var videoLeft = options.VideoFileNames[Eye.Left];
            var videoRight = options.VideoFileNames[Eye.Right];

            using var videoReaderLeft = new VideoCapture(videoLeft);
            using var videoReaderRight = new VideoCapture(videoRight);
            using var videoWriterLeft = new VideoWriter(videoLeft + "trim.avi", 100, new Size(videoReaderLeft.Width, videoReaderLeft.Height), true);
            using var videoWriterRight = new VideoWriter(videoRight + "trim.avi", 100, new Size(videoReaderRight.Width, videoReaderRight.Height), true);

            var range = options.CustomRange;
            if (range.IsEmpty)
            {
                range = new Range(0, (long)Math.Round(videoReaderLeft.Get(CapProp.FrameCount)));
            }

            var taskLeft = Task.Run(() =>
            {
                videoReaderLeft.Set(CapProp.PosFrames, (double)range.Begin);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var img = videoReaderLeft.QueryFrame();
                    if (img is null) return;

                    videoWriterLeft.Write(img);

                    if (videoReaderLeft.Get(CapProp.PosFrames) > range.End) break;

                    var percent = (int)Math.Round((videoReaderLeft.Get(CapProp.PosFrames) - range.Begin) / (range.End - range.Begin) * 100);
                    progress?.Report(percent);
                }
            });
            var taskRight = Task.Run(() =>
            {
                videoReaderRight.Set(CapProp.PosFrames, (double)range.Begin);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var img = videoReaderRight.QueryFrame();
                    if (img is null) return;

                    videoWriterRight.Write(img);
                    if (videoReaderRight.Get(CapProp.PosFrames) > range.End) break;
                }
            });

            await Task.WhenAll(taskLeft, taskRight);
        }

        /// <summary>
        /// Rewrites a video as a color AVI, named like the original plus "out2.avi".
        /// </summary>
        public static void ConvertVideoToRGB(string fileName) => CopyVideo(fileName, fileName + "out2.avi");

        /// <summary>
        /// Rewrites a video as MP4, named like the original plus "out2.mp4". It doesn't work yet (see #74).
        /// </summary>
        public static void ConvertToMP4(string fileName) => CopyVideo(fileName, fileName + "out2.mp4");

        private static void CopyVideo(string input, string output)
        {
            using var videoReader = new VideoCapture(input);
            using var videoWriter = new VideoWriter(output, 100, new Size(videoReader.Width, videoReader.Height), true);
            while (true)
            {
                var img = videoReader.QueryFrame();
                if (img is null) return;

                videoWriter.Write(img);
            }
        }
    }
}
