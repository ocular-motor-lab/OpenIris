//-----------------------------------------------------------------------
// <copyright file="AppearanceGazeMap.cs">
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

    /// <summary>
    /// A linear map from the embedding of a gaze network (<see cref="EyeData.Appearance"/>, the
    /// webcam's appearance-based gaze) to the gaze angles in degrees, fitted by the N-point calibration
    /// with the signal source Appearance:
    /// <code>horizontal = CenterHorizontal + sum(embedding[i] * Horizontal[i]), and the same for vertical</code>
    /// It is fitted as saccade.js does (https://saccade.jspsych.org, packages/core/src/ridge.ts and
    /// grids.ts, MIT license): ridge regression on every frame of every target, without an intercept,
    /// the targets taken from their center, each frame weighted by how much the network trusts it. The
    /// angles have the signs of the targets. It replaces the horizontal and vertical positions of the
    /// eye model; NaN in frames without an embedding.
    /// </summary>
    [Serializable]
    public class AppearanceGazeMap : EyeCalibrationData
    {
        /// <summary>
        /// The ridge penalty saccade.js uses for a fit on frames (its FRAME_LAMBDA), chosen on 200 sessions
        /// for 9 to 25 points, with the weights of the frames rescaled to a mean of 1.
        /// </summary>
        public const double FrameLambda = 3;

        // Added to the weights so frames at 0 still count a little (saccade.js's RIDGE_EPS).
        private const double WeightEpsilon = 1e-7;

        /// <summary>
        /// Gets or sets the weights of the embedding for the horizontal angle.
        /// </summary>
        public double[] Horizontal { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Gets or sets the weights of the embedding for the vertical angle.
        /// </summary>
        public double[] Vertical { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Gets or sets the horizontal angle of an embedding of zeros: the center of the targets.
        /// </summary>
        public double CenterHorizontal { get; set; }

        /// <summary>
        /// Gets or sets the vertical angle of an embedding of zeros: the center of the targets.
        /// </summary>
        public double CenterVertical { get; set; }

        public override CalibratedEyeData Adjust(EyeData eyeData, CalibratedEyeData fromEyeModel)
        {
            var gaze = Apply(eyeData?.Appearance);
            fromEyeModel.HorizontalPosition = gaze?.horizontal ?? double.NaN;
            fromEyeModel.VerticalPosition = gaze?.vertical ?? double.NaN;
            return fromEyeModel;
        }

        /// <summary>
        /// The gaze angles in degrees, or null without an embedding (or one of another network).
        /// </summary>
        public (double horizontal, double vertical)? Apply(AppearanceEmbedding? appearance)
        {
            var e = appearance?.Values;
            if (e is null || e.Length != Horizontal.Length || e.Length != Vertical.Length) return null;

            double h = CenterHorizontal, v = CenterVertical;
            for (int i = 0; i < e.Length; i++)
            {
                h += e[i] * Horizontal[i];
                v += e[i] * Vertical[i];
            }

            return (h, v);
        }

        /// <summary>
        /// Fits the map from the frames collected on each target (saccade.js's "frames" fit): every frame
        /// is a row, weighted by its <see cref="AppearanceEmbedding.Weight"/>, with the weights rescaled
        /// to a mean of 1. A target whose frames all have weight 0 gets equal weights.
        /// </summary>
        /// <param name="targets">Each target, in degrees, with the embeddings of its frames.</param>
        /// <param name="lambda">The ridge penalty.</param>
        /// <returns>The map, or null without frames.</returns>
        public static AppearanceGazeMap? Fit(IEnumerable<(PointF target, IReadOnlyList<AppearanceEmbedding> frames)> targets, double lambda = FrameLambda)
        {
            var list = targets.Where(t => t.frames.Count > 0).ToList();
            if (list.Count == 0) return null;

            var rows = new List<(float[] e, double x, double y, double w)>();
            foreach (var (target, frames) in list)
            {
                var own = frames.Sum(f => (double)f.Weight) > 1e-6;
                rows.AddRange(frames.Select(f => (f.Values, (double)target.X, (double)target.Y, own ? f.Weight : 1.0)));
            }

            var mean = rows.Average(r => r.w);
            if (mean > 0) rows = rows.Select(r => (r.e, r.x, r.y, r.w / mean)).ToList();

            // saccade.js centers the targets on the middle of the screen; the targets here are in degrees,
            // around wherever the calibration put them.
            var centerX = list.Average(t => (double)t.target.X);
            var centerY = list.Average(t => (double)t.target.Y);
            var (horizontal, vertical) = Solve(rows, lambda, centerX, centerY);

            return new AppearanceGazeMap
            {
                Horizontal = horizontal,
                Vertical = vertical,
                CenterHorizontal = centerX,
                CenterVertical = centerY,
            };
        }

        /// <summary>
        /// Weighted ridge regression from embeddings to the targets minus their center, without an
        /// intercept, solved by Cholesky (saccade.js's solveRidge).
        /// </summary>
        /// <param name="rows">Each row: the embedding, the target, and its weight.</param>
        /// <param name="lambda">The ridge penalty.</param>
        /// <param name="centerX">Subtracted from the horizontal targets.</param>
        /// <param name="centerY">Subtracted from the vertical targets.</param>
        /// <returns>The weights for each coordinate.</returns>
        public static (double[] x, double[] y) Solve(IReadOnlyList<(float[] e, double x, double y, double w)> rows, double lambda, double centerX, double centerY)
        {
            if (rows is null || rows.Count == 0) throw new ArgumentException("No rows to fit.", nameof(rows));
            var d = rows[0].e.Length;
            if (d == 0 || rows.Any(r => r.e.Length != d)) throw new ArgumentException("The embeddings are empty or of different lengths.", nameof(rows));

            // Normal equations: a = X'WX + lambda I, b = X'W(y - center).
            var a = new double[d, d];
            var b = new double[d, 2];
            var xw = new double[d];
            foreach (var r in rows)
            {
                var s = Math.Sqrt(r.w + WeightEpsilon);
                for (int i = 0; i < d; i++) xw[i] = r.e[i] * s;
                var yx = (r.x - centerX) * s;
                var yy = (r.y - centerY) * s;
                for (int i = 0; i < d; i++)
                {
                    var v = xw[i];
                    for (int j = i; j < d; j++) a[i, j] += v * xw[j];
                    b[i, 0] += v * yx;
                    b[i, 1] += v * yy;
                }
            }

            for (int i = 0; i < d; i++)
            {
                a[i, i] += lambda;
                for (int j = 0; j < i; j++) a[i, j] = a[j, i];
            }

            // Cholesky, a = L L', in the lower triangle of a.
            for (int i = 0; i < d; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    var sum = a[i, j];
                    for (int k = 0; k < j; k++) sum -= a[i, k] * a[j, k];
                    a[i, j] = i == j ? Math.Sqrt(sum) : sum / a[j, j];
                }
            }

            // L z = b, then L' w = z.
            var z = new double[d, 2];
            for (int i = 0; i < d; i++)
            {
                double s0 = b[i, 0], s1 = b[i, 1];
                for (int k = 0; k < i; k++)
                {
                    s0 -= a[i, k] * z[k, 0];
                    s1 -= a[i, k] * z[k, 1];
                }

                z[i, 0] = s0 / a[i, i];
                z[i, 1] = s1 / a[i, i];
            }

            var wx = new double[d];
            var wy = new double[d];
            for (int i = d - 1; i >= 0; i--)
            {
                double s0 = z[i, 0], s1 = z[i, 1];
                for (int k = i + 1; k < d; k++)
                {
                    s0 -= a[k, i] * wx[k];
                    s1 -= a[k, i] * wy[k];
                }

                wx[i] = s0 / a[i, i];
                wy[i] = s1 / a[i, i];
            }

            return (wx, wy);
        }
    }
}
