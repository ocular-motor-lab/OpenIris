//-----------------------------------------------------------------------
// <copyright file="CameraEyeWebCam.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Class to control a camera in the context of eye tracking.
    /// </summary>
    /// <remarks>This class could potentially be used to control any camera that can 
    /// be accessed using openCV.</remarks>
    public sealed class CameraEyeWebCam : CameraEye , IDisposable
    {
        private long numberFramesGabbed;

        /// <summary>
        /// Capture (camera) object.
        /// </summary>
        private readonly VideoCapture capture;

        /// <summary>
        /// Initializes a new instance of the CameraEyeWebCam class.
        /// </summary>
        /// <param name="whichEye">Left or right eye (or both).</param>
        /// <param name="camIndex">Index of the camera.</param>
        public CameraEyeWebCam(Eye whichEye, int camIndex)
        {
            WhichEye = whichEye;
            capture = new VideoCapture(camIndex);
            FrameSize = new Size(
                    (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth),
                    (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight));

            FrameRate = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                capture.Stop();
                capture.Dispose();
            }

            base.Dispose();
        }

        /// <summary>
        /// Overrides the StartCapture method.
        /// </summary>
        public override void Start()
        {
            capture.Start();
        }

        /// <summary>
        /// Overrides the StopCapture method.
        /// </summary>
        public override void Stop()
        {
            capture.Stop();
        }

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        protected override ImageEye GrabImageFromCamera()
        {
            // Set up the timestamp for the image
            ImageEyeTimestamp timestamp = new ImageEyeTimestamp
            {
                FrameNumber = (ulong)numberFramesGabbed++,
                Seconds = EyeTrackerDebug.TimeElapsed.Milliseconds / 1000
            };

            // Retrieve the new frame
            var tempImage = capture.QueryFrame().ToImage<Gray, byte>();

            var imageEye = new ImageEye(tempImage, WhichEye, timestamp, null);

            return imageEye;
        }
    }
}
