using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace OpenIris.UI
{
    /// <summary>
    /// Extension methods for the GUI.
    /// </summary>
    public static class RichTextBoxExtensions
    {
        /// <summary>
        /// Adds text to the rich text box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="text">The text.</param>
        /// <param name="color">The color.</param>
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}
