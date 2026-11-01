//-----------------------------------------------------------------------
// <copyright file="CalibratedEyeData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;

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
    /// the calibration methods: horizontal and vertical are each the arcsine of the pupil's
    /// displacement on the eye globe of the calibration's eye model (pupil center minus globe center,
    /// divided by the globe radius, in the image), minus the same for the reference. Calculated
    /// separately, they are not Fick or Helmholtz angles, though all agree near the reference.</para>
    /// <para>The signs assume the eye images show the eyes as seen by someone facing the subject (the
    /// subject's right is the left of the image, up is up). Each eye tracking system makes its images
    /// look like that with its camera orientation (CameraEye.CameraOrientation); a camera that sees
    /// the eye through a mirror needs a mirrored orientation.</para>
    /// </remarks>
    public struct CalibratedEyeData : IEquatable<CalibratedEyeData>
    {
        // TODO: MOVE TO TRANSFORMS then all the simple values can be getters out of the matrix.

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
        /// Gets or sets the area of the pupil ellipse, in mm², taking the eye globe as 24 mm across.
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Calibrated head data, calculated from the raw head sensor data
    /// (CalibrationParameters.GetCalibratedHeadData).
    /// </summary>
    /// <remarks>
    /// <para>Same convention as the eyes (see CalibratedEyeData): positive is rightward, upward and
    /// clockwise, all from the subject's point of view. As for the eyes, the signs are the contract,
    /// and how the angles combine (the order of the rotations) is up to whatever calculates them,
    /// which must say which one it uses. With the head upright, facing straight ahead:</para>
    /// <list type="bullet">
    /// <item>X points to the subject's right, Y up, Z forward (out of the face). This is a
    /// left-handed set of axes, the price of positive being right and up for both eyes and head.</item>
    /// <item>Yaw: degrees, positive when the head turns to the subject's right (towards +X).</item>
    /// <item>Pitch: degrees, positive when the head tilts up, chin up (towards +Y).</item>
    /// <item>Roll: degrees, positive clockwise from the subject's point of view (right ear down).</item>
    /// <item>Velocities: degrees per second, with the same signs.</item>
    /// <item>Accelerations: g, along X, Y and Z.</item>
    /// <item>X, Y, Z position: mm, relative to the head at calibration.</item>
    /// </list>
    /// <para>Systems without a head sensor leave everything at 0.</para>
    /// </remarks>
    public struct CalibratedHeadData : IEquatable<CalibratedHeadData>
    {
        /// <summary>Acceleration towards the subject's right (+X), in g.</summary>
        public double XAcceleration { get; set; }

        /// <summary>Acceleration upwards (+Y), in g.</summary>
        public double YAcceleration { get; set; }

        /// <summary>Acceleration forwards (+Z), in g.</summary>
        public double ZAcceleration { get; set; }

        /// <summary>Roll in degrees, positive clockwise from the subject's point of view (right ear down).</summary>
        public double Roll { get; set; }

        /// <summary>Pitch in degrees, positive chin up.</summary>
        public double Pitch { get; set; }

        /// <summary>Yaw in degrees, positive turning to the subject's right.</summary>
        public double Yaw { get; set; }

        /// <summary>Roll velocity in degrees per second, positive as Roll.</summary>
        public double RollVelocity { get; set; }

        /// <summary>Pitch velocity in degrees per second, positive as Pitch.</summary>
        public double PitchVelocity { get; set; }

        /// <summary>Yaw velocity in degrees per second, positive as Yaw.</summary>
        public double YawVelocity { get; set; }

        /// <summary>Position towards the subject's right, in mm, relative to the head at calibration.</summary>
        public double X { get; set; }

        /// <summary>Position upwards, in mm, relative to the head at calibration.</summary>
        public double Y { get; set; }

        /// <summary>Position forwards, in mm, relative to the head at calibration.</summary>
        public double Z { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if ( obj is CalibratedHeadData c)
            {
                return c.Equals(this);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(CalibratedHeadData other)
        {
            return (XAcceleration == other.XAcceleration) &&
                (YAcceleration == other.YAcceleration) &&
                (ZAcceleration == other.ZAcceleration) &&
                (Roll == other.Roll) &&
                (Pitch == other.Pitch) &&
                (Yaw == other.Yaw) &&
                (RollVelocity == other.RollVelocity) &&
                (PitchVelocity == other.PitchVelocity) &&
                (YawVelocity == other.YawVelocity) &&
                (X == other.X) &&
                (Y == other.Y) &&
                (Z == other.Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)(XAcceleration * YAcceleration * ZAcceleration * Roll * Pitch * Yaw);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(CalibratedHeadData left, CalibratedHeadData right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(CalibratedHeadData left, CalibratedHeadData right)
        {
            return !(left == right);
        }
    }
}