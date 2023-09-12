using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using System.Drawing;
using System;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using OpenIris.ImageProcessing;
using static OpenIris.ImageProcessing.PupilTracking;
using System.ComponentModel;
using FlyCapture2Managed;

namespace SpinnakerInterface
{
#nullable enable
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Flycapture Single Camera", typeof(EyeTrackingSystemSettings))]


    class EyeTrackingSystemFlyCapture_SingleCam : EyeTrackingSystemBase
    {
        protected CameraEyeFlyCapture? camera = null;

        protected override CameraEye?[]? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettings;

            try
            {
                camera = new CameraEyeFlyCapture(
                whichEye: Settings.Eye,
                requestedFrameRate: (float)Settings.FrameRate,
                roi: new Rectangle { Width = 1920, Height = 460 })
                {
                    PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8
                };

                camera.Start();
            }
            catch (Exception ex)
            {
                if (camera != null)
                {
                   camera.Stop();
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }

            switch (settings.Eye)
            {
                case Eye.Left:
                    return new EyeCollection<CameraEye?>(camera, null);
                case Eye.Right:
                    return new EyeCollection<CameraEye?>(null, camera);
                case Eye.Both:
                    return new EyeCollection<CameraEye?>(camera);
                default: return new EyeCollection<CameraEye?>(camera);
            }
        }
    }
}




