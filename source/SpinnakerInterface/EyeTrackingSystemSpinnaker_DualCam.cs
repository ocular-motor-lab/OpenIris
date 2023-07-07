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
using System.Threading;
using System.ComponentModel;
using SpinnakerNET.GenApi;
using System.Runtime.CompilerServices;

namespace SpinnakerInterface
{
#nullable enable

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Dual Camera", typeof(EyeTrackingSystemSettingsSpinnaker_DualCam))]

    class EyeTrackingSystemSpinnaker_DualCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker? leftEyeCamera = null;
        protected CameraEyeSpinnaker? rightEyeCamera = null;

        protected override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker_DualCam;
            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(2,settings.Eye,settings.LeftEyeCameraSerialNumber,settings.RightEyeCameraSerialNumber);

                // TODO for Roksana add checks on the settings so things don't crash if somebody enters crazy numbers

                leftEyeCamera = new CameraEyeSpinnaker(
                whichEye: Eye.Left,
                camera: cameraList[0],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X=settings.LeftOffset.X, Y=settings.LeftOffset.Y, Width = 720, Height = 450 });

                rightEyeCamera = new CameraEyeSpinnaker(
                whichEye: Eye.Right,
                camera: cameraList[1],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X = settings.RightOffset.X, Y = settings.RightOffset.Y, Width = 720, Height = 450 });

                settings.LeftEyeCameraSerialNumber = cameraList[0].DeviceSerialNumber.ToString();
                settings.RightEyeCameraSerialNumber = cameraList[1].DeviceSerialNumber.ToString();

                //start cameras
                CameraEyeSpinnaker.BeginSynchronizedAcquisition(leftEyeCamera, rightEyeCamera);

                return new EyeCollection<CameraEye?>(leftEyeCamera, rightEyeCamera);
            }
            catch (Exception ex)
            {
                leftEyeCamera?.Stop();
                rightEyeCamera?.Stop();

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }  
        }

        public override void Dispose()
        {
            leftEyeCamera?.Stop();
            leftEyeCamera?.Dispose();

            rightEyeCamera?.Stop();
            rightEyeCamera?.Dispose();
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

        protected override void SaveCameraExposure()
        {
            // TODO for Roksana 

            // Figure out current exposure from the cameras
            var leftGain = leftEyeCamera?.Gain ?? 0;
            var rightGain = rightEyeCamera?.Gain ?? 0;

            // TODO check if they are the same and throw error if not


            var mySettings = Settings as EyeTrackingSystemSettingsSpinnaker_DualCam;

            mySettings.Gain = 
        }

        protected override void SaveCameraMove()
        {

        }
    }


    public class EyeTrackingSystemSettingsSpinnaker_DualCam : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnaker_DualCam()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
            Eye = Eye.Both;
        }

        [NeedsRestarting]
        [Category("Camera properties"), Description("LeftEye - CameraSerialNumber")]
        public string LeftEyeCameraSerialNumber { get => this.leftEyeCameraSerialNumber; set => SetProperty(ref leftEyeCameraSerialNumber, value, nameof(LeftEyeCameraSerialNumber)); }
        private string leftEyeCameraSerialNumber = null;
        
        [NeedsRestarting]
        [Category("Camera properties"), Description("RightEye - CameraSerialNumber")]
        public string RightEyeCameraSerialNumber { get => this.rightEyeCameraSerialNumber; set => SetProperty(ref rightEyeCameraSerialNumber, value, nameof(RightEyeCameraSerialNumber)); }
        private string rightEyeCameraSerialNumber = null;
        
        [NeedsRestarting]
        [Category("Camera properties"), Description("Gain")]
        public float Gain { get => this.gain; set => SetProperty(ref gain, value, nameof(Gain)); }
        private float gain = 9;

        [NeedsRestarting]
        [Category("Camera properties"), Description("Left Offset")]
        public Point LeftOffset { get => this.leftOffset; set => SetProperty(ref leftOffset, value, nameof(LeftOffset)); }
        private Point leftOffset = new Point(0, 0);

        [NeedsRestarting]
        [Category("Camera properties"), Description("Right Offset")]
        public Point RightOffset { get => this.rightOffset; set => SetProperty(ref rightOffset, value, nameof(RightOffset)); }
        private Point rightOffset = new Point(0, 0);
    }
}
