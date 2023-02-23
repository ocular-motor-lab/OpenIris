//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using OpenIris.ImageGrabbing;
    using OpenIris.HeadTracking;

    /// <summary>
    /// Micromedical system.
    /// </summary>
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystemAttribute("Micromedical200Hz", typeof(EyeTrackingSystemSettingsMicromedical))]
    public class EyeTrackingSystemMicromedical200Hz : EyeTrackingSystemMicromedical
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsMicromedical;

            var (serialNumberLeft, serialNumberRight) = FindCameras(settings);

            var roi = new Rectangle(176, 112, 400, 260);

             var cameraRightEye = new CameraEyeFlyCapture(Eye.Right, serialNumberRight, settings.FrameRate, roi)
            {
                CameraOrientation = CameraOrientation.UprightMirrored,
                ShouldAdjustFrameRate = true,
                PixelMode = 1
            };
            var cameraLeftEye = new CameraEyeFlyCapture(Eye.Left, serialNumberLeft, settings.FrameRate, roi)
            {
                CameraOrientation = CameraOrientation.Rotated180Mirrored,
                ShouldAdjustFrameRate = true,
                PixelMode = 1,
            };

            //cameraRightEye.SetShutter(9);
            //cameraRightEye.SetGain(2);      
            cameraRightEye.Start();
            cameraRightEye.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
            cameraRightEye.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.input);

            //cameraLeftEye.SetShutter(9);
            //cameraLeftEye.SetGain(2);
            cameraLeftEye.Start();
            cameraLeftEye.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
            cameraLeftEye.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.input);

            // Syncrhonize the cameras
            //CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, EyeTracker.Settings.EyeTrackingSystemSettings.FrameRate, 0.003f);
            CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);

            var cameras = new EyeCollection<CameraEye>(cameraLeftEye, cameraRightEye);
         
            return cameras;
        }
    }
}
