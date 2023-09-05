//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettingsForm.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
    /// Configuration window.
    /// </summary>
    public partial class EyeTrackerSettingsForm : Form
    {
        private static EyeTrackerSettingsForm? instance;

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="settings">Eye Tracker Settings object.</param>
        public static EyeTrackerSettingsForm Show(EyeTrackerSettings settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (instance is null)
            {
                instance = new EyeTrackerSettingsForm(settings);
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
        /// <param name="settings">Eye Tracker Settings object.</param>
        private EyeTrackerSettingsForm(EyeTrackerSettings settings)
        {
            InitializeComponent();

            propertyGridGeneralSettings.ToolbarVisible = false;
            propertyGridGeneralSettings.PropertySort = PropertySort.Categorized;

            propertyGridTrackingSettings.ToolbarVisible = false;
            propertyGridTrackingSettings.PropertySort = PropertySort.Categorized;

            propertyGridSystemSettings.ToolbarVisible = false;
            propertyGridSystemSettings.PropertySort = PropertySort.Categorized;

            propertyGridCalibrationSettings.ToolbarVisible = false;
            propertyGridCalibrationSettings.PropertySort = PropertySort.Categorized;

            propertyGridGeneralSettings.SelectedObject = settings;
            propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
            propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
            propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;
        }

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settings = propertyGridGeneralSettings.SelectedObject as EyeTrackerSettings ?? throw new InvalidOperationException("never should happen");

            this.BeginInvoke(new Action(() =>
            {
                propertyGridSystemSettings.SelectedObject = settings.EyeTrackingSystemSettings;
                propertyGridTrackingSettings.SelectedObject = settings.TrackingPipelineSettings;
                propertyGridCalibrationSettings.SelectedObject = settings.CalibrationSettings;

                propertyGridGeneralSettings.Refresh();
                propertyGridTrackingSettings.Refresh();
                propertyGridCalibrationSettings.Refresh();
            }));
        }

    }
}
