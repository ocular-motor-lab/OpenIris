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
    /// Configuration window, with two tabs: Profiles (the settings profiles and the settings of this
    /// computer) and Advanced (all the settings of the active profile).
    /// </summary>
    public partial class EyeTrackerSettingsForm : Form
    {
        private static EyeTrackerSettingsForm? instance;

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

            // The tabs: the four columns of settings go in Advanced.
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

            propertyGridGeneralSettings.ToolbarVisible = false;
            propertyGridGeneralSettings.PropertySort = PropertySort.Categorized;

            propertyGridTrackingSettings.ToolbarVisible = false;
            propertyGridTrackingSettings.PropertySort = PropertySort.Categorized;

            propertyGridSystemSettings.ToolbarVisible = false;
            propertyGridSystemSettings.PropertySort = PropertySort.Categorized;

            propertyGridCalibrationSettings.ToolbarVisible = false;
            propertyGridCalibrationSettings.PropertySort = PropertySort.Categorized;

            // The shared settings are in the Profiles tab: here only the profile's own.
            propertyGridGeneralSettings.SelectedObject = new SettingsSubset(settings, p => !SettingsSubset.IsShared(p));
            propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
            propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
            propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;

            UpdateTitle(settings);
        }

        private void UpdateTitle(EyeTrackerSettings settings) => Text = $"Configuration - profile {settings.Profile}";

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settings = (propertyGridGeneralSettings.SelectedObject as SettingsSubset)?.Settings as EyeTrackerSettings ?? throw new InvalidOperationException("never should happen");

            propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
            propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
            propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;

            propertyGridGeneralSettings.Refresh();
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
