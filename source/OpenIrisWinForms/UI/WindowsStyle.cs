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
