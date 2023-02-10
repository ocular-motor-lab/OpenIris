//-----------------------------------------------------------------------
// <copyright file="EyeTrackerImageEyeBox.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
        public void UpdateImageEyeBox(ImageEye imageEye, EyePhysicalModel eyeGlobe, int thresholdDark, int threshdoldBright, Rectangle croppingBox)
        {
            this.imageBoxEye.SuspendLayout();

            if (imageEye != null)
            {
                // Draw image of the eye with tracking information
                var image = imageEye.Image.Convert<Bgr, byte>();
                ImageEyeDrawing.DrawAllData(image, imageEye.EyeData, eyeGlobe, thresholdDark, threshdoldBright, croppingBox);

                this.imageBoxEye.Image = image;
            }

            imageBoxEye.ResumeLayout();
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        public void UpdateImageEyeBox(ImageEye imageEye)
        {
            this.imageBoxEye.SuspendLayout();

            if (imageEye != null)
            {
                // Draw image of the eye with tracking information
                Image<Bgr, byte> imageEyeColor = imageEye.Image.Convert<Bgr, byte>();

                this.imageBoxEye.Image = imageEyeColor;
            }

            imageBoxEye.ResumeLayout();
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
