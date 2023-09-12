//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemWebCam.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Generic system with any one camera.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Single webcam", typeof(EyeTrackingSystemSettingsWebCam), VideoEyeConfiguration.OneVideo)]
    public class EyeTrackingSystemSingleWebCam : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye?[] CreateAndStartCameras()
        {
            var cameraSettings = Settings as EyeTrackingSystemSettingsWebCam
                ?? throw new InvalidOperationException("Wrong type of settings;");

            var camera = new CameraEyeWebCam(Settings.Eye, 0)
            {
                CameraOrientation = cameraSettings.CameraOrientation
            };

            return Settings.Eye switch
            {
                Eye.Both => new EyeCollection<CameraEye?>(camera),
                Eye.Left => new EyeCollection<CameraEye?>(camera, null),
                Eye.Right => new EyeCollection<CameraEye?>(null, camera),
                _ => throw new NotImplementedException(),
            };
        }
    }

    /// <summary>
    /// Settings for single camera
    /// </summary>
    public class EyeTrackingSystemSettingsWebCam : EyeTrackingSystemSettings
    {
        /// <summary>
        /// 
        /// </summary>
        [Category("Camera properties"), Description("Orientation of the camera.")]
        [NeedsRestarting]
        public CameraOrientation CameraOrientation
        {
            get
            {
                return cameraOrientation;
            }
            set
            {
                if (value != cameraOrientation)
                {
                    cameraOrientation = value;
                    OnPropertyChanged(this, nameof(CameraOrientation));
                }
            }
        }
        private CameraOrientation cameraOrientation = CameraOrientation.Upright;

    }
}
