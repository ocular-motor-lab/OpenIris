//-----------------------------------------------------------------------
// <copyright file="CalibrationPipelineAuto.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System.ComponentModel.Composition;

    /// <summary>
    /// Takes the first frame where every eye is tracked well: its pupils center the eye models, with
    /// the iris diameter as radius, and the first such frame processed with them is the reference.
    /// </summary>
    [Export(typeof(CalibrationPipelineBase)), PluginDescription("Auto", typeof(CalibrationSettings))]
    public class CalibrationPipelineAuto : CalibrationPipelineBase
    {
        /// <summary>
        /// The eye models, from the first frame with every eye tracked.
        /// </summary>
        public override (bool completed, CalibrationParameters? calibration) ProcessForCalibrationModel(EyeTrackerImagesAndData frame, EyeTrackingPipelineSettings processingSettings)
        {
            if (!AllEyesTracked(frame)) return (false, null);

            var calibration = new CalibrationParameters();
            foreach (var eye in Eyes(EyesToCalibrate))
            {
                var eyeData = frame.Images[eye]!.EyeData!;
                calibration.EyeCalibrationParameters[eye].SetEyeModel(new EyePhysicalModel(eyeData.Pupil.Center, (float)(eyeData.Iris.Radius * 2.0)));
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
