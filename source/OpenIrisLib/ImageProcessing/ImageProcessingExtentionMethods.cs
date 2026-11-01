//-----------------------------------------------------------------------
// <copyright file="PositionTrackerEllipseFitting.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using System;
    using System.Drawing;

    /// <summary>
    /// Extension methods for image processing.
    /// </summary>
    public static class ImageProcessingExtentionMethods
    {
        /// <summary>
        /// Converts a position found in a resized image back to the original image. OpenCV's Resize
        /// aligns pixel centers, so x in an image resized by scale maps back to (x + 0.5) / scale - 0.5,
        /// not x / scale (which is off by 0.5 - 0.5 / scale pixels).
        /// </summary>
        internal static double ResizedToOriginal(double x, double scale) => (x + 0.5) / scale - 0.5;

        /// <summary>
        /// Gets the smallest rectangle that fits the ellipse within.
        /// </summary>
        /// <param name="ellipse">Ellipse to be fit.</param>
        /// <returns>Rectangle around the pupil.</returns>
        public static RectangleF GetEllipseBoundingBox(this Ellipse ellipse)
        {
            var A = ellipse.RotatedRect.Size.Width;
            var B = ellipse.RotatedRect.Size.Height;

            var angle = (double)ellipse.RotatedRect.Angle / 180 * Math.PI;
            if (angle > Math.PI)
            {
                angle = angle - Math.PI;
            }
            if (angle > Math.PI / 2)
            {
                angle = Math.PI - angle;
            }
            if (angle < 0)
            {
                angle = angle + Math.PI;
            }
            var t = 2.0 * Math.Atan((1.0 / Math.Sin(angle)) * (Math.Sqrt(Math.Pow(A * Math.Cos(angle), 2) + Math.Pow(B * Math.Sign(angle), 2)) + A * Math.Cos(angle)) / B);
            var w = A * Math.Cos(t) * Math.Cos(angle) - B * Math.Sin(t) * Math.Sin(angle);

            angle = angle + Math.PI / 2;
            if (angle > Math.PI)
            {
                angle = angle - Math.PI;
            }
            if (angle > Math.PI / 2)
            {
                angle = Math.PI - angle;
            }
            t = 2.0 * Math.Atan((1.0 / Math.Sin(angle)) * (Math.Sqrt(Math.Pow(A * Math.Cos(angle), 2) + Math.Pow(B * Math.Sign(angle), 2)) + A * Math.Cos(angle)) / B);
            var h = A * Math.Cos(t) * Math.Cos(angle) - B * Math.Sin(t) * Math.Sin(angle);

            //var w = A * (1 + Math.Sin(2 * ellipse.angle / 180 * Math.PI) / 2) / 2 + B * (1 + Math.Sin(2 * ellipse.angle / 180 * Math.PI + Math.PI) / 2) / 2;
            //var h = A * (1 + Math.Sin(2 * ellipse.angle / 180 * Math.PI + Math.PI) / 2) / 2 + B * (1 + Math.Sin(2 * ellipse.angle / 180 * Math.PI) / 2) / 2;
            var sizeRect = new SizeF((float)w, (float)h);
            var rect2 = new RectangleF(
                (float)Math.Round(ellipse.RotatedRect.Center.X - sizeRect.Width / 2f),
                (float)Math.Round(ellipse.RotatedRect.Center.Y - sizeRect.Height / 2f),
                (float)Math.Round(sizeRect.Width),
                (float)Math.Round(sizeRect.Height));

            return rect2;
        }


        /// <summary>
        /// Finds the bounding box of the countour. It should not be necessary but I don't understand
        /// how countours deal with removing points. I suspect that the bounding box does not get
        /// recalculated automatically
        /// </summary>
        /// <param name="countourVoV">Contour</param>
        /// <returns></returns>
        public static RectangleF GetCountourBoundingBox(this VectorOfPointF countourVoV)
        {
            var minx = 500.0f;
            var maxx = 0.0f;
            var miny = 500.0f;
            var maxy = 0.0f;

            var countour = countourVoV.ToArray();

            // Go trhough the points in the countour in reverese order to avoid problems when deleting
            for (int i = countour.Length - 1; i >= 0; i--)
            {
                if (countour[i].X > maxx)
                {
                    maxx = countour[i].X;
                }
                if (countour[i].X < minx)
                {
                    minx = countour[i].X;
                }
                if (countour[i].Y > maxy)
                {
                    maxy = countour[i].Y;
                }
                if (countour[i].Y < miny)
                {
                    miny = countour[i].Y;
                }
            }
            return new RectangleF(minx, miny, maxx - minx, maxy - miny);
        }

        /// <summary>
        /// Fit a parabola to a set of points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="eyeModel"></param>
        /// <returns></returns>
        public static Point[] FitParabola(this Point[] points, EyePhysicalModel eyeModel)
        {
            var s0 = points.Length;

            var s1 = 0.0;
            for (int i = 0; i < points.Length; i++) s1 += points[i].X;

            var s2 = 0.0;
            for (int i = 0; i < points.Length; i++) s2 += Math.Pow(points[i].X, 2);

            var s3 = 0.0;
            for (int i = 0; i < points.Length; i++) s3 += Math.Pow(points[i].X, 3);

            var s4 = 0.0;
            for (int i = 0; i < points.Length; i++) s4 += Math.Pow(points[i].X, 4);

            var d = new Image<Gray, double>(1, 3);
            d.Data[0, 0, 0] = 0.0;
            for (int i = 0; i < points.Length; i++) d.Data[0, 0, 0] += Math.Pow(points[i].X, 2) * points[i].Y;
            d.Data[1, 0, 0] = 0.0;
            for (int i = 0; i < points.Length; i++) d.Data[1, 0, 0] += points[i].X * points[i].Y;
            d.Data[2, 0, 0] = 0.0;
            for (int i = 0; i < points.Length; i++) d.Data[2, 0, 0] += points[i].Y;

            var A = new Image<Gray, double>(3, 3);

            A.Data[0, 0, 0] = s4;
            A.Data[1, 0, 0] = s3;
            A.Data[2, 0, 0] = s2;

            A.Data[0, 1, 0] = s3;
            A.Data[1, 1, 0] = s2;
            A.Data[2, 1, 0] = s1;

            A.Data[0, 2, 0] = s2;
            A.Data[1, 2, 0] = s1;
            A.Data[2, 2, 0] = s0;

            var Ainv = new Image<Gray, double>(3, 3);
            CvInvoke.Invert(A, Ainv, Emgu.CV.CvEnum.DecompMethod.LU);

            var a1 = Ainv.Data[0, 0, 0] * d.Data[0, 0, 0] + Ainv.Data[1, 0, 0] * d.Data[1, 0, 0] + Ainv.Data[2, 0, 0] * d.Data[2, 0, 0];
            var a2 = Ainv.Data[0, 1, 0] * d.Data[0, 0, 0] + Ainv.Data[1, 1, 0] * d.Data[1, 0, 0] + Ainv.Data[2, 1, 0] * d.Data[2, 0, 0];
            var a3 = Ainv.Data[0, 2, 0] * d.Data[0, 0, 0] + Ainv.Data[1, 2, 0] * d.Data[1, 0, 0] + Ainv.Data[2, 2, 0] * d.Data[2, 0, 0];

            var top2 = new Point[21];
            var x1 = (eyeModel.Center.X - eyeModel.Radius);
            var step = (eyeModel.Radius * 2) / 20;
            for (int i = 0; i < top2.Length; i++)
            {
                top2[i].X = (int)(i * step + x1);
                top2[i].Y = (int)(a1 * top2[i].X * top2[i].X + a2 * top2[i].X + a3);
            }
            return top2;
        }
    }
}