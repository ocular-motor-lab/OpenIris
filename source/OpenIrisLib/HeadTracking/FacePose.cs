//-----------------------------------------------------------------------
// <copyright file="FacePose.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// The pose of the head (orientation and position) relative to a camera, from six points of the
    /// face in its image, for systems that find the face, such as the Webcam system. It fits a
    /// generic face (solvePnP), so it needs no calibration; the camera's field of view only changes
    /// the distance.
    /// </summary>
    /// <remarks>
    /// The rotation is relative to facing the camera, in the axes of the face: X to the subject's
    /// left, Y up, Z out of the face (right-handed, for the math). The angles follow the convention of
    /// CalibratedHeadData (positive to the subject's right, up and clockwise), in Fick order: yaw,
    /// then pitch, then roll.
    /// </remarks>
    public static class FacePose
    {
        /// <summary>
        /// The six points of the generic face, in mm, in the axes of the face (X to the subject's
        /// left, Y up, Z out of the face), with the nose tip at the origin: nose tip, chin, outer
        /// corners of the right and left eyes, right and left corners of the mouth. A common model,
        /// scaled to 90 mm between the outer eye corners.
        /// </summary>
        private static readonly MCvPoint3D32f[] Model =
        {
            new MCvPoint3D32f(0, 0, 0),
            new MCvPoint3D32f(0, -66, -13),
            new MCvPoint3D32f(-45, 34, -27),
            new MCvPoint3D32f(45, 34, -27),
            new MCvPoint3D32f(-30, -30, -25),
            new MCvPoint3D32f(30, -30, -25),
        };

        /// <summary>
        /// Estimates the pose of the head.
        /// </summary>
        /// <param name="points">The six points in the image, in pixels, in the order of the model:
        /// nose tip, chin, outer corner of the right eye, outer corner of the left eye, right and left
        /// corners of the mouth (right and left of the subject).</param>
        /// <param name="imageSize">Size of the image.</param>
        /// <param name="horizontalFieldOfView">Horizontal field of view of the camera, in degrees.</param>
        /// <returns>The head data with the pose, or null if it couldn't be estimated.</returns>
        public static HeadData? Estimate(PointF[] points, Size imageSize, double horizontalFieldOfView)
        {
            if (points is null || points.Length != Model.Length) throw new ArgumentException("Six points are needed.", nameof(points));

            var focal = imageSize.Width / 2.0 / Math.Tan(horizontalFieldOfView * Math.PI / 360);
            using var camera = new Matrix<double>(new double[,] { { focal, 0, imageSize.Width / 2.0 }, { 0, focal, imageSize.Height / 2.0 }, { 0, 0, 1 } });
            using var distortion = new Matrix<double>(4, 1);
            using var rotationVector = new Matrix<double>(3, 1);
            using var translation = new Matrix<double>(3, 1);
            if (!CvInvoke.SolvePnP(Model, points, camera, distortion, rotationVector, translation, false, SolvePnpMethod.Iterative)) return null;

            using var cameraRotation = new Matrix<double>(3, 3);
            CvInvoke.Rodrigues(rotationVector, cameraRotation);

            // Relative to facing the camera: the camera's axes are x right in the image (the subject's
            // left), y down and z away from the camera, so facing it is diag(1, -1, -1).
            var rotation = new double[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    rotation[row * 3 + column] = (row == 0 ? 1 : -1) * cameraRotation[row, column];
                }
            }

            return new HeadData
            {
                FaceRotation = rotation,

                // The nose tip, from the camera: to the subject's right, up, and the distance.
                FacePosition = new[] { -translation[0, 0], -translation[1, 0], translation[2, 0] },
            };
        }

        /// <summary>
        /// The calibrated head data from a pose: the angles relative to the reference pose (or to
        /// facing the camera, without one), and the position from the camera.
        /// </summary>
        /// <param name="pose">Head data with the pose, or null if the face wasn't found.</param>
        /// <param name="reference">Head data with the reference pose, or null.</param>
        /// <returns>The calibrated head data. NaN if there is no pose.</returns>
        public static CalibratedHeadData Calibrated(HeadData? pose, HeadData? reference)
        {
            if (pose?.FaceRotation is null || pose.FacePosition is null)
            {
                return new CalibratedHeadData { Roll = double.NaN, Pitch = double.NaN, Yaw = double.NaN, X = double.NaN, Y = double.NaN, Z = double.NaN };
            }

            // Relative to the reference: reference transposed times the pose.
            var r = pose.FaceRotation;
            if (reference?.FaceRotation is double[] q)
            {
                var relative = new double[9];
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        relative[row * 3 + column] = q[0 * 3 + row] * r[0 * 3 + column] + q[1 * 3 + row] * r[1 * 3 + column] + q[2 * 3 + row] * r[2 * 3 + column];
                    }
                }

                r = relative;
            }

            // Fick angles from where the face points (forward, its Z axis) and where its right
            // (-X) points. Positive: towards the subject's right, chin up, right ear down.
            var forwardX = r[2];
            var forwardY = r[5];
            var forwardZ = r[8];
            var rightY = -r[3];
            const double degrees = 180 / Math.PI;

            return new CalibratedHeadData
            {
                Yaw = Math.Atan2(-forwardX, forwardZ) * degrees,
                Pitch = Math.Asin(Math.Max(-1, Math.Min(1, forwardY))) * degrees,
                Roll = Math.Asin(Math.Max(-1, Math.Min(1, -rightY))) * degrees,
                X = pose.FacePosition[0],
                Y = pose.FacePosition[1],
                Z = pose.FacePosition[2],
            };
        }
    }
}
