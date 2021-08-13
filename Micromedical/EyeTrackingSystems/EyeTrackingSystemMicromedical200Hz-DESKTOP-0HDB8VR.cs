//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2017 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace VORLab.VOG
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using VORLab.VOG.ImageGrabbing;
    using VORLab.VOG.HeadTracking;

    /// <summary>
    /// Micromedical system.
    /// </summary>
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Micromedical200Hz", typeof(EyeTrackingSystemSettingsMicromedical))]
    public class EyeTrackingSystemMicromedical200Hz : EyeTrackingSystemMicromedical
    {

        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsMicromedical;

            var (serialNumberLeft, serialNumberRight) = FindCameras();

            var roi = new Rectangle(176, 112, 400, 260);

            var cameraRightEye = new CameraEyePointGreyWithTeensyHeadSensor(Eye.Right, serialNumberRight, settings.FrameRate, roi)
            {
                CameraOrientation = CameraOrientation.Upright_Mirrored,
                ShouldAdjustFrameRate = true,
                PixelMode = 1
            };
            //cameraRightEye.SetShutter(9);
            //cameraRightEye.SetGain(2);      
            cameraRightEye.Init();
            cameraRightEye.Start();
            cameraRightEye.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
            cameraRightEye.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.input);

            var cameraLeftEye = new CameraEyePointGreyWithTeensyHeadSensor(Eye.Left, serialNumberLeft, settings.FrameRate, roi)
            {
                CameraOrientation = CameraOrientation.Rotated180_Mirrored,
                ShouldAdjustFrameRate = true,
                PixelMode = 1,
            };
            //cameraLeftEye.SetShutter(9);
            //cameraLeftEye.SetGain(2);
            cameraLeftEye.Init();
            cameraLeftEye.Start();
            cameraRightEye.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
            cameraRightEye.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.input);

            // Syncrhonize the cameras
            //CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, EyeTracker.Settings.EyeTrackingSystemSettings.FrameRate, 0.003f);
            CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);

            return new EyeCollection<CameraEye>(cameraLeftEye, cameraRightEye);
        }
    }
}
