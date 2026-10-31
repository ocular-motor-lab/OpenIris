//-----------------------------------------------------------------------
// <copyright file="WebcamEyeLocator.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Finds the eyes in the webcam image and cuts out one image per eye, centered on the eye
    /// corners, level and at a fixed scale, like the image of a head-mounted eye camera.
    /// </summary>
    /// <remarks>
    /// Works like MediaPipe's face and iris tracking: the face mesh gives the eye corners, and the
    /// region of the face for the next frame. The face detector only runs when the face is lost.
    /// Takes ~11 ms per frame (~20 ms when the detector runs). Not thread safe: OpenIris
    /// pre-processes images on a single thread.
    /// </remarks>
    internal sealed class WebcamEyeLocator : IDisposable
    {
        private const float MinFaceScore = 0.5f;

        /// <summary>
        /// Size of the region given to the face mesh, in multiples of the size of the face.
        /// </summary>
        private const float FaceRegionScale = 1.5f;

        private readonly FaceDetector detector = new FaceDetector();
        private readonly FaceMesh mesh = new FaceMesh();
        private readonly EyeFilter left = new EyeFilter();
        private readonly EyeFilter right = new EyeFilter();
        private readonly int eyeImageSize;
        private RotatedSquare? faceRegion;
        private ulong lastFrameNumber;

        /// <param name="eyeImageSize">Width and height of the eye images.</param>
        public WebcamEyeLocator(int eyeImageSize)
        {
            this.eyeImageSize = eyeImageSize;
        }

        /// <summary>
        /// Cuts out the eye images from a webcam image.
        /// </summary>
        /// <param name="image">The webcam image.</param>
        /// <returns>The images of the left and right eye. Black if the face is not found.</returns>
        public EyeCollection<ImageEye?> Process(ImageEye image)
        {
            var timestamp = image.TimeStamp;

            // A video went back: start again.
            if (timestamp.FrameNumber < lastFrameNumber) faceRegion = null;
            lastFrameNumber = timestamp.FrameNumber;

            var points = FindFaceMesh(image.Image, out var newFace);
            if (points is null)
            {
                return new EyeCollection<ImageEye?>(
                    new ImageEye(new Image<Gray, byte>(eyeImageSize, eyeImageSize), Eye.Left, timestamp, new EyeCrop(false, default)),
                    new ImageEye(new Image<Gray, byte>(eyeImageSize, eyeImageSize), Eye.Right, timestamp, new EyeCrop(false, default)));
            }

            // Without mirroring, the subject's right eye is on the left of the image.
            var rightEye = right.Filter(
                points[FaceMesh.RightEyeOuterCorner], points[FaceMesh.RightEyeInnerCorner], timestamp.Seconds, newFace);
            var leftEye = left.Filter(
                points[FaceMesh.LeftEyeInnerCorner], points[FaceMesh.LeftEyeOuterCorner], timestamp.Seconds, newFace);

            return new EyeCollection<ImageEye?>(
                new ImageEye(leftEye.CutOut(image.Image, eyeImageSize), Eye.Left, timestamp, new EyeCrop(true, leftEye)),
                new ImageEye(rightEye.CutOut(image.Image, eyeImageSize), Eye.Right, timestamp, new EyeCrop(true, rightEye)));
        }

        /// <summary>
        /// Finds the face mesh in the region where the face was in the last frame, or with the face
        /// detector if it was lost.
        /// </summary>
        /// <param name="image">The webcam image.</param>
        /// <param name="newFace">Whether the face was just found by the detector.</param>
        /// <returns>The 468 points of the mesh, or null if there is no face.</returns>
        private PointF[]? FindFaceMesh(Image<Gray, byte> image, out bool newFace)
        {
            newFace = false;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (faceRegion is null)
                {
                    var face = detector.Detect(image);
                    if (face is null) return null;

                    var angle = (float)Math.Atan2(face.EyeImageRight.Y - face.EyeImageLeft.Y, face.EyeImageRight.X - face.EyeImageLeft.X);
                    faceRegion = new RotatedSquare(face.Center, face.Size * FaceRegionScale, angle);
                    newFace = true;
                }

                var (score, points) = mesh.Find(image, faceRegion.Value);
                if (score >= MinFaceScore)
                {
                    faceRegion = RegionAround(points);
                    return points;
                }

                // Lost the face: try the detector, once.
                faceRegion = null;
                if (newFace) return null;
            }

            return null;
        }

        /// <summary>
        /// The region for the face mesh in the next frame: level with the outer eye corners and
        /// around all the points.
        /// </summary>
        private static RotatedSquare RegionAround(PointF[] points)
        {
            var a = points[FaceMesh.RightEyeOuterCorner];
            var b = points[FaceMesh.LeftEyeOuterCorner];
            var angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            // Bounding box in face coordinates (rotated to be level).
            var u = points.Select(p => p.X * cos + p.Y * sin).ToArray();
            var v = points.Select(p => -p.X * sin + p.Y * cos).ToArray();
            var centerU = (u.Min() + u.Max()) / 2;
            var centerV = (v.Min() + v.Max()) / 2;
            var size = Math.Max(u.Max() - u.Min(), v.Max() - v.Min());

            return new RotatedSquare(
                new PointF(centerU * cos - centerV * sin, centerU * sin + centerV * cos),
                size * FaceRegionScale,
                angle);
        }

        public void Dispose()
        {
            detector.Dispose();
            mesh.Dispose();
        }

        /// <summary>
        /// Smooths where an eye image is cut out, so it holds still while the head does.
        /// </summary>
        private sealed class EyeFilter
        {
            // Strong smoothing when still, little lag when the head moves fast.
            private readonly OneEuroFilter x = new OneEuroFilter(1.5, 5.0);
            private readonly OneEuroFilter y = new OneEuroFilter(1.5, 5.0);
            private readonly OneEuroFilter width = new OneEuroFilter(0.5, 1.0);
            private readonly OneEuroFilter angle = new OneEuroFilter(0.5, 1.0);

            /// <summary>
            /// Adds the eye corners of a new frame and returns the region of the eye image.
            /// </summary>
            /// <param name="cornerLeft">Corner on the left of the image.</param>
            /// <param name="cornerRight">Corner on the right of the image.</param>
            /// <param name="seconds">Time of the frame.</param>
            /// <param name="reset">Start again, without the previous frames.</param>
            public RotatedSquare Filter(PointF cornerLeft, PointF cornerRight, double seconds, bool reset)
            {
                var dx = cornerRight.X - cornerLeft.X;
                var dy = cornerRight.Y - cornerLeft.Y;
                var newX = (cornerLeft.X + cornerRight.X) / 2;
                var newY = (cornerLeft.Y + cornerRight.Y) / 2;
                var newWidth = Math.Sqrt(dx * dx + dy * dy);
                var newAngle = Math.Atan2(dy, dx);

                if (reset)
                {
                    x.Reset(newX, seconds);
                    y.Reset(newY, seconds);
                    width.Reset(newWidth, seconds);
                    angle.Reset(newAngle, seconds);
                }
                else
                {
                    x.Filter(newX, seconds, width.Value);
                    y.Filter(newY, seconds, width.Value);
                    width.Filter(newWidth, seconds, width.Value);
                    angle.Filter(newAngle, seconds);
                }

                return new RotatedSquare(
                    new PointF((float)x.Value, (float)y.Value),
                    (float)width.Value * EyeCrop.SizeInEyeWidths,
                    (float)angle.Value);
            }
        }
    }
}
