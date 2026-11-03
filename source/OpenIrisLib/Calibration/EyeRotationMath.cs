//-----------------------------------------------------------------------
// <copyright file="EyeRotationMath.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Numerics;

    /// <summary>
    /// The rotation of the eye from the reference, from the pupil on the eye model (see
    /// docs/plugin-development/geometry.md#rotations).
    /// </summary>
    /// <remarks>
    /// <para>The frame is the head-fixed one of 3D oculomotor research (Haslwanter 1995): right-handed,
    /// x forward along the line of sight at the reference, y to the subject's left, z up. Up is the
    /// image's up, made perpendicular to the line of sight at the reference.</para>
    /// <para>The direction of the line of sight comes from the pupil on the eye globe, with weak
    /// perspective, turned by the eye model's angle kappa (from the pupillary axis to the visual axis). The rotation that takes the reference line of sight to the current one follows
    /// Listing's law, with Listing's plane perpendicular to the line of sight at the reference: its
    /// axis is perpendicular to it. The torsion, when known, is a rotation about the current line of
    /// sight after that, positive clockwise as the subject sees it.</para>
    /// </remarks>
    public static class EyeRotationMath
    {
        private const double RadToDeg = 180 / Math.PI;

        /// <summary>
        /// The direction of the line of sight in camera coordinates (OpenCV's: x right, y down, z
        /// forward from the camera). The pupillary axis goes from the center of the globe through the
        /// pupil, which is on the side of the globe facing the camera; the line of sight is that axis
        /// turned by the eye model's angle kappa (KappaAngleH towards the nose, KappaAngleV up, in
        /// degrees). Kappa is applied about the axes of the image (up, and the subject's left-right),
        /// exact for the eye near the camera's axis. NaN if the pupil is outside the globe.
        /// </summary>
        /// <param name="pupil">Center of the pupil in the image, in pixels.</param>
        /// <param name="eyeModel">The eye globe in the same image.</param>
        /// <param name="whichEye">Which eye, for the direction of the nose.</param>
        public static Vector3 LineOfSight(PointF pupil, EyePhysicalModel eyeModel, Eye whichEye)
        {
            var x = (pupil.X - eyeModel.Center.X) / eyeModel.Radius;
            var y = (pupil.Y - eyeModel.Center.Y) / eyeModel.Radius;
            var z2 = 1 - x * x - y * y;
            if (z2 < 0) return new Vector3(float.NaN);

            var pupillaryAxis = new Vector3(x, y, -(float)Math.Sqrt(z2));
            if (eyeModel.KappaAngleH == 0 && eyeModel.KappaAngleV == 0) return pupillaryAxis;

            // In camera coordinates, up is -y and the subject's left is +x (the image shows the eye as
            // someone facing the subject sees it). Right-handed about up turns the line of sight to the
            // subject's left, which is the nose for the right eye; right-handed about left turns it down.
            var up = -Vector3.UnitY;
            var left = Vector3.UnitX;
            var nasal = whichEye == Eye.Right ? 1 : -1;
            var kappa =
                Quaternion.CreateFromAxisAngle(up, (float)(nasal * eyeModel.KappaAngleH / RadToDeg)) *
                Quaternion.CreateFromAxisAngle(left, (float)(-eyeModel.KappaAngleV / RadToDeg));
            return Vector3.Transform(pupillaryAxis, kappa);
        }

        /// <summary>
        /// The rotation of the eye from the reference, in the head-fixed frame (see the class).
        /// </summary>
        /// <param name="reference">Line of sight at the reference, in camera coordinates (LineOfSight).</param>
        /// <param name="current">Current line of sight, in camera coordinates.</param>
        /// <param name="torsionDeg">Torsion in degrees, positive clockwise as the subject sees it, or NaN
        /// if unknown (then Listing's law alone).</param>
        public static Quaternion FromLinesOfSight(Vector3 reference, Vector3 current, double torsionDeg)
        {
            // The head-fixed axes in camera coordinates: forward is the reference line of sight, up
            // is the image's up (-y) made perpendicular to it, left completes the right-handed frame.
            var forward = Vector3.Normalize(reference);
            var up = Vector3.Normalize(-Vector3.UnitY - Vector3.Dot(-Vector3.UnitY, forward) * forward);
            var left = Vector3.Cross(up, forward);

            var gaze = Vector3.Normalize(new Vector3(Vector3.Dot(current, forward), Vector3.Dot(current, left), Vector3.Dot(current, up)));

            // Listing's law: the shortest rotation from the reference (x) to the current line of sight.
            var axis = Vector3.Cross(Vector3.UnitX, gaze);
            var sin = axis.Length();
            var listing = sin < 1e-9f
                ? Quaternion.Identity
                : Quaternion.CreateFromAxisAngle(axis / sin, (float)Math.Atan2(sin, gaze.X));

            if (double.IsNaN(torsionDeg)) return listing;

            // The torsion about the current line of sight, after Listing's rotation.
            return Quaternion.CreateFromAxisAngle(gaze, (float)(torsionDeg / RadToDeg)) * listing;
        }

        /// <summary>
        /// The Fick angles of a rotation in the head-fixed frame (horizontal about the head's vertical
        /// axis, then vertical about the rotated horizontal axis, then torsion about the line of sight),
        /// in degrees with OpenIris's signs: positive to the subject's right, up, and clockwise as the
        /// subject sees it.
        /// </summary>
        /// <param name="rotation">The rotation in the head-fixed frame.</param>
        public static (double horizontal, double vertical, double torsion) FickAngles(Quaternion rotation)
        {
            var m = Matrix4x4.CreateFromQuaternion(rotation);

            // System.Numerics uses row vectors: m.Mij is the element (j, i) of the rotation matrix R
            // that takes column vectors. R = Rz(h) Ry(v) Rx(t), right-handed about z (up), y (left)
            // and x (forward).
            double R(int row, int column) => (row, column) switch
            {
                (0, 0) => m.M11, (1, 0) => m.M12, (2, 0) => m.M13,
                (2, 1) => m.M23, (2, 2) => m.M33,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var h = Math.Atan2(R(1, 0), R(0, 0));
            var v = Math.Asin(Math.Max(-1, Math.Min(1, -R(2, 0))));
            var t = Math.Atan2(R(2, 1), R(2, 2));

            // Right-handed about up is leftward, about left is downward, about forward is clockwise
            // as the subject sees it.
            return (-h * RadToDeg, -v * RadToDeg, t * RadToDeg);
        }
    }
}
