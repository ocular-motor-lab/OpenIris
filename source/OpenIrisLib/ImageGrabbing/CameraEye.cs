//-----------------------------------------------------------------------
// <copyright file="CameraEye.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
        private bool disposedValue;

        /// <summary>
        /// Gets the frame rate of the camera.
        /// </summary>
        public double FrameRate { get; protected set; }
        
        /// <summary>
        /// Frame size of the camera.
        /// </summary>
        public Size FrameSize { get; protected set; }

        /// <summary>
        /// Gets left or right eye (or both).
        /// </summary>
        public Eye WhichEye { get; set; }

        /// <summary>
        /// Gets a value indicating whether the camera is upside down, rotated or mirrored.
        /// </summary>
        public CameraOrientation CameraOrientation { get; set; }

        /// <summary>
        /// Last frame number captured.
        /// </summary>
        public ulong LastFrameNumber { get; protected set; }

        /// <summary>
        /// Gets the diagnostics info.
        /// </summary>
        public virtual object Info { get { return string.Empty; } }

        /// <summary>
        /// Overrides the StartCapture method.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Finish grabbing images from the camera and free resources.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        protected abstract ImageEye GrabImageFromCamera();

        /// <summary>
        /// Gets the current time in the camera. Not all cameras will implement this feature
        /// Needs to be fast and not interfere with the image transmission. 
        /// </summary>
        /// <returns>The current time in the camera.</returns>
        public virtual double GetCameraTime() => double.NaN;

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        public ImageEye GrabImageEye()
        {
            // Grab the image from the particular camera implementation
            var image = GrabImageFromCamera();

            image.TimeStamp.TimeGrabbed = EyeTrackerDebug.TimeElapsed.TotalSeconds;
            image.TimeStamp.DateTimeGrabbed = DateTime.Now;

            if (image is null) return null;

            // Check if the timestamp is correct
            if (LastFrameNumber > image.TimeStamp.FrameNumber)
                throw new InvalidOperationException("Frame numbers should keep growing.");

            // Keep track of the last frame number
            LastFrameNumber = image.TimeStamp.FrameNumber;

            image.CorrectOrientation(CameraOrientation);

            return image;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
