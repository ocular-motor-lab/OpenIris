//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemEyeSeeCam.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Micromedical system.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("EyeSeeCam")]
    public class EyeTrackingSystemEyeSeeCam : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye[] CreateAndStartCameras()
        {
            var frameRate = Settings.FrameRate;
            var roi = new System.Drawing.Rectangle(0, 60, 376 * 2, 120 * 2);

            ////EyeTracker.Settings.FrameRate
            var camera = new CameraEyeFlyCapture(Eye.Right, frameRate, roi)
            {
                ShouldAdjustFrameRate = true,
                CameraOrientation = CameraOrientation.Upright,
                AutoExposure = true,
                //if (!settings.AutoExposure)
                //{
                //    cameraLeftEye.ShutterDuration = settings.ShutterDuration;
                //    cameraLeftEye.Gain = settings.Gain;
                //}

                PixelMode = 1,

            };
            camera.Start();


            //// Syncrhonize the cameras
            ////CameraEyeFlyCapture.SyncCameras(new CameraEyeFlyCapture[] { cameraLeftEye, cameraRightEye }, EyeTracker.Settings.FrameRate);

            return new EyeCollection<CameraEye>(null, camera);
        }

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        protected override VideoEye[] CreateVideos(string[] filenames)
        {
            return new EyeCollection<VideoEye>(
                new VideoEyeFlyCapture(Eye.Left, filenames[(int)Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.BottomLeftHorizontal),
                new VideoEyeFlyCapture(Eye.Right, filenames[(int)Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopRightHorizontal));
        }
    }
}
