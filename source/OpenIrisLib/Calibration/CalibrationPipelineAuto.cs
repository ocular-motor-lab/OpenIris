﻿//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System.ComponentModel.Composition;

    [Export(typeof(CalibrationPipelineBase)), PluginDescription("Auto", typeof(CalibrationSettings))]
    public class CalibrationPipelineAuto : CalibrationPipelineBase
    {
        /// <summary>
        /// Process data towards setting a new physical model
        /// </summary>
        public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye imageEye, EyeTrackingPipelineSettings processingSettings)
        {
            if (imageEye is null) return (false, EyePhysicalModel.EmptyModel);

            if (imageEye?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, EyePhysicalModel.EmptyModel);

            return (true, new EyePhysicalModel(imageEye.EyeData.Pupil.Center, (float)(imageEye.EyeData.Iris.Radius * 2.0)));
        }

        /// <summary>
        /// Process data for setting a new reference.
        /// </summary>
        public override (bool referenceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            if (image is null) return ( false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }
    }

}