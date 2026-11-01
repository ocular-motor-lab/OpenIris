//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelinePupilCRSettings.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;

    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Settings for any pipeline that uses thresholds for pupil and reflections. Important to make them compatible with the remote
    /// UI client
    /// </summary>
    [Serializable]
    public class EyeTrackingPipelinePupilCRSettings : EyeTrackingPipelineSettings
    {
        /// <summary>
        /// Gets or sets threshold to find the dark pixels that should belong to the pupil (left eye)
        /// </summary>
        [Category("Pupil tracking settings"), Description("Threshold to find the dark pixels that should belong to the pupil (left eye).")]
        public int DarkThresholdLeftEye { get => thresholdDarkLeftEye; set => SetProperty(ref thresholdDarkLeftEye, value, nameof(DarkThresholdLeftEye)); }
        private int thresholdDarkLeftEye = 60;

        [Category("Pupil tracking settings"), Description("Threshold to find the dark pixels that should belong to the pupil (right eye).")]
        public int DarkThresholdRightEye { get => thresholdDarkRightEye; set => SetProperty(ref thresholdDarkRightEye, value, nameof(DarkThresholdRightEye)); }
        private int thresholdDarkRightEye = 60;

        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (left eye).")]
        public int BrightThresholdLeftEye { get => thresholdBrightLeftEye; set => SetProperty(ref thresholdBrightLeftEye, value, nameof(BrightThresholdLeftEye)); }
        private int thresholdBrightLeftEye = 250;

        [Category("Corneal Reflection tracking settings"), Description("Threshold to find the bright pixels that should belong to the reflexions (right eye).")]
        public int BrightThresholdRightEye { get => thresholdBrightRightEye; set => SetProperty(ref thresholdBrightRightEye, value, nameof(BrightThresholdRightEye)); }
        private int thresholdBrightRightEye = 250;

        [Browsable(false)]
        public double MinPupRadPix { get { return minPupRadmm / MmPerPix; } }

        [Category("Pupil tracking settings"), Description("Minimum radius of the pupil in mms.")]
        public double MinPupRadmm { get => minPupRadmm; set => SetProperty(ref minPupRadmm, value, nameof(MinPupRadmm)); }
        private double minPupRadmm = 1;

        [Browsable(false)]
        public double MaxPupRadPix { get { return maxPupRadmm / MmPerPix; } }

        [Category("Pupil tracking settings"), Description("Maximum radius of the pupil in mms.")]
        public double MaxPupRadmm { get => maxPupRadmm; set => SetProperty(ref maxPupRadmm, value, nameof(MaxPupRadmm)); }
        private double maxPupRadmm = 15;


        [Browsable(false)]
        public double MinCRRadPix { get { return minCRRadmm / MmPerPix; } }

        [Browsable(false)]
        public double MaxCRRadPix { get { return maxCRRadmm / MmPerPix; } }

        [Category("Corneal Reflection tracking settings"), Description("Minimum radius of the CR in mm.")]
        public double MinCRRadmm { get => minCRRadmm; set => SetProperty(ref minCRRadmm, value, nameof(MinCRRadmm)); }
        private double minCRRadmm = 0.1;

        [Category("Corneal Reflection tracking settings"), Description("Maximum radius of the CR in mm (about a small pupil).")]
        public double MaxCRRadmm { get => maxCRRadmm; set => SetProperty(ref maxCRRadmm, value, nameof(MaxCRRadmm)); }
        private double maxCRRadmm = 1.5;
    }
}