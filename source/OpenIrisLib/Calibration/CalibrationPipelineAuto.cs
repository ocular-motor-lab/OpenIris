//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System.ComponentModel.Composition;

    [Export(typeof(ICalibrationPipeline)), PluginDescription("Auto", typeof(CalibrationSettings))]
    public class CalibrationPipelineAuto : ICalibrationPipeline
    {
        /// <summary>
        /// Name of the plugin, gets set automatically.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates weather the calibration was cancelled.
        /// </summary>
        public bool Cancelled { get; }

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public CalibrationUIControl? GetCalibrationUI() => null;

        /// <summary>
        /// Process data towards setting a new physical model
        /// </summary>
        public (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings processingSettings, ImageEye imageEye)
        {
            if (imageEye is null) return (false, EyePhysicalModel.EmptyModel);

            if (imageEye?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, EyePhysicalModel.EmptyModel);

            return (true, new EyePhysicalModel(imageEye.EyeData.Pupil.Center, (float)(imageEye.EyeData.Iris.Radius * 2.0)));
        }

        /// <summary>
        /// Process data for setting a new reference.
        /// </summary>
        public (bool referebceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(CalibrationParameters currentCalibration, CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings processingSettings, ImageEye image)
        {
            if (image is null) return ( false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }

        public void Dispose()
        {
        }
    }

}