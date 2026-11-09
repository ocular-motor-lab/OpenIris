//-----------------------------------------------------------------------
// <copyright file="LinearGazeMap.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using OpenIris.Calibration;

    /// <summary>
    /// A linear map from an eye signal, in pixels, to the gaze angles in degrees, fitted by the Remote
    /// Linear calibration from targets at known positions:
    /// <code>[horizontal vertical] = [sx sy 1] * [[Gxx Gxy] [Gyx Gyy] [XOffset YOffset]]</code>
    /// The signal is a point of the eye minus another (Source minus Reference): for example the pupil
    /// minus the first corneal reflection, the pupil alone (Reference None, as with the webcam), or one
    /// reflection minus another (dual Purkinje). The angles have the signs of the targets, which the
    /// calibration gives in degrees, positive to the subject's right and up. It replaces the
    /// horizontal and vertical positions of the eye model; NaN when the signal isn't found.
    /// </summary>
    [Serializable]
    public class LinearGazeMap : EyeCalibrationData
    {
        public double Gxx { get; set; }

        public double Gxy { get; set; }

        public double Gyx { get; set; }

        public double Gyy { get; set; }

        public double XOffset { get; set; }

        public double YOffset { get; set; }

        /// <summary>
        /// Point of the eye the signal starts from.
        /// </summary>
        public EyeSignalSource Source { get; set; } = EyeSignalSource.Pupil;

        /// <summary>
        /// Point subtracted from the source. None for the source alone.
        /// </summary>
        public EyeSignalSource Reference { get; set; } = EyeSignalSource.CR1;

        public override CalibratedEyeData Adjust(EyeData eyeData, CalibratedEyeData fromEyeModel)
        {
            var gaze = Apply(eyeData);
            fromEyeModel.HorizontalPosition = gaze?.horizontal ?? double.NaN;
            fromEyeModel.VerticalPosition = gaze?.vertical ?? double.NaN;
            return fromEyeModel;
        }

        /// <summary>
        /// The gaze angles in degrees, or null if a point of the signal wasn't found in this frame.
        /// </summary>
        public (double horizontal, double vertical)? Apply(EyeData eyeData)
        {
            if (!(Signal(eyeData, Source, Reference) is PointF s)) return null;

            return (s.X * Gxx + s.Y * Gyx + XOffset, s.X * Gxy + s.Y * Gyy + YOffset);
        }

        /// <summary>
        /// The eye signal of a frame, in pixels: source minus reference. Null if one of them wasn't
        /// found (a reflection that isn't there, or at 0, 0).
        /// </summary>
        public static PointF? Signal(EyeData eyeData, EyeSignalSource source, EyeSignalSource reference)
        {
            if (!(Point(eyeData, source) is PointF s) || !(Point(eyeData, reference) is PointF r)) return null;
            return new PointF(s.X - r.X, s.Y - r.Y);
        }

        private static PointF? Point(EyeData eyeData, EyeSignalSource which)
        {
            if (which == EyeSignalSource.None) return new PointF(0, 0);

            // Not a point of the eye: it goes through an AppearanceGazeMap.
            if (which == EyeSignalSource.Appearance) return null;

            PointF point;
            if (which == EyeSignalSource.Pupil)
            {
                if (eyeData.Pupil.IsEmpty) return null;
                point = eyeData.Pupil.Center;
            }
            else
            {
                var index = which - EyeSignalSource.CR1;
                var reflections = eyeData.CornealReflections;
                if (reflections is null || reflections.Length <= index) return null;
                point = reflections[index].Center;
            }

            return point.X == 0 && point.Y == 0 ? (PointF?)null : point;
        }
    }
}
