//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using OpenIris.ImageGrabbing;


    public enum InteracousticsImageMode
    {
        Default,
        BestResoltuionGoodFrameRate100Hz,
        BestFrameRateGoodRangeLowResolution150Hz,
        BestResolutionGoodRange75Hz
    }

    /// <summary>
    /// Micromedical eye tracking system system.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Interacoustics", typeof(EyeTrackingSystemSettingsInteracoustics))]
    public class EyeTrackingSystemInteracoustics : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsInteracoustics;

            var roi = new Rectangle(176, 112, 400, 260);
            var pixelMode = 0;
            var frameRate = 100;
            switch (settings.InteracousticsImageMode)
            {
                case InteracousticsImageMode.Default:
                    break;
                case InteracousticsImageMode.BestResoltuionGoodFrameRate100Hz:
                    roi = new Rectangle(176, 104, 400, 268);
                    pixelMode = 0;
                    frameRate = 100;
                    break;
                case InteracousticsImageMode.BestFrameRateGoodRangeLowResolution150Hz:
                    roi = new Rectangle(0, 30*2, 376 * 2, 180 * 2);
                    pixelMode = 1;
                    frameRate = 150;
                    break;
                case InteracousticsImageMode.BestResolutionGoodRange75Hz:
                    roi = new Rectangle(176, 40, 400, 360);
                    pixelMode = 0;
                    frameRate = 75;
                    break;
                default:
                    break;
            }

            var (serialNumberLeft, serialNumberRight) = FindCameras(settings);

            CameraEyeFlyCapture cameraRightEye = null;
            CameraEyeFlyCapture cameraLeftEye = null;

            try
            {
                if (settings.Eye == Eye.Both || settings.Eye == Eye.Right)
                {
                    cameraRightEye = new CameraEyeFlyCapture(Eye.Right, serialNumberRight, frameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.Rotated180Mirrored,
                        ShouldAdjustFrameRate = true,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = pixelMode,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 100
                    };
                }

                if (settings.Eye == Eye.Both || settings.Eye == Eye.Left)
                {
                    cameraLeftEye = new CameraEyeFlyCapture(Eye.Left, serialNumberLeft, frameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.Rotated180Mirrored,
                        ShouldAdjustFrameRate = true,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = pixelMode,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 100
                    };

                }

                cameraLeftEye?.Start();
                cameraRightEye?.Start();

                cameraLeftEye?.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
                cameraLeftEye?.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.output);
                cameraLeftEye?.SetGPIO(2, CameraEyeFlyCapture.GPIOMode.output);
                cameraLeftEye?.SetGPIO(3, CameraEyeFlyCapture.GPIOMode.input);
                cameraRightEye?.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.input);
                cameraRightEye?.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.output);
                cameraRightEye?.SetGPIO(2, CameraEyeFlyCapture.GPIOMode.output);
                cameraRightEye?.SetGPIO(3, CameraEyeFlyCapture.GPIOMode.input);

                if (cameraLeftEye != null && cameraRightEye != null)
                {
                    // Syncrhonize the cameras if necessary
                    CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);
                }

                // Return the new cameras
                var cameras = new EyeCollection<CameraEye>(cameraLeftEye, cameraRightEye);
                cameraLeftEye = null;
                cameraRightEye = null;
                return cameras;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting cameras. " + ex.Message);

                cameraLeftEye?.Stop();
                cameraRightEye?.Stop();

                throw new InvalidOperationException("Error starting cameras. " + ex.Message, ex);
            }
            finally
            {
                cameraLeftEye?.Dispose();
                cameraRightEye?.Dispose();
            }
        }

        /// <summary>
        /// Tries to find the cameras from the Micromedical system.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected static (uint leftSerialNumber, uint rightSerialNumber) FindCameras(EyeTrackingSystemSettingsInteracoustics settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cameraList = CameraEyeFlyCapture.GetListOfFlyCaptureCameras("Firefly");
            if (cameraList.Length < 2) throw new OpenIrisException("Not enough cameras connected.");

            var leftSerialNumber = settings.SerialNumberCameraLeftEye;
            var rightSerialNumber = settings.SerialNumberCameraRightEye;

            if (cameraList.Contains(leftSerialNumber) && cameraList.Contains(rightSerialNumber))
            {
                return (leftSerialNumber, rightSerialNumber);
            }

            return (cameraList[0], cameraList[1]);
        }

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns>List of image eye source objects.</returns>
        protected override EyeCollection<VideoEye> CreateVideos(EyeCollection<string> filenames)
        {
            if (filenames is null) throw new ArgumentNullException(nameof(filenames));

            var videoLeftEye = (filenames[Eye.Left]?.Length > 1)
                ? new VideoEyeFlyCapture(Eye.Left, filenames[Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopLeftHorizontal)
                : null;

            var videoRightEye = (filenames[Eye.Right]?.Length > 1)
                ? new VideoEyeFlyCapture(Eye.Right, filenames[Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopLeftHorizontal)
                : null;

            return new EyeCollection<VideoEye>(videoLeftEye, videoRightEye);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public override EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData procesedImages)
        {
            return base.PostProcessImagesAndData(procesedImages);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    }

    public class EyeTrackingSystemSettingsInteracoustics : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsInteracoustics()
        {
            PixPerMm = 6;
            DistanceCameraToEyeMm = 70;
            AutoExposure = false;
        }

        /// <summary>
        /// Gets or sets the frame rate of the cameras.
        /// </summary>       
        [Description("Image  mode")]
        [NeedsRestarting]
        public InteracousticsImageMode InteracousticsImageMode
        {
            get
            {
                return interacousticsImageMode;
            }
            set
            {
                if (value != this.interacousticsImageMode)
                {
                    this.interacousticsImageMode = value;
                    this.OnPropertyChanged(this, "InteracousticsImageMode");
                }
            }
        }

        private InteracousticsImageMode interacousticsImageMode;


        /// <summary>
        /// Gets or sets the frame rate of the cameras.
        /// </summary>       
        [Description("Frame rate")]
        [NeedsRestarting]
        public new float FrameRate
        {
            get
            {
                return 100.0f;
            }
            set
            {

            }
        }

        public bool AutoExposure
        {
            get
            {
                return this.autoExposure;
            }
            set
            {
                if (value != this.autoExposure)
                {
                    this.autoExposure = value;
                    this.OnPropertyChanged(this, "AutoExposure");
                }
            }
        }
        private bool autoExposure = true;

        public float ShutterDuration
        {
            get
            {
                return this.shutterDuration;
            }
            set
            {
                if (value != this.shutterDuration)
                {
                    this.shutterDuration = value;
                    this.OnPropertyChanged(this, "ShutterDuration");
                }
            }
        }
        private float shutterDuration = 9;

        public float Gain
        {
            get
            {
                return this.gain;
            }
            set
            {
                if (value != this.gain)
                {
                    this.gain = value;
                    this.OnPropertyChanged(this, "Gain");
                }
            }
        }
        private float gain = 3;
        
        public uint SerialNumberCameraLeftEye
        {
            get
            {
                return this.serialNumberCameraLeftEye;
            }
            set
            {
                if (value != this.serialNumberCameraLeftEye)
                {
                    this.serialNumberCameraLeftEye = value;
                    this.OnPropertyChanged(this, "SerialNumberCameraLeftEye");
                }
            }
        }
        private uint serialNumberCameraLeftEye = 1348166;

        public uint SerialNumberCameraRightEye
        {
            get
            {
                return this.serialNumberCameraRightEye;
            }
            set
            {
                if (value != this.serialNumberCameraRightEye)
                {
                    this.serialNumberCameraRightEye = value;
                    this.OnPropertyChanged(this, "SerialNumberCameraRightEye");
                }
            }
        }
        private uint serialNumberCameraRightEye = 1348239;
    }
}
