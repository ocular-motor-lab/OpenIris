//-----------------------------------------------------------------------
// <copyright file="ImageEyeBox.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris;
    using OpenIris.ImageProcessing;

    /// <summary>
    /// Control that displays the image of the eye.
    /// </summary>
    public partial class ImageEyeBox : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the EyeTrackerImageEyeBox class.
        /// </summary>
        public ImageEyeBox()
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
            if (imageEye is not null)
            {
                imageBoxEye.SuspendLayout();
                // Draw image of the eye with tracking information
                var image = imageEye.Image.Convert<Bgr, byte>();
                var eyeData = imageEye.EyeData ?? new EyeData();
                EyeImageOverlay.DrawAllData(image, eyeData, eyeGlobe, thresholdDark, threshdoldBright, croppingBox, mmPerPix);

                imageBoxEye.Image = image;
                imageBoxEye.ResumeLayout();
            }
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        public void UpdateImageEyeBox(ImageEye? imageEye)
        {
            if (imageEye is null) return;
         
            imageBoxEye.SuspendLayout();

            Image<Bgr, byte> imageEyeColor = imageEye.Image.Convert<Bgr, byte>();
            imageBoxEye.Image = imageEyeColor;

            imageBoxEye.ResumeLayout();
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        /// <param name="calibration"></param>
        /// <param name="settings"></param>
        public void UpdateImageEyeBox(ImageEye imageEye, EyeCalibration calibration, EyeTrackingPipelineSettings settings)
        {
            this.imageBoxEye.SuspendLayout();

            if (imageEye != null)
            {
                // Draw image of the eye with tracking information
                var imageEyeColor = ImageEyeBox.DrawAllData(imageEye, calibration, settings);

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


        // The drawing is in EyeImageOverlay, in the core library. These stay for plugins that call them here.

        /// <summary>Same as EyeImageOverlay.DrawAllData.</summary>
        public static Image<Bgr, byte>? DrawAllData(ImageEye? imageEye, EyeCalibration calibrationParameters, EyeTrackingPipelineSettings settings) => EyeImageOverlay.DrawAllData(imageEye, calibrationParameters, settings);

        /// <summary>Same as EyeImageOverlay.DrawAllData.</summary>
        public static void DrawAllData(Image<Bgr, byte>? image, EyeData data, EyePhysicalModel eyeGlobe, double thresholdDark, double threshdoldBright, Rectangle croppingRectangle, double mmPerPix) => EyeImageOverlay.DrawAllData(image, data, eyeGlobe, thresholdDark, threshdoldBright, croppingRectangle, mmPerPix);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawCross"/>.</summary>
        public static void DrawCross(Image<Bgr, byte> image, EyeData data) => EyeImageOverlay.DrawCross(image, data);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawThresdholds"/>.</summary>
        public static void DrawThresdholds(Image<Bgr, byte> image, double thresholdDark, double threshdoldBright) => EyeImageOverlay.DrawThresdholds(image, thresholdDark, threshdoldBright);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawEyelids"/>.</summary>
        public static void DrawEyelids(Image<Bgr, byte> image, EyeData data, EyePhysicalModel eyeModel) => EyeImageOverlay.DrawEyelids(image, data, eyeModel);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawPupil"/>.</summary>
        public static void DrawPupil(Image<Bgr, byte> image, EyeData data) => EyeImageOverlay.DrawPupil(image, data);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawCR"/>.</summary>
        public static void DrawCR(Image<Bgr, byte> image, EyeData data) => EyeImageOverlay.DrawCR(image, data);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawIris"/>.</summary>
        public static void DrawIris(Image<Bgr, byte> image, EyeData data, EyePhysicalModel eyeModel) => EyeImageOverlay.DrawIris(image, data, eyeModel);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawCroppingBox"/>.</summary>
        public static void DrawCroppingBox(Image<Bgr, byte> image, Rectangle croppingRectangle) => EyeImageOverlay.DrawCroppingBox(image, croppingRectangle);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawEyeGlobe"/>.</summary>
        public static void DrawEyeGlobe(Image<Bgr, byte> image, EyePhysicalModel globe, bool detailed) => EyeImageOverlay.DrawEyeGlobe(image, globe, detailed);

        /// <summary>Same as <see cref="EyeImageOverlay.DrawMMScale"/>.</summary>
        public static void DrawMMScale(Image<Bgr, byte> image, double mmPerPix) => EyeImageOverlay.DrawMMScale(image, mmPerPix);
    }
}
