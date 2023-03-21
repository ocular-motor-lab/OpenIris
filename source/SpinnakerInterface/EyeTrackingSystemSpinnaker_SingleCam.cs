using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using SpinnakerNET;
using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using Spinnaker;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using OpenIris.ImageProcessing;
using static OpenIris.ImageProcessing.PupilTracking;
using System.ComponentModel;

namespace SpinnakerInterface
{
#nullable enable

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Single Camera", typeof(EyeTrackingSystemSettingsSpinnaker))]

    class EyeTrackingSystemSpinnaker_SingleCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker camera = null;
        
        public override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker;

            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(Settings.Eye, 1);

                camera = new CameraEyeSpinnaker(
                whichEye: Settings.Eye,
                camera: cameraList[0],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });

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
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker;

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

    public class EyeTrackingSystemSettingsSpinnaker : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnaker()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
            Eye = Eye.Both;
        }

        [Category("Camera properties"), Description("Gain")]
        public float Gain { get => this.gain; set => SetProperty(ref gain, value, nameof(Gain)); }
        private float gain = 9;

        [Category("Camera properties"), Description("SerialNumberCamera")]
        public uint SerialNumberCamera { get => this.serialNumberCamera; set => SetProperty(ref serialNumberCamera, value, nameof(SerialNumberCamera)); }
        private uint serialNumberCamera = 22428697;

    }
}




