//-----------------------------------------------------------------------
// <copyright file="TextInputDialog.cs">
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
    /// Asks for a line of text, such as the name of a settings profile.
    /// </summary>
    public static class TextInputDialog
    {
        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <returns>The text, or null if cancelled.</returns>
        public static string? Show(IWin32Window? owner, string title, string prompt, string text = "")
        {
            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(400, 110),
            };
            var label = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
            var textBox = new TextBox { Text = text, Location = new Point(12, 36), Width = 376 };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(232, 72) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(313, 72) };
            form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
