//-----------------------------------------------------------------------
// <copyright file="RobustEllipseFit.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    /// <summary>
    /// Fits an ellipse to contour points robustly, with a prior that it is round. Points far from the
    /// ellipse (a reflection on the edge, eyelashes) count less, and when part of the contour is
    /// missing (the eyelid covers the top) the prior keeps the ellipse from stretching (#77).
    /// </summary>
    /// <remarks>
    /// Iteratively reweighted least squares (Levenberg-Marquardt) over center, semi-axes and angle.
    /// The distance of a point to the ellipse is measured along the line from the center, which for
    /// pupils (nearly round) is close to the shortest distance and much cheaper.
    /// </remarks>
    public static class RobustEllipseFit
    {
        /// <summary>
        /// How much the distances of points far from the ellipse count.
        /// </summary>
        public enum Loss
        {
            /// <summary>Squared distance, as an ordinary fit.</summary>
            Squared,

            /// <summary>Absolute distance.</summary>
            Absolute,

            /// <summary>Squared up to <see cref="HuberThreshold"/>, absolute beyond.</summary>
            Huber,

            /// <summary>Huber, then Tukey's: points beyond a few times the typical distance don't count.</summary>
            Tukey,
        }

        // TEMPORARY knobs for comparing variants (#77).
        internal static Loss LossFunction = Loss.Huber;
        internal static double PriorPoints = 0.1;
        internal static double PriorFreeRatio = 1; // No penalty while the axes' ratio is under this.

        /// <summary>
        /// Distance (pixels) where Huber's loss changes from squared to absolute.
        /// </summary>
        private const double HuberThreshold = 1;

        private const int Iterations = 15;

        /// <summary>
        /// Fits the ellipse.
        /// </summary>
        /// <param name="points">Contour points.</param>
        /// <returns>The ellipse, as CvInvoke.FitEllipse returns it.</returns>
        public static RotatedRect Fit(VectorOfPointF points)
        {
            var p = points.ToArray();
            var start = CvInvoke.FitEllipse(points);

            // Start round, at the center of the ordinary fit: with a gap in the contour, its shape
            // can be far off, but its center is usually close.
            var radius = Math.Sqrt(start.Size.Width * start.Size.Height) / 2;
            var x = new double[] { start.Center.X, start.Center.Y, radius, radius, start.Angle * Math.PI / 180 };

            switch (LossFunction)
            {
                case Loss.Tukey:
                    x = Solve(p, x, Loss.Huber, HuberThreshold);
                    var scale = 1.4826 * Median(Distances(p, x));
                    x = Solve(p, x, Loss.Tukey, Math.Max(1, 4.685 * scale));
                    break;
                default:
                    x = Solve(p, x, LossFunction, HuberThreshold);
                    break;
            }

            return new RotatedRect(new PointF((float)x[0], (float)x[1]), new SizeF((float)(2 * x[2]), (float)(2 * x[3])), (float)(x[4] * 180 / Math.PI));
        }

        /// <summary>
        /// Levenberg-Marquardt, with the weights of the loss recomputed every iteration.
        /// </summary>
        private static double[] Solve(PointF[] p, double[] x, Loss loss, double threshold)
        {
            var n = p.Length;
            var priorWeight = Math.Sqrt(PriorPoints * n);
            var lambda = 1e-3;
            var residuals = new double[n + 1];
            var shifted = new double[n + 1];
            var jacobian = new double[n + 1, 5];

            var cost = Cost(p, x, loss, threshold, priorWeight, residuals, out var weights);
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                // Numeric derivatives.
                for (int k = 0; k < 5; k++)
                {
                    var step = k == 4 ? 1e-4 : 1e-3;
                    var xk = (double[])x.Clone();
                    xk[k] += step;
                    Residuals(p, xk, priorWeight, shifted);
                    for (int i = 0; i <= n; i++) jacobian[i, k] = (shifted[i] - residuals[i]) / step;
                }

                // Normal equations, weighted: (JᵀWJ + λ diag) Δ = -JᵀWr.
                var a = new double[5, 5];
                var g = new double[5];
                for (int i = 0; i <= n; i++)
                {
                    var w = weights[i];
                    for (int r = 0; r < 5; r++)
                    {
                        g[r] -= w * jacobian[i, r] * residuals[i];
                        for (int c = 0; c < 5; c++) a[r, c] += w * jacobian[i, r] * jacobian[i, c];
                    }
                }

                var improved = false;
                for (int attempt = 0; attempt < 10 && !improved; attempt++)
                {
                    var damped = (double[,])a.Clone();
                    for (int r = 0; r < 5; r++) damped[r, r] += lambda * (a[r, r] + 1e-9);
                    var delta = SolveLinear(damped, g);
                    if (delta is null) break;

                    var candidate = new double[5];
                    for (int k = 0; k < 5; k++) candidate[k] = x[k] + delta[k];
                    candidate[2] = Math.Abs(candidate[2]);
                    candidate[3] = Math.Abs(candidate[3]);

                    var candidateResiduals = new double[n + 1];
                    var candidateCost = Cost(p, candidate, loss, threshold, priorWeight, candidateResiduals, out var candidateWeights);
                    if (candidateCost < cost)
                    {
                        x = candidate;
                        cost = candidateCost;
                        residuals = candidateResiduals;
                        weights = candidateWeights;
                        lambda = Math.Max(lambda / 10, 1e-7);
                        improved = true;
                    }
                    else
                    {
                        lambda *= 10;
                    }
                }

                if (!improved) break;
            }

            return x;
        }

        /// <summary>
        /// Distance of each point to the ellipse along the line from its center, and the prior (last).
        /// </summary>
        private static void Residuals(PointF[] p, double[] x, double priorWeight, double[] residuals)
        {
            var cos = Math.Cos(x[4]);
            var sin = Math.Sin(x[4]);
            var a = Math.Max(x[2], 1e-6);
            var b = Math.Max(x[3], 1e-6);
            for (int i = 0; i < p.Length; i++)
            {
                var dx = p[i].X - x[0];
                var dy = p[i].Y - x[1];
                var u = (dx * cos + dy * sin) / a;
                var v = (-dx * sin + dy * cos) / b;
                var s = Math.Sqrt(u * u + v * v);
                var length = Math.Sqrt(dx * dx + dy * dy);
                residuals[i] = s > 1e-9 ? length * (1 - 1 / s) : -Math.Min(a, b);
            }

            // Prior: round. The difference of the semi-axes, as a distance in pixels.
            var excess = Math.Max(0, Math.Abs(Math.Log(a / b)) - Math.Log(PriorFreeRatio));
            residuals[p.Length] = priorWeight * (a + b) / 2 * excess;
        }

        private static double Cost(PointF[] p, double[] x, Loss loss, double threshold, double priorWeight, double[] residuals, out double[] weights)
        {
            Residuals(p, x, priorWeight, residuals);
            weights = new double[residuals.Length];
            var cost = 0.0;
            for (int i = 0; i < p.Length; i++)
            {
                var d = Math.Abs(residuals[i]);
                switch (loss)
                {
                    case Loss.Squared:
                        weights[i] = 1;
                        cost += d * d;
                        break;
                    case Loss.Absolute:
                        weights[i] = 1 / Math.Max(d, 1e-3);
                        cost += 2 * d;
                        break;
                    case Loss.Huber:
                        weights[i] = d <= threshold ? 1 : threshold / d;
                        cost += d <= threshold ? d * d : 2 * threshold * d - threshold * threshold;
                        break;
                    case Loss.Tukey:
                        var t = d / threshold;
                        weights[i] = t < 1 ? (1 - t * t) * (1 - t * t) : 0;
                        cost += threshold * threshold / 3 * (t < 1 ? 1 - Math.Pow(1 - t * t, 3) : 1);
                        break;
                }
            }

            // The prior always counts squared.
            weights[p.Length] = 1;
            cost += residuals[p.Length] * residuals[p.Length];
            return cost;
        }

        private static double[] Distances(PointF[] p, double[] x)
        {
            var residuals = new double[p.Length + 1];
            Residuals(p, x, 0, residuals);
            var distances = new double[p.Length];
            for (int i = 0; i < p.Length; i++) distances[i] = Math.Abs(residuals[i]);
            return distances;
        }

        private static double Median(double[] values)
        {
            var sorted = (double[])values.Clone();
            Array.Sort(sorted);
            return sorted.Length == 0 ? 0 : sorted[sorted.Length / 2];
        }

        /// <summary>
        /// Solves a small linear system by Gaussian elimination with partial pivoting.
        /// </summary>
        private static double[]? SolveLinear(double[,] a, double[] b)
        {
            var n = b.Length;
            var m = (double[,])a.Clone();
            var y = (double[])b.Clone();
            for (int col = 0; col < n; col++)
            {
                var pivot = col;
                for (int r = col + 1; r < n; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col])) pivot = r;
                if (Math.Abs(m[pivot, col]) < 1e-12) return null;
                if (pivot != col)
                {
                    for (int c = 0; c < n; c++) (m[col, c], m[pivot, c]) = (m[pivot, c], m[col, c]);
                    (y[col], y[pivot]) = (y[pivot], y[col]);
                }

                for (int r = col + 1; r < n; r++)
                {
                    var f = m[r, col] / m[col, col];
                    for (int c = col; c < n; c++) m[r, c] -= f * m[col, c];
                    y[r] -= f * y[col];
                }
            }

            var result = new double[n];
            for (int r = n - 1; r >= 0; r--)
            {
                var sum = y[r];
                for (int c = r + 1; c < n; c++) sum -= m[r, c] * result[c];
                result[r] = sum / m[r, r];
            }

            return result;
        }
    }
}
