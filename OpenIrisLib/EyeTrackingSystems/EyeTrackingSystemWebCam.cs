//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemWebCam.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
    /// Generic system with any one camera.
    /// </summary>
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Single webcam", typeof(EyeTrackingSystemSettingsWebCam), VideoEyeConfiguration.SingleVideoOneEye)]
    public class EyeTrackingSystemWebCam : EyeTrackingSystem
    {
        /// <summary>
        /// Gets the cameras. In this case two, left and right eye. 
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye?> CreateCameras()
        {
            var cameraSettings = Settings as EyeTrackingSystemSettingsWebCam
                ?? throw new InvalidOperationException("Wrong type of settings;");

            var camera = new CameraEyeWebCam(Eye.Both, 0)
            {
                CameraOrientation = cameraSettings.CameraOrientation
            };
            return new EyeCollection<CameraEye?>(camera);
        }

        /// <summary>
        /// Prepares images for processing. Split, rotate, etc. 
        /// </summary>
        /// <remarks>An specific implementation of ImageEyeGrabber can optionally override this 
        /// method to prepare the images. For instance, if a system has only one camera capturing both eyes.
        /// This method would be where the image gets split into two.</remarks>
        /// <param name="images">Images captured from the cameras.</param>
        /// <returns>Images prepared for processing.</returns>
        public override EyeCollection<ImageEye?> PreProcessImagesFromVideos(EyeCollection<ImageEye?> images)
        {
            return PreProcessImagesFromCameras(images);
        }

        /// <summary>
        /// Prepares images for processing. Split, rotate, etc. 
        /// </summary>
        /// <remarks>An specific implementation of ImageEyeGrabber can optionally override this 
        /// method to prepare the images. For instance, if a system has only one camera capturing both eyes.
        /// This method would be where the image gets split into two.</remarks>
        /// <param name="images">Images captured from the cameras.</param>
        /// <returns>Images prepared for processing.</returns>
        public override EyeCollection<ImageEye?> PreProcessImagesFromCameras(EyeCollection<ImageEye?>  images)
        {
            var image = images[0] ?? throw new NullReferenceException("Image is null.");

            if (images.Count == 1 && image.WhichEye == Eye.Both)
            {
                var width = image.Size.Width;
                var height = image.Size.Height;

                var imageLeft = image.Copy(new Rectangle(width / 2, 0, width / 2, height));
                imageLeft.WhichEye = Eye.Left;

                var imageRight = image.Copy(new Rectangle(0, 0, width / 2, height));
                imageRight.WhichEye = Eye.Right;

                return new EyeCollection<ImageEye?>(imageLeft, imageRight);
            }
            return images;
        }
    }

    /// <summary>
    /// Settings for single camera
    /// </summary>
    public class EyeTrackingSystemSettingsWebCam : EyeTrackingSystemSettings
    {
        /// <summary>
        /// 
        /// </summary>
        [Category("Camera properties"), Description("Orientation of the camera.")]
        [NeedsRestarting]
        public CameraOrientation CameraOrientation
        {
            get
            {
                return cameraOrientation;
            }
            set
            {
                if (value != cameraOrientation)
                {
                    cameraOrientation = value;
                    OnPropertyChanged(this, nameof(CameraOrientation));
                }
            }
        }
        private CameraOrientation cameraOrientation = CameraOrientation.Upright;

    }
}
