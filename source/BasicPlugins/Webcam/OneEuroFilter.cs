//-----------------------------------------------------------------------
// <copyright file="OneEuroFilter.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;

    /// <summary>
    /// The 1€ filter (Casiez, Roussel and Vogel, CHI 2012): a low-pass filter whose cutoff rises
    /// with speed, so it smooths a lot when still and lags little when moving.
    /// </summary>
    internal sealed class OneEuroFilter
    {
        private const double DerivativeCutoff = 1.0;

        private readonly double minCutoff;
        private readonly double beta;
        private double value;
        private double derivative;
        private double lastSeconds;

        /// <param name="minCutoff">Cutoff frequency when still, in Hz.</param>
        /// <param name="beta">Increase of the cutoff with speed, in Hz per (unit / s).</param>
        public OneEuroFilter(double minCutoff, double beta)
        {
            this.minCutoff = minCutoff;
            this.beta = beta;
        }

        public double Value => value;

        public void Reset(double newValue, double seconds)
        {
            value = newValue;
            derivative = 0;
            lastSeconds = seconds;
        }

        /// <summary>
        /// Adds a sample and returns the filtered value.
        /// </summary>
        /// <param name="sample">The new sample.</param>
        /// <param name="seconds">Its time.</param>
        /// <param name="unit">Size of one unit of speed for beta, for instance the eye width when
        /// filtering positions in pixels.</param>
        public double Filter(double sample, double seconds, double unit = 1.0)
        {
            var dt = seconds - lastSeconds;
            if (dt <= 0 || dt > 1) dt = 1 / 30.0;
            lastSeconds = seconds;

            derivative += Alpha(DerivativeCutoff, dt) * ((sample - value) / dt - derivative);
            var cutoff = minCutoff + beta * Math.Abs(derivative) / unit;
            value += Alpha(cutoff, dt) * (sample - value);
            return value;
        }

        private static double Alpha(double cutoff, double dt)
        {
            var tau = 1 / (2 * Math.PI * cutoff);
            return 1 / (1 + tau / dt);
        }
    }
}
