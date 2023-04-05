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
        
        public override EyeCollection<CameraEye?>? CreateAndStartCameras()
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

        public override EyeCollection<ImageEye> PreProcessImages(EyeCollection<ImageEye> images)
        {
            var settings = Settings as EyeTrackingSystemSettings;

            switch (settings.Eye)
            {
                case Eye.Both:
                    var roiLeft = new Rectangle(images[Eye.Both].Size.Width/2, 0, images[Eye.Both].Size.Width/2, images[Eye.Both].Size.Height);
                    var roiRight = new Rectangle(0, 0, images[Eye.Both].Size.Width/2, images[Eye.Both].Size.Height);

                    var imageLeft = images[Eye.Both].Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    var imageRight = images[Eye.Both].Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye>(imageLeft, imageRight);
                default:
                    return images;
            }
        }
    }
}




