﻿//-----------------------------------------------------------------------
// <copyright file="ImageEyeProcess.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Cvb;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Features2D;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Linq;

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

            // Cropping rectangle and eye ROI (minimum size 20x20 pix)
            var eyeROI = imageEye.WhichEye == Eye.Left ? settings.CroppingLeftEye : settings.CroppingRightEye;
            eyeROI = new Rectangle(
                new Point(eyeROI.Left, eyeROI.Top),
                new Size((imageEye.Size.Width - eyeROI.Left - eyeROI.Width), (imageEye.Size.Height - eyeROI.Top - eyeROI.Height)));
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;

            var smallSize = new Size(200, (int)Math.Round((double)eyeROI.Height / eyeROI.Width * 200));
            var scaleDownX = (float)smallSize.Width / eyeROI.Width;
            var scaleDownY = (float)smallSize.Height / eyeROI.Height;

            var imageThreshold = imageEye.ThresholdDarkResized(
                thresholdDark,
                smallSize,
                eyeROI);

            EyeTrackerDebug.AddImage("pupil1", imageEye.WhichEye, imageThreshold);

            var blobParams = new SimpleBlobDetectorParams()
            {
                FilterByArea = true,
                MinArea = (float)(Math.PI * Math.Pow(trackingSettings.MinPupRadPix * scaleDownX, 2)),
                MaxArea = (float)(Math.PI * Math.Pow(12 / trackingSettings.GetMmPerPix() * scaleDownX, 2)),
                FilterByCircularity = false,
                FilterByConvexity = false,
                FilterByInertia = false,
            };
            var detector = new SimpleBlobDetector(blobParams);

            var blobs = detector.Detect(imageThreshold);

            var pupil = new PupilData();
            if (blobs.Length > 0)
            {
                var pupilBlob = blobs.OrderByDescending(blob => blob.Response).First();

                pupil = new PupilData(new PointF( pupilBlob.Point.X/ scaleDownX, pupilBlob.Point.Y/ scaleDownY), new SizeF(pupilBlob.Size/ scaleDownX, pupilBlob.Size/ scaleDownY), 0.0f);
            }


            //var pupilAprox = FindPupilBlob(detector, blobs, imageEye, imageEye.Image.ROI, maxPupRad, minPupArea, imageSizeForBlobSearch, thresholdDark);
            //if (pupilAprox.IsEmpty) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);
            //EyeTrackerDebug.TrackPipelineTime("Find pupil aprox");

            //var pupil = pupilAprox;
            //var pupil = PositionTrackerEllipseFitting.CalculatePositionCentroid(imageEye, pupilAprox, 200, thresholdDark);
            //EyeTrackerDebug.TrackPipelineTime("Get pupil center");

            //var cornealReflections = CornealReflectionTracking.FindCornealReflectionsBlob(detector, blobs, imageEye, pupilAprox, trackingSettings);
            //EyeTrackerDebug.TrackPipelineTime("FindCRs");

            // Create the data structure
            var eyeData = new EyeData()
            {
                WhichEye = imageEye.WhichEye,
                Timestamp = imageEye.TimeStamp,
                Pupil = pupil,
                Iris = new IrisData(pupil.Center, (float)(12 / trackingSettings.GetMmPerPix())),
                CornealReflections = null,
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
        /// <param name="pipelineName"></param>
        /// <returns></returns>
        public EyeTrackingPipelineUIControl? GetPipelineUI()
        {
            return new UI.EyeTrackingPipelinePupilCRQuickSettings();
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
        public int DarkThresholdLeftEye { get => thresholdDarkLeftEye; set => SetProperty(ref thresholdDarkLeftEye, value, nameof(DarkThresholdLeftEye)); }
        private int thresholdDarkLeftEye = 60;

        [Category("Pupil tracking settings"), Description("Threshold to find the dark pixels that should belong to the pupil (right eye).")]
        public int DarkThresholdRightEye { get => thresholdDarkRightEye; set => SetProperty(ref thresholdDarkRightEye, value, nameof(DarkThresholdRightEye)); }
        private int thresholdDarkRightEye = 60;

        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (left eye).")]
        public int BrightThresholdLeftEye { get => thresholdBrightLeftEye; set => SetProperty(ref thresholdBrightLeftEye, value, nameof(BrightThresholdLeftEye)); }
        private int thresholdBrightLeftEye = 250;

        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (right eye).")]
        public int BrightThresholdRightEye { get => thresholdBrightRightEye; set => SetProperty(ref thresholdBrightRightEye, value, nameof(BrightThresholdRightEye)); }
        private int thresholdBrightRightEye = 250;

        [Browsable(false)]
        public double MinPupRadPix { get { return this.minPupRadmm / this.GetMmPerPix(); } }

        [Category("Pupil tracking settings"), Description("Minimum radius of the pupil in mms.")]
        public double MinPupRadmm { get => minPupRadmm; set => SetProperty(ref minPupRadmm, value, nameof(MinPupRadmm)); }
        private double minPupRadmm = 1;

        [Browsable(false)]
        public double MinCRRadPix { get { return this.minCRRadmm / this.GetMmPerPix(); } }

        [Browsable(false)]
        public double MaxCRRadPix { get { return this.maxCRRadmm / this.GetMmPerPix(); } }

        [Category("Corneal Reflection tracking settings"), Description("Minimum radius of the CR in mmms.")]
        public double MinCRRadmm { get => minCRRadmm; set => SetProperty(ref minCRRadmm, value, nameof(MinCRRadmm)); }
        private double minCRRadmm = 0.3;

        [Category("Corneal Reflection tracking settings"), Description("Maximum radius of the CR in mms.")]
        public double MaxCRRadmm { get => maxCRRadmm; set => SetProperty(ref maxCRRadmm, value, nameof(MaxCRRadmm)); }
        private double maxCRRadmm = 5;
    }
}