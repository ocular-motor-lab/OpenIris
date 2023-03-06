using Emgu.CV.UI;
using System.Windows.Forms;

namespace OpenIris
{
    /// <summary>
    /// Interface for UI for pipelines, one per eye
    /// </summary>
    public class EyeTrackingPipelineUIControl : UserControl
    {
        public EyeTrackingPipelineUIControl()
        {
            WhichEye = Eye.Left;
            PipelineName = string.Empty;
        }

        public EyeTrackingPipelineUIControl(Eye whichEye, string pipelineName)
        {
            WhichEye = whichEye;
            PipelineName = pipelineName;
        }

        /// <summary>
        /// Gets or sets left or right eye.
        /// </summary>
        public Eye WhichEye { get; }

        /// <summary>
        /// Name of the pipeline.
        /// </summary>
        public string PipelineName { get; }

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
