using Emgu.CV.UI;
using System.Windows.Forms;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines, one per eye
    /// </summary>
    public abstract class EyeTrackingPipelineUIControl : UserControl
    {
        public EyeTrackingPipelineUIControl(Eye whichEye)
        {
            WhichEye = whichEye;
        }

        /// <summary>
        /// Gets or sets left or right eye.
        /// </summary>
        public Eye WhichEye { get; }

        /// <summary>
        /// Name of the pipeline.
        /// </summary>
        public string PipelineName { get; set; }

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

        public abstract void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages);
    }
}
