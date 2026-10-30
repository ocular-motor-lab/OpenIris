//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
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
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystemAttribute("MicromedicalUSB", typeof(EyeTrackingSystemSettingsMicromedical))]
    public class EyeTrackingSystemMicromedicalUSB : EyeTrackingSystemMicromedical
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsMicromedical;

            // Default region of interest for Micromedical cameras
            var roi = new Rectangle(176, 112, 400, 260);

            var (serialNumberLeft, serialNumberRight) = FindCameras(settings);

            CameraEyeFlyCapture cameraRightEye = null;
            CameraEyeFlyCapture cameraLeftEye = null;

            try
            {
                if (settings.Eye == Eye.Both || settings.Eye == Eye.Right)
                {
                    cameraRightEye = new CameraEyeFlyCapture(Eye.Right, serialNumberRight, settings.FrameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.UprightMirrored,
                        ShouldAdjustFrameRate = false,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = 0,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 0
                    };
                }

                if (settings.Eye == Eye.Both || settings.Eye == Eye.Left)
                {
                    cameraLeftEye = new CameraEyeFlyCapture(Eye.Left, serialNumberLeft, settings.FrameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.Rotated180Mirrored,
                        ShouldAdjustFrameRate = false,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = 0,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 0
                    };
                }

                cameraLeftEye?.Start();
                cameraRightEye?.Start();

                if (settings.UseHeadSensor)
                {
                    headSensor = new HeadSensorTeensyMPU(settings);
                    headSensor.StartHeadSensorAndSyncWithCamera(cameraRightEye);
                }

                // Return the new cameras
                var cameras = new EyeCollection<CameraEye>(cameraLeftEye, cameraRightEye);
                cameraLeftEye = null;
                cameraRightEye = null;
                return cameras;
            }
            catch (Exception ex)
            {
                cameraLeftEye?.Stop();
                cameraRightEye?.Stop();
                headSensor = null;

                throw new InvalidOperationException("Error starting cameras. " + ex.Message, ex);
            }
            finally
            {
                cameraLeftEye?.Dispose();
                cameraRightEye?.Dispose();
            }
        }
    }
}
