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
    using Emgu.CV;
    using Emgu.CV.UI;
    using System.Windows.Forms;

    /// <summary>
    /// Control with some quick settings of the eye tracker for a single eye.
    /// </summary>
    public partial class EyeTrackingPipelineJOMQuickSettings : EyeTrackingPipelineUIControl
    {
        private EyeTrackingPipelineJOMSettings trackingSettings;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerQuickSettings class.
        /// </summary>
        public EyeTrackingPipelineJOMQuickSettings() 
        {
            InitializeComponent();

            sliderIrisRadius.Text = "Iris Radius";
            sliderPupilThreshold.Text = "Pupil Threshold";
            sliderCRThreshold.Text = "CR Threshold";

            sliderIrisRadius.Range = new Range(0, 500);
            sliderPupilThreshold.Range = new Range(0, 255);
            sliderCRThreshold.Range = new Range(0, 255);

            sliderIrisRadius.Dock = DockStyle.Fill;
            sliderPupilThreshold.Dock = DockStyle.Fill;
            sliderCRThreshold.Dock = DockStyle.Fill;

            trackingSettings = new EyeTrackingPipelineJOMSettings();
        }

        /// <summary>
        /// Update the pipeline UI.
        /// </summary>
        /// <param name="imageBox">image box for the eye image.</param>
        /// <param name="dataAndImages"></param>
        public override void UpdatePipelineEyeImage(ImageBox imageBox, EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return;

            var image = dataAndImages.Images[WhichEye];
            var settings = dataAndImages.TrackingSettings as EyeTrackingPipelineJOMSettings ?? throw new Exception();
            var eyeCalibration = dataAndImages.Calibration.EyeCalibrationParameters[WhichEye];

            // Update Images
            imageBox.Image = ImageEyeDrawing.DrawAllData(image, eyeCalibration, settings);

            Image<Emgu.CV.Structure.Gray, byte>? imageTorsion = null;
            Image<Emgu.CV.Structure.Gray, byte>? imageTorsionRef = null;

            // Torsion image
            if (image?.ImageTorsion != null)
            {
                if (image.ImageTorsion.Size.Width > 4)
                {
                    imageTorsion = new Image<Emgu.CV.Structure.Gray, byte>(image.ImageTorsion.Size.Height, image.ImageTorsion.Size.Width);
                    CvInvoke.Transpose(image.ImageTorsion, imageTorsion);
                }
            }

            // Torsion reference
            if (eyeCalibration != null)
            {
                var torsionRef = eyeCalibration.ImageTorsionReference;
                if (torsionRef != null && torsionRef.Size.Width > 4)
                {
                    imageTorsionRef = new Image<Emgu.CV.Structure.Gray, byte>(torsionRef.Size.Height, torsionRef.Size.Width);
                    CvInvoke.Transpose(torsionRef, imageTorsionRef);
                }
            }

            imageBoxIris.Image = imageTorsion;
            imageBoxIrisRefeference.Image = imageTorsionRef;
        }

        public override void UpdatePipelineUI(EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return;

            trackingSettings = dataAndImages.TrackingSettings as EyeTrackingPipelineJOMSettings ?? throw new Exception("Wrong settings for JOM pipeline.");

            // Update ranges if needed
            if (sliderIrisRadius.Range.End != trackingSettings.MaxIrisRadPixd)
            {
                sliderIrisRadius.Range = new Range(0, trackingSettings.MaxIrisRadPixd);
                trackingSettings.IrisRadiusPixLeft = Math.Min(trackingSettings.IrisRadiusPixLeft, trackingSettings.MaxIrisRadPixd);
                trackingSettings.IrisRadiusPixRight = Math.Min(trackingSettings.IrisRadiusPixRight, trackingSettings.MaxIrisRadPixd);
            }

            // Update values
            if (WhichEye == Eye.Left)
            {
                sliderPupilThreshold.Value = trackingSettings.DarkThresholdLeftEye;
                sliderCRThreshold.Value = trackingSettings.BrightThresholdLeftEye;
                sliderIrisRadius.Value = (int)trackingSettings.IrisRadiusPixLeft;
            }
            else
            {
                sliderPupilThreshold.Value = trackingSettings.DarkThresholdRightEye;
                sliderCRThreshold.Value = trackingSettings.BrightThresholdRightEye;
                sliderIrisRadius.Value = (int)trackingSettings.IrisRadiusPixRight;
            }
        }

        private void sliderPupilThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (sender == sliderPupilThreshold)
            {
                if (WhichEye == Eye.Left)
                {
                    trackingSettings.DarkThresholdLeftEye = sliderPupilThreshold.Value;
                }
                else
                {
                    trackingSettings.DarkThresholdRightEye = sliderPupilThreshold.Value;
                }
            }
            if (sender == sliderCRThreshold)
            {
                if (WhichEye == Eye.Left)
                {
                    trackingSettings.BrightThresholdLeftEye = sliderCRThreshold.Value;
                }
                else
                {
                    trackingSettings.BrightThresholdRightEye = sliderCRThreshold.Value;
                }
            }
            if (sender == sliderIrisRadius)
            {
                if (WhichEye == Eye.Left)
                {
                    trackingSettings.IrisRadiusPixLeft = sliderIrisRadius.Value;
                }
                else
                {
                    trackingSettings.IrisRadiusPixRight = sliderIrisRadius.Value;
                }
            }
        }
    }
}
