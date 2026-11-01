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
                range = new Range(0, (long)Math.Round(videoReaderLeft.GetCaptureProperty(CapProp.FrameCount)));
            }

            var taskLeft = Task.Run(() =>
            {
                videoReaderLeft.SetCaptureProperty(CapProp.PosFrames, (double)range.Begin);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var img = videoReaderLeft.QueryFrame();
                    if (img is null) return;

                    videoWriterLeft.Write(img);

                    if (videoReaderLeft.GetCaptureProperty(CapProp.PosFrames) > range.End) break;

                    var percent = (int)Math.Round((videoReaderLeft.GetCaptureProperty(CapProp.PosFrames) - range.Begin) / (range.End - range.Begin) * 100);
                    progress?.Report(percent);
                }
            });
            var taskRight = Task.Run(() =>
            {
                videoReaderRight.SetCaptureProperty(CapProp.PosFrames, (double)range.Begin);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var img = videoReaderRight.QueryFrame();
                    if (img is null) return;

                    videoWriterRight.Write(img);
                    if (videoReaderRight.GetCaptureProperty(CapProp.PosFrames) > range.End) break;
                }
            });

            await Task.WhenAll(taskLeft, taskRight);
        }

        /// <summary>
        /// Rewrites a video as a color AVI, named like the original plus "out2.avi".
        /// </summary>
        public static void ConvertVideoToRGB(string fileName) => CopyVideo(fileName, fileName + "out2.avi");

        /// <summary>
        /// Rewrites a video as MP4, named like the original plus "out2.mp4". TODO: it does not work.
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
