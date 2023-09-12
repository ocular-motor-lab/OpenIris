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
using System.Threading;
using System.ComponentModel;
using SpinnakerNET.GenApi;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace SpinnakerInterface
{
#nullable enable

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Quad Camera", typeof(EyeTrackingSystemSettingsSpinnaker_QuadCam))]

    class EyeTrackingSystemSpinnaker_QuadCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker? leftEyeCamera1 = null;
        protected CameraEyeSpinnaker? leftEyeCamera2 = null;
        protected CameraEyeSpinnaker? rightEyeCamera1 = null;
        protected CameraEyeSpinnaker? rightEyeCamera2 = null;

        protected override CameraEye?[]? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker_QuadCam ?? throw new ArgumentNullException("Settings are null and shouldn't");
            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(4, settings.Eye, settings.LeftEyeCamera1SerialNumber, settings.RightEyeCamera1SerialNumber, settings.LeftEyeCamera2SerialNumber, settings.RightEyeCamera2SerialNumber);

                // TODO for Roksana add checks on the settings so things don't crash if somebody enters crazy numbers

                leftEyeCamera1 = new CameraEyeSpinnaker(
                whichEye: Eye.Left,
                camera: cameraList[0],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X = settings.LeftOffset.X, Y = settings.LeftOffset.Y, Width = 720, Height = 450 });

                leftEyeCamera2 = new CameraEyeSpinnaker(
                whichEye: Eye.Left,
                camera: cameraList[0],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X = settings.LeftOffset.X, Y = settings.LeftOffset.Y, Width = 720, Height = 450 });


                rightEyeCamera1 = new CameraEyeSpinnaker(
                whichEye: Eye.Right,
                camera: cameraList[1],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X = settings.RightOffset.X, Y = settings.RightOffset.Y, Width = 720, Height = 450 });

                rightEyeCamera2 = new CameraEyeSpinnaker(
                whichEye: Eye.Right,
                camera: cameraList[1],
                frameRate: (double)settings.FrameRate,
                gain: (int)settings.Gain,
                roi: new Rectangle { X = settings.RightOffset.X, Y = settings.RightOffset.Y, Width = 720, Height = 450 });

                settings.LeftEyeCamera1SerialNumber = cameraList[0].DeviceSerialNumber.ToString();
                settings.RightEyeCamera1SerialNumber = cameraList[1].DeviceSerialNumber.ToString();

                //start cameras
                CameraEyeSpinnaker.BeginSynchronizedAcquisition(leftEyeCamera1, leftEyeCamera2, rightEyeCamera1, rightEyeCamera2);

                return new CameraEye?[] { leftEyeCamera1, leftEyeCamera2, rightEyeCamera1, rightEyeCamera2 };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }  
        }

        public override void Dispose()
        {
            leftEyeCamera1?.Stop();
            leftEyeCamera1?.Dispose();

            rightEyeCamera1?.Stop();
            rightEyeCamera1?.Dispose();

            leftEyeCamera2?.Stop();
            leftEyeCamera2?.Dispose();

            rightEyeCamera2?.Stop();
            rightEyeCamera2?.Dispose();
        }
        public override EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData processedImages)
        {
            ExtraData extraData = new ExtraData();
            var imSourceData = processedImages.Images[Eye.Left]?.ImageSourceData;
            if (imSourceData != null)
            {
                var (exposureEndLineStatusAll, _) = (ValueTuple<long, IManagedImage>)imSourceData;
                extraData.Int0 = Convert.ToInt32(exposureEndLineStatusAll);
                processedImages.Data.ExtraData = extraData;
            }
            imSourceData = processedImages.Images[Eye.Right]?.ImageSourceData;
            if (imSourceData != null)
            {
                var (exposureEndLineStatusAll, _) = (ValueTuple<long, IManagedImage>)imSourceData;
                extraData.Int1 = Convert.ToInt32(exposureEndLineStatusAll);
                processedImages.Data.ExtraData = extraData;
            }
            return processedImages;
        }

        protected override void SaveCameraExposure()
        {
            // Figure out current exposure from the cameras
            var leftGain = leftEyeCamera1?.Gain ?? 1;
            var rightGain = rightEyeCamera1?.Gain ?? 1;
            
            //check if they are the same and throw error if not
            if (leftGain != rightGain)
            {
                throw new Exception("The left and right camera gain aren't equal.");
            }
            
            var mySettings = Settings as EyeTrackingSystemSettingsSpinnaker_QuadCam;
            mySettings.Gain = leftGain; 
        }

        protected override void SaveCameraMove()
        {
            var leftOffset = leftEyeCamera1?.Offset?? new Point((int)leftEyeCamera1.MaxROI_Offset.X/2, (int)leftEyeCamera1.MaxROI_Offset.Y/2);
            var rightOffset = rightEyeCamera1?.Offset ?? new Point((int)rightEyeCamera1.MaxROI_Offset.X/2, (int)rightEyeCamera1.MaxROI_Offset.Y/2);

            var mySettings = Settings as EyeTrackingSystemSettingsSpinnaker_QuadCam;
            mySettings.LeftOffset = leftOffset;
            mySettings.RightOffset = rightOffset;
        }
    }


    public class EyeTrackingSystemSettingsSpinnaker_QuadCam : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnaker_QuadCam()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
            Eye = Eye.Both;
        }

        [NeedsRestarting]
        [Category("Camera properties"), Description("LeftEye - CameraSerialNumber")]
        public string LeftEyeCamera1SerialNumber { get => this.leftEyeCamera1SerialNumber; set => SetProperty(ref leftEyeCamera1SerialNumber, value, nameof(LeftEyeCamera1SerialNumber)); }
        private string leftEyeCamera1SerialNumber = null;

        [NeedsRestarting]
        [Category("Camera properties"), Description("RightEye - CameraSerialNumber")]
        public string RightEyeCamera1SerialNumber { get => this.rightEyeCamera1SerialNumber; set => SetProperty(ref rightEyeCamera1SerialNumber, value, nameof(RightEyeCamera1SerialNumber)); }
        private string rightEyeCamera1SerialNumber = null;


        [NeedsRestarting]
        [Category("Camera properties"), Description("LeftEye - CameraSerialNumber")]
        public string LeftEyeCamera2SerialNumber { get => this.leftEyeCamera2SerialNumber; set => SetProperty(ref leftEyeCamera2SerialNumber, value, nameof(LeftEyeCamera2SerialNumber)); }
        private string leftEyeCamera2SerialNumber = null;

        [NeedsRestarting]
        [Category("Camera properties"), Description("RightEye - CameraSerialNumber")]
        public string RightEyeCamera2SerialNumber { get => this.rightEyeCamera2SerialNumber; set => SetProperty(ref rightEyeCamera2SerialNumber, value, nameof(RightEyeCamera2SerialNumber)); }
        private string rightEyeCamera2SerialNumber = null;

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
