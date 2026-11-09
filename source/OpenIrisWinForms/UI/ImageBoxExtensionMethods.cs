//-----------------------------------------------------------------------
// <copyright file="ImageBoxExtensionMethods.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using System.Drawing;
    using System.Windows.Forms;
    using Emgu.CV;

    /// <summary>
    /// Helpers for the Emgu image boxes.
    /// </summary>
    public static class ImageBoxExtensionMethods
    {
        /// <summary>
        /// From: http://www.codeproject.com/script/Articles/View.aspx?aid=859100
        /// </summary>
        /// <param name="p">The point to be converted.</param>
        /// <param name="imageBox">The image box.</param>
        public static Point ConvertCoordinates(this Point p, Emgu.CV.UI.ImageBox imageBox)
        {
            var pic = imageBox;

            int pic_hgt = pic.ClientSize.Height;
            int pic_wid = pic.ClientSize.Width;
            int img_hgt = pic.Image.GetInputArray().GetSize().Height;
            int img_wid = pic.Image.GetInputArray().GetSize().Width;

            var x = p.X;
            var y = p.Y;

            var X0 = x;
            var Y0 = y;
            switch (pic.SizeMode)
            {
                case PictureBoxSizeMode.AutoSize:
                case PictureBoxSizeMode.Normal:
                    // These are okay. Leave them alone.
                    break;
                case PictureBoxSizeMode.CenterImage:
                    X0 = x - (pic_wid - img_wid) / 2;
                    Y0 = y - (pic_hgt - img_hgt) / 2;
                    break;
                case PictureBoxSizeMode.StretchImage:
                    X0 = (int)(img_wid * x / (float)pic_wid);
                    Y0 = (int)(img_hgt * y / (float)pic_hgt);
                    break;
                case PictureBoxSizeMode.Zoom:
                    // The image fills the width or the height, keeping its proportions, centered.
                    float scale = System.Math.Min(pic_wid / (float)img_wid, pic_hgt / (float)img_hgt);
                    float dx = (pic_wid - (img_wid * scale)) / 2;
                    float dy = (pic_hgt - (img_hgt * scale)) / 2;
                    X0 = (int)((x - dx) / scale);
                    Y0 = (int)((y - dy) / scale);
                    break;
            }

            return new Point(X0, Y0);
        }
    }
}
