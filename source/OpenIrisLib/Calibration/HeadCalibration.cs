//-----------------------------------------------------------------------
// <copyright file="HeadCalibration.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
        public HeadData ReferenceHeadData { get; set; }

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
