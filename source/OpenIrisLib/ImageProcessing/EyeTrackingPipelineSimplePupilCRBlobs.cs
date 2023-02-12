//-----------------------------------------------------------------------
// <copyright file="ImageEyeProcess.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Cvb;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using OpenIris;
    using OpenIris.ImageProcessing;
    using System.Windows.Forms;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("SimplePupilCRBlobs", typeof(EyeTrackingPipelinePupilCRSettings))]
    public sealed class EyeTrackingPipelineSimplePupilCRBlobs : IEyeTrackingPipeline, IDisposable
    {
        private readonly CvBlobDetector detector = new CvBlobDetector();
        private readonly CvBlobs blobs = new CvBlobs();

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public void Dispose()
        {
            detector.Dispose();
            blobs.Dispose();
        }
        
        /// <summary>
        /// Process images.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters, EyeTrackingPipelineSettings settings)
        {
            var trackingSettings = settings as EyeTrackingPipelinePupilCRSettings ?? throw new Exception("Wrong type of settings");

            var maxPupRad = 10 / trackingSettings.GetMmPerPix();
            var irisRad = (float)(12 / trackingSettings.GetMmPerPix());
            var minPupArea = Math.PI * Math.Pow(trackingSettings.MinPupRadPix, 2);
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;
            var imageSizeForBlobSearch = 200;// imageEye.Size.Width;

            var pupilAprox = PupilTracking.FindPupilBlob(detector, blobs, imageEye, imageEye.Image.ROI, maxPupRad, minPupArea, imageSizeForBlobSearch, thresholdDark);
            if (pupilAprox.IsEmpty) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);
            EyeTrackerDebug.TrackTime("Find pupil aprox");

            var pupil = PositionTrackerEllipseFitting.CalculatePositionCentroid(imageEye, pupilAprox, 200, thresholdDark);
            EyeTrackerDebug.TrackTime("Get pupil center");

            var cornealReflections = CornealReflectionTracking.FindCornealReflectionsBlob(detector, blobs, imageEye, pupilAprox, trackingSettings);
            EyeTrackerDebug.TrackTime("FindCRs");

            // Create the data structure
            var eyeData = new EyeData()
            {
                WhichEye = imageEye.WhichEye,
                Timestamp = imageEye.TimeStamp,
                Pupil = pupil,
                Iris = new IrisData(pupil.Center, irisRad),
                CornealReflections = cornealReflections,
                TorsionAngle = 0.0,
                Eyelids = new EyelidData(),
                DataQuality = 100.0,
                ProcessFrameResult = ProcessFrameResult.Good,
                ImageSize = imageEye.Size
            };

            return (eyeData, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        public IPipelineUI? GetPipelineUI(Eye whichEye)
        {
            return new UI.EyeTrackingPipelinePupilCRQuickSettings(whichEye);
        }
    }

    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Settings for any pipeline that uses thresholds for pupil and reflections. Important to make them compatible with the remote
    /// UI client
    /// </summary>
    [Serializable]
    public class EyeTrackingPipelinePupilCRSettings : EyeTrackingPipelineSettings
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
        public double MinPupRadPix { get { return this.minPupRadmm / this.GetMmPerPix(); } }

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

        /// <summary>
        /// Gets or sets the minimum radius of the pupil
        /// </summary>
        [Browsable(false)]
        public double MinCRRadPix { get { return this.minCRRadmm / this.GetMmPerPix(); } }

        /// <summary>
        /// Gets or sets the maximum radius of the CRs (glints)
        /// </summary>
        [Browsable(false)]
        public double MaxCRRadPix { get { return this.maxCRRadmm / this.GetMmPerPix(); } }

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
    }
}