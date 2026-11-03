//-----------------------------------------------------------------------
// <copyright file="CalibrationModelParameters.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;

    /// <summary>
    /// What a calibration found for the whole calibration, not one eye: for example where a screen
    /// is, or a correction fitted on both eyes together. Each calibration can have its own kind, with
    /// the fields it needs. The parameters of each eye are in <see cref="EyeCalibrationData"/>.
    /// </summary>
    /// <remarks>
    /// Saved in the calibration file (.cal) with its type. Kinds defined in plugins are found in the
    /// assemblies of the calibration plugins (EyeTrackerPluginManager); they must be serializable as
    /// XML (public properties, a constructor without parameters).
    /// </remarks>
    [Serializable]
    public abstract class CalibrationModelParameters
    {
    }
}
