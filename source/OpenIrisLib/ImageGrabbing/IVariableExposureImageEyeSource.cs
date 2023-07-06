//-----------------------------------------------------------------------
// <copyright file="IMovableImageEyeSource.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System.Drawing;

    /// <summary>
    /// Interface of cameras that can move the ROI.
    /// </summary>
    public interface IVariableExposureImageEyeSource
    {
        /// <summary>
        /// </summary>
        /// <returns>True if exposure changed.</returns>
        bool IncreaseExposure();

        /// <summary>
        /// </summary>
        /// <returns>True if exposure changed.</returns>
        bool ReduceExposure();
    }
}
