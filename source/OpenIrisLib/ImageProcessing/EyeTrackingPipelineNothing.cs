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
    using OpenIris.UI;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Class in charge of processing images and tracking the pupil and iris to obtain the eye
    /// position and the torsion angle.
    /// </summary>
    [Export(typeof(IEyeTrackingPipeline)), PluginDescriptionAttribute("Nothing", typeof(EyeTrackingPipelineSettings))]
    public sealed class EyeTrackingPipelineNothing : EyeTrackingPipelineBase, IDisposable
    {
        /// <summary>
        /// Process images.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <returns></returns>
        public override (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters)
        {
            return (new EyeData(imageEye, ProcessFrameResult.Good), null);
        }
    }
}