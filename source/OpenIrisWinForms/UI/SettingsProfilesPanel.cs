//-----------------------------------------------------------------------
// <copyright file="SettingsProfilesPanel.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// The Profiles tab of the configuration window: the settings profiles, to switch between them
    /// and manage them, the general settings of the active profile, and the settings of this
    /// computer, which are the same in every profile.
    /// </summary>
    internal sealed class SettingsProfilesPanel : UserControl
    {
        private readonly EyeTracker eyeTracker;
        private readonly ListBox profilesList;
        private readonly Button useButton;
        private readonly Button renameButton;
        private readonly Button deleteButton;
        private readonly Button exportButton;
        private readonly PropertyGrid profileSettingsGrid;
        private readonly PropertyGrid computerSettingsGrid;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public SettingsProfilesPanel(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
            Dock = DockStyle.Fill;
            Padding = new Padding(8);

            // The profiles: a list, with the buttons to manage them on its right.
            profilesList = new ListBox { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = WindowsStyle.Scale(22), IntegralHeight = false };
            profilesList.DrawItem += DrawProfile;
            profilesList.SelectedIndexChanged += (o, e) => UpdateButtons();
            profilesList.DoubleClick += (o, e) => UseSelected();

            Button NewButton(string text, Action onClick)
            {
                var button = new Button { Text = text, Width = 110, Height = 30, Margin = new Padding(8, 0, 0, 6) };
                button.Click += (o, e) => onClick();
                return button;
            }

            useButton = NewButton("Use", UseSelected);
            var newButton = NewButton("New...", NewProfile);
            var duplicateButton = NewButton("Duplicate...", DuplicateSelected);
            renameButton = NewButton("Rename...", RenameSelected);
            deleteButton = NewButton("Delete", DeleteSelected);
            exportButton = NewButton("Export...", ExportSelected);
            var importButton = NewButton("Import...", Import);
            exportButton.Margin = new Padding(8, 12, 0, 6);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            buttons.Controls.AddRange(new Control[] { useButton, newButton, duplicateButton, renameButton, deleteButton, exportButton, importButton });

            var hint = new Label
            {
                Dock = DockStyle.Bottom,
                Padding = new Padding(0, 6, 0, 0),
                ForeColor = SystemColors.GrayText,
                Text = "Each profile has its own general settings (below) and eye tracking system, pipeline and calibration (Advanced tab). " +
                    "The active profile, in bold, is also chosen on the main window. Switching while tracking asks to stop first.",
            };

            // As many lines as the width needs (an AutoSize label docked at the bottom stays one line).
            hint.Resize += (o, e) => hint.Height = hint.GetPreferredSize(new Size(hint.Width, 0)).Height;

            var profilesBox = new GroupBox { Text = "Profiles", Dock = DockStyle.Top, Height = 340, Padding = new Padding(8) };
            profilesBox.Controls.Add(profilesList);
            profilesBox.Controls.Add(buttons);
            profilesBox.Controls.Add(hint);

            // Below, side by side: the general settings of the active profile and the shared settings.
            GroupBox SettingsBox(string text, PropertyGrid grid)
            {
                var box = new GroupBox { Text = text, Dock = DockStyle.Fill, Padding = new Padding(8) };
                box.Controls.Add(grid);
                return box;
            }

            PropertyGrid NewGrid(Func<PropertyDescriptor, bool> include) => new PropertyGrid
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false,
                PropertySort = PropertySort.Categorized,
                SelectedObject = new SettingsSubset(eyeTracker.Settings, include),
            };

            profileSettingsGrid = NewGrid(p => !SettingsSubset.IsShared(p));
            computerSettingsGrid = NewGrid(SettingsSubset.IsShared);
            WindowsStyle.FitHelpToDescription(profileSettingsGrid);
            WindowsStyle.FitHelpToDescription(computerSettingsGrid);
            var settingsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            settingsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            settingsTable.Controls.Add(SettingsBox("General settings of the active profile", profileSettingsGrid), 0, 0);
            settingsTable.Controls.Add(SettingsBox("Settings of this computer (the same in every profile)", computerSettingsGrid), 1, 0);

            Controls.Add(settingsTable);
            Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            Controls.Add(profilesBox);
            WindowsStyle.ScaleToScreen(this);

            UpdateProfiles();
        }

        /// <summary>
        /// Shows the profiles again, for example after switching or renaming.
        /// </summary>
        public void UpdateProfiles()
        {
            var selected = profilesList.SelectedItem as string ?? eyeTracker.Settings.Profile;
            var profiles = eyeTracker.Settings.Profiles;

            profilesList.BeginUpdate();
            profilesList.Items.Clear();
            profilesList.Items.AddRange(profiles);
            profilesList.SelectedItem = profiles.Contains(selected, StringComparer.OrdinalIgnoreCase) ? profiles.First(p => string.Equals(p, selected, StringComparison.OrdinalIgnoreCase)) : eyeTracker.Settings.Profile;
            profilesList.EndUpdate();

            RefreshSettings();
            UpdateButtons();
        }

        /// <summary>
        /// Shows the settings again, after they changed.
        /// </summary>
        public void RefreshSettings()
        {
            profileSettingsGrid.Refresh();
            computerSettingsGrid.Refresh();
        }

        private string? Selected => profilesList.SelectedItem as string;

        private bool IsActive(string? profile) => string.Equals(profile, eyeTracker.Settings.Profile, StringComparison.OrdinalIgnoreCase);

        private void UpdateButtons()
        {
            useButton.Enabled = Selected != null && !IsActive(Selected);
            renameButton.Enabled = Selected != null;
            deleteButton.Enabled = Selected != null && !IsActive(Selected);
            exportButton.Enabled = Selected != null;
        }

        private void DrawProfile(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0 && profilesList.Items[e.Index] is string profile)
            {
                using var font = new Font(e.Font, IsActive(profile) ? FontStyle.Bold : FontStyle.Regular);
                var text = IsActive(profile) ? $"{profile}   (active)" : profile;
                TextRenderer.DrawText(e.Graphics, text, font, e.Bounds, e.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            e.DrawFocusRectangle();
        }

        private async void UseSelected()
        {
            if (Selected is string profile && !IsActive(profile))
            {
                await ProfileSwitcher.SwitchAsync(this, eyeTracker, profile);
                UpdateProfiles();
            }
        }

        private void NewProfile()
        {
            var systems = EyeTrackerPluginManager.EyeTrackingsyStemFactory?.ClassesAvaiable.Select(c => c.Name).OrderBy(n => n).ToArray() ?? new string[0];
            var answer = NewProfileDialog.Show(this, systems, eyeTracker.Settings.EyeTrackerSystem);
            if (answer.HasValue)
            {
                var (name, system) = answer.Value;
                Try(() => eyeTracker.Settings.CreateProfile(name, system));
                Select(name);
            }
        }

        private void DuplicateSelected()
        {
            if (!(Selected is string profile)) return;

            var name = TextInputDialog.Show(this, "Duplicate profile", $"Name of the copy of '{profile}':", $"{profile} copy");
            if (name != null)
            {
                Try(() => eyeTracker.Settings.DuplicateProfile(profile, name));
                Select(name);
            }
        }

        private void RenameSelected()
        {
            if (!(Selected is string profile)) return;

            var name = TextInputDialog.Show(this, "Rename profile", $"New name of the profile '{profile}':", profile);
            if (name != null && name != profile)
            {
                Try(() => eyeTracker.Settings.RenameProfile(profile, name));
                Select(name);
            }
        }

        private void DeleteSelected()
        {
            if (!(Selected is string profile) || IsActive(profile)) return;

            if (MessageBox.Show(this, $"Delete the settings profile '{profile}'?", "Delete profile", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                Try(() => eyeTracker.Settings.DeleteProfile(profile));
            }
        }

        private void ExportSelected()
        {
            if (!(Selected is string profile)) return;

            using var dialog = new SaveFileDialog
            {
                Title = $"Export the settings profile '{profile}'",
                FileName = profile + ".xml",
                Filter = "OpenIris settings profile (*.xml)|*.xml",
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Try(() => eyeTracker.Settings.ExportProfile(profile, dialog.FileName));
            }
        }

        private void Import()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import a settings profile",
                Filter = "OpenIris settings profile (*.xml)|*.xml|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            // The name of the file, unless there is a profile with that name already.
            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            var suggestion = name;
            for (var i = 2; eyeTracker.Settings.Profiles.Contains(suggestion, StringComparer.OrdinalIgnoreCase); i++) suggestion = $"{name} {i}";

            var newName = TextInputDialog.Show(this, "Import profile", $"Name of the profile imported from {Path.GetFileName(dialog.FileName)}:", suggestion);
            if (newName != null)
            {
                Try(() => eyeTracker.Settings.ImportProfile(dialog.FileName, newName));
                Select(newName);
            }
        }

        private void Select(string name)
        {
            UpdateProfiles();
            var item = profilesList.Items.Cast<string>().FirstOrDefault(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
            if (item != null) profilesList.SelectedItem = item;
        }

        private void Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Settings profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateProfiles();
        }
    }
}
