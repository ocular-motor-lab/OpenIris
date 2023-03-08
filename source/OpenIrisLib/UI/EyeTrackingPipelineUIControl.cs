using Emgu.CV.UI;
using System.Windows.Forms;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines, one per eye
    /// </summary>
    public class EyeTrackingPipelineUIControl : UserControl
    {
        /// <summary>
        /// Gets or sets left or right eye.
        /// </summary>
        public Eye WhichEye { get; set;  }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBox"></param>
        /// <param name="dataAndImages"></param>
        public virtual void UpdatePipelineEyeImage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages)
        {
            imageBox.Image = ImageEyeDrawing.DrawAllData(
                                    dataAndImages.Images[WhichEye],
                                    dataAndImages.Calibration.EyeCalibrationParameters[WhichEye],
                                    dataAndImages.TrackingSettings);
        }

        public virtual void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages) { }
    }
}
