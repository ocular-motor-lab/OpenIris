//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelineBase.cs">
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
    using System.Collections.Generic;
    using OpenIris.UI;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    public abstract class EyeTrackingPipelineBase : IEyeTrackingPipeline
    {
        private readonly CvBlobDetector detector = new CvBlobDetector();
        private readonly CvBlobs blobs = new CvBlobs();

        /// <summary>
        /// Name of the pipeline.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Which eye is this pipeline for
        /// </summary>
        public Eye WhichEye { get; set; }

        /// <summary>
        /// Settings for the pipeline processing.
        /// </summary>
        public EyeTrackingPipelineSettings Settings { get; set; }

        /// <summary>
        /// Initializes an instance.
        /// </summary>
        protected EyeTrackingPipelineBase()
        {
            Settings = new EyeTrackingPipelineSettings();
            Name = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the pipeline.</param>
        /// <param name="eye">Which eye is the pipeline for.</param>
        /// <param name="settings">Settings of the pipeline.</param>
        /// <returns>The system.</returns>
        public static IEyeTrackingPipeline Create(string name, Eye eye, EyeTrackingPipelineSettings? settings = null)
        {
            var pipeline = EyeTrackerPluginManager.EyeTrackingPipelineFactory?.Create(name)
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.EyeTrackingPipelineFactory?.GetDefaultSettings(name) as EyeTrackingPipelineSettings
                ?? throw new OpenIrisException("Bad settings");

            pipeline.Name = name;
            pipeline.WhichEye = eye;
            pipeline.Settings = settings;

            return pipeline;
        }

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Process images.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <returns></returns>
        public abstract (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters);

        /// <summary>
        /// Updates the image of the eye on the setup tab.
        /// </summary>
        /// <param name="whichEye">Which eye to draw.</param>
        /// <param name="dataAndImages">Data of the corresponding image.</param>
        /// <returns>The new image with all the overlay of the data.</returns>
        public virtual IInputArray? UpdatePipelineEyeImage(Eye whichEye, EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return null;

            return ImageEyeBox.DrawAllData(
                                    dataAndImages.Images[whichEye],
                                    dataAndImages.Calibration.EyeCalibrationParameters[whichEye],
                                    dataAndImages.TrackingSettings);
        }

        /// <summary>
        /// Get the list of tracking settings that will be shown as sliders in the setup UI.
        /// </summary>
        /// <returns></returns>
        public virtual List<(string text, RangeDouble range, string settingName)>? GetQuickSettingsList()
        {
            return null;
        }
    }
}