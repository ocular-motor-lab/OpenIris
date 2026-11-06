//-----------------------------------------------------------------------
// <copyright file="WindowsStyle.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms;

    /// <summary>
    /// The look of the OpenIris windows: the Windows 10/11 font and flat menus and toolbars.
    /// </summary>
    public static class WindowsStyle
    {
        /// <summary>
        /// The font of every control that doesn't set its own: Segoe UI 9, the Windows UI font.
        /// </summary>
        public static Font DefaultFont { get; } = new Font("Segoe UI", 9F);

        /// <summary>
        /// Applies the style. Call it in Main after Application.SetCompatibleTextRenderingDefault, before
        /// any window is created.
        /// </summary>
        public static void Apply()
        {
            // .NET Framework has no Application.SetDefaultFont (.NET 6+), which does just this:
            // controls without a font of their own use Control.DefaultFont, Microsoft Sans Serif 8.25
            // otherwise. The forms scale their layout to the new font (AutoScaleMode.Font).
            typeof(Control).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, DefaultFont);

            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new FlatColorTable()) { RoundedEdges = false };

            Application.AddMessageFilter(new HideFocusCuesOnClick());
        }

        /// <summary>
        /// Hides the dotted focus rectangles (focus cues) when a window is clicked, as Windows 10/11 apps do.
        /// Windows shows them in a window once the keyboard was used in it, or if it was opened from the
        /// keyboard (Enter, F5 in Visual Studio), and then a clicked tab, button or check box gets one.
        /// Tab and Alt show them again, for the keyboard.
        /// </summary>
        private class HideFocusCuesOnClick : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_RBUTTONDOWN = 0x0204;
            private const int WM_CHANGEUISTATE = 0x0127;
            private const int WM_QUERYUISTATE = 0x0129;
            private const int UIS_SET = 1;
            private const int UISF_HIDEFOCUS = 0x1;
            private const int GA_ROOT = 2;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN) return false;

                // Before the click is handled, so the control it focuses paints without the rectangle.
                // The top-level window passes the new state to all its controls.
                var window = GetAncestor(m.HWnd, GA_ROOT);
                if (window != System.IntPtr.Zero && ((int)SendMessage(window, WM_QUERYUISTATE, System.IntPtr.Zero, System.IntPtr.Zero) & UISF_HIDEFOCUS) == 0)
                {
                    SendMessage(window, WM_CHANGEUISTATE, (System.IntPtr)(UIS_SET | (UISF_HIDEFOCUS << 16)), System.IntPtr.Zero);
                }

                return false;
            }

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr GetAncestor(System.IntPtr hwnd, int flags);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr SendMessage(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam);
        }

        /// <summary>
        /// The display scaling: 1 at 100% (96 DPI), 1.5 at 150%.
        /// </summary>
        public static float ScreenScale
        {
            get
            {
                using var g = Graphics.FromHwnd(System.IntPtr.Zero);
                return g.DpiX / 96F;
            }
        }

        /// <summary>
        /// Pixels at 100% scaling, in pixels of the screen.
        /// </summary>
        public static int Scale(int pixels) => (int)System.Math.Round(pixels * ScreenScale);

        /// <summary>
        /// Scales a window or control laid out in code, in pixels at 100% scaling, to the screen.
        /// The ones from the designer scale themselves (AutoScaleMode).
        /// </summary>
        public static void ScaleToScreen(Control control)
        {
            var scale = ScreenScale;
            if (scale != 1F) control.Scale(new SizeF(scale, scale));
        }

        /// <summary>
        /// Makes the help at the bottom of a PropertyGrid as tall as the description of the selected
        /// setting needs, so it can be read whole (it is two lines otherwise, cut with "..."). Up to
        /// half of the grid; it can still be resized by dragging its top edge.
        /// </summary>
        public static void FitHelpToDescription(PropertyGrid grid)
        {
            // The help is a private control of the PropertyGrid (.NET Framework 4.8): its Lines
            // property sets its height, and userSized keeps the grid from setting it back.
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            var help = typeof(PropertyGrid).GetField("doccomment", Private)?.GetValue(grid) as Control;
            var lines = help?.GetType().GetProperty("Lines", Private | BindingFlags.Public);
            var lineHeightField = help?.GetType().GetField("lineHeight", Private);
            var description = help?.GetType().GetField("m_labelDesc", Private)?.GetValue(help) as Label;
            var fullDescription = help?.GetType().GetField("fullDesc", Private); // the label may have it cut with "..."
            var userSized = help?.GetType().BaseType?.GetField("userSized", Private);
            var layout = typeof(PropertyGrid).GetMethod("OnLayoutInternal", Private);
            if (help is null || lines is null || lineHeightField is null || description is null || fullDescription is null || userSized is null || layout is null) return;
            var measure = new Label { Padding = description.Padding };

            void Fit()
            {
                if (!help.Visible || description.Width <= 0) return;

                _ = lines.GetValue(help); // updates lineHeight to the font
                var lineHeight = System.Math.Max(1, (int)lineHeightField.GetValue(help));
                measure.Font = description.Font;
                measure.Text = fullDescription.GetValue(help) as string ?? description.Text;
                var textHeight = measure.GetPreferredSize(new Size(description.Width, 0)).Height;
                var count = 2 + System.Math.Max(1, (textHeight + lineHeight - 1) / lineHeight); // the name, the description, and the line the help keeps free
                count = System.Math.Min(count, System.Math.Max(3, grid.Height / 2 / lineHeight));

                userSized.SetValue(help, true);
                if ((int)lines.GetValue(help) == count) return;
                lines.SetValue(help, count);
                layout.Invoke(grid, new object[] { false }); // PerformLayout doesn't move the help

            }

            // After the grid has shown the new description in the help.
            void FitLater()
            {
                if (grid.IsHandleCreated) grid.BeginInvoke((System.Action)Fit);
            }

            grid.HandleCreated += (o, e) => FitLater(); // the first setting is selected before
            grid.VisibleChanged += (o, e) => FitLater();
            grid.SelectedGridItemChanged += (o, e) => FitLater();
            grid.SelectedObjectsChanged += (o, e) => FitLater();
            grid.SizeChanged += (o, e) => FitLater();
        }

        /// <summary>
        /// Menus and toolbars with flat colors instead of the gradients of Office 2003.
        /// </summary>
        private class FlatColorTable : ProfessionalColorTable
        {
            private static readonly Color Background = SystemColors.Control;
            private static readonly Color Hover = Color.FromArgb(229, 241, 251);
            private static readonly Color HoverBorder = Color.FromArgb(204, 228, 247);
            private static readonly Color Pressed = Color.FromArgb(204, 228, 247);
            private static readonly Color Border = Color.FromArgb(204, 204, 204);

            public override Color MenuStripGradientBegin => Background;
            public override Color MenuStripGradientEnd => Background;
            public override Color ToolStripGradientBegin => Background;
            public override Color ToolStripGradientMiddle => Background;
            public override Color ToolStripGradientEnd => Background;
            public override Color ToolStripBorder => Background;
            public override Color StatusStripGradientBegin => Background;
            public override Color StatusStripGradientEnd => Background;

            public override Color ToolStripDropDownBackground => SystemColors.Window;
            public override Color ImageMarginGradientBegin => SystemColors.Window;
            public override Color ImageMarginGradientMiddle => SystemColors.Window;
            public override Color ImageMarginGradientEnd => SystemColors.Window;
            public override Color MenuBorder => Border;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => SystemColors.Window;

            public override Color MenuItemSelected => Hover;
            public override Color MenuItemBorder => HoverBorder;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemPressedGradientBegin => SystemColors.Window;
            public override Color MenuItemPressedGradientMiddle => SystemColors.Window;
            public override Color MenuItemPressedGradientEnd => SystemColors.Window;

            public override Color ButtonSelectedHighlight => Hover;
            public override Color ButtonSelectedBorder => HoverBorder;
            public override Color ButtonSelectedGradientBegin => Hover;
            public override Color ButtonSelectedGradientMiddle => Hover;
            public override Color ButtonSelectedGradientEnd => Hover;
            public override Color ButtonPressedHighlight => Pressed;
            public override Color ButtonPressedBorder => HoverBorder;
            public override Color ButtonPressedGradientBegin => Pressed;
            public override Color ButtonPressedGradientMiddle => Pressed;
            public override Color ButtonPressedGradientEnd => Pressed;
            public override Color ButtonCheckedHighlight => Pressed;
            public override Color ButtonCheckedGradientBegin => Pressed;
            public override Color ButtonCheckedGradientMiddle => Pressed;
            public override Color ButtonCheckedGradientEnd => Pressed;
            public override Color CheckBackground => Pressed;
            public override Color CheckSelectedBackground => Pressed;
            public override Color CheckPressedBackground => Pressed;
            public override Color GripDark => Border;
            public override Color GripLight => Background;
            public override Color OverflowButtonGradientBegin => Background;
            public override Color OverflowButtonGradientMiddle => Background;
            public override Color OverflowButtonGradientEnd => Background;
        }
    }
}
