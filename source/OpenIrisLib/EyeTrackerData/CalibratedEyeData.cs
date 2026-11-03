//-----------------------------------------------------------------------
// <copyright file="CalibratedEyeData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Form: http://www.opt.indiana.edu/v665/CD/CD_Version/CH3/CH3.HTM Much of the effort in
    /// teaching is to describe spherical coordinate systems and how to transform spherical rotation
    /// onto the Euclidean space of the tangent or projection screen. For this purpose a gimbaled
    /// laser projector can be used to illustrate three classic coordinate systems introduced early
    /// by Fick, Helmholtz and Listing. The laser is mounted on the back of a large-scale Plexiglas
    /// sphere that simulates rotation of the eye about various axes that pass through its center of
    /// rotation. The laser projects from the sphere onto a flat front projection screen to
    /// illustrate the path the eye would follow on the tangent screen during pure horizontal
    /// rotation about a vertical axis and pure vertical rotation about a horizontal axis. These
    /// paths are either straight or curved depending on whether the coordinate system uses axes that
    /// move with the eye or remain stationary in the head. (See Fig 3.2) The figures below
    /// illustrate a projection of a vertical cross onto a sphere about the eye (Left) and the
    /// projection of that cross onto a Tangent plane (Right) for five different coordinate systems.
    /// </summary>
    public enum EyeCoordianteSystem
    {
        /// <summary>
        /// Fick Coordinate System. (Head-fixed vertical axis/ Eye-fixed horizontal axis) Used with
        /// table-mounted projectors (e.g.,major amblyoscope).
        /// </summary>
        Fick,

        /// <summary>
        /// Helmholtz Coordinate System. (Eye-fixed vertical axis/ Head-fixed horizontal axis) Used
        /// with wall-mounted projectors.
        /// </summary>
        Helmholtz,

        /// <summary>
        /// Harms Coordinate System. (Vertical and horizontal axes both eye-fixed) Used with
        /// hand-held Lancaster projectors. Torsion as in Listing. Given the horizontal/verstical
        /// symmetry of this system.
        /// </summary>
        Hass,

        /// <summary>
        /// Hess Coordinate System. (Vertical and horizontal axes both head-fixed) Used with
        /// hand-held prisms. Torsion as in Listing. Given the horizontal/verstical symmetry of this system.
        /// </summary>
        Hess,

        /// <summary>
        /// Listing Coordinate System. (Based on polar geometry.) Used with perimeter.
        /// </summary>
        Listing,
    }

    /// <summary>
    /// Calibrated eye data, calculated from the raw data with the calibration
    /// (CalibrationParameters.GetCalibratedEyeData).
    /// </summary>
    /// <remarks>
    /// <para>Coordinate convention, the same for the eyes and the head (see CalibratedHeadData):
    /// positive is rightward, upward and clockwise, all from the subject's point of view.</para>
    /// <list type="bullet">
    /// <item>HorizontalPosition: degrees, positive when the eye looks to the subject's right.</item>
    /// <item>VerticalPosition: degrees, positive when the eye looks up.</item>
    /// <item>TorsionalPosition: degrees, positive when the eye rotates clockwise as the subject sees
    /// it (the top of the eye moves towards the subject's right; counterclockwise in the image).</item>
    /// </list>
    /// <para>All three are relative to the eye at the calibration's reference, so 0 is the reference
    /// position.</para>
    /// <para>The signs and the zero are the contract. How the three angles combine into an
    /// orientation (Fick, Helmholtz, rotation vectors...) is up to whatever calculates them, which
    /// must say which one it uses. Today that is CalibrationParameters.GetCalibratedEyeData for all
    /// the calibration methods: the Fick angles of the rotation of the eye from the reference
    /// (EyeRotationMath), with Listing's law (Listing's plane perpendicular to the line of sight at
    /// the reference) plus the measured torsion.</para>
    /// <para>The signs assume the eye images show the eyes as seen by someone facing the subject (the
    /// subject's right is the left of the image, up is up). Each eye tracking system makes its images
    /// look like that with its camera orientation (CameraEye.CameraOrientation); a camera that sees
    /// the eye through a mirror needs a mirrored orientation.</para>
    /// </remarks>
    public struct CalibratedEyeData : IEquatable<CalibratedEyeData>
    {
        // Could be stored as a transform, with these values as getters (see #61).

        /// <summary>
        /// Gets or sets the horizontal position of the eye, in degrees, relative to the reference
        /// position during calibration. Positive towards the subject's right.
        /// </summary>
        public double HorizontalPosition { get; set; }

        /// <summary>
        /// Gets or sets the vertical position of the eye, in degrees, relative to the reference
        /// position during calibration. Positive up.
        /// </summary>
        public double VerticalPosition { get; set; }

        /// <summary>
        /// Gets or sets the torsional position of the eye, in degrees, relative to the reference
        /// position during calibration. Positive clockwise from the subject's point of view (the top
        /// of the eye towards the subject's right).
        /// </summary>
        public double TorsionalPosition { get; set; }

        /// <summary>
        /// The data of an eye that isn't tracked: angles NaN, the rest 0.
        /// </summary>
        public static readonly CalibratedEyeData NotTracked = new CalibratedEyeData { HorizontalPosition = double.NaN, VerticalPosition = double.NaN, TorsionalPosition = double.NaN };

        /// <summary>
        /// Gets the line of sight, as a unit vector: x to the subject's right, y up, z forward, with
        /// (0, 0, 1) at the reference. From the three angles as Fick angles. NaN if they are.
        /// </summary>
        public Vector3 Gaze => GetGazeAndUp().gaze;

        /// <summary>
        /// Gets the up direction of the eye (where the top of the eye points), as a unit vector with
        /// the axes of Gaze, (0, 1, 0) at the reference. If the torsion is unknown (NaN), the one of
        /// Listing's law.
        /// </summary>
        public Vector3 Up => GetGazeAndUp().up;

        /// <summary>
        /// Gets Gaze and Up, calculated together. A method, so it isn't serialized with the data.
        /// </summary>
        public (Vector3 gaze, Vector3 up) GetGazeAndUp() => EyeRotationMath.GazeAndUp(EyeRotationMath.FromFickAngles(HorizontalPosition, VerticalPosition, TorsionalPosition));

        /// <summary>
        /// Gets or sets the area of the pupil ellipse, in mm², with the pixel scale (MmPerPix). NaN if
        /// the pixel scale is unknown.
        /// </summary>
        public double PupilArea { get; set; }

        /// <summary>
        /// Gets or sets the percent opening of the eyelids. 100% being the opening during calibration.
        /// </summary>
        public double PercentOpening { get; set; }

        /// <summary>
        /// Gets or sets the data quality metric, from 0 to 100.
        /// </summary>
        public double DataQuality { get; set; }

        /// <summary>
        /// Gets a hashcode for the object.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            return ((int)(this.HorizontalPosition * this.VerticalPosition)) ^ ((int)this.PupilArea * (int)this.PercentOpening);
        }

        /// <summary>
        /// Compares the object with another CalibratedEyeData object and checks if they are equal.
        /// </summary>
        /// <param name="obj">Another CalibratedEyeData object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is not CalibratedEyeData) return false;

            return Equals((CalibratedEyeData)obj);
        }

        /// <summary>
        /// Compares the object with another CalibratedEyeData object and checks if they are equal.
        /// </summary>
        /// <param name="other">Another CalibratedEyeData object.</param>
        /// <returns>True if they are equal.</returns>
        public bool Equals(CalibratedEyeData other)
        {
            if (HorizontalPosition != other.HorizontalPosition)
            {
                return false;
            }

            if (VerticalPosition != other.VerticalPosition)
            {
                return false;
            }

            if (PupilArea != other.PupilArea)
            {
                return false;
            }

            return this.PercentOpening == other.PercentOpening;
        }

        /// <summary>
        /// Compares to CalibratedEyeData objects.
        /// </summary>
        /// <param name="data1">CalibratedEyeData object 1.</param>
        /// <param name="data2">CalibratedEyeData object 2.</param>
        /// <returns>True if they are equal.</returns>
        public static bool operator ==(CalibratedEyeData data1, CalibratedEyeData data2)
        {
            return data1.Equals(data2);
        }

        /// <summary>
        /// Compares to CalibratedEyeData objects.
        /// </summary>
        /// <param name="data1">CalibratedEyeData object 1.</param>
        /// <param name="data2">CalibratedEyeData object 2.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(CalibratedEyeData data1, CalibratedEyeData data2)
        {
            return !data1.Equals(data2);
        }
    }
}
