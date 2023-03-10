//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelineNothing.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("Nothing", typeof(EyeTrackingPipelineSettings))]
    public sealed class EyeTrackingPipelineNothing : IEyeTrackingPipeline, IDisposable
    {
        /// <summary>
        /// Disposes objects.
        /// </summary>
        public void Dispose()
        {
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
            return (new EyeData(imageEye, ProcessFrameResult.Good), null);
        }

        /// <summary>
        /// Updates the image of the eye on the setup tab.
        /// </summary>
        /// <param name="whichEye">Which eye to draw.</param>
        /// <param name="dataAndImages">Data of the corresponding image.</param>
        /// <returns>The new image with all the overlay of the data.</returns>
        public IInputArray? UpdatePipelineEyeImage(Eye whichEye, EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return null;

            return ImageEyeDrawing.DrawAllData(
                                    dataAndImages.Images[whichEye],
                                    dataAndImages.Calibration.EyeCalibrationParameters[whichEye],
                                    dataAndImages.TrackingSettings);
        }

        /// <summary>
        /// Get the list of tracking settings that will be shown as sliders in the setup UI.
        /// </summary>
        /// <returns></returns>
        public List<(string text, Range range, string settingName)>? GetQuickSettingsList(Eye whichEye, EyeTrackingPipelineSettings settings)
        {
            return null;
        }
    }
}