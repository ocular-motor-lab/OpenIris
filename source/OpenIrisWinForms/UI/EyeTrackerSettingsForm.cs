//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettingsForm.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Windows.Forms;
    using OpenIris;

    /// <summary>
    /// Configuration window, with two tabs: Profiles (the settings profiles, the general settings of
    /// the active profile and the settings of this computer) and Advanced (the settings of the
    /// active profile's eye tracking system, pipeline and calibration).
    /// </summary>
    public partial class EyeTrackerSettingsForm : Form
    {
        private static EyeTrackerSettingsForm? instance;

        private readonly EyeTrackerSettings settings;
        private readonly SettingsProfilesPanel profilesPanel;

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker, with the settings.</param>
        public static EyeTrackerSettingsForm Show(EyeTracker eyeTracker)
        {
            if (eyeTracker is null) throw new ArgumentNullException(nameof(eyeTracker));
            var settings = eyeTracker.Settings;

            if (instance is null)
            {
                instance = new EyeTrackerSettingsForm(eyeTracker);
                instance.FormClosed += (o, e) =>
                {
                    settings.PropertyChanged -= instance.Settings_PropertyChanged;
                    settings.Save();

                    instance.Dispose();
                    instance = null;
                };
                instance.Show();

                settings.PropertyChanged += instance.Settings_PropertyChanged;
            }

            instance.BringToFront();
            return instance;
        }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerSettingsForm class.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker, with the settings.</param>
        private EyeTrackerSettingsForm(EyeTracker eyeTracker)
        {
            InitializeComponent();
            var settings = eyeTracker.Settings;

            // The tabs: Profiles, with the general settings, and Advanced, with the settings of the
            // system, the pipeline and the calibration.
            this.settings = settings;
            profilesPanel = new SettingsProfilesPanel(eyeTracker);
            var profilesTab = new TabPage("Profiles");
            profilesTab.Controls.Add(profilesPanel);
            var advancedTab = new TabPage("Advanced");
            Controls.Remove(tableLayoutPanel1);
            advancedTab.Controls.Add(tableLayoutPanel1);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(profilesTab);
            tabs.TabPages.Add(advancedTab);
            Controls.Add(tabs);

            propertyGridTrackingSettings.ToolbarVisible = false;
            propertyGridTrackingSettings.PropertySort = PropertySort.Categorized;

            propertyGridSystemSettings.ToolbarVisible = false;
            propertyGridSystemSettings.PropertySort = PropertySort.Categorized;

            propertyGridCalibrationSettings.ToolbarVisible = false;
            propertyGridCalibrationSettings.PropertySort = PropertySort.Categorized;

            propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
            propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
            propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;

            UpdateTitle(settings);
        }

        private void UpdateTitle(EyeTrackerSettings settings) => Text = $"Configuration - profile {settings.Profile}";

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
            propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
            propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;

            profilesPanel.RefreshSettings();
            propertyGridSystemSettings.Refresh();
            propertyGridTrackingSettings.Refresh();
            propertyGridCalibrationSettings.Refresh();

            if (e.PropertyName == nameof(EyeTrackerSettings.Profile) || e.PropertyName == nameof(EyeTrackerSettings.Profiles))
            {
                profilesPanel.UpdateProfiles();
                UpdateTitle(settings);
            }
        }
    }
}
