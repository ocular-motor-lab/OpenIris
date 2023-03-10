//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberMicromedical.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// </summary>
    [Export(typeof(IEyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("RemoteGrasshoppercs", typeof(EyeTrackerSystemSettingsRemoteGrasshopper))]
    public class EyeTrackingSystemRemoteGrasshoppercs : EyeTrackingSystemBase
    {
        protected CameraEyeFlyCapture? camera = null;

        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye?> CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackerSystemSettingsRemoteGrasshopper ??
                throw new InvalidOperationException("Wrong settings.");

            var cameraList = CameraEyeFlyCapture.GetListOfFlyCaptureCameras("Grasshopper");
            if (cameraList.Length < 1) throw new InvalidOperationException("There should be one Grasshopper camera connected.");

            var cameraIndex = cameraList[0];

            var height = 0;
            if (settings.FrameRate > 300)
            {
                height = 350;
            }
            else
            {
                height = 500;
            }

            // Default region of interest for Micromedical cameras
            var roi = new Rectangle(0, 426, 1920, height);


            // Initialize left camera if necessary
            try
            {
                camera = new CameraEyeFlyCapture(Eye.Both, cameraIndex , settings.FrameRate, roi)
                {
                    CameraOrientation = CameraOrientation.Upright,
                    ShouldAdjustFrameRate = false,
                    ShutterDuration = settings.ShutterDuration,
                    Gain = settings.Gain,
                    AutoExposure = settings.AutoExposure,
                    PixelMode = 0,
                    PixelFormat = CameraEyeFlyCapture.CameraPixelFormat.Raw8,
                    BufferSize = 100
                };

                camera.Start();
            }
            catch (Exception ex)
            {
                camera?.Stop();

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs.", ex);
            }

            return new EyeCollection<CameraEye?>(camera);
        }

        /// <summary>
        /// Prepares the images for processing. 
        /// </summary>
        /// <param name="images">Raw image from the camera.</param>
        /// <returns>Images prepared for processing.</returns>
        public override EyeCollection<ImageEye?> PreProcessImagesFromCameras(EyeCollection<ImageEye?> images)
        {
            var settings = Settings as EyeTrackerSystemSettingsRemoteGrasshopper ??
                 throw new InvalidOperationException("Wrong settings.");

            var imageBothEyes = images[Eye.Both] ?? throw new InvalidOperationException("No image for both eyes");

            var roiLeft = new Rectangle(1160, 0, 760, imageBothEyes.Size.Height);
            var roiRight = new Rectangle(0, 0, 760, imageBothEyes.Size.Height);

            switch (settings.Eye)
            {
                case Eye.Left:
                    var imageLeft = imageBothEyes.Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    return new EyeCollection<ImageEye?>(imageLeft, null);
                case Eye.Right:
                    var imageRight = imageBothEyes.Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye?>(null, imageRight);
                case Eye.Both:
                    imageLeft = imageBothEyes.Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    imageRight = imageBothEyes.Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye?>(imageLeft, imageRight);
                default:
                    return images;
            }
        }

        public override EyeCollection<ImageEye?> PreProcessImagesFromVideos(EyeCollection<ImageEye?> images)
        {
            if (images[Eye.Left] is null || images[Eye.Right] is null) throw new InvalidOperationException("Images cannot be null.");

            // Because only the right image has the timestamp in the bytes we copy the raw frame number to the left image
            images[Eye.Left]!.TimeStamp.FrameNumberRaw = images[Eye.Right]!.TimeStamp.FrameNumberRaw;
            return base.PreProcessImagesFromVideos(images);
        }

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        public override EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> filenames)
        {
            VideoEyeFlyCapture? videoLeftEye = null;
            VideoEyeFlyCapture? videoRightEye = null;


            if (filenames[Eye.Left] != null && filenames[Eye.Left]?.Length > 1)
            {
                videoLeftEye = new VideoEyeFlyCapture(Eye.Left, filenames[Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.BottomLeftHorizontal, 24);
            }

            if (filenames[Eye.Right] != null && filenames[Eye.Right]?.Length > 1)
            {
                videoRightEye = new VideoEyeFlyCapture(Eye.Right, filenames[Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopLeftHorizontal, 24);
            }

            return new EyeCollection<VideoEye?>(videoLeftEye, videoRightEye);
        }
        

        public class EyeTrackerSystemSettingsRemoteGrasshopper : EyeTrackingSystemSettings
        {
            public EyeTrackerSystemSettingsRemoteGrasshopper()
            {
                MmPerPix = 0.12;
                DistanceCameraToEyeMm = 200;
                FrameRate = 100;
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
                    if (newFrameRate > 500)
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
        }
    }
}
