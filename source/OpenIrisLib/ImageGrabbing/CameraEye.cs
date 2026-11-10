//-----------------------------------------------------------------------
// <copyright file="CameraEye.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
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
        /// Gets the frame rate of the camera.
        /// </summary>
        public double FrameRate { get; protected set; }
        
        /// <summary>
        /// Frame size of the camera.
        /// </summary>
        public Size FrameSize { get; protected set; }

        /// <summary>
        /// Gets the size of the whole sensor, in pixels. The frame size, unless the camera reads only a
        /// window of the sensor.
        /// </summary>
        public virtual Size SensorSize => FrameSize;

        /// <summary>
        /// Gets or sets the intrinsics of the camera for the whole sensor, as the camera sends the images
        /// (before the camera orientation). The eye tracking system sets them from its settings. Each
        /// image gets them moved to its window and turned with the orientation (ImageEye.Intrinsics).
        /// </summary>
        public CameraIntrinsics Intrinsics { get; set; }

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
        /// Gets what the camera is (model, serial number, firmware...), for the log of each
        /// recording. Empty if the camera doesn't say.
        /// </summary>
        public string Description { get; protected set; } = string.Empty;

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

            // Cameras return null when there's no image, for example when they're stopped.
            if (image is null) return null;

            image.TimeStamp.TimeGrabbed = EyeTrackerDebug.TimeElapsed.TotalSeconds;
            image.TimeStamp.DateTimeGrabbed = DateTime.Now;

            // Check if the timestamp is correct
            if (LastFrameNumber > image.TimeStamp.FrameNumber)
                throw new InvalidOperationException("Frame numbers should keep growing.");

            // Keep track of the last frame number
            LastFrameNumber = image.TimeStamp.FrameNumber;

            // The whole frame, for cameras that don't say which part of the sensor they read.
            if (image.Roi.IsEmpty) image.Roi = new Rectangle(Point.Empty, image.Size);
            image.Intrinsics = Intrinsics.Cropped(image.Roi.Location);

            image.CorrectOrientation(CameraOrientation);

            return image;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
