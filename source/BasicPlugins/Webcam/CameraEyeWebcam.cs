//-----------------------------------------------------------------------
// <copyright file="CameraEyeWebcam.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
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
    using Emgu.CV.Structure;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// A webcam, or any camera Windows sees through DirectShow.
    /// </summary>
    internal sealed class CameraEyeWebcam : CameraEye
    {
        private const int MaxFailedReads = 50;

        private readonly VideoCapture capture;
        private readonly Mat frame = new Mat();
        private ulong frameNumber;

        /// <summary>
        /// Opens a webcam.
        /// </summary>
        /// <param name="whichEye">Which eye the camera sees, or both.</param>
        /// <param name="cameraIndex">Index of the camera, 0 for the first one.</param>
        /// <param name="requestedSize">Resolution to ask for, or empty for the camera's default.
        /// The camera may choose another one.</param>
        /// <param name="requestedFrameRate">Frame rate to ask for, or 0 for the camera's default.</param>
        public CameraEyeWebcam(Eye whichEye, int cameraIndex, Size requestedSize = default, double requestedFrameRate = 0)
        {
            WhichEye = whichEye;

            WebcamInfo.LogWebcams(cameraIndex);

            capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow);
            if (!capture.IsOpened) throw new InvalidOperationException($"Could not open webcam number {cameraIndex}.");

            // The order matters with OpenCV 4.1's DirectShow: frame rate, size, then format. Setting
            // the frame rate restarts the webcam in its default format, and a format set before the
            // size is lost (tested with a C920).
            if (requestedFrameRate > 0) capture.Set(CapProp.Fps, requestedFrameRate);

            if (!requestedSize.IsEmpty)
            {
                capture.Set(CapProp.FrameWidth, requestedSize.Width);
                capture.Set(CapProp.FrameHeight, requestedSize.Height);
            }

            // Webcams send their high resolutions at full frame rate only compressed: a C920 gives
            // 1280x720 at 30 fps in MJPG but 10 fps in YUY2, the one DirectShow picks by default.
            capture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));

            WebcamInfo.LogOpened(capture);

            FrameSize = new Size(
                (int)capture.Get(CapProp.FrameWidth),
                (int)capture.Get(CapProp.FrameHeight));
            var frameRate = capture.Get(CapProp.Fps);
            FrameRate = frameRate > 0 ? frameRate : requestedFrameRate > 0 ? requestedFrameRate : 30;
        }

        /// <inheritdoc/>
        public override object Info => $"Webcam {FrameSize.Width}x{FrameSize.Height} at {FrameRate:0.#} fps";

        /// <inheritdoc/>
        public override void Start()
        {
            frameNumber = 0;
        }

        /// <inheritdoc/>
        public override void Stop()
        {
        }

        /// <inheritdoc/>
        protected override ImageEye GrabImageFromCamera()
        {
            // Read waits for the next frame, which sets the pace of the grabbing loop.
            for (int failed = 0; ; failed++)
            {
                capture.Read(frame);
                if (!frame.IsEmpty) break;
                if (failed >= MaxFailedReads) throw new InvalidOperationException("The webcam stopped sending images.");
            }

            var image = new Image<Gray, byte>(frame.Width, frame.Height);
            if (frame.NumberOfChannels == 1)
            {
                frame.CopyTo(image);
            }
            else
            {
                CvInvoke.CvtColor(frame, image, frame.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray);
            }

            // Webcams don't give their own timestamps or frame counters through OpenCV, so frames
            // dropped by the camera itself can't be counted, and two webcams can't be synchronized.
            frameNumber++;
            var timestamp = new ImageEyeTimestamp(EyeTrackerDebug.TimeElapsed.TotalSeconds, frameNumber, frameNumber);

            return new ImageEye(image, WhichEye, timestamp, null);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                capture.Dispose();
                frame.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
