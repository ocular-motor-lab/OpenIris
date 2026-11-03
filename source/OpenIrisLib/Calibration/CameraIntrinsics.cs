//-----------------------------------------------------------------------
// <copyright file="CameraIntrinsics.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Drawing;
    using Emgu.CV.CvEnum;

    /// <summary>
    /// The intrinsics of a camera, as OpenCV defines them (the camera matrix K, without distortion):
    /// the focal lengths and the principal point, in pixels of an image. Each image carries the
    /// intrinsics in its own pixels: cropping, flipping or transposing the image changes them the same
    /// way (see ImageEye.Intrinsics). Empty (Fx 0) if unknown. See docs/plugin-development/geometry.md.
    /// </summary>
    [Serializable, TypeConverter(typeof(StructConverter<CameraIntrinsics>))]
    public struct CameraIntrinsics : IEquatable<CameraIntrinsics>
    {
        /// <summary>
        /// Initializes the intrinsics.
        /// </summary>
        public CameraIntrinsics(float fx, float fy, float cx, float cy)
        {
            Fx = fx;
            Fy = fy;
            Cx = cx;
            Cy = cy;
        }

        /// <summary>
        /// Gets or sets the horizontal focal length, in pixels.
        /// </summary>
        public float Fx { get; set; }

        /// <summary>
        /// Gets or sets the vertical focal length, in pixels.
        /// </summary>
        public float Fy { get; set; }

        /// <summary>
        /// Gets or sets the horizontal position of the principal point, in pixels.
        /// </summary>
        public float Cx { get; set; }

        /// <summary>
        /// Gets or sets the vertical position of the principal point, in pixels.
        /// </summary>
        public float Cy { get; set; }

        /// <summary>
        /// Gets whether the intrinsics are unknown.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => Fx == 0;

        /// <summary>
        /// The intrinsics of a camera that has not been calibrated, from the size of its sensor and the
        /// pixels per mm at the distance of the eye (weak perspective): the focal length is the pixels
        /// per mm times the distance, and the principal point is the center of the sensor.
        /// </summary>
        /// <param name="sensorSize">Size of the sensor (the whole image the camera can take), in pixels.</param>
        /// <param name="pixPerMm">Pixels per mm at the distance of the eye.</param>
        /// <param name="distanceMm">Distance from the camera to the eye, in mm.</param>
        public static CameraIntrinsics FromPixelScale(Size sensorSize, double pixPerMm, double distanceMm)
        {
            var f = (float)(pixPerMm * distanceMm);
            return new CameraIntrinsics(f, f, (sensorSize.Width - 1) / 2.0f, (sensorSize.Height - 1) / 2.0f);
        }

        /// <summary>
        /// The intrinsics of a part of the image.
        /// </summary>
        /// <param name="origin">Where the part starts in the image.</param>
        public CameraIntrinsics Cropped(Point origin) =>
            IsEmpty ? this : new CameraIntrinsics(Fx, Fy, Cx - origin.X, Cy - origin.Y);

        /// <summary>
        /// The intrinsics of the image flipped. The focal lengths stay positive, as OpenCV expects: a
        /// flip mirrors the axes of the camera, which belongs in its extrinsics.
        /// </summary>
        /// <param name="flipType">How the image is flipped.</param>
        /// <param name="size">Size of the image.</param>
        public CameraIntrinsics Flipped(FlipType flipType, Size size)
        {
            if (IsEmpty) return this;

            var flipX = flipType == FlipType.Horizontal || flipType == FlipType.Both;
            var flipY = flipType == FlipType.Vertical || flipType == FlipType.Both;
            return new CameraIntrinsics(Fx, Fy, flipX ? size.Width - 1 - Cx : Cx, flipY ? size.Height - 1 - Cy : Cy);
        }

        /// <summary>
        /// The intrinsics of the image transposed (x and y swapped).
        /// </summary>
        public CameraIntrinsics Transposed() => IsEmpty ? this : new CameraIntrinsics(Fy, Fx, Cy, Cx);

        /// <inheritdoc/>
        public bool Equals(CameraIntrinsics other) => Fx == other.Fx && Fy == other.Fy && Cx == other.Cx && Cy == other.Cy;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CameraIntrinsics other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => (Fx, Fy, Cx, Cy).GetHashCode();

        /// <summary>
        /// Compares two intrinsics.
        /// </summary>
        public static bool operator ==(CameraIntrinsics x, CameraIntrinsics y) => x.Equals(y);

        /// <summary>
        /// Compares two intrinsics.
        /// </summary>
        public static bool operator !=(CameraIntrinsics x, CameraIntrinsics y) => !x.Equals(y);

        /// <inheritdoc/>
        public override string ToString() => IsEmpty ? "Unknown" : $"f ({Fx}, {Fy}), c ({Cx}, {Cy})";
    }
}
