//-----------------------------------------------------------------------
// <copyright file="CameraExtrinsics.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Numerics;

    /// <summary>
    /// Where a camera is and how it's turned, as OpenCV defines its extrinsics: the rotation (rvec,
    /// a Rodrigues rotation vector in radians) and the translation (tvec, in mm) that take a point
    /// from the head frame to the camera's, X_camera = R X_head + t.
    /// </summary>
    /// <remarks>
    /// <para>The head frame is the one of the rotation math (EyeRotationMath, Haslwanter 1995),
    /// right-handed as OpenCV needs: x forward, y to the subject's left, z up, with the origin at the
    /// center of the eye the camera looks at (between the eyes for a camera that sees both). The
    /// camera frame is OpenCV's, in the image as OpenIris shows it (after the camera orientation):
    /// x right, y down, z forward from the camera.</para>
    /// <para>Empty (tvec 0, a camera at the center of the eye) if unknown: then the camera is taken
    /// to face the eye with the image's up as the head's up (Facing). See
    /// docs/plugin-development/geometry.md.</para>
    /// </remarks>
    [Serializable, TypeConverter(typeof(StructConverter<CameraExtrinsics>))]
    public struct CameraExtrinsics : IEquatable<CameraExtrinsics>
    {
        /// <summary>
        /// Initializes the extrinsics.
        /// </summary>
        public CameraExtrinsics(float rx, float ry, float rz, float tx, float ty, float tz)
        {
            Rx = rx;
            Ry = ry;
            Rz = rz;
            Tx = tx;
            Ty = ty;
            Tz = tz;
        }

        /// <summary>Gets or sets the rotation vector (rvec), x, in radians.</summary>
        public float Rx { get; set; }

        /// <summary>Gets or sets the rotation vector (rvec), y, in radians.</summary>
        public float Ry { get; set; }

        /// <summary>Gets or sets the rotation vector (rvec), z, in radians.</summary>
        public float Rz { get; set; }

        /// <summary>Gets or sets the translation (tvec), x, in mm.</summary>
        public float Tx { get; set; }

        /// <summary>Gets or sets the translation (tvec), y, in mm.</summary>
        public float Ty { get; set; }

        /// <summary>Gets or sets the translation (tvec), z, in mm.</summary>
        public float Tz { get; set; }

        /// <summary>
        /// Gets whether the extrinsics are unknown.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => Tx == 0 && Ty == 0 && Tz == 0;

        /// <summary>
        /// A camera facing the eye at the given distance, as OpenIris assumes for cameras without
        /// extrinsics: the head's forward points at the camera, the subject's left is to the right of
        /// the image and up is up (the eye as seen by someone facing the subject).
        /// </summary>
        /// <param name="distanceMm">Distance from the camera to the center of the eye, in mm.</param>
        public static CameraExtrinsics Facing(double distanceMm)
        {
            // R has rows (0, 1, 0), (0, 0, -1), (-1, 0, 0): 120° about (1, 1, -1) / sqrt(3).
            var r = (float)(2 * Math.PI / 3 / Math.Sqrt(3));
            return new CameraExtrinsics(r, r, -r, 0, 0, (float)distanceMm);
        }

        /// <summary>
        /// Gets the rotation from the head frame to the camera's (Rodrigues of rvec).
        /// </summary>
        [Browsable(false)]
        public Quaternion Rotation
        {
            get
            {
                var rvec = new Vector3(Rx, Ry, Rz);
                var angle = rvec.Length();
                return angle < 1e-12 ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(rvec / angle, angle);
            }
        }

        /// <summary>
        /// Gets the head's up (z) in camera coordinates.
        /// </summary>
        [Browsable(false)]
        public Vector3 HeadUp => Vector3.Transform(Vector3.UnitZ, Rotation);

        /// <inheritdoc/>
        public bool Equals(CameraExtrinsics other) =>
            Rx == other.Rx && Ry == other.Ry && Rz == other.Rz && Tx == other.Tx && Ty == other.Ty && Tz == other.Tz;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CameraExtrinsics other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => (Rx, Ry, Rz, Tx, Ty, Tz).GetHashCode();

        /// <summary>
        /// Compares two extrinsics.
        /// </summary>
        public static bool operator ==(CameraExtrinsics x, CameraExtrinsics y) => x.Equals(y);

        /// <summary>
        /// Compares two extrinsics.
        /// </summary>
        public static bool operator !=(CameraExtrinsics x, CameraExtrinsics y) => !x.Equals(y);

        /// <inheritdoc/>
        public override string ToString() => IsEmpty ? "Unknown" : $"rvec ({Rx}, {Ry}, {Rz}), tvec ({Tx}, {Ty}, {Tz}) mm";
    }
}
