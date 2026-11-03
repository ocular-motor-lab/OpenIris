//-----------------------------------------------------------------------
// <copyright file="HeadCalibration.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;

    /// <summary>
    /// Class containing all the info regarding the calibration of the head.
    /// </summary>
    [Serializable]
    public class HeadCalibration
    {
        /// <summary>
        /// Gets or sets the head data at the reference: the head angles are relative to it.
        /// </summary>
        public HeadData ReferenceHeadData { get; set; }

        /// <summary>
        /// Gets or sets where the eyes are in the head. Calibration files from before 0.2 have the
        /// defaults.
        /// </summary>
        public HeadModel HeadModel { get; set; } = new HeadModel();

        /// <summary>
        /// Makes a copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public HeadCalibration Copy() => new HeadCalibration { ReferenceHeadData = ReferenceHeadData, HeadModel = HeadModel.Copy() };

        /// <summary>
        /// Update the calibration info with the head datae.
        /// </summary>
        /// <param name="headData">Head data.</param>
        internal void UpdateInfo(HeadData headData)
        {
            ReferenceHeadData = headData;
        }
    }
}
