//-----------------------------------------------------------------------
// <copyright file="EyeTrackerQuickSettings.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using OpenIris;
    using Emgu.CV.UI;
    using System.Windows.Forms;

    /// <summary>
    /// Control with some quick settings of the eye tracker for a single eye.
    /// </summary>
    public partial class EyeTrackingPipelinePupilCRQuickSettings : EyeTrackingPipelineUIControl
    {
        private EyeTrackingPipelinePupilCRSettings trackingSettings;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerQuickSettings class.
        /// </summary>
        public EyeTrackingPipelinePupilCRQuickSettings(Eye whichEye, string pipelineName)
            : base(whichEye, pipelineName)
        {
            InitializeComponent();

            sliderPupil.Text = "Pupil Threshold";
            sliderCR.Text = "CR Threshold";

            sliderPupil.Range = new Range(0, 255);
            sliderCR.Range = new Range(0, 255);

            trackingSettings = new EyeTrackingPipelinePupilCRSettings();
        }

        /// <summary>
        /// Updates the control.
        /// </summary>
        public void UpdateValues(EyeTrackingPipelinePupilCRSettings currentTrackingSettings)
        {
            trackingSettings = currentTrackingSettings as EyeTrackingPipelinePupilCRSettings;
            
            if (trackingSettings is null) return;


            if (WhichEye == Eye.Left)
            {
                sliderPupil.Value = trackingSettings.DarkThresholdLeftEye;
                sliderCR.Value = trackingSettings.BrightThresholdLeftEye;
            }
            else
            {
                sliderPupil.Value = trackingSettings.DarkThresholdRightEye;
                sliderCR.Value = trackingSettings.BrightThresholdRightEye;
            }
        }

        /// <summary>
        /// Update the pipeline UI.
        /// </summary>
        /// <param name="imageBox">image box for the eye image.</param>
        /// <param name="dataAndImages"></param>
        public override void UpdatePipelineEyeImage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages)
        {
            var image = dataAndImages.Images[WhichEye];
            var settings = dataAndImages.TrackingSettings as EyeTrackingPipelinePupilCRSettings ?? throw new Exception();
            var eyeCalibration = dataAndImages.Calibration.EyeCalibrationParameters[WhichEye];

            // Update Images
            imageBox.Image = ImageEyeDrawing.DrawAllData(image, eyeCalibration, settings);
        }
        public override void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages)
        {
            var settings = dataAndImages.TrackingSettings as EyeTrackingPipelinePupilCRSettings ?? throw new Exception();

            UpdateValues(settings);
        }

        private void sliderPupil_ValueChanged(object sender, EventArgs e)
        {
            if ( sender == sliderPupil)
            {
                if (WhichEye == Eye.Left)
                {
                    trackingSettings.DarkThresholdLeftEye = sliderPupil.Value;
                }
                else
                {
                    trackingSettings.DarkThresholdRightEye = sliderPupil.Value;
                }

            }

            if ( sender == sliderCR)
            {
                if (WhichEye == Eye.Left)
                {
                    trackingSettings.BrightThresholdLeftEye = sliderCR.Value;
                }
                else
                {
                    trackingSettings.BrightThresholdRightEye = sliderCR.Value;
                }
            }
        }
    }
}
