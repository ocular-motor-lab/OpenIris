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
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// The Profiles tab of the configuration window: the settings profiles, to switch between them
    /// and manage them, and the settings of this computer, which are the same in every profile.
    /// </summary>
    internal sealed class SettingsProfilesPanel : UserControl
    {
        private readonly EyeTracker eyeTracker;
        private readonly ListBox profilesList;
        private readonly Button useButton;
        private readonly Button renameButton;
        private readonly Button deleteButton;
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
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            buttons.Controls.AddRange(new Control[] { useButton, newButton, duplicateButton, renameButton, deleteButton });

            var hint = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0),
                ForeColor = SystemColors.GrayText,
                Text = "Each profile has its own eye tracking system, pipeline, calibration and settings (Advanced tab). " +
                    "The active profile, in bold, is also chosen on the main window. Switching while tracking asks to stop first.",
            };

            var profilesBox = new GroupBox { Text = "Profiles", Dock = DockStyle.Top, Height = 260, Padding = new Padding(8) };
            profilesBox.Controls.Add(profilesList);
            profilesBox.Controls.Add(buttons);
            profilesBox.Controls.Add(hint);

            // The shared settings.
            computerSettingsGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false,
                PropertySort = PropertySort.Categorized,
                SelectedObject = new SettingsSubset(eyeTracker.Settings, SettingsSubset.IsShared),
            };
            var computerBox = new GroupBox { Text = "Settings of this computer (the same in every profile)", Dock = DockStyle.Fill, Padding = new Padding(8) };
            computerBox.Controls.Add(computerSettingsGrid);

            Controls.Add(computerBox);
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

            computerSettingsGrid.Refresh();
            UpdateButtons();
        }

        private string? Selected => profilesList.SelectedItem as string;

        private bool IsActive(string? profile) => string.Equals(profile, eyeTracker.Settings.Profile, StringComparison.OrdinalIgnoreCase);

        private void UpdateButtons()
        {
            useButton.Enabled = Selected != null && !IsActive(Selected);
            renameButton.Enabled = Selected != null;
            deleteButton.Enabled = Selected != null && !IsActive(Selected);
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
