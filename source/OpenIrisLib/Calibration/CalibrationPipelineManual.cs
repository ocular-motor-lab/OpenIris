//-----------------------------------------------------------------------
// <copyright file="CalibrationPipelineManual.cs">
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

        /// <summary>
        /// Keeps the last images for the user interface, until the user sets the eye models.
        /// </summary>
        public override (bool completed, CalibrationParameters? calibration) ProcessForCalibrationModel(EyeTrackerImagesAndData frame, EyeTrackingPipelineSettings processingSettings)
        {
            foreach (var eye in Eyes(EyesToCalibrate))
            {
                if (frame.Images[eye] is ImageEye image) LastImages[eye] = image;
            }

            var eyeModels = EyeModels;
            if (eyeModels is null) return (false, null);

            var calibration = new CalibrationParameters();
            foreach (var eye in Eyes(EyesToCalibrate))
            {
                calibration.EyeCalibrationParameters[eye].SetEyeModel(eyeModels[eye]);
            }

            return (true, calibration);
        }

        /// <summary>
        /// The references, from the first frame with every eye tracked.
        /// </summary>
        public override (bool completed, CalibrationParameters? calibration) ProcessForZeroReference(EyeTrackerImagesAndData frame, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            if (!AllEyesTracked(frame)) return (false, null);

            return (true, WithReferences(currentCalibration, frame));
        }
    }

}