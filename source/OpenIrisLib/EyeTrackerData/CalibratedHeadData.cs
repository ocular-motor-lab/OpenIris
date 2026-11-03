//-----------------------------------------------------------------------
// <copyright file="CalibratedHeadData.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.Numerics;

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
    /// <item>X, Y, Z position: mm. Where from depends on the system: the Webcam system (FacePose) gives it from the camera, with Z the distance to the camera.</item>
    /// </list>
    /// <para>Systems without a head sensor leave everything at 0.</para>
    /// </remarks>
    public struct CalibratedHeadData : IEquatable<CalibratedHeadData>
    {
        /// <summary>No head data: all 0.</summary>
        public static readonly CalibratedHeadData Empty = default;

        /// <summary>Roll in degrees, positive clockwise from the subject's point of view (right ear down).</summary>
        public double Roll { get; set; }

        /// <summary>Pitch in degrees, positive chin up.</summary>
        public double Pitch { get; set; }

        /// <summary>Yaw in degrees, positive turning to the subject's right.</summary>
        public double Yaw { get; set; }

        /// <summary>Position towards the subject's right, in mm (from where depends on the system).</summary>
        public double X { get; set; }

        /// <summary>Position upwards, in mm (from where depends on the system).</summary>
        public double Y { get; set; }

        /// <summary>Position forwards, in mm (from where depends on the system; the distance to the camera for the Webcam system).</summary>
        public double Z { get; set; }

        /// <summary>
        /// Gets where the face points (forward) and the top of the head points (up), as unit vectors
        /// with the axes of the eyes' Gaze and Up (x right, y up, z forward): (0, 0, 1) and (0, 1, 0)
        /// at zero. From Yaw, Pitch and Roll as Fick angles. A method, so it isn't serialized.
        /// </summary>
        public (Vector3 forward, Vector3 up) GetForwardAndUp() => EyeRotationMath.GazeAndUp(EyeRotationMath.FromFickAngles(Yaw, Pitch, Roll));

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
            return (Roll == other.Roll) &&
                (Pitch == other.Pitch) &&
                (Yaw == other.Yaw) &&
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
            return (Roll, Pitch, Yaw, X, Y, Z).GetHashCode();
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
