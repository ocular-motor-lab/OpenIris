//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemDualWebCam.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Generic system with any two cameras.
    /// </summary>
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Dual webcam", typeof(EyeTrackingSystemSettingsDualWebcam))]
    public class EyeTrackingSystemDualWebCam : EyeTrackingSystem
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye?> CreateCameras()
        {
            var cameraSettings = Settings as EyeTrackingSystemSettingsDualWebcam ?? throw new InvalidOperationException("null settings.");

            var cameraLefteye = new CameraEyeWebCam(Eye.Left, 0);
            var cameraRightEye = new CameraEyeWebCam(Eye.Right, 1);

            cameraLefteye.CameraOrientation = cameraSettings.LeftCameraOrientation;
            cameraRightEye.CameraOrientation = cameraSettings.RightCameraOrientation;

            return new EyeCollection<CameraEye?>(cameraLefteye, cameraRightEye);
        }
    }

    /// <summary>
    /// Settings for this system.
    /// </summary>
    public class EyeTrackingSystemSettingsDualWebcam : EyeTrackingSystemSettings
    {
        /// <summary>
        /// 
        /// </summary>
        [Category("Camera properties"), Description("Orientation of the left eye camera.")]
        [NeedsRestarting]
        public CameraOrientation LeftCameraOrientation
        {
            get
            {
                return leftCameraOrientation;
            }
            set
            {
                if (value != leftCameraOrientation)
                {
                    leftCameraOrientation = value;
                    OnPropertyChanged(this, nameof(LeftCameraOrientation));
                }
            }
        }
        private CameraOrientation leftCameraOrientation = CameraOrientation.Upright;

        /// <summary>
        /// 
        /// </summary>
        [Category("Camera properties"), Description("Orientation of the right eye camera.")]
        [NeedsRestarting]
        public CameraOrientation RightCameraOrientation
        {
            get
            {
                return rightCameraOrientation;
            }
            set
            {
                if (value != rightCameraOrientation)
                {
                    rightCameraOrientation = value;
                    OnPropertyChanged(this, nameof(RightCameraOrientation));
                }
            }
        }
        private CameraOrientation rightCameraOrientation = CameraOrientation.Upright;
    }

}
