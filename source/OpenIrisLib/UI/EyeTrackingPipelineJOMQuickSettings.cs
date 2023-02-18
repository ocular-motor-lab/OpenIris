//-----------------------------------------------------------------------
// <copyright file="EyeTrackerQuickSettings.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Windows.Forms;
    using OpenIris;
    using Emgu.CV;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;

    /// <summary>
    /// Control with some quick settings of the eye tracker for a single eye.
    /// </summary>
    public partial class EyeTrackingPipelineJOMQuickSettings : EyeTrackingPipelineUI
    {
        private EyeTrackingPipelineJOMSettings trackingSettings;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerQuickSettings class.
        /// </summary>
        public EyeTrackingPipelineJOMQuickSettings(Eye whichEye) 
            : base(whichEye)
        {
            InitializeComponent();

            trackBarIrisRadius.Maximum = 500;
            trackBarIrisRadius.Minimum = 0;

            trackingSettings = new EyeTrackingPipelineJOMSettings();
        }

        /// <summary>
        /// Updates the control.
        /// </summary>
        public void UpdateValues(EyeTrackingPipelinePupilCRSettings currentTrackingSettings)
        {
            trackingSettings = currentTrackingSettings as EyeTrackingPipelineJOMSettings;
            
            if (trackingSettings is null) return;

            if (trackBarIrisRadius.Maximum != trackingSettings.MaxIrisRadPixd)
            {
                trackBarIrisRadius.Maximum = trackingSettings.MaxIrisRadPixd;
            }

            trackingSettings.IrisRadiusPixLeft = Math.Min(trackingSettings.IrisRadiusPixLeft, trackingSettings.MaxIrisRadPixd);
            trackingSettings.IrisRadiusPixRight = Math.Min(trackingSettings.IrisRadiusPixRight, trackingSettings.MaxIrisRadPixd);


            if (WhichEye == Eye.Left)
            {
                trackBarPupilThreshold.Value = trackingSettings.DarkThresholdLeftEye;
                textBoxPupilThreshold.Text = trackingSettings.DarkThresholdLeftEye.ToString();

                trackBarIrisRadius.Value = (int)Math.Round(trackingSettings.IrisRadiusPixLeft);
                textBoxReflectionThreshold.Text = trackingSettings.BrightThresholdLeftEye.ToString();

                trackBarReflectionThreshold.Value = trackingSettings.BrightThresholdLeftEye;
                textBoxIrisRadius.Text = trackingSettings.IrisRadiusPixLeft.ToString();
            }
            else
            {
                trackBarIrisRadius.Value = (int)Math.Round(trackingSettings.IrisRadiusPixRight);
                textBoxIrisRadius.Text = trackingSettings.IrisRadiusPixRight.ToString();

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
        private void TrackBarIrisRadius_Scroll(object sender, EventArgs e)
        {
            if (WhichEye == Eye.Left)
            {
                trackingSettings.IrisRadiusPixLeft = trackBarIrisRadius.Value;
            }
            else
            {
                trackingSettings.IrisRadiusPixRight = trackBarIrisRadius.Value;
            }
        }

        private void TextBoxIrisRadius_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxIrisRadius.Text, out int value))
            {
                value = Math.Max(Math.Min(value, trackingSettings.MaxIrisRadPixd), 0);

                if (WhichEye == Eye.Left)
                {
                    trackingSettings.IrisRadiusPixLeft = value;
                }
                else
                {
                    trackingSettings.IrisRadiusPixRight = value;
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

            Image<Gray, byte>? imageTorsion = null;
            Image<Gray, byte>? imageTorsionRef = null;

            // Torsion image
            if (image?.ImageTorsion != null)
            {
                if (image.ImageTorsion.Size.Width > 4)
                {
                    imageTorsion = new Image<Gray, byte>(image.ImageTorsion.Size.Height, image.ImageTorsion.Size.Width);
                    CvInvoke.Transpose(image.ImageTorsion, imageTorsion);
                }
            }

            // Torsion reference
            if (eyeCalibration != null)
            {
                var torsionRef = eyeCalibration.ImageTorsionReference;
                if (torsionRef != null && torsionRef.Size.Width > 4)
                {
                    imageTorsionRef = new Image<Gray, byte>(torsionRef.Size.Height, torsionRef.Size.Width);
                    CvInvoke.Transpose(torsionRef, imageTorsionRef);
                }
            }

            imageBoxIris.Image = imageTorsion;
            imageBoxIrisRefeference.Image = imageTorsionRef;
        }

        public override void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return;

            var settings = dataAndImages.TrackingSettings as EyeTrackingPipelinePupilCRSettings ?? throw new Exception();

            UpdateValues(settings);
        }
    }
}
