//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemDualWebCam.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Dual webcam", typeof(EyeTrackingSystemSettingsDualWebcam))]
    public class EyeTrackingSystemDualWebCam : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye?[] CreateAndStartCameras()
        {
            var cameraSettings = Settings as EyeTrackingSystemSettingsDualWebcam ?? throw new InvalidOperationException("null settings.");

            var cameraLefteye = cameraSettings.Eye switch
            {
                Eye.Both => new CameraEyeWebCam(Eye.Left, 0),
                Eye.Left => new CameraEyeWebCam(Eye.Left, 0),
                _ => null,
            };

            var cameraRightEye = cameraSettings.Eye switch
            {
                Eye.Both  => new CameraEyeWebCam(Eye.Left, 1),
                Eye.Right => new CameraEyeWebCam(Eye.Left, 0),
                _ => null,
            };

            if (cameraLefteye != null)
            {
                cameraLefteye.CameraOrientation = cameraSettings.LeftCameraOrientation;
            }

            if (cameraRightEye != null)
            {
                cameraRightEye.CameraOrientation = cameraSettings.RightCameraOrientation;
            }

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
