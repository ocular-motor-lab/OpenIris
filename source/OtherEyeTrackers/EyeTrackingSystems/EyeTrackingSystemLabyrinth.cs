//-----------------------------------------------------------------------
// <copyright file="ImageEyeGrabberLabyrinth.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Labyrinth system.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("LabyrinthJOM", typeof(EyeTrackingSystemSettings))]
    public class EyeTrackingSystemLabyrinth : EyeTrackingSystemBase
    {
        private CameraEyeFlyCapture camera;

        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye[] CreateAndStartCameras()
        {

            this.camera = new CameraEyeFlyCapture(Eye.Both, Settings.FrameRate, new Rectangle(16 - 16, 250, 1264, 350));
            this.camera.ShouldAdjustFrameRate = false;
            this.camera.Start();
            
            return new EyeCollection<CameraEye>(this.camera);
        }

        /// <summary>
        /// Prepares the images for processing. Splits the single image into left and right eye
        /// and rotates them appropriately. 
        /// </summary>
        /// <param name="images">Raw image from the camera.</param>
        /// <returns>Images prepared for processing.</returns>
        public override EyeCollection<ImageEye> PreProcessImages(ImageEye[] images)
        {
            ImageEye imageLeft = null;
            ImageEye imageRight = null;
            try
            {
                // Total ROI is 1264 by 512
                var roiLeft = new Rectangle(682, 0, 432, 350);
                var roiRight = new Rectangle(150, 0, 432, 350);

                imageLeft = images[(int)Eye.Both].GetCroppedAndTransposedImage(roiLeft);
                imageLeft.WhichEye = Eye.Left;
                
                imageRight = images[(int)Eye.Both].GetCroppedAndTransposedImage(roiRight);
                imageRight.WhichEye = Eye.Right;
                imageRight.FlipVertical();
                imageRight.FlipHorizontal();

                // Copy the embedded fields into both images
                var bitsSeconds = BitConverter.GetBytes(images[(int)Eye.Both].TimeStamp.Seconds);
                var bitsFrameNumber = BitConverter.GetBytes(images[(int)Eye.Both].TimeStamp.FrameNumber);

                for (int i = 0; i < bitsSeconds.Length; i++)
                {
                    imageLeft.UpdateData(i, 0, 0, bitsSeconds[i]);
                    imageRight.UpdateData(i, 0, 0, bitsSeconds[i]);
                }

                for (int i = 0; i < bitsFrameNumber.Length; i++)
                {
                    imageLeft.UpdateData(bitsSeconds.Length + i, 0, 0, bitsFrameNumber[i]);
                    imageRight.UpdateData(bitsSeconds.Length + i, 0, 0, bitsFrameNumber[i]);
                }

                var newImages = new EyeCollection<ImageEye>(imageLeft, imageRight);

                imageLeft = null;
                imageRight = null;

                return newImages;
            }
            finally
            {
                if (imageLeft != null)
                {
                    imageLeft.Dispose();
                }

                if (imageRight != null)
                {
                    imageRight.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <param name="fileNames">Filenames of the videos.</param>
        /// <returns>List of image eye source objects.</returns>
        protected override VideoEye[] CreateVideos(string[] fileNames)
        {
            return new EyeCollection<VideoEye>(
                new VideoEyeFlyCapture(Eye.Left, fileNames[(int)Eye.Left], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopLeftVertical),
                new VideoEyeFlyCapture(Eye.Right, fileNames[(int)Eye.Right], VideoEyeFlyCapture.PositionOfEmbeddedInfo.TopLeftVertical));
        }
        
        //public override IHeadTracker GetHeadSensor()
        //{
        //    return null;
        //    //return new HeadSensorLabyrinthJOM(this.camera);
        //    //return new HeadSensorMPUFTDI(this.Camera);
        //}
    }
}
