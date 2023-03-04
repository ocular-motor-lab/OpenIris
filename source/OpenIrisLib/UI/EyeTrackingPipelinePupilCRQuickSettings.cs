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

    /// <summary>
    /// Control with some quick settings of the eye tracker for a single eye.
    /// </summary>
    public partial class EyeTrackingPipelinePupilCRQuickSettings : EyeTrackingPipelineUIControl
    {
        private EyeTrackingPipelinePupilCRSettings trackingSettings;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerQuickSettings class.
        /// </summary>
        public EyeTrackingPipelinePupilCRQuickSettings(Eye whichEye)
            : base(whichEye)
        {
            InitializeComponent();

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
                trackBarPupilThreshold.Value = trackingSettings.DarkThresholdLeftEye;
                textBoxPupilThreshold.Text = trackingSettings.DarkThresholdLeftEye.ToString();

                trackBarReflectionThreshold.Value = trackingSettings.BrightThresholdLeftEye;
                textBoxReflectionThreshold.Text = trackingSettings.BrightThresholdLeftEye.ToString();
            }
            else
            {
                trackBarPupilThreshold.Value = trackingSettings.DarkThresholdRightEye;
                textBoxPupilThreshold.Text = trackingSettings.DarkThresholdRightEye.ToString();

                trackBarReflectionThreshold.Value = trackingSettings.BrightThresholdRightEye;
                textBoxReflectionThreshold.Text = trackingSettings.BrightThresholdRightEye.ToString();
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void TrackBarPupilThreshold_Scroll(object sender, EventArgs e)
        {
            if (WhichEye == Eye.Left)
            {
                trackingSettings.DarkThresholdLeftEye = trackBarPupilThreshold.Value;
            }
            else
            {
                trackingSettings.DarkThresholdRightEye = trackBarPupilThreshold.Value;
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void TextBoxPupilThreshold_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxPupilThreshold.Text, out int value))
            {
                value = Math.Max(Math.Min(value, 255), 0);

                if (WhichEye == Eye.Left)
                {
                    trackingSettings.DarkThresholdLeftEye = value;
                }
                else
                {
                    trackingSettings.DarkThresholdRightEye = value;
                }
            }
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event parameters.</param>
        private void TrackBarReflectionThreshold_Scroll(object sender, EventArgs e)
        {
            if (WhichEye == Eye.Left)
            {
                trackingSettings.BrightThresholdLeftEye = trackBarReflectionThreshold.Value;
            }
            else
            {
                trackingSettings.BrightThresholdRightEye = trackBarReflectionThreshold.Value;
            }
        }

        private void TextBoxReflectionThreshold_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxReflectionThreshold.Text, out int value))
            {
                value = Math.Max(Math.Min(value, 255), 0);

                if (WhichEye == Eye.Left)
                {
                    trackingSettings.BrightThresholdLeftEye = value;
                }
                else
                {
                    trackingSettings.BrightThresholdRightEye = value;
                }
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
    }
}
