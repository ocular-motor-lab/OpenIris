//-----------------------------------------------------------------------
// <copyright file="CameraEyeVideoSimulation.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System;

    /// <summary>
    /// This camera actually uses a video but it does behave like a camera in most senses
    /// and it is very useful to debug.
    /// </summary>
    public sealed class CameraEyeVideoSimulation : CameraEye, IDisposable
    { 
        private readonly VideoEye videoEye;    
        private long numberFramesGrabbed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whichEye"></param>
        /// <param name="fileName"></param>
        /// <param name="frameRate">Desidered frame rate.</param>
        public CameraEyeVideoSimulation(Eye whichEye, string fileName, float frameRate)
        {
            WhichEye = whichEye;
            videoEye = new VideoEye(whichEye, fileName);

            FrameRate = frameRate;
            FrameSize = videoEye.FrameSize;
            CameraOrientation = videoEye.CameraOrientation;
        }

        /// <summary>
        /// Gets the number of frames of the video.
        /// </summary>
        public long NumberOfFrames => videoEye.NumberOfFrames; 

        /// <summary>
        /// Number of frames to play before looping. Necessary to play videos where the left and the
        /// right eye have different durations. UGLY.
        /// </summary>
        public long LoopAtFrame { get; set; }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public override void Start() => videoEye.Start();

        /// <summary>
        /// Stops the camera
        /// </summary>
        public override void Stop() => videoEye.Stop();

        /// <summary>
        /// Disposes reserouces.
        /// </summary>
        public void Dispose() => videoEye?.Dispose();

        /// <summary>
        /// Grabs the images from the videos.
        /// </summary>
        /// <returns></returns>
        protected override ImageEye GrabImageFromCamera()
        {
            // If reached the end of the video loop
            if (videoEye.LastFrameNumber >= LoopAtFrame) videoEye.Scroll(0);

            ImageEye image = videoEye.GrabImageEyeFromVideo();
            numberFramesGrabbed++;

            // Sleep to keep the frame rate up to reported frame rate, never higher.
            System.Threading.Thread.Sleep((int)Math.Floor( 1000.0 / (FrameRate)));

            // Change the frame number so it doesn't loop
            var newTimeStamp = image.TimeStamp;
            newTimeStamp.FrameNumber = (ulong)numberFramesGrabbed;
            image.TimeStamp = newTimeStamp;

            return image;
        }
    }
}