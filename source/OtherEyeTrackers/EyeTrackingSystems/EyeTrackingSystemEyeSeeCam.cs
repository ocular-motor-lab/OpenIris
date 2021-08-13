//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemEyeSeeCam.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("EyeSeeCam")]
    public class EyeTrackingSystemEyeSeeCam : EyeTrackingSystem
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateCameras()
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
        public override EyeCollection<VideoEye> CreateVideos(EyeCollection<string> filenames)
        {
            return new EyeCollection<VideoEye>(
                new VideoEyeFlyCapture(Eye.Left, filenames[Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.BottomLeftHorizontal),
                new VideoEyeFlyCapture(Eye.Right, filenames[Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopRightHorizontal));
        }
    }
}
