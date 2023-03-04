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
    using OpenIris.HeadTracking;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Micromedical eye tracking system system.
    /// </summary>
    [Export(typeof(IEyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Micromedical", typeof(EyeTrackingSystemSettingsMicromedical))]
    public class EyeTrackingSystemMicromedical : EyeTrackingSystemBase
    {
        private HeadSensorTeensyMPU headSensor;

        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateCameras()
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
                        ShouldAdjustFrameRate = true,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = 0,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 100
                    };
                }

                if (settings.Eye == Eye.Both || settings.Eye == Eye.Left)
                {
                    cameraLeftEye = new CameraEyeFlyCapture(Eye.Left, serialNumberLeft, settings.FrameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.Rotated180Mirrored,
                        ShouldAdjustFrameRate = true,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = 0,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 100
                    };
                }

                cameraLeftEye?.Start();
                cameraRightEye?.Start();

                if (settings.UseHeadSensor)
                {
                    headSensor = new HeadSensorTeensyMPU(settings);
                    headSensor.StartHeadSensorAndSyncWithCamera(cameraRightEye);
                }

                // Syncrhonize the cameras if necessary
                if (settings.Eye == Eye.Both && settings.SyncCameras)
                {
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

        /// <summary>
        /// Tries to find the cameras from the Micromedical system.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected static (uint leftSerialNumber, uint rightSerialNumber) FindCameras(EyeTrackingSystemSettingsMicromedical settings)
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
        public override EyeCollection<VideoEye> CreateVideos(EyeCollection<string> filenames)
        {
            if (filenames is null) throw new ArgumentNullException(nameof(filenames));

            var videoLeftEye = (filenames[Eye.Left] != null && filenames[Eye.Left].Length > 1)
                ? new VideoEyeFlyCapture(Eye.Left, filenames[Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.BottomLeftHorizontal)
                : null;

            var videoRightEye = (filenames[Eye.Right] != null && filenames[Eye.Right].Length > 1)
                ? new VideoEyeFlyCapture(Eye.Right, filenames[Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopRightHorizontal)
                : null;

            return new EyeCollection<VideoEye>(videoLeftEye, videoRightEye);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IHeadDataSource CreateHeadDataSourceWithCameras() => headSensor;

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
    public class EyeTrackingSystemSettingsMicromedical : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsMicromedical()
        {
            MmPerPix = 0.15;
            DistanceCameraToEyeMm = 70;
            UseHeadSensor = true;
        }

        public bool UseHeadSensor
        {
            get
            {
                return this.useHeadSensor;
            }
            set
            {
                if (value != this.useHeadSensor)
                {
                    this.useHeadSensor = value;
                    this.OnPropertyChanged(this, "UseHeadSensor");
                }
            }
        }
        private bool useHeadSensor = false;


        public bool UseHeadSensorRotation
        {
            get
            {
                return this.useHeadSensorRotation;
            }
            set
            {
                if (value != this.useHeadSensorRotation)
                {
                    this.useHeadSensorRotation = value;
                    this.OnPropertyChanged(this, "UseHeadSensorRotation");
                }
            }
        }
        private bool useHeadSensorRotation = false;

        public double[][] HeadSensorRotation
        {
            get
            {
                return this.headSensorRotation;
            }
            set
            {
                if (value != this.headSensorRotation) // TODO: Does not detect changes to the inside values
                {
                    this.headSensorRotation = value;
                    this.OnPropertyChanged(this, "headSensorRotation");
                }
            }
        }
        
        private double[][] headSensorRotation = {
            new double[] { -0.8192, -0.0000,    -0.5736     },
            new double[] {  0.5540,  0.2588,    -0.7912     },
            new double[] {  0.1485, -0.9659,    - 0.2120    } };

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

        /// <summary>
        /// Gets or sets the frame rate of the cameras.
        /// </summary>       
        [Description("Frame rate")]
        [NeedsRestarting]
        public new float FrameRate
        {
            get
            {
                return this.frameRate;
            }
            set
            {
                var newFrameRate = value;
                if (newFrameRate > 100)
                {
                    newFrameRate = 100;
                }
                if (newFrameRate < 10)
                {
                    newFrameRate = 10;
                }

                if (newFrameRate != this.frameRate)
                {
                    this.frameRate = newFrameRate;
                    this.OnPropertyChanged(this, "FrameRate");
                }
            }
        }
        private float frameRate = 100.0f;

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

        public bool LEDLeftON
        {
            get
            {
                return this.lEDLeftON;
            }
            set
            {
                if (value != this.lEDLeftON)
                {
                    this.lEDLeftON = value;
                    this.OnPropertyChanged(this, "LEDLeftON");
                }
            }
        }
        private bool lEDLeftON = false;

        public bool LEDRightON
        {
            get
            {
                return this.lEDRightON;
            }
            set
            {
                if (value != this.lEDRightON)
                {
                    this.lEDRightON = value;
                    this.OnPropertyChanged(this, "LEDRightON");
                }
            }
        }
        private bool lEDRightON = false;

        public bool SyncCameras
        {
            get
            {
                return this.syncCameras;
            }
            set
            {
                if (value != this.syncCameras)
                {
                    this.syncCameras = value;
                    this.OnPropertyChanged(this, "SyncCameras");
                }
            }
        }
        private bool syncCameras = true;

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
        private uint serialNumberCameraLeftEye = 13261089;

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
        private uint serialNumberCameraRightEye = 13261090;


    }
}
