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
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("SimpleBlob", typeof(EyeTrackingPipelineWithThresholdsSettings))]
    public sealed class EyeTrackingPipelineSimpleBlob : IEyeTrackingPipeline, IDisposable
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
            var trackingSettings = settings as EyeTrackingPipelineWithThresholdsSettings ?? throw new Exception("Wrong type of settings");

            var maxPupRad = 10 / trackingSettings.GetMmPerPix();
            var irisRad = (float)(12 / trackingSettings.GetMmPerPix());
            var minPupArea = Math.PI * Math.Pow(trackingSettings.MinPupRadPix, 2);
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;
            var imageSizeForBlobSearch = 200;// imageEye.Size.Width;

            var pupil = PupilTracking.FindPupilBlob(detector, blobs, imageEye, imageEye.Image.ROI, maxPupRad, minPupArea, imageSizeForBlobSearch, thresholdDark);

            pupil = PositionTrackerEllipseFitting.CalculatePositionCentroid(imageEye, pupil, 200, thresholdDark);

            // Create the data structure
            var eyeData = new EyeData()
            {
                WhichEye = imageEye.WhichEye,
                Timestamp = imageEye.TimeStamp,
                Pupil = pupil,
                Iris = new IrisData(pupil.Center, irisRad),
                CornealReflections = new CornealReflectionData[] { },
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
            return null;
        }
    }
}