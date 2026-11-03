//-----------------------------------------------------------------------
// <copyright file="LightPosition.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;

    /// <summary>
    /// Where a light (an LED that makes a corneal reflection) is, in mm, in the head frame (x forward,
    /// y to the subject's left, z up; see CameraExtrinsics). In the system settings the origin is the
    /// point between the eyes; in a calibration, the center of that eye. (0, 0, 0) if unknown.
    /// </summary>
    [Serializable, TypeConverter(typeof(StructConverter<LightPosition>))]
    public struct LightPosition : IEquatable<LightPosition>
    {
        /// <summary>
        /// Initializes the position.
        /// </summary>
        public LightPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Gets or sets the position forward, in mm.</summary>
        public float X { get; set; }

        /// <summary>Gets or sets the position to the subject's left, in mm.</summary>
        public float Y { get; set; }

        /// <summary>Gets or sets the position up, in mm.</summary>
        public float Z { get; set; }

        /// <summary>
        /// Gets whether the position is unknown.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => X == 0 && Y == 0 && Z == 0;

        /// <inheritdoc/>
        public bool Equals(LightPosition other) => X == other.X && Y == other.Y && Z == other.Z;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is LightPosition other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => (X, Y, Z).GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => IsEmpty ? "Unknown" : $"({X}, {Y}, {Z}) mm";
    }
}
