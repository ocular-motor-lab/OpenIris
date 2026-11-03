//-----------------------------------------------------------------------
// <copyright file="NPointSequence.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Calibration
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// A target of the N-point calibration, as the displays draw it: where it is, in degrees (positive
    /// right and up), how big it is drawn now, and whether its data is being collected.
    /// </summary>
    public sealed class NPointTarget
    {
        /// <summary>Initializes the target.</summary>
        public NPointTarget(PointF position, double scale, bool recording, int number, int count)
        {
            Position = position;
            Scale = scale;
            Recording = recording;
            Number = number;
            Count = count;
        }

        /// <summary>Gets where the target is, in degrees, positive right and up.</summary>
        public PointF Position { get; }

        /// <summary>Gets how big it is drawn, relative to its size: from 2 to 1 while the eye settles on it, 1 while recording.</summary>
        public double Scale { get; }

        /// <summary>Gets whether the data of the target is being collected.</summary>
        public bool Recording { get; }

        /// <summary>Gets which target this is, from 1.</summary>
        public int Number { get; }

        /// <summary>Gets how many targets there are.</summary>
        public int Count { get; }
    }

    /// <summary>
    /// The sequence of targets of the N-point calibration when OpenIris controls it (the Local and
    /// Remote display modes): for each target, it moves from the previous one, shrinks while the eye
    /// settles on it, and then its data is collected. A function of the time since the start.
    /// </summary>
    public sealed class NPointSequence
    {
        /// <summary>Time to move from one target to the next, in seconds.</summary>
        public const double MoveSeconds = 0.25;

        private readonly double settleSeconds;
        private readonly double collectSeconds;

        /// <summary>
        /// Initializes the sequence.
        /// </summary>
        /// <param name="points">The targets, in order.</param>
        /// <param name="settleSeconds">Time for the eye to settle on each target before collecting.</param>
        /// <param name="collectSeconds">Time collecting the data of each target.</param>
        public NPointSequence(IReadOnlyList<PointF> points, double settleSeconds, double collectSeconds)
        {
            if (points.Count == 0) throw new ArgumentException("No targets.", nameof(points));
            Points = points;
            this.settleSeconds = Math.Max(0, settleSeconds);
            this.collectSeconds = Math.Max(0.05, collectSeconds);
        }

        /// <summary>Gets the targets, in order.</summary>
        public IReadOnlyList<PointF> Points { get; }

        private double SecondsPerTarget => MoveSeconds + settleSeconds + collectSeconds;

        /// <summary>Gets how long the whole sequence takes, in seconds.</summary>
        public double Duration => Points.Count * SecondsPerTarget;

        /// <summary>
        /// The target at a time since the start, or null once the sequence is over.
        /// </summary>
        /// <param name="seconds">Seconds since the start.</param>
        public NPointTarget? TargetAt(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var index = (int)Math.Floor(seconds / SecondsPerTarget);
            if (index >= Points.Count) return null;

            var t = seconds - index * SecondsPerTarget;
            var point = Points[index];
            if (t < MoveSeconds)
            {
                var from = index == 0 ? point : Points[index - 1];
                var f = (float)(t / MoveSeconds);
                return new NPointTarget(new PointF(from.X + (point.X - from.X) * f, from.Y + (point.Y - from.Y) * f), 2, false, index + 1, Points.Count);
            }

            t -= MoveSeconds;
            if (t < settleSeconds) return new NPointTarget(point, 2 - t / settleSeconds, false, index + 1, Points.Count);

            return new NPointTarget(point, 1, true, index + 1, Points.Count);
        }

        /// <summary>
        /// The usual layouts of 5, 9 or 13 targets: the center first, then the others in random order
        /// so the subject can't anticipate them. 5: the center and the corners; 9: a 3 × 3 grid; 13: the
        /// grid and the corners of a grid half as big.
        /// </summary>
        /// <param name="count">5, 9 or 13.</param>
        /// <param name="extentX">Horizontal distance from the center to the outer targets, in degrees.</param>
        /// <param name="extentY">Vertical distance from the center to the outer targets, in degrees.</param>
        /// <param name="random">For the order of the targets.</param>
        public static PointF[] Layout(int count, float extentX, float extentY, Random random)
        {
            var ring = new List<PointF>();
            if (count == 5)
            {
                ring.AddRange(new[] { new PointF(-extentX, extentY), new PointF(extentX, extentY), new PointF(-extentX, -extentY), new PointF(extentX, -extentY) });
            }
            else
            {
                foreach (var y in new[] { extentY, 0, -extentY })
                {
                    foreach (var x in new[] { -extentX, 0, extentX })
                    {
                        if (x != 0 || y != 0) ring.Add(new PointF(x, y));
                    }
                }

                if (count == 13)
                {
                    ring.AddRange(new[] { new PointF(-extentX / 2, extentY / 2), new PointF(extentX / 2, extentY / 2), new PointF(-extentX / 2, -extentY / 2), new PointF(extentX / 2, -extentY / 2) });
                }
            }

            return new[] { new PointF(0, 0) }.Concat(ring.OrderBy(_ => random.Next())).ToArray();
        }
    }
}
