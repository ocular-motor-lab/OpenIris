//-----------------------------------------------------------------------
// <copyright file="EyelidData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
    using System;
    using System.Drawing;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the data about the eyelids.
    /// </summary>
    [Serializable]
    public class EyelidData
    {
        /// <summary>
        /// Initializes a new item of the EyelidData class.
        /// </summary>
        public EyelidData()
        {
            this.Upper = new PointF[4];
            this.Lower = new PointF[4];
        }

        /// <summary>
        /// Gets or sets the eyelid points of the upper eyelid contour.
        /// </summary>
        public PointF[] Upper { get; set; }

        /// <summary>
        /// Gets or sets the eyelid points of the lower eyelid contour.
        /// </summary>
        public PointF[] Lower { get; set; }

        /// <summary>
        /// Copies the eyelid data.
        /// </summary>
        /// <returns></returns>
        public EyelidData Copy()
        {
            var eyelidData = new EyelidData();

            Upper.CopyTo(eyelidData.Upper, 0);
            Lower.CopyTo(eyelidData.Lower, 0);

            return eyelidData;
        }
    }
}
