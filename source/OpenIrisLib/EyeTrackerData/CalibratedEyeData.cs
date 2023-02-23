//-----------------------------------------------------------------------
// <copyright file="CalibratedEyeData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
    /// Calibrated data. Calculated from the raw data using the CalibrationInfo.
    /// </summary>
    public struct CalibratedEyeData : IEquatable<CalibratedEyeData>
    {
        // TODO: MOVE TO TRANSFORMS then all the simple values can be getters out of the matrix.

        /// <summary>
        /// Gets or sets the horizontal position of the eye, in degrees, relative to the reference
        /// position during calibration.
        /// </summary>
        public double HorizontalPosition { get; set; }

        /// <summary>
        /// Gets or sets the vertical position of the eye, in degrees, relative to the reference
        /// position during calibration.
        /// </summary>
        public double VerticalPosition { get; set; }

        /// <summary>
        /// Gets or sets the torsional position of the eye, in degrees, relative to the reference
        /// position during calibration.
        /// </summary>
        public double TorsionalPosition { get; set; }

        /// <summary>
        /// Gets or sets the pupil Area, in squared milimeters.
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
            if (!(obj is CalibratedEyeData))
                return false;

            return Equals((CalibratedEyeData)obj);
        }

        /// <summary>
        /// Compares the object with another CalibratedEyeData object and checks if they are equal.
        /// </summary>
        /// <param name="other">Another CalibratedEyeData object.</param>
        /// <returns>True if they are equal.</returns>
        public bool Equals(CalibratedEyeData other)
        {
            if (this.HorizontalPosition != other.HorizontalPosition)
            {
                return false;
            }

            if (this.VerticalPosition != other.VerticalPosition)
            {
                return false;
            }

            if (this.PupilArea != other.PupilArea)
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
    /// Calibrated head data. Calculated from the raw data using the HeadCalibration.
    /// TODO: define reference frames unanbiguosly
    /// TODO: MOVE TO 4D MATRICES "transforms"
    /// </summary>
    public struct CalibratedHeadData : IEquatable<CalibratedHeadData>
    {
        public double XAcceleration { get; set; }
        public double YAcceleration { get; set; }
        public double ZAcceleration { get; set; }

        public double Roll { get; set; }
        public double Pitch { get; set; }
        public double Yaw { get; set; }

        public double RollVelocity { get; set; }
        public double PitchVelocity { get; set; }
        public double YawVelocity { get; set; }

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
                (ZAcceleration == other.XAcceleration) &&
                (Roll == other.Roll) &&
                (Pitch == other.Pitch) &&
                (Yaw == other.Yaw) &&
                (RollVelocity == other.RollVelocity) &&
                (PitchVelocity == other.PitchVelocity) &&
                (YawVelocity == other.YawVelocity);
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