//-----------------------------------------------------------------------
// <copyright file="CameraCalibrationTarget.cs">
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
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    /// <summary>
    /// A printed dot grid for the camera calibration (Tools > Camera calibration): square grids of
    /// dots, one for each eye, with a fiducial of missing dots at the center, where the eye goes: the
    /// center dot, some to its right and some above it. How many says which grid and which eye: UpArm
    /// above, UpArm + 1 to the right for the left eye, UpArm + 2 for the right eye. The arms have
    /// different lengths, so the fiducial also gives the numbering of the dots, the orientation and
    /// whether the image is mirrored. The PDF comes from tools/make-calibration-target.py: keep both
    /// in step.
    /// </summary>
    public sealed class CameraCalibrationTarget
    {
        /// <summary>
        /// The length of the printed rulers, to measure the scale of the print, in mm.
        /// </summary>
        public const double RulerMm = 150;

        /// <summary>
        /// The distance between the centers of the grids of the two eyes on the page, in mm.
        /// </summary>
        public const double DistanceBetweenEyesMm = 63;

        /// <summary>
        /// The grids of the printed page (OpenIris-calibration-targets.pdf).
        /// </summary>
        public static readonly IReadOnlyList<CameraCalibrationTarget> Printed = new[]
        {
            new CameraCalibrationTarget("A", 1.0, 0.5, 31, 1),
            new CameraCalibrationTarget("B", 1.5, 0.75, 21, 2),
            new CameraCalibrationTarget("C", 0.75, 0.375, 41, 3),
        };

        /// <summary>
        /// Initializes a grid.
        /// </summary>
        public CameraCalibrationTarget(string name, double pitchMm, double dotDiameterMm, int dotsPerSide, int upArm)
        {
            Name = name;
            PitchMm = pitchMm;
            DotDiameterMm = dotDiameterMm;
            DotsPerSide = dotsPerSide;
            UpArm = upArm;
        }

        /// <summary>Gets the number of missing dots above the center of the fiducial.</summary>
        public int UpArm { get; }

        /// <summary>
        /// The number of missing dots to the right of the center of the fiducial for an eye.
        /// </summary>
        public int RightArm(Eye eye) => UpArm + (eye == Eye.Right ? 2 : 1);

        /// <summary>
        /// The missing dots of the fiducial for an eye, as (column, row) from the center, rows growing downward.
        /// </summary>
        public IReadOnlyList<(int Column, int Row)> Fiducial(Eye eye) =>
            new[] { (0, 0) }
                .Concat(Enumerable.Range(1, RightArm(eye)).Select(k => (k, 0)))
                .Concat(Enumerable.Range(1, UpArm).Select(k => (0, -k)))
                .ToArray();

        /// <summary>Gets the name of the grid on the printed page.</summary>
        public string Name { get; }

        /// <summary>Gets the distance between dots, as designed, in mm.</summary>
        public double PitchMm { get; }

        /// <summary>Gets the diameter of the dots, as designed, in mm.</summary>
        public double DotDiameterMm { get; }

        /// <summary>Gets the number of dots in each row and column.</summary>
        public int DotsPerSide { get; }

        /// <summary>Gets the distance between the outer dots, as designed, in mm.</summary>
        public double SpanMm => (DotsPerSide - 1) * PitchMm;

        /// <inheritdoc/>
        public override string ToString() => $"{Name}: pitch {PitchMm} mm, dots {DotDiameterMm} mm";
    }

    /// <summary>
    /// The dots of a grid found in an image, numbered from the fiducial when it is in view.
    /// </summary>
    public sealed class DotGrid
    {
        internal DotGrid(PointF[] positions, (int Column, int Row)[] indices, bool fiducialFound, bool mirrored, PointF center, double pitchPixels,
            CameraCalibrationTarget? target = null, Eye eye = Eye.Both)
        {
            Target = target;
            Eye = eye;
            Positions = positions;
            Indices = indices;
            FiducialFound = fiducialFound;
            Mirrored = mirrored;
            Center = center;
            PitchPixels = pitchPixels;
        }

        /// <summary>Gets which printed grid it is, from the fiducial (null if not found).</summary>
        public CameraCalibrationTarget? Target { get; }

        /// <summary>Gets which eye the grid is for, from the fiducial (Both if not found).</summary>
        public Eye Eye { get; }

        /// <summary>Gets the centers of the dots, in pixels of the image.</summary>
        public PointF[] Positions { get; }

        /// <summary>
        /// Gets the column and row of each dot on the printed grid, from the center of the fiducial
        /// (columns grow to the right, rows downward, as printed). Only relative to some dot if the
        /// fiducial wasn't found.
        /// </summary>
        public (int Column, int Row)[] Indices { get; }

        /// <summary>Gets whether the fiducial was found, so the numbering is the printed one.</summary>
        public bool FiducialFound { get; }

        /// <summary>Gets whether the grid looks mirrored in the image (a wrong camera orientation).</summary>
        public bool Mirrored { get; }

        /// <summary>Gets where the center of the fiducial is in the image, in pixels.</summary>
        public PointF Center { get; }

        /// <summary>Gets the average distance between neighboring dots, in pixels.</summary>
        public double PitchPixels { get; }

        /// <summary>Gets the number of dots.</summary>
        public int Count => Positions.Length;

        /// <summary>
        /// Finds the dots of a grid in an image: dark round blobs on a light background, numbered by
        /// walking from neighbor to neighbor, so perspective and some distortion don't matter.
        /// </summary>
        /// <param name="image">Image of the grid.</param>
        /// <returns>The dots, or null if there aren't enough of them.</returns>
        public static DotGrid? Find(Image<Gray, byte> image)
        {
            var centers = FindDots(image);
            if (centers.Count < 12) return null;

            var lattice = new Lattice(centers);
            var assigned = lattice.Walk();
            if (assigned.Count < 12) return null;

            var indices = assigned.Keys.ToArray();
            var positions = indices.Select(i => centers[assigned[i]]).ToArray();
            var occupied = new HashSet<(int, int)>(indices);

            var fiducial = FindFiducial(occupied);
            var (a, b) = AffineSteps(positions, indices);
            var pitch = (Length(a) + Length(b)) / 2;

            if (fiducial is null)
            {
                return new DotGrid(positions, indices, false, false, AffineAt(positions, indices, (0, 0)), pitch);
            }

            var (h, u, v, target, eye) = fiducial.Value;
            var printed = indices
                .Select(i => (Dot(Sub(i, h), u), -Dot(Sub(i, h), v)))
                .ToArray();

            // Printed right and up in the image (y down): not mirrored, right x up is negative.
            var right = Add(Scale(a, u.Item1), Scale(b, u.Item2));
            var up = Add(Scale(a, v.Item1), Scale(b, v.Item2));
            var mirrored = right.X * up.Y - right.Y * up.X > 0;

            return new DotGrid(positions, printed, true, mirrored, AffineAt(positions, printed, (0, 0)), pitch, target, eye);
        }

        // Dark round blobs: local threshold, contours, then centroids weighted by how dark each pixel is.
        private static List<PointF> FindDots(Image<Gray, byte> image)
        {
            var blockSize = Math.Max(15, Math.Min(image.Width, image.Height) / 8) | 1;
            using var smooth = image.SmoothGaussian(3);
            using var binary = new Image<Gray, byte>(image.Size);
            CvInvoke.AdaptiveThreshold(smooth, binary, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, blockSize, 10);

            var blobs = new List<(Rectangle Box, double Area)>();
            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
                for (int i = 0; i < contours.Size; i++)
                {
                    using var contour = contours[i];
                    var area = CvInvoke.ContourArea(contour);
                    if (area < 6) continue;
                    var perimeter = CvInvoke.ArcLength(contour, true);
                    if (4 * Math.PI * area / (perimeter * perimeter) < 0.6) continue;
                    blobs.Add((CvInvoke.BoundingRectangle(contour), area));
                }
            }

            if (blobs.Count == 0) return new List<PointF>();

            // The dots are all about the same size: drop specks and merged blobs.
            var medianArea = blobs.Select(x => x.Area).OrderBy(x => x).ElementAt(blobs.Count / 2);
            var data = image.Data;
            var centers = new List<PointF>();
            foreach (var (box, area) in blobs)
            {
                if (area < medianArea / 3 || area > medianArea * 3) continue;

                var margin = Math.Max(2, box.Width / 4);
                var x0 = Math.Max(0, box.X - margin);
                var y0 = Math.Max(0, box.Y - margin);
                var x1 = Math.Min(image.Width - 1, box.Right + margin);
                var y1 = Math.Min(image.Height - 1, box.Bottom + margin);

                // Background: the brightest of the mean of the border rows and columns.
                double border = 0;
                int n = 0;
                for (int x = x0; x <= x1; x++) { border += data[y0, x, 0] + data[y1, x, 0]; n += 2; }
                for (int y = y0; y <= y1; y++) { border += data[y, x0, 0] + data[y, x1, 0]; n += 2; }
                var background = border / n;

                double sw = 0, sx = 0, sy = 0;
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        var w = background - data[y, x, 0];
                        if (w <= 0) continue;
                        sw += w;
                        sx += w * x;
                        sy += w * y;
                    }
                }

                if (sw > 0) centers.Add(new PointF((float)(sx / sw), (float)(sy / sw)));
            }

            return centers;
        }

        // The fiducial of one of the printed grids: missing dots h, h + k u (printed right) and h + k v
        // (printed up), all surrounded by dots, so the edge of the view doesn't count.
        private static ((int, int) Center, (int, int) Right, (int, int) Up, CameraCalibrationTarget Target, Eye Eye)? FindFiducial(HashSet<(int, int)> occupied)
        {
            var patterns = CameraCalibrationTarget.Printed
                .SelectMany(t => new[] { Eye.Left, Eye.Right }.Select(e => (Target: t, Eye: e, Right: t.RightArm(e), Up: t.UpArm)))
                .ToArray();

            var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            var minX = occupied.Min(i => i.Item1);
            var maxX = occupied.Max(i => i.Item1);
            var minY = occupied.Min(i => i.Item2);
            var maxY = occupied.Max(i => i.Item2);

            var found = new List<((int, int), (int, int), (int, int), CameraCalibrationTarget, Eye)>();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var h = (x, y);
                    if (occupied.Contains(h)) continue;

                    foreach (var u in steps)
                    {
                        if (occupied.Contains(Add(h, u))) continue;

                        foreach (var v in new[] { (-u.Item2, u.Item1), (u.Item2, -u.Item1) })
                        {
                            foreach (var pattern in patterns)
                            {
                                var holes = new[] { h }
                                    .Concat(Enumerable.Range(1, pattern.Right).Select(k => Add(h, Scale(u, k))))
                                    .Concat(Enumerable.Range(1, pattern.Up).Select(k => Add(h, Scale(v, k))))
                                    .ToArray();
                                if (holes.Any(occupied.Contains)) continue;

                                var surrounded = holes
                                    .SelectMany(hole => steps.Select(s => Add(hole, s)))
                                    .Where(p => !holes.Contains(p))
                                    .All(occupied.Contains);
                                if (surrounded) found.Add((h, u, v, pattern.Target, pattern.Eye));
                            }
                        }
                    }
                }
            }

            return found.Count == 1 ? found[0] : null;
        }

        // Least squares image position = origin + column a + row b.
        private static (PointF A, PointF B) AffineSteps(PointF[] positions, (int Column, int Row)[] indices)
        {
            var (_, a, b) = FitAffine(positions, indices);
            return (a, b);
        }

        private static PointF AffineAt(PointF[] positions, (int Column, int Row)[] indices, (int Column, int Row) at)
        {
            // Near the point, so distortion and perspective don't move it.
            var near = Enumerable.Range(0, indices.Length)
                .Where(i => Math.Abs(indices[i].Column - at.Column) <= 4 && Math.Abs(indices[i].Row - at.Row) <= 4)
                .ToArray();
            if (near.Length < 6) near = Enumerable.Range(0, indices.Length).ToArray();

            var (o, a, b) = FitAffine(near.Select(i => positions[i]).ToArray(), near.Select(i => indices[i]).ToArray());
            return new PointF(o.X + a.X * at.Column + b.X * at.Row, o.Y + a.Y * at.Column + b.Y * at.Row);
        }

        private static (PointF O, PointF A, PointF B) FitAffine(PointF[] positions, (int Column, int Row)[] indices)
        {
            // Normal equations of [1 c r] for x and y.
            var m = new double[3, 3];
            var vx = new double[3];
            var vy = new double[3];
            for (int i = 0; i < positions.Length; i++)
            {
                var row = new double[] { 1, indices[i].Column, indices[i].Row };
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++) m[j, k] += row[j] * row[k];
                    vx[j] += row[j] * positions[i].X;
                    vy[j] += row[j] * positions[i].Y;
                }
            }

            var sx = Solve3(m, vx);
            var sy = Solve3(m, vy);
            return (new PointF((float)sx[0], (float)sy[0]), new PointF((float)sx[1], (float)sy[1]), new PointF((float)sx[2], (float)sy[2]));
        }

        private static double[] Solve3(double[,] m, double[] v)
        {
            double Det(double[,] a) =>
                a[0, 0] * (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) -
                a[0, 1] * (a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0]) +
                a[0, 2] * (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]);

            var d = Det(m);
            var result = new double[3];
            for (int c = 0; c < 3; c++)
            {
                var mc = (double[,])m.Clone();
                for (int r = 0; r < 3; r++) mc[r, c] = v[r];
                result[c] = d == 0 ? 0 : Det(mc) / d;
            }

            return result;
        }

        private static (int, int) Add((int, int) x, (int, int) y) => (x.Item1 + y.Item1, x.Item2 + y.Item2);

        private static (int, int) Sub((int, int) x, (int, int) y) => (x.Item1 - y.Item1, x.Item2 - y.Item2);

        private static (int, int) Scale((int, int) x, int s) => (x.Item1 * s, x.Item2 * s);

        private static int Dot((int, int) x, (int, int) y) => x.Item1 * y.Item1 + x.Item2 * y.Item2;

        private static PointF Add(PointF x, PointF y) => new PointF(x.X + y.X, x.Y + y.Y);

        private static PointF Scale(PointF x, int s) => new PointF(x.X * s, x.Y * s);

        private static double Length(PointF x) => Math.Sqrt(x.X * x.X + x.Y * x.Y);

        /// <summary>
        /// The dots as a lattice: the main directions from the neighbors, then numbering by walking
        /// from the dot nearest the middle to its neighbors, predicting each step from the last one.
        /// </summary>
        private sealed class Lattice
        {
            private readonly List<PointF> points;
            private readonly Dictionary<(int, int), List<int>> cells = new Dictionary<(int, int), List<int>>();
            private readonly float cellSize;

            public Lattice(List<PointF> points)
            {
                this.points = points;

                // Cells about the size of the pitch, from the spread of the dots.
                var width = points.Max(p => p.X) - points.Min(p => p.X);
                var height = points.Max(p => p.Y) - points.Min(p => p.Y);
                cellSize = (float)Math.Max(4, Math.Sqrt(Math.Max(1, width * height) / points.Count));

                for (int i = 0; i < points.Count; i++)
                {
                    var key = Cell(points[i]);
                    if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<int>();
                    list.Add(i);
                }
            }

            public Dictionary<(int, int), int> Walk()
            {
                var (a, b) = MainSteps();
                var indexOf = new Dictionary<(int, int), int>();
                var taken = new bool[points.Count];

                var meanX = points.Average(p => p.X);
                var meanY = points.Average(p => p.Y);
                var seed = Enumerable.Range(0, points.Count).OrderBy(i => Sq(points[i].X - meanX) + Sq(points[i].Y - meanY)).First();

                indexOf[(0, 0)] = seed;
                taken[seed] = true;
                var queue = new Queue<(int, int)>();
                queue.Enqueue((0, 0));
                var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    var p = points[indexOf[at]];
                    foreach (var s in steps)
                    {
                        var next = (at.Item1 + s.Item1, at.Item2 + s.Item2);
                        if (indexOf.ContainsKey(next)) continue;

                        // The step from the dot behind, if there is one, else the main directions.
                        var behind = (at.Item1 - s.Item1, at.Item2 - s.Item2);
                        var step = indexOf.TryGetValue(behind, out var b0)
                            ? new PointF(p.X - points[b0].X, p.Y - points[b0].Y)
                            : new PointF(a.X * s.Item1 + b.X * s.Item2, a.Y * s.Item1 + b.Y * s.Item2);

                        var predicted = new PointF(p.X + step.X, p.Y + step.Y);
                        var found = Nearest(predicted, (float)(0.3 * Length(step)));
                        if (found < 0 || taken[found]) continue;

                        indexOf[next] = found;
                        taken[found] = true;
                        queue.Enqueue(next);
                    }
                }

                return indexOf;
            }

            // The two lattice directions: four-fold average angle of the vectors to the nearest neighbors.
            private (PointF A, PointF B) MainSteps()
            {
                double sumCos = 0, sumSin = 0;
                var distances = new List<double>();
                for (int i = 0; i < points.Count; i++)
                {
                    var j = Nearest(points[i], 2 * cellSize, exclude: i);
                    if (j < 0) continue;

                    var dx = points[j].X - points[i].X;
                    var dy = points[j].Y - points[i].Y;
                    var angle = Math.Atan2(dy, dx);
                    sumCos += Math.Cos(4 * angle);
                    sumSin += Math.Sin(4 * angle);
                    distances.Add(Math.Sqrt(dx * dx + dy * dy));
                }

                if (distances.Count == 0) return (new PointF(cellSize, 0), new PointF(0, cellSize));

                var pitch = distances.OrderBy(x => x).ElementAt(distances.Count / 2);
                var phi = Math.Atan2(sumSin, sumCos) / 4;
                return (new PointF((float)(pitch * Math.Cos(phi)), (float)(pitch * Math.Sin(phi))),
                        new PointF((float)(-pitch * Math.Sin(phi)), (float)(pitch * Math.Cos(phi))));
            }

            private int Nearest(PointF q, float maxDistance, int exclude = -1)
            {
                var reach = (int)Math.Ceiling(maxDistance / cellSize);
                var (cx, cy) = Cell(q);
                var best = -1;
                var bestD = (double)maxDistance * maxDistance;
                for (int x = cx - reach; x <= cx + reach; x++)
                {
                    for (int y = cy - reach; y <= cy + reach; y++)
                    {
                        if (!cells.TryGetValue((x, y), out var list)) continue;
                        foreach (var i in list)
                        {
                            if (i == exclude) continue;
                            var d = Sq(points[i].X - q.X) + Sq(points[i].Y - q.Y);
                            if (d < bestD)
                            {
                                bestD = d;
                                best = i;
                            }
                        }
                    }
                }

                return best;
            }

            private (int, int) Cell(PointF p) => ((int)Math.Floor(p.X / cellSize), (int)Math.Floor(p.Y / cellSize));

            private static double Sq(double x) => x * x;
        }
    }
}
