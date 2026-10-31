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

            capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow);
            if (!capture.IsOpened) throw new InvalidOperationException($"Could not open webcam number {cameraIndex}.");

            if (!requestedSize.IsEmpty)
            {
                capture.SetCaptureProperty(CapProp.FrameWidth, requestedSize.Width);
                capture.SetCaptureProperty(CapProp.FrameHeight, requestedSize.Height);
            }

            if (requestedFrameRate > 0) capture.SetCaptureProperty(CapProp.Fps, requestedFrameRate);

            FrameSize = new Size(
                (int)capture.GetCaptureProperty(CapProp.FrameWidth),
                (int)capture.GetCaptureProperty(CapProp.FrameHeight));
            var frameRate = capture.GetCaptureProperty(CapProp.Fps);
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
