﻿//-----------------------------------------------------------------------
// <copyright file="ImageEyeBox.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris;
    using OpenIris.ImageProcessing;

    /// <summary>
    /// Control that displays the image of the eye.
    /// </summary>
    public partial class ImageEyeBox : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the EyeTrackerImageEyeBox class.
        /// </summary>
        public ImageEyeBox()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        /// <param name="eyeGlobe"></param>
        /// <param name="thresholdDark">Threshold dark.</param>
        /// <param name="threshdoldBright">Threshold bright.</param>
        /// <param name="croppingBox">Cropping rectangle where to search for a pupil.</param>
        /// <param name="mmPerPix">Resolution of the image in mm per pix.</param>
        public void UpdateImageEyeBox(ImageEye imageEye, EyePhysicalModel eyeGlobe, int thresholdDark, int threshdoldBright, Rectangle croppingBox, double mmPerPix)
        {
            if (imageEye is not null)
            {
                imageBoxEye.SuspendLayout();
                // Draw image of the eye with tracking information
                var image = imageEye.Image.Convert<Bgr, byte>();
                var eyeData = imageEye.EyeData ?? new EyeData();
                ImageEyeBox.DrawAllData(image, eyeData, eyeGlobe, thresholdDark, threshdoldBright, croppingBox, mmPerPix);

                imageBoxEye.Image = image;
                imageBoxEye.ResumeLayout();
            }
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        public void UpdateImageEyeBox(ImageEye? imageEye)
        {
            if (imageEye is null) return;
         
            imageBoxEye.SuspendLayout();

            Image<Bgr, byte> imageEyeColor = imageEye.Image.Convert<Bgr, byte>();
            imageBoxEye.Image = imageEyeColor;

            imageBoxEye.ResumeLayout();
        }


        /// <summary>
        /// Updates the image in the control.
        /// </summary>
        /// <param name="imageEye">New image to draw.</param>
        /// <param name="calibration"></param>
        /// <param name="settings"></param>
        public void UpdateImageEyeBox(ImageEye imageEye, EyeCalibration calibration, EyeTrackingPipelineSettings settings)
        {
            this.imageBoxEye.SuspendLayout();

            if (imageEye != null)
            {
                // Draw image of the eye with tracking information
                var imageEyeColor = ImageEyeBox.DrawAllData(imageEye, calibration, settings);

                this.imageBoxEye.Image = imageEyeColor;
            }

            imageBoxEye.ResumeLayout();
        }

        /// <summary>
        /// Resets the image to blank.
        /// </summary>
        public void ResetImage()
        {
            imageBoxEye.Image = null;
        }


        /// <summary>
        /// Gets the image with data overlay for display.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="calibrationParameters"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Image<Bgr, byte>? DrawAllData(ImageEye? imageEye, EyeCalibration calibrationParameters, EyeTrackingPipelineSettings settings)
        {
            if (imageEye == null) return null;

            var image = imageEye.Image.Convert<Bgr, byte>();
            var eyeData = imageEye.EyeData ?? new EyeData();

            var trackingSettings = settings as EyeTrackingPipelinePupilCRSettings;
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings?.DarkThresholdLeftEye ?? 0 : trackingSettings?.DarkThresholdRightEye ?? 0;
            var threshdoldBright = (imageEye.WhichEye == Eye.Left) ? trackingSettings?.BrightThresholdLeftEye ?? 255 : trackingSettings?.BrightThresholdRightEye ?? 255;

            var croppingRectangle = (imageEye.WhichEye == Eye.Left) ? settings.CroppingLeftEye : settings.CroppingRightEye;

            DrawAllData(image, eyeData, calibrationParameters.EyePhysicalModel, thresholdDark, threshdoldBright, croppingRectangle, settings.MmPerPix);
            return image;
        }

        /// <summary>
        /// Draws the data into the image of the eye.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        /// <param name="eyeGlobe"></param>
        /// <param name="thresholdDark"></param>
        /// <param name="threshdoldBright"></param>
        /// <param name="croppingRectangle"></param>
        /// <param name="mmPerPix"></param>
        public static void DrawAllData(Image<Bgr, byte>? image, EyeData data, EyePhysicalModel eyeGlobe, double thresholdDark, double threshdoldBright, Rectangle croppingRectangle, double mmPerPix)
        {
            if (image is null) return;

            if (thresholdDark > 0)
            {
                ImageEyeBox.DrawThresdholds(image, thresholdDark, threshdoldBright);

                // Draw eyelids
                ImageEyeBox.DrawEyelids(image, data, eyeGlobe);
            }

            ImageEyeBox.DrawEyeGlobe(image, eyeGlobe, false);

            if (!croppingRectangle.IsEmpty)
            {
                ImageEyeBox.DrawCroppingBox(image, croppingRectangle);
            }

            ImageEyeBox.DrawPupil(image, data);

            ImageEyeBox.DrawCR(image, data);

            ImageEyeBox.DrawIris(image, data, eyeGlobe);

            ImageEyeBox.DrawCross(image, data);

            ImageEyeBox.DrawMMScale(image, mmPerPix);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        public static void DrawCross(Image<Bgr, byte> image, EyeData data)
        {
            if (data is null || image is null || data.Pupil.IsEmpty) return;

            var center = new Point((int)(data.Pupil.Center.X), (int)(data.Pupil.Center.Y));
            var color = (data.WhichEye == Eye.Left) ? Color.RoyalBlue : Color.Red;
            var c = new Bgr(color);

            var angle = !double.IsNaN(data.TorsionAngle) ? data.TorsionAngle : 0.0;

            // Draw torsion line
            CvInvoke.Line(image,
                    new Point(
                        (int)Math.Round((double)center.X - (Math.Cos(angle / 180 * Math.PI) * data.Iris.Radius)),
                        (int)Math.Round((double)center.Y + (Math.Sin(angle / 180 * Math.PI) * data.Iris.Radius))),
                    new Point(
                        (int)Math.Round((double)center.X + (Math.Cos(angle / 180 * Math.PI) * data.Iris.Radius)),
                        (int)Math.Round((double)center.Y - (Math.Sin(angle / 180 * Math.PI) * data.Iris.Radius))),
                    c.MCvScalar,
                    2,
                    Emgu.CV.CvEnum.LineType.AntiAlias);

            CvInvoke.Line(image,
                    new Point(
                        (int)Math.Round((double)center.X - (Math.Sin(angle / 180 * Math.PI) * data.Iris.Radius)),
                        (int)Math.Round((double)center.Y - (Math.Cos(angle / 180 * Math.PI) * data.Iris.Radius))),
                    new Point(
                        (int)Math.Round((double)center.X + (Math.Sin(angle / 180 * Math.PI) * data.Iris.Radius)),
                        (int)Math.Round((double)center.Y + (Math.Cos(angle / 180 * Math.PI) * data.Iris.Radius))),
                    c.MCvScalar,
                    2,
                    Emgu.CV.CvEnum.LineType.AntiAlias);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="thresholdDark"></param>
        /// <param name="threshdoldBright"></param>
        public static void DrawThresdholds(Image<Bgr, byte> image, double thresholdDark, double threshdoldBright)
        {
            var imageThreshold1 = image.Convert<Gray, byte>().ThresholdBinary(new Gray(threshdoldBright), new Gray(255));
            var imageThreshold2 = image.Convert<Gray, byte>().ThresholdBinaryInv(new Gray(thresholdDark), new Gray(255));

            image.SetValue(new Bgr(0, 255, 255), imageThreshold1);
            image.SetValue(new Bgr(255, 255, 0), imageThreshold2);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        /// <param name="eyeModel"></param>
        public static void DrawEyelids(Image<Bgr, byte> image, EyeData data, EyePhysicalModel eyeModel)
        {
            if (data is null || image is null || data.Eyelids is null) return;

            if (eyeModel.IsEmpty)
            {
                eyeModel = new EyePhysicalModel(data.Pupil.Center, data.Iris.Radius * 2);
            }

            var c1 = new Point(
                (int)(eyeModel.Center.X - eyeModel.Radius),
                (int)((data.Eyelids.Upper[0].Y + data.Eyelids.Upper[1].Y +
                1.5 * data.Eyelids.Lower[0].Y + 1.5 * data.Eyelids.Lower[1].Y) / 5.0));

            var c2 = new Point(
                (int)(eyeModel.Center.X + eyeModel.Radius),
                (int)((data.Eyelids.Upper[3].Y + 1.5 * data.Eyelids.Lower[3].Y + data.Eyelids.Upper[2].Y + 1.5 * data.Eyelids.Lower[2].Y) / 5.0));

            if (data.Eyelids.Upper != null)
            {
                var upperEyelid = (Point[])Array.ConvertAll(data.Eyelids.Upper, point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y)));

                var top = new Point[8];
                top[0] = c1;
                top[5] = c2;
                top[6] = c1;
                top[7] = c2;
                Array.Copy(upperEyelid, 0, top, 1, 4);

                var top2 = top.FitParabola(eyeModel);

                CvInvoke.Polylines(image, top2, false, (new Bgr(Color.Orange)).MCvScalar, 2, Emgu.CV.CvEnum.LineType.AntiAlias);
            }




            if (data.Eyelids.Lower != null)
            {
                var lowerEyelid = (Point[])Array.ConvertAll(data.Eyelids.Lower, point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y)));

                var bottom = new Point[8];
                bottom[0] = c1;
                bottom[5] = c2;
                bottom[6] = c1;
                bottom[7] = c2;
                Array.Copy(lowerEyelid, 0, bottom, 1, 4);
                //image.DrawPolyline(bottom, false, new Bgr(Color.Green), 2);

                //image.DrawPolyline(bottom, false, new Bgr(Color.Green), 2);
                var bottom2 = bottom.FitParabola(eyeModel);

                CvInvoke.Polylines(image, bottom2, false, (new Bgr(Color.Orange)).MCvScalar, 2, Emgu.CV.CvEnum.LineType.AntiAlias);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        public static void DrawPupil(Image<Bgr, byte> image, EyeData data)
        {
            if (data is null || image is null || data.Pupil.IsEmpty) return;

            // Draw pupil circle
            CvInvoke.Ellipse(image, new RotatedRect(data.Pupil.Center, data.Pupil.Size, data.Pupil.Angle), (new Bgr(Color.RoyalBlue)).MCvScalar, 1, Emgu.CV.CvEnum.LineType.AntiAlias);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        public static void DrawCR(Image<Bgr, byte> image, EyeData data)
        {
            if (data is null || image is null) return;

            // Draw CR crosses
            if (data.CornealReflections != null)
            {
                int n = 0;
                foreach (var cr in data.CornealReflections)
                {
                    n++;
                    var length = (int)Math.Round(data.Iris.Radius / 3.0);
                    var center = new Point((int)(cr.Center.X), (int)(cr.Center.Y));
                    var color = Color.Black;

                    // Draw torsion line
                    image.Draw(
                        new LineSegment2D(new Point(center.X, center.Y - length), new Point(center.X, center.Y + length)),
                        new Bgr(color),
                        1);
                    image.Draw(
                        new LineSegment2D(new Point(center.X - length, center.Y), new Point(center.X + length, center.Y)),
                        new Bgr(color),
                        1);
                    image.Draw(n.ToString(), new Point(center.X + length / 4, center.Y + length / 3), Emgu.CV.CvEnum.FontFace.HersheyPlain, 0.5, new Bgr(color));
                }
            }
        }

        ///
        public static void DrawIris(Image<Bgr, byte> image, EyeData data, EyePhysicalModel eyeModel)
        {
            if (data is null || image is null || data.Pupil.IsEmpty) return;

            // Draw iris circle
            var points = new Point[36];
            for (int i = 0; i < points.Length; i++)
            {
                var theta = 2 * Math.PI / points.Length * i;

                points[i].X = (int)Math.Round(Math.Cos(theta) * data.Iris.Radius);
                points[i].Y = (int)Math.Round(Math.Sin(theta) * data.Iris.Radius);

                // TODO: FIX THIS
                if (!eyeModel.IsEmpty)
                {
                    points[i] = RotatePoint(points[i], eyeModel, data.Pupil.Center);
                }
                else
                {
                    points[i].X = (int)Math.Round(points[i].X + data.Iris.Center.X);
                    points[i].Y = (int)Math.Round(points[i].Y + data.Iris.Center.Y);
                }
            }

            CvInvoke.Polylines(image, points, true, (new Bgr(Color.RoyalBlue)).MCvScalar, 1, Emgu.CV.CvEnum.LineType.AntiAlias);
        }

        private static Point RotatePoint(Point point, EyePhysicalModel eyeModel, PointF center)
        {
            var angle = Math.Atan2((center.Y - eyeModel.Center.Y), (center.X - eyeModel.Center.X));
            var ecc = Math.Asin(Math.Sqrt((center.Y - eyeModel.Center.Y) * (center.Y - eyeModel.Center.Y) + (center.X - eyeModel.Center.X) * (center.X - eyeModel.Center.X)) / eyeModel.Radius);
            var q = new Quaternions(Math.Cos(ecc / 2), -Math.Sin(angle) * Math.Sin(ecc / 2), Math.Cos(angle) * Math.Sin(ecc / 2), 0);

            var x = point.X;
            var y = point.Y;

            double
                 t2 = q.W * q.X,
                 t3 = q.W * q.Y,
                 t4 = q.W * q.Z,
                 t5 = -q.X * q.X,
                 t6 = q.X * q.Y,
                 t7 = q.X * q.Z,
                 t8 = -q.Y * q.Y,
                 t9 = q.Y * q.Z,
                 t10 = -q.Z * q.Z;

            double rr = Math.Sqrt(x * x + y * y) / eyeModel.Radius;

            if (rr <= 1)
            {
                var z = Math.Sqrt(1 - rr * rr) * eyeModel.Radius;

                // var p2 = q.RotatePoint(p);
                double xx = 2.0 * ((t8 + t10) * x + (t6 - t4) * y + (t3 + t7) * z) + x;
                double yy = 2.0 * ((t4 + t6) * x + (t5 + t10) * y + (t9 - t2) * z) + y;
                //double zz = 2.0 * ((t7 - t3) * x + (t2 + t9) * y + (t5 + t8) * z) + z;

                point.X = (int)Math.Round(xx + eyeModel.Center.X);
                point.Y = (int)Math.Round(yy + eyeModel.Center.Y);
            }

            return point;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="croppingRectangle"></param>
        public static void DrawCroppingBox(Image<Bgr, byte> image, Rectangle croppingRectangle)
        {
            // Draw cropping box
            var eyeROI = new Rectangle(
                croppingRectangle.Left,
                croppingRectangle.Top,
                image.Size.Width - croppingRectangle.Left - croppingRectangle.Width,
                image.Size.Height - croppingRectangle.Top - croppingRectangle.Height);

            image.Draw(
                eyeROI,
                new Bgr(Color.DarkRed),
                2);
            image.Draw("ROI", new Point(croppingRectangle.Left + 5, croppingRectangle.Top + 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, new Bgr(Color.DarkRed), 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="globe"></param>
        /// <param name="detailed"></param>
        public static void DrawEyeGlobe(Image<Bgr, byte> image, EyePhysicalModel globe, bool detailed)
        {
            if (detailed)
            {
                for (int i = 0; i <= 90; i += 10)
                {
                    var circle = new CircleF(globe.Center, (float)((1 / (1 + (1 - Math.Cos(i / 180 * Math.PI)) / 2)) * Math.Sin(i / 90.0 * (Math.PI / 2.0)) * globe.Radius));
                    image.Draw(circle, new Bgr(Color.White), 1);
                }
            }
            else
            {
                var circle = new CircleF(globe.Center, globe.Radius);
                image.Draw(circle, new Bgr(Color.White), 1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="mmPerPix"></param>
        public static void DrawMMScale(Image<Bgr, byte> image, double mmPerPix)
        {
            CvInvoke.PutText(image, "10mm", new Point(2, image.Size.Height - 10), Emgu.CV.CvEnum.FontFace.HersheyPlain, 0.6, new Bgr(Color.White).MCvScalar);

            CvInvoke.Line(image,
                    new Point(
                        2,
                        image.Size.Height - 2),
                    new Point(
                        2 + (int)Math.Round(10.0 / mmPerPix),
                        image.Size.Height - 2),
                    new Bgr(Color.White).MCvScalar,
                    2,
                    Emgu.CV.CvEnum.LineType.AntiAlias);
            CvInvoke.Line(image,
                    new Point(
                        2,
                        image.Size.Height - 4),
                    new Point(
                        2 + (int)Math.Round(10.0 / mmPerPix),
                        image.Size.Height - 4),
                    new Bgr(Color.Black).MCvScalar,
                    2,
                    Emgu.CV.CvEnum.LineType.AntiAlias);
        }
    }
}
