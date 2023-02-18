//-----------------------------------------------------------------------
// <copyright file="CameraEye.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Class to control a camera in the context of eye tracking.
    /// </summary>
    /// <remarks>This class could potentially be used to control any camera that can 
    /// be accessed using openCV.</remarks>
    public abstract class CameraEye : IImageEyeSource
    {
        /// <summary>
        /// Gets left or right eye (or both).
        /// </summary>
        public Eye WhichEye { get; set; }

        /// <summary>
        /// Gets a value indicating whether the camera is upside down, rotated or mirrored.
        /// </summary>
        public CameraOrientation CameraOrientation { get; set; }

        /// <summary>
        /// Gets the frame rate of the camera.
        /// </summary>
        public double FrameRate { get; protected set; }
        
        /// <summary>
        /// Frame size of the camera.
        /// </summary>
        public Size FrameSize { get; protected set; }

        /// <summary>
        /// Last frame number captured.
        /// </summary>
        public long LastFrameNumber { get; protected set; }

        /// <summary>
        /// Gets the diagnostics info.
        /// </summary>
        public virtual object Info { get { return string.Empty; } }

        /// <summary>
        /// Overrides the StartCapture method.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Overrides the StopCapture method.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        protected abstract ImageEye GrabImageFromCamera();

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        public ImageEye GrabImageEye()
        {
            // Grab the image from the particular camera implementation
            var image = GrabImageFromCamera();

            if (EyeTracker.DEBUG) image.TimeStamp.TimeGrabbed = EyeTrackerDebug.TimeElapsed.TotalSeconds;

            if (image is null) return null;

            // Check if the timestamp is correct
            if (LastFrameNumber > (long)image.TimeStamp.FrameNumber)
                throw new InvalidOperationException("Frame numbers should keep growing.");

            // Keep track of the last frame number
            LastFrameNumber = (long)image.TimeStamp.FrameNumber;

            image.CorrectOrientation(CameraOrientation);

            return image;
        }
    }
}
