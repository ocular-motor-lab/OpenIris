//-----------------------------------------------------------------------
// <copyright file="PupilData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
    using System;
    using System.Drawing;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the geometric properties of the measured pupil.
    /// </summary>
    [Serializable]
    public struct PupilData
    {
        /// <summary>
        /// The center of the box.
        /// </summary>
        public PointF Center { get; set; }

        /// <summary>
        /// The size of the box.
        /// </summary>
        public SizeF Size { get; set; }

        /// <summary>
        /// The angle between the horizontal axis and the first side (i.e. width) in degrees.
        /// </summary>
        /// <remarks>Positive value means counter-clock wise rotation.</remarks>
        public float Angle { get; set; }

        /// <summary>
        /// Gets a value indicating if the pupil information is empty.
        /// </summary>
        /// <returns>True if the pupil is empty.</returns>
        public bool IsEmpty => Size.IsEmpty;

        /// <summary>
        /// Create a PupilData structure with the specific parameters.
        /// </summary>
        /// <param name="center">The center of the box.</param>
        /// <param name="size">The size of the box.</param>
        /// <param name="angle">The angle of the box in degrees. Possitive value means counter-clock wise rotation.</param>
        public PupilData(PointF center, SizeF size, float angle)
            : this()
        {
            this.Center = center;
            this.Size = size;
            this.Angle = angle;
        }

        /// <summary>
        /// Initializes a new instance of the Ellipse class from a PupilData object.
        /// </summary>
        /// <param name="pupilData">Pupil Data.</param>
        /// <returns>New Ellipse object.</returns>
        public static implicit operator Ellipse(PupilData pupilData)
        {
            return new Ellipse(pupilData.Center, pupilData.Size, pupilData.Angle);
        }
    }
}
