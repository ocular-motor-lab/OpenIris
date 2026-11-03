//-----------------------------------------------------------------------
// <copyright file="ImageView.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.CvEnum;

    /// <summary>
    /// Shows an image, fast: OpenCV resizes it to the size on the screen and GDI copies it, without
    /// the scaling of GDI+ (Emgu's ImageBox took ~60 ms per eye image at 150% scaling, more than the
    /// whole 100 ms of a UI refresh). Only for showing: no zoom, pan or menu.
    /// </summary>
    public class ImageView : Control
    {
        private readonly Mat resized = new Mat();
        private readonly Mat frame = new Mat();
        private IInputArray? image;
        private Rectangle imageBounds;
        private bool hasFrame;
        private string caption = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageView"/> class.
        /// </summary>
        public ImageView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>
        /// Gets or sets the image: 8 bit, gray, BGR or BGRA. It is copied, resized, when set: it can
        /// change or be disposed afterwards. After a resize the control shows it again when the next
        /// image is set.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IInputArray? Image
        {
            get => image;
            set
            {
                image = value;
                Render();
            }
        }

        /// <summary>
        /// Gets or sets how the image fills the control: Zoom (keeps the proportions) or StretchImage.
        /// </summary>
        [DefaultValue(PictureBoxSizeMode.Zoom)]
        public PictureBoxSizeMode SizeMode { get; set; } = PictureBoxSizeMode.Zoom;

        /// <summary>
        /// Gets or sets whether the image goes at the top of the control (Zoom), not in the middle.
        /// </summary>
        [DefaultValue(false)]
        public bool AlignTop { get; set; }

        /// <summary>
        /// Gets or sets a short text drawn over the top left corner, for example which eye it is.
        /// </summary>
        [DefaultValue("")]
        public string Caption
        {
            get => caption;
            set
            {
                caption = value ?? string.Empty;
                Invalidate();
            }
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            // The background around the image, not under it: no flicker.
            using (var background = new SolidBrush(BackColor))
            using (var around = new Region(ClientRectangle))
            {
                if (hasFrame) around.Exclude(imageBounds);
                g.FillRegion(background, around);
            }

            if (hasFrame)
            {
                var header = new BITMAPINFOHEADER
                {
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = frame.Width,
                    biHeight = -frame.Height, // Top down.
                    biPlanes = 1,
                    biBitCount = 32,
                };
                var hdc = g.GetHdc();
                try
                {
                    SetDIBitsToDevice(hdc, imageBounds.X, imageBounds.Y, frame.Width, frame.Height, 0, 0, 0, frame.Height, frame.DataPointer, ref header, 0);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }

            if (caption.Length > 0)
            {
                TextRenderer.DrawText(g, caption, Font, Point.Empty, ForeColor, BackColor);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                resized.Dispose();
                frame.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Resizes the image to the screen, as 32 bit BGRA, the format GDI copies directly.
        /// </summary>
        private void Render()
        {
            if (image is null || ClientSize.Width < 1 || ClientSize.Height < 1)
            {
                hasFrame = false;
                Invalidate();
                return;
            }

            using (var input = image.GetInputArray())
            {
                var source = input.GetMat();
                if (source is null || source.IsEmpty || source.Depth != DepthType.Cv8U) return;

                imageBounds = Fit(source.Size);
                if (imageBounds.Width < 1 || imageBounds.Height < 1) return;

                CvInvoke.Resize(source, resized, imageBounds.Size, 0, 0, Inter.Linear);
                switch (resized.NumberOfChannels)
                {
                    case 1: CvInvoke.CvtColor(resized, frame, ColorConversion.Gray2Bgra); break;
                    case 3: CvInvoke.CvtColor(resized, frame, ColorConversion.Bgr2Bgra); break;
                    case 4: resized.CopyTo(frame); break;
                    default: return;
                }

                hasFrame = true;
            }

            Invalidate();
        }

        /// <summary>
        /// Where the image goes in the control.
        /// </summary>
        private Rectangle Fit(Size size)
        {
            var area = ClientRectangle;
            if (SizeMode != PictureBoxSizeMode.Zoom) return area;

            var scale = Math.Min((double)area.Width / size.Width, (double)area.Height / size.Height);
            var width = (int)Math.Round(size.Width * scale);
            var height = (int)Math.Round(size.Height * scale);
            return new Rectangle((area.Width - width) / 2, AlignTop ? 0 : (area.Height - height) / 2, width, height);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [DllImport("gdi32.dll")]
        private static extern int SetDIBitsToDevice(IntPtr hdc, int xDest, int yDest, int width, int height, int xSrc, int ySrc, int startScan, int scanLines, IntPtr bits, ref BITMAPINFOHEADER header, int colorUse);
    }
}
