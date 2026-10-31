//-----------------------------------------------------------------------
// <copyright file="EyeRotation.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.EyeTrackingSystems
{
#nullable enable

    using System;

    /// <summary>
    /// Orientation of the eye: a rotation from the primary position, as a unit quaternion, with
    /// conversions to and from the usual coordinate systems. Immutable, so it can be shared between
    /// the window and the cameras without locks.
    ///
    /// Head-fixed axes, right-handed: X along the line of sight in the primary position, Y interaural
    /// (to the subject's left), Z vertical (up). Angles in degrees. In every system horizontal is
    /// positive to the left (rotation about +Z), vertical positive up (rotation about -Y), and torsion
    /// is the rotation about the line of sight (about -X), applied last, in the eye's own frame. The
    /// signs are the ones of OpenIris's data, so the synthetic eyes give back what was set.
    ///
    /// - Listing: direction of the gaze (Angle, 0 = left, 90 = up), how far (Eccentricity) and
    ///   Torsion. Listing's law is Torsion = 0: the axis of rotation is in the frontal plane.
    /// - Fick: horizontal about the head-fixed vertical axis, then vertical about the eye-fixed
    ///   horizontal axis, then torsion.
    /// - Helmholtz: vertical about the head-fixed horizontal axis, then horizontal about the
    ///   eye-fixed vertical axis, then torsion.
    /// - Hess and Harms: two ways of giving the direction of the gaze; their torsion is Listing's.
    ///   Hess is the eccentricity split in horizontal and vertical (H = e cos a, V = e sin a);
    ///   Harms is the angle of the gaze direction projected on the horizontal and vertical planes.
    /// - Rotation vector: tan(angle / 2) times the axis of rotation.
    /// </summary>
    public sealed class EyeRotation
    {
        private const double ToRadians = Math.PI / 180;
        private const double ToDegrees = 180 / Math.PI;

        /// <summary>The primary position.</summary>
        public static readonly EyeRotation Primary = new EyeRotation(1, 0, 0, 0);

        private EyeRotation(double w, double x, double y, double z)
        {
            // Unit length, and W >= 0 (q and -q are the same rotation).
            var norm = Math.Sqrt(w * w + x * x + y * y + z * z) * (w < 0 ? -1 : 1);
            W = w / norm;
            X = x / norm;
            Y = y / norm;
            Z = z / norm;
        }

        /// <summary>Quaternion components.</summary>
        public double W { get; }
        /// <summary>Quaternion components.</summary>
        public double X { get; }
        /// <summary>Quaternion components.</summary>
        public double Y { get; }
        /// <summary>Quaternion components.</summary>
        public double Z { get; }

        private static EyeRotation About(double x, double y, double z, double degrees)
        {
            var half = degrees * ToRadians / 2;
            var s = Math.Sin(half);
            return new EyeRotation(Math.Cos(half), x * s, y * s, z * s);
        }

        private static EyeRotation Torsional(double degrees) => About(-1, 0, 0, degrees);
        private static EyeRotation Horizontal(double degrees) => About(0, 0, 1, degrees);
        private static EyeRotation Vertical(double degrees) => About(0, -1, 0, degrees);

        private static EyeRotation Multiply(EyeRotation a, EyeRotation b) => new EyeRotation(
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W);

        private EyeRotation Inverse() => new EyeRotation(W, -X, -Y, -Z);

        /// <summary>
        /// Rotation matrix, M[row, column].
        /// </summary>
        public double[,] Matrix()
        {
            double w = W, x = X, y = Y, z = Z;
            return new double[,]
            {
                { 1 - 2 * (y * y + z * z), 2 * (x * y - w * z), 2 * (x * z + w * y) },
                { 2 * (x * y + w * z), 1 - 2 * (x * x + z * z), 2 * (y * z - w * x) },
                { 2 * (x * z - w * y), 2 * (y * z + w * x), 1 - 2 * (x * x + y * y) },
            };
        }

        private static double Clamp(double value) => Math.Max(-1, Math.Min(1, value));

        #region Listing, Hess and Harms

        /// <summary>From Listing coordinates.</summary>
        public static EyeRotation FromListing(double angle, double eccentricity, double torsion)
        {
            var a = angle * ToRadians;
            // The axis is in the frontal plane, perpendicular to the direction of the gaze.
            return Multiply(About(0, -Math.Sin(a), Math.Cos(a), eccentricity), Torsional(torsion));
        }

        /// <summary>To Listing coordinates. At the primary position the angle is 0.</summary>
        public (double angle, double eccentricity, double torsion) ToListing()
        {
            var m = Matrix();
            // Direction of the gaze: where the line of sight (X) points now.
            double gx = m[0, 0], gy = m[1, 0], gz = m[2, 0];
            var eccentricity = Math.Acos(Clamp(gx)) * ToDegrees;
            var angle = eccentricity < 1e-9 ? 0 : Math.Atan2(gz, gy) * ToDegrees;

            // What is left after the Listing rotation is the torsion.
            var rest = Multiply(FromListing(angle, eccentricity, 0).Inverse(), this);
            var torsion = -2 * Math.Atan2(rest.X, rest.W) * ToDegrees;
            return (angle, eccentricity, torsion);
        }

        /// <summary>From Hess coordinates.</summary>
        public static EyeRotation FromHess(double horizontal, double vertical, double torsion) =>
            FromListing(Math.Atan2(vertical, horizontal) * ToDegrees, Math.Sqrt(horizontal * horizontal + vertical * vertical), torsion);

        /// <summary>To Hess coordinates.</summary>
        public (double horizontal, double vertical, double torsion) ToHess()
        {
            var (angle, eccentricity, torsion) = ToListing();
            return (eccentricity * Math.Cos(angle * ToRadians), eccentricity * Math.Sin(angle * ToRadians), torsion);
        }

        /// <summary>From Harms coordinates (horizontal and vertical between -90 and 90).</summary>
        public static EyeRotation FromHarms(double horizontal, double vertical, double torsion)
        {
            // Direction of the gaze, (1, tan h, tan v) normalized.
            var gy = Math.Tan(horizontal * ToRadians);
            var gz = Math.Tan(vertical * ToRadians);
            var gx = 1 / Math.Sqrt(1 + gy * gy + gz * gz);
            return FromListing(Math.Atan2(gz, gy) * ToDegrees, Math.Acos(gx) * ToDegrees, torsion);
        }

        /// <summary>To Harms coordinates.</summary>
        public (double horizontal, double vertical, double torsion) ToHarms()
        {
            var m = Matrix();
            var (_, _, torsion) = ToListing();
            return (Math.Atan2(m[1, 0], m[0, 0]) * ToDegrees, Math.Atan2(m[2, 0], m[0, 0]) * ToDegrees, torsion);
        }

        #endregion

        #region Fick and Helmholtz

        /// <summary>From Fick coordinates.</summary>
        public static EyeRotation FromFick(double horizontal, double vertical, double torsion) =>
            Multiply(Multiply(Horizontal(horizontal), Vertical(vertical)), Torsional(torsion));

        /// <summary>To Fick coordinates.</summary>
        public (double horizontal, double vertical, double torsion) ToFick()
        {
            // M = Rz(h) Ry(-v) Rx(-t).
            var m = Matrix();
            return (
                Math.Atan2(m[1, 0], m[0, 0]) * ToDegrees,
                Math.Asin(Clamp(m[2, 0])) * ToDegrees,
                -Math.Atan2(m[2, 1], m[2, 2]) * ToDegrees);
        }

        /// <summary>From Helmholtz coordinates.</summary>
        public static EyeRotation FromHelmholtz(double horizontal, double vertical, double torsion) =>
            Multiply(Multiply(Vertical(vertical), Horizontal(horizontal)), Torsional(torsion));

        /// <summary>To Helmholtz coordinates.</summary>
        public (double horizontal, double vertical, double torsion) ToHelmholtz()
        {
            // M = Ry(-v) Rz(h) Rx(-t).
            var m = Matrix();
            return (
                Math.Asin(Clamp(m[1, 0])) * ToDegrees,
                -Math.Atan2(-m[2, 0], m[0, 0]) * ToDegrees,
                -Math.Atan2(-m[1, 2], m[1, 1]) * ToDegrees);
        }

        #endregion

        #region Rotation vector

        /// <summary>From a rotation vector, tan(angle / 2) times the axis.</summary>
        public static EyeRotation FromRotationVector(double x, double y, double z) => new EyeRotation(1, x, y, z);

        /// <summary>To a rotation vector. Rotations of 180 degrees have no finite vector.</summary>
        public (double x, double y, double z) ToRotationVector() =>
            W < 1e-9 ? (double.NaN, double.NaN, double.NaN) : (X / W, Y / W, Z / W);

        #endregion
    }
}
