//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
    using OpenIris.Calibration;
#nullable enable

    using System.ComponentModel.Composition;

    [Export(typeof(CalibrationPipelineBase)), PluginDescription("Manual Calibration", typeof(CalibrationSettings))]
    public class CalibrationPipelineManual : CalibrationPipelineBase
    {
        /// <summary>
        /// Last image of each eye, for the user interface to show while the user sets the eye models.
        /// </summary>
        public EyeCollection<ImageEye?> LastImages { get; } = new EyeCollection<ImageEye?>(null, null);

        /// <summary>
        /// Eye models chosen by the user. The calibration of the eye models ends once they are set.
        /// </summary>
        public EyeCollection<EyePhysicalModel>? EyeModels { get; set; }

        public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye image, EyeTrackingPipelineSettings processingSettings)
        {
            LastImages[image.WhichEye] = image;

            var eyeModels = EyeModels;
            if (eyeModels is null) return (false, EyePhysicalModel.EmptyModel);

            return (true, eyeModels[image.WhichEye]);
        }

        public override (bool referenceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            if (image == null) return (false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }
    }

}