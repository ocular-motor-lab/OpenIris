//-----------------------------------------------------------------------
// <copyright file="EyeTrackerImageEyeBox.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris;

    /// <summary>
    /// Control that displays the image of the eye.
    /// </summary>
    public partial class EyeTrackerImageEyeBox : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the EyeTrackerImageEyeBox class.
        /// </summary>
        public EyeTrackerImageEyeBox()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        /// <param name="eyeGlobe"></param>
        /// <param name="thresholdDark">Threshold dark.</param>
        /// <param name="threshdoldBright">Threshold bright.</param>
        /// <param name="croppingBox">Cropping rectangle where to search for a pupil.</param>
        /// <param name="mmPerPix">Resolution of the image in mm per pix.</param>
        public void UpdateImageEyeBox(ImageEye imageEye, EyePhysicalModel eyeGlobe, int thresholdDark, int threshdoldBright, Rectangle croppingBox, double mmPerPix)
        {
            if (imageEye != null)
            {
                imageBoxEye.SuspendLayout();
                // Draw image of the eye with tracking information
                var image = imageEye.Image.Convert<Bgr, byte>();
                ImageEyeDrawing.DrawAllData(image, imageEye.EyeData, eyeGlobe, thresholdDark, threshdoldBright, croppingBox, mmPerPix);

                imageBoxEye.Image = image;
                imageBoxEye.ResumeLayout();
            }
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        public void UpdateImageEyeBox(ImageEye imageEye)
        {
            if (imageEye != null)
            {
                imageBoxEye.SuspendLayout();

                Image<Bgr, byte> imageEyeColor = imageEye.Image.Convert<Bgr, byte>();

                imageBoxEye.Image = imageEyeColor;

                imageBoxEye.ResumeLayout();
            }
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        public void UpdateImageEyeBox(ImageEye imageEye, EyeCalibration calibration, EyeTrackingPipelineSettings settings)
        {
            this.imageBoxEye.SuspendLayout();

            if (imageEye != null)
            {
                // Draw image of the eye with tracking information
                var imageEyeColor = ImageEyeDrawing.DrawAllData(imageEye, calibration, settings);

                this.imageBoxEye.Image = imageEyeColor;
            }

            imageBoxEye.ResumeLayout();
        }

        /// <summary>
        /// Resets the image to blank.
        /// </summary>
        public void ResetImage()
        {
            imageBoxEye.Image = null;
        }
    }
}
