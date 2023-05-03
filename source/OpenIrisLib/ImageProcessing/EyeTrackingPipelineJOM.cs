//-----------------------------------------------------------------------
// <copyright file="ImageEyeProcess.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using OpenIris.ImageProcessing;
    using System.Windows.Forms;
    using System.Collections.Generic;
    using Emgu.CV.UI;
    using OpenIris.UI;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(EyeTrackingPipelineBase)), PluginDescriptionAttribute("JOM", typeof(EyeTrackingPipelineJOMSettings))]
    public sealed class EyeTrackingPipelineJOM : EyeTrackingPipelineBase, IDisposable
    {
        private readonly PupilTracking pupilTracker;
        private readonly CornealReflectionTracking cornealReflectionTracker;
        private readonly PositionTrackerEllipseFitting positionTracker;
        private readonly EyeLidTracking eyeLidTracker;
        private readonly TorsionTracking torsionTracker;
        private readonly EyeTrackerMask eyeTrackerMask;
        private readonly IrisTracker irisTracker;

        /// <summary>
        /// Initializes a new instance of the ImageEyeProcess class.
        /// </summary>
        private EyeTrackingPipelineJOM()
        {
            pupilTracker = new PupilTracking();
            cornealReflectionTracker = new CornealReflectionTracking();
            positionTracker = new PositionTrackerEllipseFitting();
            eyeLidTracker = new EyeLidTracking();
            torsionTracker = new TorsionTracking();
            irisTracker = new IrisTracker();
            eyeTrackerMask = new EyeTrackerMask();
        }

        /// <summary>
        /// Dispose objects.
        /// </summary>
        public override void Dispose()
        {
            pupilTracker.Dispose();
            cornealReflectionTracker.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Processes the current image to get the eye movement data.
        /// </summary>
        /// <param name="imageEye">Image from current frame.</param>
        /// <param name="eyeCalibrationParameters">Calibration info.</param>
        /// <param name="trackingSetting">Configuration parameters.</param>
        /// <returns>The data obtained from processing the image.</returns>
        public override (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (eyeCalibrationParameters is null) throw new ArgumentNullException(nameof(eyeCalibrationParameters));
            var settings = Settings as EyeTrackingPipelineJOMSettings ?? throw new ArgumentNullException(nameof(Settings));

            // Copy the calibration variables just in case they change during the processing to avoid
            // inconsistencies The next frame will use the updated calibration
            var eyeModel = eyeCalibrationParameters.EyePhysicalModel;
            var referencePupil = eyeCalibrationParameters.ReferenceData.Pupil;
            var referenceTorsionImage = eyeCalibrationParameters.ImageTorsionReference;
            var eyeROI = imageEye.WhichEye == Eye.Left ? settings.CroppingLeftEye : settings.CroppingRightEye;
            var irisRadius = (imageEye.WhichEye == Eye.Left) ? settings.IrisRadiusPixLeft : settings.IrisRadiusPixRight;

            // Cropping rectangle and eye ROI (minimum size 20x20 pix)
            eyeROI = new Rectangle(
                new Point(eyeROI.Left, eyeROI.Top),
                new Size((imageEye.Size.Width - eyeROI.Left - eyeROI.Width), (imageEye.Size.Height - eyeROI.Top - eyeROI.Height)));
            eyeROI.Intersect(imageEye.Image.ROI);
            if (eyeROI.Width < 20 || eyeROI.Height < 20) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);

            // 1 - Quick search of the pupil
            var pupilAprox = pupilTracker.FindPupil(imageEye, eyeROI, settings);
            EyeTrackerDebug.TrackProcessingTime("FindPupil");
            if (pupilAprox.IsEmpty) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);

            // Provisional eye model if there is none from calibration
            if (eyeModel.IsEmpty) eyeModel = new EyePhysicalModel(pupilAprox.Center, (float)irisRadius * 2);

            // 2 - Eyelid tracking
            var eyelids = eyeLidTracker.FindEyelids(imageEye, pupilAprox, eyeModel, settings);
            var filteredEyelids = eyeLidTracker.UpdateFilteredEyelids(eyelids, eyeModel);
            EyeTrackerDebug.TrackProcessingTime("FindEyeLids");

            // 3 - Corneal reflection tracker
            var cornealReflections = cornealReflectionTracker.FindCornealReflections(imageEye, pupilAprox, settings);
            EyeTrackerDebug.TrackProcessingTime("FindCRs");

            // 4 - Mask of reflections and eyelids
            var eyeMask = this.eyeTrackerMask.GetMask(imageEye, filteredEyelids, eyeModel, settings);
            EyeTrackerDebug.TrackProcessingTime("GetMask");

            // 5 - Refine the pupil position
            var pupil = positionTracker.CalculatePosition(imageEye, eyeMask, pupilAprox, referencePupil, settings);
            EyeTrackerDebug.TrackProcessingTime("CalculatePosition");

            // 6 - Find the iris
            var iris = this.irisTracker.FindIris(imageEye, pupil, settings);
            EyeTrackerDebug.TrackProcessingTime("UpdateIris");

            // 7 - Calculate torsion angle
            var torsionAngle = torsionTracker.CalculateTorsionAngle(imageEye, eyeModel, referenceTorsionImage, eyeMask, pupil, iris, settings, out Image<Gray, byte>? imageTorsion, out double dataQuality);
            EyeTrackerDebug.TrackProcessingTime("CalculateTorsionAngle");

            // Create the data structure
            var eyeData = new EyeData(imageEye, ProcessFrameResult.Good)
            {
                Pupil = pupil,
                Iris = iris,
                CornealReflections = cornealReflections,
                TorsionAngle = torsionAngle,
                Eyelids = eyelids,
                DataQuality = dataQuality,
            };

            return (eyeData, imageTorsion);
        }

        /// <summary>
        /// Get the list of tracking settings that will be shown as sliders in the setup UI.
        /// </summary>
        /// <returns></returns>
        public override List<(string text, RangeDouble range, string settingName)>? GetQuickSettingsList()
        {
            var theSettings = Settings as EyeTrackingPipelineJOMSettings ?? throw new InvalidOperationException("bad settings");

            return WhichEye switch
            {
                Eye.Left => new List<(string text, RangeDouble range, string SettingName)>
                    {
                        ("Pupil threshold (px)",    new RangeDouble(0, 255),                        nameof(theSettings.DarkThresholdLeftEye)),
                        ("CR threshold (px)",       new RangeDouble(0, 255),                        nameof(theSettings.BrightThresholdLeftEye)),
                        ("Iris radius (px)",        new RangeDouble(0, theSettings.MaxIrisRadPixd), nameof(theSettings.IrisRadiusPixLeft)),
                        ("Min Pup radius (mm)",     new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MinPupRadmm)),
                        ("Max Pup radius (mm)",     new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MaxPupRadmm)),
                        ("Min CR radius (mm)",      new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MinCRRadmm)),
                        ("Max CR radius (mm)",      new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MaxCRRadmm)),
                    },
                Eye.Right => new List<(string text, RangeDouble range, string SettingName)>
                    {
                        ("Pupil threshold (px)",    new RangeDouble(0, 255),                        nameof(theSettings.DarkThresholdRightEye)),
                        ("CR threshold (px)",       new RangeDouble(0, 255),                        nameof(theSettings.BrightThresholdRightEye)),
                        ("Iris radius (px)",        new RangeDouble(0, theSettings.MaxIrisRadPixd), nameof(theSettings.IrisRadiusPixRight)),
                        ("Min Pup radius (mm)",     new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MinPupRadmm)),
                        ("Max Pup radius (mm)",     new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MaxPupRadmm)),
                        ("Min CR radius (mm)",      new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MinCRRadmm)),
                        ("Max CR radius (mm)",      new RangeDouble(0, theSettings.MaxIrisRadmm),   nameof(theSettings.MaxCRRadmm)),
                    },
                _ => throw new Exception("Wrong eye."),
            };
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [Serializable]
    public class EyeTrackingPipelineJOMSettings : EyeTrackingPipelinePupilCRSettings
    {
        #region General tracking settings

        [Category("General tracking settings"), Description("Value indicating whether to use Geometric transformation or not.")]
        public bool UseGeometricTransformation { get => useGeometricTransformation; set => SetProperty(ref useGeometricTransformation, value, nameof(UseGeometricTransformation)); }
        private bool useGeometricTransformation = false;


        #endregion General tracking settings

        #region Pupil tracking settings

        [Category("Pupil tracking settings"), Description("Method to track the pupil.")]
        public PupilTracking.PupilTrackingMethod PupilTrackingMethod { get => pupilTrackingMethod; set => SetProperty(ref pupilTrackingMethod, value, nameof(PupilTrackingMethod)); }
        private PupilTracking.PupilTrackingMethod pupilTrackingMethod = PupilTracking.PupilTrackingMethod.Blob;

        [Category("Pupil tracking settings"), Description("Method to track the position.")]
        public PositionTrackerEllipseFitting.PositionTrackingMethod PositionTrackingMethod { get => positionTrackingMethod; set => SetProperty(ref positionTrackingMethod, value, nameof(PositionTrackingMethod)); }
        private PositionTrackerEllipseFitting.PositionTrackingMethod positionTrackingMethod = PositionTrackerEllipseFitting.PositionTrackingMethod.EllipseFitting;

        #endregion Pupil tracking settings

        #region Corneal reflection tracking settings

        [Category("Corneal Reflection tracking settings"), Description("Method to track the CR.")]
        public CornealReflectionTracking.CornealReflectionTrackingMethod CornealReflectionTrackingMethod { get => cornealReflectionTrackingMethod; set => SetProperty(ref cornealReflectionTrackingMethod, value, nameof(CornealReflectionTrackingMethod)); }
        private CornealReflectionTracking.CornealReflectionTrackingMethod cornealReflectionTrackingMethod = CornealReflectionTracking.CornealReflectionTrackingMethod.Blob;

        #endregion corneal reflection tracking settings

        #region EyeLid tracking settings

        [Category("EyeLid tracking settings"), Description("Method to track the EyeLid.")]
        public EyeLidTracking.EyeLidTrackingMethod EyelidTrackingMethod { get => eyelidTrackingMethod; set => SetProperty(ref eyelidTrackingMethod, value, nameof(EyelidTrackingMethod)); }
        private EyeLidTracking.EyeLidTrackingMethod eyelidTrackingMethod = EyeLidTracking.EyeLidTrackingMethod.None;

        #endregion EyeLid tracking settings

        #region Torsion tracking settings

        [Category("Torsion settings"), Description("Should we calculate torsion.")]
        public bool CalculateTorsion { get => calculateTorsion; set => SetProperty(ref calculateTorsion, value, nameof(CalculateTorsion)); }
        private bool calculateTorsion = true;

        [Category("Torsion settings"), Description("Current radius of the left iris.")]
        public double IrisRadiusPixLeft
        {
            get => irisRadiusPixLeft;

            set => SetProperty(ref irisRadiusPixLeft, Math.Min(value, MaxIrisRadPixd), nameof(IrisRadiusPixLeft));
        }
        private double irisRadiusPixLeft = 80;

        [Category("Torsion settings"), Description("Current radius of the right iris.")]
        public double IrisRadiusPixRight
        {
            get => irisRadiusPixRight;
            set => SetProperty(ref irisRadiusPixRight, Math.Min(value, MaxIrisRadPixd), nameof(IrisRadiusPixRight));
        }
        private double irisRadiusPixRight = 80;

        // [Browsable(false)]
        public int MaxIrisRadPixd { get { return (int)Math.Ceiling(maxIrisRadmm / MmPerPix); } }

        [Category("Torsion settings"), Description("Maximum radius of the iris in milimiters.")]
        [NeedsRestarting]
        public double MaxIrisRadmm { get => maxIrisRadmm; set => SetProperty(ref maxIrisRadmm, value, nameof(MaxIrisRadmm)); }
        private double maxIrisRadmm = 15;

        /// <summary>
        /// Pixel with of the iris images used for the cross correlation. The iris
        /// pattern is resized to a fixed size for two reasons. First, to keep processing time 
        /// constant. Second, to account for pupil size changes. 
        /// Better make it a multiple of 4. A typical value is 40. 60 or 80 will give better results
        /// but may be too computationally costly.
        /// </summary>
        [Category("Torsion settings"), Description("Number of pixels of the height (radial) of the iris image used for torsion calculations. ")]
        public int TorsionImageIrisWidth { get => torsionImageIrisWidth; set => SetProperty(ref torsionImageIrisWidth, value, nameof(TorsionImageIrisWidth)); }
        private int torsionImageIrisWidth = 80;

        /// <summary>
        /// Magnification of the iris, in pixels per degree. Larger values will provide better 
        /// torsion resolution but also more time to process. Default is 1 pixel per degree.
        /// </summary>
        [Category("Torsion settings"), Description("Number of pixels of the width (angular) of the iris image used for torsion calculations.")]
        public double TorsionAngularResolution { get => torsionAngularResolution; set => SetProperty(ref torsionAngularResolution, value, nameof(TorsionAngularResolution)); }
        private double torsionAngularResolution = 1;

        /// <summary>
        /// Interpolation ratio of the crosscorrelation (how many samples per degree)
        /// for subpixel resolution. The cross correlation is calculated with 1 pixel per degree.
        /// A value of 50 seems to work well and gives a subpixel resolution will be 0.02 degrees.
        /// </summary>
        [Category("Torsion settings"), Description("Interpolation rate for the calculation of the torsion angle from the crosscorrelation peak.")]
        public int TorsionInterpolation { get => torsionInterpolation; set => SetProperty(ref torsionInterpolation, value, nameof(TorsionInterpolation)); }
        private int torsionInterpolation = 50;


        /// <summary>
        /// Size of the highpass filter for the iris pattern in degrees.
        /// </summary>
        [Category("Torsion settings"), Description("Size of the highpass filter for the iris pattern in degrees. Has to be evn and less than 30.")]
        public int TorsionSobelFilterSize { get => torsionSobelFilterSize; set => SetProperty(ref torsionSobelFilterSize, value, nameof(TorsionSobelFilterSize)); }
        private int torsionSobelFilterSize = 4;


        /// <summary>
        /// Gets or sets the maximum torsion in one direction (degrees)
        /// </summary>
        [Category("Torsion settings"), Description("Maximum torsion in one direction (degrees).")]
        public double MaxTorsion { get => maxTorsion; set => SetProperty(ref maxTorsion, value, nameof(MaxTorsion)); }
        private double maxTorsion = 25;

        #endregion Torsion tracking settings
    }
}