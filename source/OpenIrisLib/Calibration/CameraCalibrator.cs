//-----------------------------------------------------------------------
// <copyright file="CameraCalibrator.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// One view of the dot grid for the camera calibration: the target at a known position along the
    /// head's forward axis, the dots found in the image (averaged over frames) and the intrinsics of
    /// that image, which say where the image is in the camera (its window and orientation).
    /// </summary>
    public sealed class CameraCalibrationCapture
    {
        /// <summary>
        /// Initializes a capture.
        /// </summary>
        public CameraCalibrationCapture(double targetPositionMm, DotGrid grid, Size imageSize, CameraIntrinsics imageIntrinsics, int frames)
        {
            TargetPositionMm = targetPositionMm;
            Grid = grid;
            ImageSize = imageSize;
            ImageIntrinsics = imageIntrinsics;
            Frames = frames;
        }

        /// <summary>Gets the position of the target, in mm forward (away from the face, toward the camera).</summary>
        public double TargetPositionMm { get; }

        /// <summary>Gets the dots found.</summary>
        public DotGrid Grid { get; }

        /// <summary>Gets the size of the image, in pixels.</summary>
        public Size ImageSize { get; }

        /// <summary>Gets the intrinsics of the camera in the pixels of the image (ImageEye.Intrinsics).</summary>
        public CameraIntrinsics ImageIntrinsics { get; }

        /// <summary>Gets the number of frames averaged.</summary>
        public int Frames { get; }
    }

    /// <summary>
    /// What the camera calibration needs besides the views: the measured grid and where the eye is.
    /// </summary>
    public sealed class CameraCalibrationOptions
    {
        /// <summary>Gets or sets the grid used.</summary>
        public CameraCalibrationTarget Target { get; set; } = CameraCalibrationTarget.Printed[0];

        /// <summary>Gets or sets the measured distance between the outer dots, horizontally, in mm.</summary>
        public double MeasuredSpanXMm { get; set; } = 30;

        /// <summary>Gets or sets the measured distance between the outer dots, vertically, in mm.</summary>
        public double MeasuredSpanYMm { get; set; } = 30;

        /// <summary>
        /// Gets or sets how far the center of rotation of the eye is behind the target at position 0,
        /// in mm. About 10 mm with the target where the pupil would be.
        /// </summary>
        public double EyeCenterBehindTargetMm { get; set; } = 10;

        /// <summary>
        /// Gets or sets the distance from the camera to the target, in mm, assumed when all the views
        /// are at the same position (then the distance can't be measured).
        /// </summary>
        public double AssumedDistanceMm { get; set; } = CameraIntrinsics.AssumedDistanceToEyeMm;

        /// <summary>Gets or sets whether to estimate the radial distortion (k1).</summary>
        public bool EstimateDistortion { get; set; } = true;

        /// <summary>Gets the measured distance between dots, horizontally, in mm.</summary>
        public double PitchXMm => MeasuredSpanXMm / (Target.DotsPerSide - 1);

        /// <summary>Gets the measured distance between dots, vertically, in mm.</summary>
        public double PitchYMm => MeasuredSpanYMm / (Target.DotsPerSide - 1);
    }

    /// <summary>
    /// The result of a camera calibration: the values for the settings and the report.
    /// </summary>
    public sealed class CameraCalibrationResult
    {
        internal CameraCalibrationResult(CameraIntrinsics intrinsics, CameraExtrinsics extrinsics, double pixPerMm, double k1, double rmsPixels, double[] rmsPerCapture, bool distanceMeasured, string report)
        {
            Intrinsics = intrinsics;
            Extrinsics = extrinsics;
            PixPerMm = pixPerMm;
            K1 = k1;
            RmsPixels = rmsPixels;
            RmsPerCapture = rmsPerCapture;
            DistanceMeasured = distanceMeasured;
            Report = report;
        }

        /// <summary>Gets the intrinsics, as the CameraIntrinsics setting wants them (whole sensor, before the orientation).</summary>
        public CameraIntrinsics Intrinsics { get; }

        /// <summary>Gets the extrinsics, as the CameraExtrinsics setting wants them.</summary>
        public CameraExtrinsics Extrinsics { get; }

        /// <summary>Gets the pixels per mm at the target at position 0 (where the pupil would be).</summary>
        public double PixPerMm { get; }

        /// <summary>Gets the radial distortion coefficient k1 (OpenCV's), not used by OpenIris yet (#78).</summary>
        public double K1 { get; }

        /// <summary>Gets the error of the fit, root mean square, in pixels.</summary>
        public double RmsPixels { get; }

        /// <summary>Gets the error of the fit for each view, root mean square, in pixels.</summary>
        public double[] RmsPerCapture { get; }

        /// <summary>Gets whether the distance was measured (views at two or more positions) or assumed.</summary>
        public bool DistanceMeasured { get; }

        /// <summary>Gets the report, as text.</summary>
        public string Report { get; }
    }

    /// <summary>
    /// Camera calibration for eye cameras, which see a few cm with a few mm of depth of focus: a flat
    /// dot grid where the eye sits, facing forward, moved in known steps along the head's forward axis.
    /// All the views together are one 3D object in the head frame, so one fit with OpenCV gives the
    /// focal length and the pose of the camera (its extrinsics) at once. The principal point is kept
    /// (the center of the sensor) and the pixels are square: neither can be measured with a narrow
    /// field of view. With all views at the same position, the distance is assumed.
    /// See docs/user-manual/camera-calibration.md.
    /// </summary>
    public static class CameraCalibrator
    {
        /// <summary>
        /// Calibrates a camera.
        /// </summary>
        /// <param name="captures">The views, all with the fiducial found and not mirrored.</param>
        /// <param name="cameraIntrinsics">The intrinsics the camera has now, for the whole sensor (CameraEye.Intrinsics): the principal point is kept.</param>
        /// <param name="options">The measured grid and where the eye is.</param>
        /// <param name="title">What is calibrated, for the report (system, eye, camera).</param>
        public static CameraCalibrationResult Calibrate(IReadOnlyList<CameraCalibrationCapture> captures, CameraIntrinsics cameraIntrinsics, CameraCalibrationOptions options, string title)
        {
            if (captures.Count == 0) throw new InvalidOperationException("Capture at least one view of the target.");
            if (cameraIntrinsics.IsEmpty || captures.Any(c => c.ImageIntrinsics.IsEmpty))
                throw new InvalidOperationException("The images have no intrinsics (for example, from videos): calibrate with the cameras.");
            if (captures.Any(c => !c.Grid.FiducialFound))
                throw new InvalidOperationException("The center of the target (the missing dots) must be in view in every capture.");
            if (captures.Any(c => c.Grid.Mirrored))
                throw new InvalidOperationException("The target looks mirrored: check the camera orientation in the settings.");

            // All views in one frame with the principal point at the middle of the first image, so the
            // window of the camera may move between views (OpenCV wants it inside the image).
            var imageSize = captures[0].ImageSize;
            var principal = new PointF(imageSize.Width / 2f, imageSize.Height / 2f);

            var pitchX = options.PitchXMm;
            var pitchY = options.PitchYMm;
            var objectPoints = new List<MCvPoint3D32f>();
            var imagePoints = new List<PointF>();
            var ranges = new List<(int Start, int Count)>();
            foreach (var capture in captures)
            {
                var shiftX = principal.X - capture.ImageIntrinsics.Cx;
                var shiftY = principal.Y - capture.ImageIntrinsics.Cy;
                ranges.Add((objectPoints.Count, capture.Grid.Count));
                for (int i = 0; i < capture.Grid.Count; i++)
                {
                    var (column, row) = capture.Grid.Indices[i];
                    objectPoints.Add(HeadPoint(options, capture.TargetPositionMm, column * pitchX, row * pitchY));
                    var p = capture.Grid.Positions[i];
                    imagePoints.Add(new PointF(p.X + shiftX, p.Y + shiftY));
                }
            }

            // Start: pixels per mm of each view, 1 / scale linear with the position, gives f and the distance.
            var positions = captures.Select(c => c.TargetPositionMm).ToArray();
            var scales = captures.Select(c => c.Grid.PitchPixels / ((pitchX + pitchY) / 2)).ToArray();
            var distanceMeasured = positions.Max() - positions.Min() > 0.1;
            double f;
            if (distanceMeasured)
            {
                var (_, slope) = FitLine(positions, scales.Select(s => 1 / s).ToArray());
                f = slope < 0 ? -1 / slope : scales.Average() * options.AssumedDistanceMm;
            }
            else
            {
                f = scales.Average() * options.AssumedDistanceMm;
            }

            var flags = CalibType.UseIntrinsicGuess | CalibType.FixPrincipalPoint | CalibType.FixAspectRatio |
                CalibType.ZeroTangentDist | CalibType.FixK2 | CalibType.FixK3;
            if (!options.EstimateDistortion) flags |= CalibType.FixK1;
            if (!distanceMeasured) flags |= CalibType.FixFocalLength;

            using var cameraMatrix = new Matrix<double>(new double[,] { { f, 0, principal.X }, { 0, f, principal.Y }, { 0, 0, 1 } });
            using var distortion = new Matrix<double>(1, 5);
            var rms = CvInvoke.CalibrateCamera(
                new[] { objectPoints.ToArray() }, new[] { imagePoints.ToArray() }, imageSize, cameraMatrix, distortion, flags,
                new MCvTermCriteria(200, 1e-10), out var rvecs, out var tvecs);

            var rvec = ToVector(rvecs[0]);
            var tvec = ToVector(tvecs[0]);
            f = cameraMatrix[0, 0];
            var k1 = distortion[0, 0];

            var rmsPerCapture = ranges.Select(r =>
            {
                var projected = CvInvoke.ProjectPoints(objectPoints.GetRange(r.Start, r.Count).ToArray(), rvecs[0], tvecs[0], cameraMatrix, distortion, null, 0);
                var sum = 0.0;
                for (int i = 0; i < r.Count; i++)
                {
                    var p = imagePoints[r.Start + i];
                    sum += (projected[i].X - p.X) * (projected[i].X - p.X) + (projected[i].Y - p.Y) * (projected[i].Y - p.Y);
                }

                return Math.Sqrt(sum / r.Count);
            }).ToArray();

            foreach (var m in rvecs.Concat(tvecs)) m.Dispose();

            var intrinsics = new CameraIntrinsics((float)f, (float)f, cameraIntrinsics.Cx, cameraIntrinsics.Cy);
            var extrinsics = new CameraExtrinsics((float)rvec.X, (float)rvec.Y, (float)rvec.Z, (float)tvec.X, (float)tvec.Y, (float)tvec.Z);

            // Depth of the target center at position 0 in the camera, for the pixels per mm there.
            var targetCenter = Vector3.Transform(ToVector3(HeadPoint(options, 0, 0, 0)), extrinsics.Rotation) + new Vector3(extrinsics.Tx, extrinsics.Ty, extrinsics.Tz);
            var pixPerMm = f / targetCenter.Z;

            var report = MakeReport(title, captures, options, intrinsics, extrinsics, pixPerMm, k1, rms, rmsPerCapture, distanceMeasured);
            return new CameraCalibrationResult(intrinsics, extrinsics, pixPerMm, k1, rms, rmsPerCapture, distanceMeasured, report);
        }

        // A point of the target in the head frame (x forward, y left, z up; origin at the center of the
        // eye): the target faces forward, so its columns go to the subject's left and its rows down.
        private static MCvPoint3D32f HeadPoint(CameraCalibrationOptions options, double positionMm, double rightMm, double downMm) =>
            new MCvPoint3D32f((float)(options.EyeCenterBehindTargetMm + positionMm), (float)rightMm, (float)-downMm);

        private static Vector3 ToVector3(MCvPoint3D32f p) => new Vector3(p.X, p.Y, p.Z);

        private static MCvPoint3D64f ToVector(Mat m)
        {
            var values = new double[3];
            m.CopyTo(values);
            return new MCvPoint3D64f(values[0], values[1], values[2]);
        }

        private static (double Intercept, double Slope) FitLine(double[] x, double[] y)
        {
            var mx = x.Average();
            var my = y.Average();
            var sxx = x.Sum(v => (v - mx) * (v - mx));
            var sxy = x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum();
            var slope = sxy / sxx;
            return (my - slope * mx, slope);
        }

        private static string MakeReport(string title, IReadOnlyList<CameraCalibrationCapture> captures, CameraCalibrationOptions options,
            CameraIntrinsics intrinsics, CameraExtrinsics extrinsics, double pixPerMm, double k1, double rms, double[] rmsPerCapture, bool distanceMeasured)
        {
            var c = CultureInfo.InvariantCulture;
            var r = new StringBuilder();
            void Line(string s = "") => r.AppendLine(s);

            // Where the camera is, seen from the eye, and how it's rolled.
            var rotation = extrinsics.Rotation;
            var t = new Vector3(extrinsics.Tx, extrinsics.Ty, extrinsics.Tz);
            var cameraInHead = -Vector3.Transform(t, Quaternion.Inverse(rotation));
            var distance = cameraInHead.Length();
            var side = Math.Atan2(cameraInHead.Y, cameraInHead.X) * 180 / Math.PI;
            var elevation = Math.Atan2(cameraInHead.Z, Math.Sqrt(cameraInHead.X * cameraInHead.X + cameraInHead.Y * cameraInHead.Y)) * 180 / Math.PI;
            var up = extrinsics.HeadUp;
            var roll = Math.Atan2(up.X, -up.Y) * 180 / Math.PI;

            Line("OpenIris camera calibration");
            Line(title);
            Line(DateTime.Now.ToString("yyyy-MM-dd HH:mm", c));
            Line();
            Line("Target");
            Line(string.Format(c, "  Grid {0}, {1} x {1} dots, designed pitch {2} mm", options.Target.Name, options.Target.DotsPerSide, options.Target.PitchMm));
            Line(string.Format(c, "  Measured outer dots {0:0.00} x {1:0.00} mm: pitch {2:0.0000} x {3:0.0000} mm", options.MeasuredSpanXMm, options.MeasuredSpanYMm, options.PitchXMm, options.PitchYMm));
            Line(string.Format(c, "  Eye's center of rotation {0:0.0} mm behind the target at position 0", options.EyeCenterBehindTargetMm));
            Line();
            Line("Views");
            for (int i = 0; i < captures.Count; i++)
            {
                var capture = captures[i];
                Line(string.Format(c, "  {0,2}. position {1,6:0.00} mm, {2,4} dots, {3,3} frames, {4:0.00} px/dot, error {5:0.000} px",
                    i + 1, capture.TargetPositionMm, capture.Grid.Count, capture.Frames, capture.Grid.PitchPixels, rmsPerCapture[i]));
            }

            Line(string.Format(c, "  Error of the fit (RMS): {0:0.000} px = {1:0.0} um at the target", rms, 1000 * rms / pixPerMm));
            Line();
            Line("Results");
            Line(string.Format(c, "  Pixels per mm at the target at position 0: {0:0.000} ({1:0.0000} mm per pixel)", pixPerMm, 1 / pixPerMm));
            Line(string.Format(c, "  Focal length: {0:0.0} px", intrinsics.Fx));
            Line(string.Format(c, "  Distance camera to eye center: {0:0.0} mm{1}", distance, distanceMeasured ? string.Empty : " (ASSUMED, all views at one position)"));
            Line(string.Format(c, "  Camera seen from the eye: {0:0.0} deg {1}, {2:0.0} deg {3} straight ahead",
                Math.Abs(side), side >= 0 ? "to the subject's left" : "to the subject's right", Math.Abs(elevation), elevation >= 0 ? "above" : "below"));
            Line(string.Format(c, "  Head up in the image: {0:0.0} deg {1} from image up", Math.Abs(roll), roll >= 0 ? "clockwise" : "counterclockwise"));
            Line(string.Format(c, "  Radial distortion k1: {0:0.0000}{1} (not used by OpenIris yet)", k1, options.EstimateDistortion ? string.Empty : " (not estimated)"));
            Line();
            Line("Settings (Configuration > the system's settings > Camera properties)");
            Line(string.Format(c, "  CameraIntrinsics: Fx {0}, Fy {1}, Cx {2}, Cy {3}", intrinsics.Fx, intrinsics.Fy, intrinsics.Cx, intrinsics.Cy));
            Line(string.Format(c, "  CameraExtrinsics: Rx {0}, Ry {1}, Rz {2}, Tx {3}, Ty {4}, Tz {5}", extrinsics.Rx, extrinsics.Ry, extrinsics.Rz, extrinsics.Tx, extrinsics.Ty, extrinsics.Tz));
            Line(string.Format(c, "  PixPerMm: {0:0.000}", pixPerMm));
            Line();
            Line("Notes");
            Line("  The principal point is the center of the sensor and the pixels are square (not measurable with a narrow field of view).");
            if (!distanceMeasured)
            {
                Line(string.Format(c, "  All views at one position: the distance ({0} mm) and so the focal length are assumed. Move the target in known steps to measure them.", options.AssumedDistanceMm));
            }

            Line("  Restart tracking after applying, and calibrate the eye again.");
            return r.ToString();
        }
    }
}
