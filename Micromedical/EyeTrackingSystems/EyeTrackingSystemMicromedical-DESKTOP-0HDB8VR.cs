//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2017 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace VORLab.VOG
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Linq;
    using VORLab.VOG.HeadTracking;
    using VORLab.VOG.ImageGrabbing;

    /// <summary>
    /// Micromedical eye tracking system system.
    /// </summary>
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Micromedical", typeof(EyeTrackingSystemSettingsMicromedical))]
    public class EyeTrackingSystemMicromedical : EyeTrackingSystem
    {
        private CameraEyeFlyCapture cameraLeftEye = null;
        private CameraEyeFlyCapture cameraRightEye = null;

        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsMicromedical;

            var USE_LEFT_CAMERA = settings.Eye == Eye.Both || settings.Eye == Eye.Left;
            var USE_RIGHT_CAMERA = settings.Eye == Eye.Both || settings.Eye == Eye.Right;

            // TODO: improve how to select the cameras. Maybe add the serial numbers to the settings

            // Default region of interest for Micromedical cameras
            var roi = new Rectangle(176, 112, 400, 260);

            // Initialize cameras
            try
            {
                var (serialNumberLeft, serialNumberRight) = FindCameras();

                if (USE_RIGHT_CAMERA)
                {
                    if (settings.UseHeadSensor)
                    {
                        cameraRightEye = new CameraEyePointGreyWithTeensyHeadSensor(Eye.Right, serialNumberRight, settings.FrameRate, roi)
                        {
                            CameraOrientation = CameraOrientation.Upright_Mirrored,
                            ShouldAdjustFrameRate = true,
                            ShutterDuration = settings.ShutterDuration,
                            Gain = settings.Gain,
                            AutoExposure = settings.AutoExposure,
                            PixelMode = 0,
                            PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                            BufferSize = 100
                        };
                    }
                    else
                    {
                        cameraRightEye = new CameraEyeFlyCapture(Eye.Right, serialNumberRight, settings.FrameRate, roi)
                        {
                            CameraOrientation = CameraOrientation.Upright_Mirrored,
                            ShouldAdjustFrameRate = true,
                            ShutterDuration = settings.ShutterDuration,
                            Gain = settings.Gain,
                            AutoExposure = settings.AutoExposure,
                            PixelMode = 0,
                            PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                            BufferSize = 100
                        };
                    }

                    cameraRightEye.Init();
                }

                if (USE_LEFT_CAMERA)
                {
                    cameraLeftEye = new CameraEyeFlyCapture(Eye.Left, serialNumberLeft, settings.FrameRate, roi)
                    {
                        CameraOrientation = CameraOrientation.Rotated180_Mirrored,
                        ShouldAdjustFrameRate = true,
                        ShutterDuration = settings.ShutterDuration,
                        Gain = settings.Gain,
                        AutoExposure = settings.AutoExposure,
                        PixelMode = 0,
                        PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Mono8,
                        BufferSize = 100
                    };

                    cameraLeftEye.Init();
                }

                if (USE_LEFT_CAMERA) cameraLeftEye.Start();
                if (USE_RIGHT_CAMERA) cameraRightEye.Start();
            }
            catch (Exception ex)
            {
                cameraLeftEye?.Stop();
                cameraRightEye?.Stop();

                throw new InvalidOperationException("Error starting cameras. " + ex.Message, ex);
            }

            // Syncrhonize the cameras if necessary
            try
            {
                if (settings.Eye == Eye.Both && settings.SyncCameras)
                {
                    CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);
                }
            }
            catch (Exception ex)
            {
                cameraLeftEye?.Stop();
                cameraRightEye?.Stop();

                throw new InvalidOperationException("Error syncrhonizing cameras.", ex);
            }

            // Return the new cameras
            return new EyeCollection<CameraEye>(cameraLeftEye, cameraRightEye);

        }

        /// <summary>
        /// Tries to find the cameras from the Micromedical system.
        /// </summary>
        /// <returns></returns>
        protected (uint leftSerialNumber, uint rightSerialNumber) FindCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsMicromedical;

            var cameraList = CameraEyeFlyCapture.GetListOfFlyCaptureCameras("Firefly");
            if (cameraList.Length < 2) throw new InvalidOperationException("Not enough cameras connected.");
            var leftSerialNumber = settings.SerialNumberCameraLeftEye;
            var rightSerialNumber = settings.SerialNumberCameraRightEye;
            if (!cameraList.Contains(leftSerialNumber) || !cameraList.Contains(rightSerialNumber))
            {
                leftSerialNumber = cameraList[0];
                rightSerialNumber = cameraList[1];
            }

            return (leftSerialNumber, rightSerialNumber);
        }

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        public override EyeCollection<VideoEye> CreateVideos(EyeCollection<string> filenames)
        {
            VideoEyeFlyCapture videoLeftEye = null;
            VideoEyeFlyCapture videoRightEye = null;

            if (filenames[Eye.Left] != null && filenames[Eye.Left].Length > 1)
            {
                videoLeftEye = new VideoEyeFlyCapture(Eye.Left, filenames[Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.BottomLeftHorizontal);
            }

            if (filenames[Eye.Right] != null && filenames[Eye.Right].Length > 1)
            {
                videoRightEye = new VideoEyeFlyCapture(Eye.Right, filenames[Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopRightHorizontal);
            }

            return new EyeCollection<VideoEye>(videoLeftEye, videoRightEye);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IHeadTracker GetHeadSensor()
        {
            return (cameraRightEye as CameraEyePointGreyWithTeensyHeadSensor)?.HeadSensor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public override EyeTrackerDataAndImages PostProcessDataAndImages(EyeTrackerDataAndImages procesedImages)
        {
            return base.PostProcessDataAndImages(procesedImages);
        }

        /// <summary>
        /// Gets the settings specific to the eye tracking system.
        /// </summary>
        /// <returns>The settings.</returns>
        public override EyeTrackingSystemSettings GetDefaultSettings()
        {
            return new EyeTrackingSystemSettingsMicromedical
            {
                MmPerPix = 0.15,
                DistanceCameraToEyeMm = 70,
                UseHeadSensor = true
            };
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public class EyeTrackingSystemSettingsMicromedical : EyeTrackingSystemSettings
        {
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
                        this.OnPropertyChanged(this,"UseHeadSensor");
                    }
                }
            }
            private bool useHeadSensor = false;

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
                        this.OnPropertyChanged(this,"AutoExposure");
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
                        this.OnPropertyChanged(this,"ShutterDuration");
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
                        this.OnPropertyChanged(this,"FrameRate");
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
                        this.OnPropertyChanged(this,"Gain");
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
                        this.OnPropertyChanged(this,"LEDLeftON");
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
                        this.OnPropertyChanged(this,"LEDRightON");
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
                        this.OnPropertyChanged(this,"SyncCameras");
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
}
