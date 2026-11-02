//-----------------------------------------------------------------------
// <copyright file="VideoFrameBuffer.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.CvEnum;

    /// <summary>
    /// Memory for the frames written to a video with <see cref="VideoTools.WriteFrame"/>. Keep one per
    /// video, and dispose it with the video.
    /// </summary>
    /// <remarks>
    /// FFmpeg 4.4, the one in OpenCV 4.12, reads 1024 bytes past the end of every gray frame (it
    /// copies a palette from there: gray is "pseudo-paletted" in that version). When the frame ends
    /// right before memory that can't be read, writing it crashes (access violation, #32). The frame
    /// here has that much room after it. Its rows are a multiple of 32 bytes and it doesn't end next
    /// to a page boundary, so OpenCV gives it to FFmpeg as it is, not a copy without the room.
    /// </remarks>
    public sealed class VideoFrameBuffer : IDisposable
    {
        private const int RoomAfterFrame = 1024;
        private const int PageSize = 4096;

        private Mat? memory;
        private Mat? frame;

        /// <summary>
        /// Gets the frame for images of this size and number of channels, allocated the first time
        /// and again only if they change.
        /// </summary>
        internal Mat Frame(Size size, int channels)
        {
            if (frame != null && frame.Size == size && frame.NumberOfChannels == channels) return frame;

            Dispose();

            var step = (size.Width * channels + 31) & ~31;
            memory = new Mat(1, step * size.Height + 64 + RoomAfterFrame, DepthType.Cv8U, 1);

            // OpenCV copies frames that end within 32 bytes of a page boundary (OpenCV allocates
            // memory aligned to 64 bytes, and the rows are multiples of 32, so moving 64 is enough).
            var start = memory.DataPointer.ToInt64();
            var end = start + (long)step * (size.Height & ~1);
            if (((end - 32) & ~(PageSize - 1)) != ((end + 32) & ~(PageSize - 1))) start += 64;

            frame = new Mat(size.Height, size.Width, DepthType.Cv8U, channels, new IntPtr(start), step);
            return frame;
        }

        /// <summary>
        /// Frees the memory.
        /// </summary>
        public void Dispose()
        {
            frame?.Dispose();
            memory?.Dispose();
            frame = null;
            memory = null;
        }
    }
}
