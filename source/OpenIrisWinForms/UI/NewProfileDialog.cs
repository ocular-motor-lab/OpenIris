//-----------------------------------------------------------------------
// <copyright file="NewProfileDialog.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Asks for the name and the eye tracking system of a new settings profile.
    /// </summary>
    public static class NewProfileDialog
    {
        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="owner">Window the dialog belongs to.</param>
        /// <param name="systems">Eye tracking systems to choose from.</param>
        /// <param name="system">Eye tracking system selected at first.</param>
        /// <returns>The name and the system, or null if cancelled.</returns>
        public static (string name, string system)? Show(IWin32Window? owner, string[] systems, string system)
        {
            using var form = new Form
            {
                Text = "New profile",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(400, 170),
            };
            var nameLabel = new Label { Text = "Name:", Location = new Point(12, 12), AutoSize = true };
            var nameBox = new TextBox { Location = new Point(12, 36), Width = 376 };
            var systemLabel = new Label { Text = "Eye tracking system (it starts with its default settings):", Location = new Point(12, 70), AutoSize = true };
            var systemBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 94), Width = 376 };
            systemBox.Items.AddRange(systems);
            systemBox.SelectedItem = system;
            if (systemBox.SelectedIndex < 0 && systems.Length > 0) systemBox.SelectedIndex = 0;
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(232, 132) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(313, 132) };
            form.Controls.AddRange(new Control[] { nameLabel, nameBox, systemLabel, systemBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            WindowsStyle.ScaleToScreen(form);

            if (form.ShowDialog(owner) != DialogResult.OK || !(systemBox.SelectedItem is string selected)) return null;
            return (nameBox.Text.Trim(), selected);
        }
    }
}
