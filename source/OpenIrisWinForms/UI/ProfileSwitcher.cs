//-----------------------------------------------------------------------
// <copyright file="ProfileSwitcher.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// Switches the settings profile from the user interface: if OpenIris is tracking, it asks to
    /// stop first, because the eye tracking system may change.
    /// </summary>
    public static class ProfileSwitcher
    {
        /// <summary>
        /// Switches to a profile, asking to stop tracking (and recording) first if needed.
        /// </summary>
        /// <param name="owner">Window for the questions and errors.</param>
        /// <param name="eyeTracker">The eye tracker.</param>
        /// <param name="profile">Name of the profile.</param>
        /// <returns>True if the profile was switched.</returns>
        public static async Task<bool> SwitchAsync(IWin32Window owner, EyeTracker eyeTracker, string profile)
        {
            if (!eyeTracker.NotStarted)
            {
                var stop = eyeTracker.Recording ? "Stop tracking and recording" : "Stop tracking";
                if (MessageBox.Show(owner, $"{stop}, and switch to the profile '{profile}'?", "Switch profile", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                {
                    return false;
                }

                eyeTracker.StopTracking();

                // Stopping the cameras and finishing the recording take a moment.
                var waiting = Stopwatch.StartNew();
                while (!eyeTracker.NotStarted)
                {
                    if (waiting.Elapsed > TimeSpan.FromSeconds(30))
                    {
                        MessageBox.Show(owner, "Tracking didn't stop, so the profile wasn't switched. See the log.", "Switch profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    await Task.Delay(100);
                }
            }

            try
            {
                eyeTracker.SwitchSettingsProfile(profile);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException)
            {
                MessageBox.Show(owner, ex.Message, "Switch profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
