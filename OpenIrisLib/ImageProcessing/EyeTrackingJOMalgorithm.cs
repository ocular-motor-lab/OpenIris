﻿//-----------------------------------------------------------------------
// <copyright file="ImageEyeProcess.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingAlgorithm)), PluginDescriptionAttribute("JOM", typeof(EyeTrackingJOMalgorithmSettings))]
    public sealed class EyeTrackingJOMalgorithm : IEyeTrackingAlgorithm, IDisposable
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
        public EyeTrackingJOMalgorithm()
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
        public void Dispose()
        {
            pupilTracker.Dispose();
            cornealReflectionTracker.Dispose();
        }

        /// <summary>
        /// Processes the current image to get the eye movement data.
        /// </summary>
        /// <param name="imageEye">Image from current frame.</param>
        /// <param name="eyeCalibrationParameters">Calibration info.</param>
        /// <param name="trackingSetting">Configuration parameters.</param>
        /// <returns>The data obtained from processing the image.</returns>
        public (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters, EyeTrackingAlgorithmSettings trackingSetting)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (eyeCalibrationParameters is null) throw new ArgumentNullException(nameof(eyeCalibrationParameters));
            var settings = trackingSetting as EyeTrackingJOMalgorithmSettings ?? throw new ArgumentNullException(nameof(trackingSetting));

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
            EyeTrackerDebug.TrackTime("FindPupil");
            if (pupilAprox.IsEmpty) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);

            // Provisional eye model if there is none from calibration
            if (eyeModel.IsEmpty) eyeModel = new EyePhysicalModel(pupilAprox.Center, (float)irisRadius * 2);

            // 2 - Eyelid tracking
            var eyelids = eyeLidTracker.FindEyelids(imageEye, pupilAprox, eyeModel, settings);
            var filteredEyelids = eyeLidTracker.UpdateFilteredEyelids(eyelids, eyeModel);
            EyeTrackerDebug.TrackTime("FindEyeLids");

            // 3 - Corneal reflection tracker
            var cornealReflections = cornealReflectionTracker.FindCornealReflections(imageEye, pupilAprox, referencePupil, settings);
            EyeTrackerDebug.TrackTime("FindCRs");

            // 4 - Mask of reflections and eyelids
            var eyeMask = this.eyeTrackerMask.GetMask(imageEye, filteredEyelids, eyeModel, settings);
            EyeTrackerDebug.TrackTime("GetMask");

            // 5 - Refine the pupil position
            var pupil = positionTracker.CalculatePosition(imageEye, eyeMask, pupilAprox, referencePupil, settings);
            EyeTrackerDebug.TrackTime("CalculatePosition");

            // 6 - Find the iris
            var iris = this.irisTracker.FindIris(imageEye, pupil, settings);
            EyeTrackerDebug.TrackTime("UpdateIris");

            // 7 - Calculate torsion angle
            var torsionAngle = torsionTracker.CalculateTorsionAngle(imageEye, eyeModel, referenceTorsionImage, eyeMask, pupil, iris, settings, out Image<Gray, byte>? imageTorsion, out double dataQuality);
            EyeTrackerDebug.TrackTime("CalculateTorsionAngle");

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
        /// Gets the current algorithm UI
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        public IAlgorithmUI? GetAlgorithmUI(Eye whichEye)
        {
            return new UI.EyeTrackerQuickSettings(whichEye);
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Settings for any algorithm that uses thresholds for pupil and reflections. Important to make them compatible with the remote
    /// UI client
    /// </summary>
    [Serializable]
    public class EyeTrackingAlgorithmSettingsWithThresholds : EyeTrackingAlgorithmSettings
    {
        /// <summary>
        /// Gets or sets threshold to find the dark pixels that should belong to the pupil (left eye)
        /// </summary>
        [Category("Pupil tracking settings"), Description("Threshold to find the dark pixels that should belong to the pupil (left eye).")]
        public int DarkThresholdLeftEye
        {
            get { return this.thresholdDarkLeftEye; }
            set
            {
                if (value != this.thresholdDarkLeftEye)
                {
                    this.thresholdDarkLeftEye = value;
                    this.OnPropertyChanged(this, nameof(DarkThresholdLeftEye));
                }
            }
        }
        private int thresholdDarkLeftEye = 60;

        /// <summary>
        /// Gets or sets threshold to find the dark pixels that should belong to the pupil (right eye)
        /// </summary>
        [Category("Pupil tracking settings"), Description("Threshold to find the dark pixels that should belong to the pupil (right eye).")]
        public int DarkThresholdRightEye
        {
            get { return this.thresholdDarkRightEye; }
            set
            {
                if (value != this.thresholdDarkRightEye)
                {
                    this.thresholdDarkRightEye = value;
                    this.OnPropertyChanged(this, nameof(DarkThresholdRightEye));
                }
            }
        }
        private int thresholdDarkRightEye = 60;

        /// <summary>
        /// Gets or sets threshold to find the bright pixels that should belong to the reflexions
        /// (left eye)
        /// </summary>
        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (left eye).")]
        public int BrightThresholdLeftEye
        {
            get { return this.thresholdBrightLeftEye; }
            set
            {
                if (value != this.thresholdBrightLeftEye)
                {
                    this.thresholdBrightLeftEye = value;
                    this.OnPropertyChanged(this, nameof(BrightThresholdLeftEye));
                }
            }
        }
        private int thresholdBrightLeftEye = 250;

        /// <summary>
        /// Gets or sets threshold to find the bright pixels that should belong to the reflexions
        /// (right eye)
        /// </summary>
        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (right eye).")]
        public int BrightThresholdRightEye
        {
            get { return this.thresholdBrightRightEye; }
            set
            {
                if (value != this.thresholdBrightRightEye)
                {
                    this.thresholdBrightRightEye = value;
                    this.OnPropertyChanged(this, nameof(BrightThresholdRightEye));
                }
            }
        }
        private int thresholdBrightRightEye = 250;

        /// <summary>
        /// Gets the minimum radius of the pupil in pixels
        /// </summary>
        [Browsable(false)]
        public double MinPupRadPix { get { return this.minPupRadmm / this.MmPerPix; } }

        /// <summary>
        /// Gets or sets the minimum radius of the pupil in mm
        /// </summary>
        [Category("Pupil tracking settings"), Description("Minimum radius of the pupil in mms.")]
        public double MinPupRadmm
        {
            get { return this.minPupRadmm; }
            set
            {
                if (value != this.minPupRadmm)
                {
                    this.minPupRadmm = value;
                    this.OnPropertyChanged(this, nameof(MinPupRadmm));
                }
            }
        }
        private double minPupRadmm = 1;
    }

    [Serializable]
    public class EyeTrackingJOMalgorithmSettings : EyeTrackingAlgorithmSettingsWithThresholds
    {
        #region General tracking settings

        /// <summary>
        /// Gets or sets a value indicating whether to use Geometric transformation or not
        /// </summary>
        [Category("General tracking settings"), Description("Value indicating whether to use Geometric transformation or not.")]
        public bool UseGeometricTransformation
        {
            get
            {
                return this.useGeometricTransformation;
            }

            set
            {
                if (value != this.useGeometricTransformation)
                {
                    this.useGeometricTransformation = value;
                    this.OnPropertyChanged(this, nameof(UseGeometricTransformation));
                }
            }
        }

        private bool useGeometricTransformation = false;


        #endregion General tracking settings

        #region Pupil tracking settings

        /// <summary>
        /// Gets or sets the method to track the pupil
        /// </summary>
        [Category("Pupil tracking settings"), Description("Method to track the pupil.")]
        public PupilTracking.PupilTrackingMethod PupilTrackingMethod
        {
            get { return this.pupilTrackingMethod; }
            set
            {
                if (value != this.pupilTrackingMethod)
                {
                    this.pupilTrackingMethod = value;
                    this.OnPropertyChanged(this, nameof(PupilTrackingMethod));
                }
            }
        }
        private PupilTracking.PupilTrackingMethod pupilTrackingMethod = PupilTracking.PupilTrackingMethod.Blob;
        
        /// <summary>
        /// Gets or sets the method to track the position
        /// </summary>
        [Category("Pupil tracking settings"), Description("Method to track the position.")]
        public PositionTrackerEllipseFitting.PositionTrackingMethod PositionTrackingMethod
        {
            get { return this.positionTrackingMethod; }
            set
            {
                if (value != this.positionTrackingMethod)
                {
                    this.positionTrackingMethod = value;
                    this.OnPropertyChanged(this, nameof(PupilTrackingMethod));
                }
            }
        }
        private PositionTrackerEllipseFitting.PositionTrackingMethod positionTrackingMethod = PositionTrackerEllipseFitting.PositionTrackingMethod.EllipseFitting;

        #endregion Pupil tracking settings

        #region Corneal reflection tracking settings

        /// <summary>
        /// Gets or sets the method to track the CR
        /// </summary>
        [Category("Corneal Reflection tracking settings"), Description("Method to track the CR.")]
        public CornealReflectionTracking.CornealReflectionTrackingMethod CornealReflectionTrackingMethod
        {
            get { return this.cornealReflectionTrackingMethod; }
            set
            {
                if (value != this.cornealReflectionTrackingMethod)
                {
                    this.cornealReflectionTrackingMethod = value;
                    this.OnPropertyChanged(this, nameof(CornealReflectionTrackingMethod));
                }
            }
        }
        private CornealReflectionTracking.CornealReflectionTrackingMethod cornealReflectionTrackingMethod = CornealReflectionTracking.CornealReflectionTrackingMethod.Blob;

        /// <summary>
        /// Gets or sets the minimum radius of the pupil
        /// </summary>
        [Browsable(false)]
        public double MinCRRadPix { get { return this.minCRRadmm / this.MmPerPix; } }

        /// <summary>
        /// Gets or sets the maximum radius of the CRs (glints)
        /// </summary>
        [Browsable(false)]
        public double MaxCRRadPix { get { return this.maxCRRadmm / this.MmPerPix; } }

        /// <summary>
        /// Gets or sets the minimum radius of the CR  in mm
        /// </summary>
        [Category("Corneal Reflection tracking settings"), Description("Minimum radius of the CR in mmms.")]
        public double MinCRRadmm
        {
            get
            {
                return this.minCRRadmm;
            }

            set
            {
                if (value != this.minCRRadmm)
                {
                    this.minCRRadmm = value;
                    this.OnPropertyChanged(this, nameof(MinCRRadmm));
                }
            }
        }

        private double minCRRadmm = 0.3;

        /// <summary>
        /// Gets or sets the maximum radius of the CRs (glints)
        /// </summary>
        [Category("Corneal Reflection tracking settings"), Description("Maximum radius of the CR in mms.")]
        public double MaxCRRadmm
        {
            get { return maxCRRadmm; }
            set
            {
                if (value != this.maxCRRadmm)
                {
                    this.maxCRRadmm = value;
                    this.OnPropertyChanged(this, nameof(MaxCRRadmm));
                }
            }
        }
        private double maxCRRadmm = 5;

        #endregion corneal reflection tracking settings

        #region EyeLid tracking settings

        /// <summary>
        /// Gets or sets the method to track the eyelid
        /// </summary>
        [Category("EyeLid tracking settings"), Description("Method to track the EyeLid.")]
        public EyeLidTracking.EyeLidTrackingMethod EyelidTrackingMethod
        {
            get { return this.eyelidTrackingMethod; }
            set
            {
                if (value != this.eyelidTrackingMethod)
                {
                    this.eyelidTrackingMethod = value;
                    this.OnPropertyChanged(this, nameof(EyelidTrackingMethod));
                }
            }
        }
        private EyeLidTracking.EyeLidTrackingMethod eyelidTrackingMethod = EyeLidTracking.EyeLidTrackingMethod.HoughLines;

        #endregion EyeLid tracking settings

        #region Torsion tracking settings

        /// <summary>
        /// Gets or sets the method to measure torsion
        /// </summary>
        [Category("Torsion settings"), Description("Should we calculate torsion.")]
        public bool CalculateTorsion
        {
            get
            {
                return this.calculateTorsion;
            }

            set
            {
                if (value != this.calculateTorsion)
                {
                    this.calculateTorsion = value;
                    this.OnPropertyChanged(this, nameof(CalculateTorsion));
                }
            }
        }

        private bool calculateTorsion = true;
        
        /// <summary>
        /// Gets or sets the current radius of the left iris
        /// </summary>
        [Category("Torsion settings"), Description("Current radius of the left iris.")]
        public double IrisRadiusPixLeft
        {
            get
            {
                return this.irisRadiusPixLeft;
            }

            set
            {
                if (value != this.irisRadiusPixLeft)
                {
                    this.irisRadiusPixLeft = value;
                    this.OnPropertyChanged(this, nameof(IrisRadiusPixLeft));
                }
            }
        }

        private double irisRadiusPixLeft = 80;

        /// <summary>
        /// Gets or sets the current radius of the right iris
        /// </summary>
        [Category("Torsion settings"), Description("Current radius of the right iris.")]
        public double IrisRadiusPixRight
        {
            get
            {
                return this.irisRadiusPixRight;
            }

            set
            {
                if (value != this.irisRadiusPixRight)
                {
                    this.irisRadiusPixRight = value;
                    this.OnPropertyChanged(this, nameof(IrisRadiusPixRight));
                }
            }
        }

        private double irisRadiusPixRight = 80;

        /// <summary>
        /// Gets or sets the maximum radius of the iris
        /// </summary>
        [Browsable(false)]
        public double MaxIrisRadPixd { get { return this.maxIrisRadmm / this.MmPerPix; } }

        /// <summary>
        /// Gets or sets the maximum radius of the iris
        /// </summary>
        [Category("Torsion settings"), Description("Maximum radius of the iris in milimiters.")]
        public double MaxIrisRadmm
        {
            get
            {
                return this.maxIrisRadmm;
            }

            set
            {
                if (value != this.maxIrisRadmm)
                {
                    this.maxIrisRadmm = value;
                    this.OnPropertyChanged(this, nameof(this.MaxIrisRadmm));
                }
            }
        }

        private double maxIrisRadmm = 15;

        /// <summary>
        /// Pixel with of the iris images used for the cross correlation. The iris
        /// pattern is resized to a fixed size for two reasons. First, to keep processing time 
        /// constant. Second, to account for pupil size changes. 
        /// Better make it a multiple of 4. A typical value is 40. 60 or 80 will give better results
        /// but may be too computationally costly.
        /// </summary>
        [Category("Torsion settings"), Description("Number of pixels of the height (radial) of the iris image used for torsion calculations.")]
        public int TorsionImageIrisWidth
        {
            get { return this.torsionImageIrisWidth; }
            set
            {
                if (value != this.torsionImageIrisWidth)
                {
                    this.torsionImageIrisWidth = value;
                    this.OnPropertyChanged(this, nameof(this.TorsionImageIrisWidth));
                }
            }
        }
        private int torsionImageIrisWidth = 80;

        /// <summary>
        /// Magnification of the iris, in pixels per degree. Larger values will provide better 
        /// torsion resolution but also more time to process. Default is 1 pixel per degree.
        /// </summary>
        [Category("Torsion settings"), Description("Number of pixels of the width (angular) of the iris image used for torsion calculations.")]
        public double TorsionAngularResolution
        {
            get { return this.torsionAngularResolution; }
            set
            {
                if (value != this.torsionAngularResolution)
                {
                    this.torsionAngularResolution = value;
                    this.OnPropertyChanged(this, nameof(this.TorsionAngularResolution));
                }
            }
        }
        private double torsionAngularResolution = 1;

        /// <summary>
        /// Interpolation ratio of the crosscorrelation (how many samples per degree)
        /// for subpixel resolution. The cross correlation is calculated with 1 pixel per degree.
        /// A value of 50 seems to work well and gives a subpixel resolution will be 0.02 degrees.
        /// </summary>
        [Category("Torsion settings"), Description("Interpolation rate for the calculation of the torsion angle from the crosscorrelation peak.")]
        public int TorsionInterpolation
        {
            get { return this.torsionInterpolation; }
            set
            {
                if (value != this.torsionInterpolation)
                {
                    this.torsionInterpolation = value;
                    this.OnPropertyChanged(this, nameof(this.TorsionInterpolation));
                }
            }
        }
        private int torsionInterpolation = 50;


        /// <summary>
        /// Size of the highpass filter for the iris pattern in degrees.
        /// </summary>
        [Category("Torsion settings"), Description("Size of the highpass filter for the iris pattern in degrees. Has to be evn and less than 30.")]
        public int TorsionSobelFilterSize
        {
            get { return this.torsionSobelFilterSize; }
            set
            {
                var newValue = (int)Math.Min(2 * Math.Round(value / 2.0), 30);
                if (newValue != this.torsionSobelFilterSize)
                {
                    this.torsionSobelFilterSize = newValue;
                    this.OnPropertyChanged(this, nameof(this.TorsionSobelFilterSize));
                }
            }
        }
        private int torsionSobelFilterSize = 4;


        /// <summary>
        /// Gets or sets the maximum torsion in one direction (degrees)
        /// </summary>
        [Category("Torsion settings"), Description("Maximum torsion in one direction (degrees).")]
        public double MaxTorsion
        {
            get
            {
                return this.maxTorsion;
            }

            set
            {
                if (value != this.maxTorsion)
                {
                    this.maxTorsion = value;
                    this.OnPropertyChanged(this, nameof(this.MaxTorsion));
                }
            }
        }

        private double maxTorsion = 25;

        #endregion Torsion tracking settings
    }
}