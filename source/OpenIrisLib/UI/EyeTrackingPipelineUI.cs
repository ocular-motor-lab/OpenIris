using Emgu.CV.UI;
using System.Windows.Forms;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines, one per eye
    /// </summary>
    public abstract class EyeTrackingPipelineUI : UserControl
    {
        public EyeTrackingPipelineUI(Eye whichEye)
        {
            WhichEye = whichEye;
        }

        /// <summary>
        /// Gets or sets left or right eye.
        /// </summary>
        public Eye WhichEye { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBox"></param>
        /// <param name="dataAndImages"></param>
        public abstract void UpdatePipelineEyeImage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages);

        public abstract void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages);

        public static void UpdatePipelineEyeImage(Eye whichEye, ImageBox imageBox, EyeTrackerImagesAndData dataAndImages)
        {
            imageBox.Image = ImageEyeDrawing.DrawAllData(
                                    dataAndImages.Images[whichEye],
                                    dataAndImages.Calibration.EyeCalibrationParameters[whichEye],
                                    dataAndImages.TrackingSettings);
        }
    }
}
