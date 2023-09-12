using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using SpinnakerNET;
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
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Single Camera", typeof(EyeTrackingSystemSettingsSpinnaker_SingleCam))]


    class EyeTrackingSystemSpinnaker_SingleCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker? camera = null;
        
        protected override CameraEye?[]? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker_SingleCam;

            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(1, settings.Eye, settings.CameraSerialNumber, null);

                camera = new CameraEyeSpinnaker(
                whichEye: Settings.Eye,
                camera: cameraList[0],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { Width = 720, Height = 450 });
                
                settings.CameraSerialNumber = cameraList[0].DeviceSerialNumber.ToString();

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

        public override void Dispose()
        {
            camera?.Stop();
            camera?.Dispose();
        }

        public override EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData procesedImages)
        {
            ExtraData extraData = new ExtraData();
            var imSourceData = procesedImages.Images[Eye.Left]?.ImageSourceData;
            if (imSourceData != null)
            {
                var (exposureEndLineStatusAll, _) = (ValueTuple<long, IManagedImage>)imSourceData;
                extraData.Int0 = Convert.ToInt32(exposureEndLineStatusAll);
                procesedImages.Data.ExtraData = extraData;
            }
            imSourceData = procesedImages.Images[Eye.Right]?.ImageSourceData;
            if (imSourceData != null)
            {
                var (exposureEndLineStatusAll, _) = (ValueTuple<long, IManagedImage>)imSourceData;
                extraData.Int1 = Convert.ToInt32(exposureEndLineStatusAll);
                procesedImages.Data.ExtraData = extraData;
            }
            return procesedImages;
        }
    }

    public class EyeTrackingSystemSettingsSpinnaker_SingleCam : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnaker_SingleCam()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
            Eye = Eye.Both;
        }

        [NeedsRestarting]
        [Category("Camera properties"), Description("BothEye - CameraSerialNumber")]
        public string CameraSerialNumber { get => this.cameraSerialNumber; set => SetProperty(ref cameraSerialNumber, value, nameof(CameraSerialNumber)); }
        private string cameraSerialNumber = null;

        //[NeedsRestarting]
        [ReadOnly(true)]
        [Category("Camera properties"), Description("Gain")]
        public double Gain { get => gain; set => SetProperty(ref gain, value, nameof(Gain)); }
        private double gain = 9;

        //[NeedsRestarting]
        [ReadOnly(true)]
        [Category("Camera properties"), Description("Left Offset")]
        public Point LeftOffset { get => this.leftOffset; set => SetProperty(ref leftOffset, value, nameof(LeftOffset)); }
        private Point leftOffset = new Point(0, 0);

        //[NeedsRestarting]
        [ReadOnly(true)]
        [Category("Camera properties"), Description("Right Offset")]
        public Point RightOffset { get => this.rightOffset; set => SetProperty(ref rightOffset, value, nameof(RightOffset)); }
        private Point rightOffset = new Point(0, 0);
    }
}




