//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationData.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// What a calibration found for one eye beyond the eye model and the reference, and how it changes
    /// the calibrated data: for example a map fitted from targets (LinearGazeMap, by the Remote Linear
    /// calibration). Each calibration can have its own kind, used as it needs: replacing the angles of
    /// the eye model, correcting them, or adding to them.
    /// </summary>
    /// <remarks>
    /// Saved in the calibration file (.cal) with its type, so the data of a recording can be
    /// calibrated again exactly the same way. Kinds defined in plugins are found in the assemblies of
    /// the calibration plugins (EyeTrackerPluginManager); they must be serializable as XML (public
    /// properties, a constructor without parameters).
    /// </remarks>
    [Serializable]
    [XmlInclude(typeof(LinearGazeMap))]
    public abstract class EyeCalibrationData
    {
        /// <summary>
        /// The calibrated data of a frame tracked well.
        /// </summary>
        /// <param name="eyeData">Data of the frame.</param>
        /// <param name="fromEyeModel">The calibrated data from the eye model and the reference.</param>
        /// <returns>The calibrated data, changed as this calibration needs.</returns>
        public abstract CalibratedEyeData Adjust(EyeData eyeData, CalibratedEyeData fromEyeModel);
    }
}
