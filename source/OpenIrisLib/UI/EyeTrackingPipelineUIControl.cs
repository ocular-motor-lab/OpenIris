using Emgu.CV.UI;
using OpenIris.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        //public virtual List<SliderTextControl> BuildPipelineUI(Eye whichEye, EyeTrackingPipelineSettings settings)
        //{
        //    var theSetings = settings as EyeTrackingPipelinePupilCRSettings;
        //    if (settings == null) return null;


        //    var sliderPupil = new SliderTextControl();
        //    sliderPupil.Text = "Pupil";
        //    sliderPupil.Range = new OpenIris.Range(0, 255);
        //    sliderPupil.ValueChanged += (o, e) =>
        //    {
        //        if (whichEye == Eye.Left) theSetings.DarkThresholdLeftEye = sliderPupil.Value;
        //        if (whichEye == Eye.Right) theSetings.DarkThresholdRightEye = sliderPupil.Value;
        //    };
        //    sliderPupil.Dock = DockStyle.Fill;

        //    var sliderCR = new SliderTextControl();
        //    sliderCR.Text = "CR";
        //    sliderCR.Range = new OpenIris.Range(0, 255);
        //    // sliderCR.ValueChanged += (o, e) => UpdateSettings();
        //    sliderCR.Dock = DockStyle.Fill;

            
        //    settings.PropertyChanged += (o, e) =>
        //    {
        //        if (e.PropertyName == nameof(theSetings.DarkThresholdLeftEye))
        //        {
        //            sliderPupil.Value = theSetings.DarkThresholdLeftEye;
        //        }
        //        if (e.PropertyName == nameof(theSetings.DarkThresholdRightEye))
        //        {
        //            sliderPupil.Value = theSetings.DarkThresholdLeftEye;
        //        }
        //        if (e.PropertyName == nameof(theSetings.DarkThresholdLeftEye))
        //        {
        //            sliderPupil.Value = theSetings.DarkThresholdLeftEye;
        //        }
        //        if (e.PropertyName == nameof(theSetings.DarkThresholdRightEye))
        //        {
        //            sliderPupil.Value = theSetings.DarkThresholdRightEye;
        //        }
        //    };

        //    return new List<SliderTextControl> { sliderPupil, sliderCR };
        //}
    }
}
