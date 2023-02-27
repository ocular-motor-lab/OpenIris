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
    using System.ComponentModel.Composition;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("Nothing", typeof(EyeTrackingPipelinePupilCRSettings))]
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
            return (new EyeData(), null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        public EyeTrackingPipelineUI? GetPipelineUI(Eye whichEye)
        {
            return new UI.EyeTrackingPipelinePupilCRQuickSettings(whichEye);
        }
    }
}