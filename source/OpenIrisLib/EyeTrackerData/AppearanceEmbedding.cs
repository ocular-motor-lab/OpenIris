//-----------------------------------------------------------------------
// <copyright file="AppearanceEmbedding.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;

    /// <summary>
    /// What a gaze network found in an image of both eyes in one frame (appearance-based gaze, for
    /// example the webcam's eye-embedding network): a list of numbers that change with where the eyes
    /// look, which an <see cref="AppearanceGazeMap"/> fitted by the N-point calibration turns into gaze
    /// angles, and how much the network trusts the frame. Not saved in the data files.
    /// </summary>
    [Serializable]
    public sealed class AppearanceEmbedding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceEmbedding"/> class.
        /// </summary>
        /// <param name="values">The embedding.</param>
        /// <param name="weight">How much the network trusts the frame, 0 to 1.</param>
        public AppearanceEmbedding(float[] values, float weight)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Weight = weight;
        }

        /// <summary>
        /// Gets the embedding. Its length depends on the network (128 for the webcam's).
        /// </summary>
        public float[] Values { get; }

        /// <summary>
        /// Gets how much the network trusts the frame, 0 to 1: low in blinks or when looking away.
        /// The calibration weights the frames with it.
        /// </summary>
        public float Weight { get; }
    }
}
