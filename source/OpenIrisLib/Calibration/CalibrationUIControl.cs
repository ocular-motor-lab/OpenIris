using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenIris
{
    /// <summary>
    /// Interface for Calibration UI.
    /// </summary>
    public interface ICalibrationUIControl
    {
        /// <summary>
        /// Update the UI.
        /// </summary>
        void UpdateUI();
    }
}
