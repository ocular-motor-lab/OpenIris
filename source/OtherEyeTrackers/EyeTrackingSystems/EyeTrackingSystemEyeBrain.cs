//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemEyeBrain.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// EyeBrain system.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("EyeBrain")]
    public class EyeTrackingSystemEyeBrain : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            //// TODO: It should check if the proper camera is really plugged to the computer
            var camera = new CameraEyeUEye()
            {
                WhichEye = Eye.Both
            };
            camera.Init();

            return new EyeCollection<CameraEye>(camera);
        }

        /// <summary>
        /// Prepares the images for processing. Splits the single image into left and right eye
        /// and rotates them appropriately. 
        /// </summary>
        /// <param name="images">Raw image from the camera.</param>
        /// <returns>Images prepared for processing.</returns>
        public override EyeCollection<ImageEye?> PreProcessImages(EyeCollection<ImageEye?>  images)
        {
            var imageLeft = images[Eye.Both]?.Copy(new Rectangle(400, 0, 400, 300));
            imageLeft.WhichEye = Eye.Left;
            var imageRight = images[Eye.Both]?.Copy(new Rectangle(0, 0, 400, 300));
            imageRight.WhichEye = Eye.Right;

            return new EyeCollection<ImageEye?>(imageLeft, imageRight);
        }
    }
}
