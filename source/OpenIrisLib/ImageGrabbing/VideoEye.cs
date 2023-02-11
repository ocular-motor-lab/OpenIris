//-----------------------------------------------------------------------
// <copyright file="VideoEye.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;

    /// <summary>
    /// Captures eye images from video files.
    /// </summary>
    public class VideoEye : IImageEyeSource, IDisposable
    {
        /// <summary>
        /// True after Stop is called.
        /// </summary>
        private bool stopping;

        /// <summary>
        /// Capture video object.
        /// </summary>
        protected VideoCapture Video { get; set; }

        /// <summary>
        /// Video player associated with this video.
        /// </summary>
        public VideoPlayer? VideoPlayer { get; set; }

        /// <summary>
        /// Initializes a new instance of the VideoEyeGeneric class.
        /// </summary>
        /// <param name="whichEye">Left or right eye.</param>
        /// <param name="fileName">File name of the video.</param>
        public VideoEye(Eye whichEye, string fileName)
        {
            WhichEye = whichEye;
            Video = new VideoCapture(fileName);
            FrameRate = Video.GetCaptureProperty(CapProp.Fps);
            NumberOfFrames = (long)Video.GetCaptureProperty(CapProp.FrameCount);
            FrameSize = new Size(
                        (int)Video.GetCaptureProperty(CapProp.FrameWidth),
                        (int)Video.GetCaptureProperty(CapProp.FrameHeight));
        }
        
        /// <summary>
        /// Disposes reserouces.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();

                Video.Dispose();
            }
        }

        /// <summary>
        /// Disposes reserouces.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets arbitrary aditional information about image source.
        /// </summary>
        public object Info => string.Empty; 

        /// <summary>
        /// Gets the frame rate of the video file.
        /// </summary>
        public Size FrameSize { get; private set; }

        /// <summary>
        /// Last frame number captured.
        /// </summary>
        public long LastFrameNumber { get; private set; }

        /// <summary>
        /// Gets the frame rate of the video file.
        /// </summary>
        public long NumberOfFrames { get; private set; }

        /// <summary>
        /// Gets the frame rate of the video file.
        /// </summary>
        public double FrameRate { get; private set; }

        /// <summary>
        /// Gets left or right eye (or both).
        /// </summary>
        public Eye WhichEye { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the camera is upside down, rotated or mirrored.
        /// </summary>
        public CameraOrientation CameraOrientation { get; set; }

        /// <summary>
        /// Retrieves an image. This will be called by image grabber. Part of IImageEyeSource interface.
        /// </summary>
        /// <returns>Image of the eye grabbed.</returns>
        public ImageEye? GrabImageEye()
        {
            if (stopping) return null;

            try
            {
                var image = GrabImageEyeFromVideo();

                if (image == null) return null;

                if (LastFrameNumber > (long)image.TimeStamp.FrameNumber)
                        throw new InvalidOperationException("Frame numbers should keep growing.");

                LastFrameNumber = (long)image.TimeStamp.FrameNumber;
                return image;
            }
            catch (Exception) when (stopping)
            {
                return null;
            }
        }

        /// <summary>
        /// Scroll video playback to a particular frame number. Careful with this calls the may not
        /// be thread safe.
        /// </summary>
        /// <param name="frameNumber">Frame number to scroll to.</param>
        public void Scroll(ulong frameNumber)
        {
            if (frameNumber != Video.GetCaptureProperty(CapProp.PosFrames))
            {
                Video.SetCaptureProperty(CapProp.PosFrames, (double)frameNumber);
            }
        }

        /// <summary>
        /// Starts capturing images it is expected that the frame numbers start at 0 and they are
        /// always monotonic. There could be dropped frames though. The <see
        /// cref="EyeTrackerImageGrabber"/> object will take care of it.
        /// </summary>
        public void Start()
        {
            Video.Start();
        }

        /// <summary>
        /// Stops capturing images.
        /// </summary>
        public void Stop()
        {
            stopping = true;

            Video.Stop();
        }

        /// <summary>
        /// Retrieves an image from the video file and adds the timestamp.
        /// </summary>
        /// <returns>Image of the eye grabbed.</returns>
        public virtual ImageEye GrabImageEyeFromVideo()
        {
            // Set up the timestamp for the image
            ImageEyeTimestamp timestamp = new ImageEyeTimestamp
            {
                FrameNumber = (ulong)Video.GetCaptureProperty(CapProp.PosFrames),
                FrameNumberRaw = (ulong)Video.GetCaptureProperty(CapProp.PosFrames),
                Seconds = Video.GetCaptureProperty(CapProp.PosMsec) / 1000
            };

            // Retrieve the new frame
            var grayImage = Video.QueryFrame().ToImage<Gray, byte>();

            var imageEye = new ImageEye(grayImage.Bitmap, WhichEye, timestamp);
            imageEye.CorrectOrientation(CameraOrientation);

            LastFrameNumber = (long)timestamp.FrameNumberRaw;
            return imageEye;
        }
    }
}