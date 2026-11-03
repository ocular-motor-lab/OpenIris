//-----------------------------------------------------------------------
// <copyright file="HeadModel.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Numerics;

    /// <summary>
    /// Where the eyes are in the head, parallel to the eye model (EyePhysicalModel). The origin of the
    /// head is the midpoint between the centers of the eyes.
    /// </summary>
    /// <remarks>
    /// With the head's pose in a camera's coordinates (rotation R, the midpoint's position), the center
    /// of an eye in that camera is the midpoint + R · that eye's vector. Head-mounted cameras move with
    /// the head, so there the eyes don't move in the images when the head turns. See
    /// docs/plugin-development/geometry.md.
    /// </remarks>
    [Serializable]
    public class HeadModel
    {
        /// <summary>
        /// Gets or sets the vector from the midpoint between the eyes to the center of the left eye, in
        /// the left eye's camera: x, y in pixels, z (depth) in the same pixel units. Zero if unknown.
        /// </summary>
        public Vector3 LeftEye { get; set; }

        /// <summary>
        /// Gets or sets the vector from the midpoint between the eyes to the center of the right eye, in
        /// the right eye's camera: x, y in pixels, z (depth) in the same pixel units. Zero if unknown.
        /// </summary>
        public Vector3 RightEye { get; set; }

        /// <summary>
        /// Gets or sets the distance between the centers of the eyes, in mm. A typical adult value by
        /// default.
        /// </summary>
        public double InterpupillaryDistance { get; set; } = 63;

        /// <summary>
        /// Gets or sets the vector from the point in the neck the head turns around to the midpoint
        /// between the eyes, in mm, in the head's frame (x forward, y to the subject's left, z up), as
        /// the neck model of virtual reality headsets. It places the eyes when only the head's rotation
        /// is known. Typical values by default.
        /// </summary>
        public Vector3 NeckToEyes { get; set; } = new Vector3(80, 0, 75);

        /// <summary>
        /// Makes a copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public HeadModel Copy() => (HeadModel)MemberwiseClone();
    }
}
