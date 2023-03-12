//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
    using OpenIris.Calibration;
#nullable enable

    using System.ComponentModel.Composition;

    [Export(typeof(ICalibrationPipeline)), PluginDescription("Manual Calibration", typeof(CalibrationSettings))]
    public class CalibrationPipelineManual : CalibrationPipelineBase
    {
        private CalibrationPipelineManualUI? ui;

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public override ICalibrationUIControl? GetCalibrationUI()
        {
            
            return ui;
        }

        public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel( EyeTrackingPipelineSettings processingSettings, ImageEye image)
        {
            if (ui is null)
            {
                ui = new CalibrationPipelineManualUI();
            }

            ui.lastImages[image.WhichEye] = image;

            if (ui.eyeModels is null) return (false, EyePhysicalModel.EmptyModel);

            return (true, ui.eyeModels[image.WhichEye]);
        }

        public override (bool referebceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings, ImageEye image)
        {
            ui = null; // No UI for resetting reference

            if (image == null) return (false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }
    }

}