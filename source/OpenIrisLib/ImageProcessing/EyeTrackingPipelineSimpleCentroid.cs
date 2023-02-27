//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelineSimpleCentroid.cs">
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

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("SimpleCentroid", typeof(EyeTrackingPipelinePupilCRSettings))]
    public sealed class EyeTrackingPipelineSimpleCentroid : IEyeTrackingPipeline, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ImageEyeProcess class.
        /// </summary>
        public EyeTrackingPipelineSimpleCentroid()
        {
        }

        /// <summary>
        /// Dispose objects.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Processes the current image to get the eye movement data.
        /// </summary>
        /// <param name="imageEye">Image from current frame.</param>
        /// <param name="eyeCalibrationParameters">Calibration info.</param>
        /// <param name="trackingSetting">Configuration parameters.</param>
        /// <returns>The data obtained from processing the image.</returns>
        public (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters, EyeTrackingPipelineSettings trackingSettings)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (eyeCalibrationParameters is null) throw new ArgumentNullException(nameof(eyeCalibrationParameters));
            var settings = trackingSettings as EyeTrackingPipelinePupilCRSettings ?? throw new ArgumentNullException(nameof(trackingSettings));

            // Copy the calibration variables just in case they change during the processing to avoid
            // inconsistencies The next frame will use the updated calibration
            var eyeModel = eyeCalibrationParameters.EyePhysicalModel;
            var referencePupil = eyeCalibrationParameters.ReferenceData.Pupil;
            var referenceTorsionImage = eyeCalibrationParameters.ImageTorsionReference;
            var eyeROI = imageEye.WhichEye == Eye.Left ? settings.CroppingLeftEye : settings.CroppingRightEye;
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? settings.DarkThresholdLeftEye : settings.DarkThresholdRightEye;

            // Cropping rectangle and eye ROI (minimum size 20x20 pix)
            eyeROI = new Rectangle(
                new Point(eyeROI.Left, eyeROI.Top),
                new Size((imageEye.Size.Width - eyeROI.Left - eyeROI.Width), (imageEye.Size.Height - eyeROI.Top - eyeROI.Height)));
            eyeROI.Intersect(imageEye.Image.ROI);
            if (eyeROI.Width < 20 || eyeROI.Height < 20) return (new EyeData(imageEye, ProcessFrameResult.MissingPupil), null);


            //// Threshold the image to find the dark parts (pupil)
            var imageThreshold = imageEye.ThresholdDark(thresholdDark);

            imageThreshold.ROI = eyeROI;
            var moments = imageThreshold.GetMoments(true);
            imageThreshold.ROI = new Rectangle();

            var centerPupilThreshold = new PointF((float)(moments.GravityCenter.X + eyeROI.Location.X), (float)(moments.GravityCenter.Y + eyeROI.Location.Y));
            var radius = (float)Math.Sqrt(imageThreshold.GetAverage().Intensity / 255.0 * imageThreshold.Width * imageThreshold.Height / Math.PI);

            // Save stuff for debugging
            EyeTrackerDebug.AddImage("pupil", imageEye.WhichEye, imageThreshold);

            var pupil = new PupilData(centerPupilThreshold, new SizeF(radius * 2, radius * 2), (float)0.0);



            // Create the data structure
            var eyeData = new EyeData(imageEye, ProcessFrameResult.Good)
            {
                Pupil = pupil,
                Iris = new IrisData( pupil.Center,  (float)(12.0f/trackingSettings.GetMmPerPix())),
                CornealReflections = null,
                TorsionAngle = 0,
                Eyelids = null,
                DataQuality = 0,
            };

            return (eyeData, null);
        }
        
        /// <summary>
        /// Gets the current pipeline UI
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        public EyeTrackingPipelineUI? GetPipelineUI(Eye whichEye)
        {
            return new UI.EyeTrackingPipelineJOMQuickSettings(whichEye);
        }
    }

}