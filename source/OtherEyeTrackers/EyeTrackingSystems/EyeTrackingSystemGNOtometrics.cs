//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemGNOtometrics.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel.Composition;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Micromedical system.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("GNOtometrics")]
    public class EyeTrackingSystemGNOtometrics : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye[] CreateAndStartCameras()
        {
            var frameRate = Settings.FrameRate;


            var roi = new System.Drawing.Rectangle(176, 108, 400, 272);
            var mode = 0;

            if (frameRate < 190)
            {
                frameRate = 100;
            }
            else
            {
                frameRate = 200;
                roi = new System.Drawing.Rectangle(88, 50, 200, 136);
                mode = 1;
            }

            ////EyeTracker.Settings.FrameRate
            var camera = new CameraEyeFlyCapture(Eye.Right, frameRate, roi)
            {
                ShouldAdjustFrameRate = true,
                CameraOrientation = CameraOrientation.Rotated90,
                AutoExposure = true,
                //if (!settings.AutoExposure)
                //{
                //    cameraLeftEye.ShutterDuration = settings.ShutterDuration;
                //    cameraLeftEye.Gain = settings.Gain;
                //}

                PixelMode = mode
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
                null,
                new VideoEyeFlyCapture(Eye.Right, filenames[(int)Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopRightHorizontal));
        }
    }
}
