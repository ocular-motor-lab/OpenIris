//-----------------------------------------------------------------------
// <copyright file="EyeLidTrackerHoughLines.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using OpenIris.UI;

    /// <summary>
    /// Tracks the eyelids using the Hough transform. It finds four segments of the eyelids independently around the pupil.
    /// </summary>
    public class EyeLidTracking
    {
        /// <summary>
        /// Possible methods for finding the eyelids.
        /// </summary>
        public enum EyeLidTrackingMethod
        {
            /// <summary>
            /// No eyelid tracking.
            /// </summary>
            None, 

            /// <summary>
            /// Hough lines automatic detection.
            /// </summary>
            HoughLines,

            /// <summary>
            /// Fixed eyelids relative to the pupil.
            /// </summary>
            Fixed,
        }

        /// <summary>
        /// Four positions of the eyelid segments.
        /// </summary>
        internal enum EyeLidCorners
        {
            /// <summary>
            /// Top left corner.
            /// </summary>
            TopLeft,

            /// <summary>
            /// Top right corner.
            /// </summary>
            TopRight,

            /// <summary>
            /// Bottom left corner.
            /// </summary>
            BottomLeft,

            /// <summary>
            /// Bottom right corner.
            /// </summary>
            BottomRight
        }

        /// <summary>
        /// Accumulator threshold for the Hough Lines function.
        /// </summary>
        private static readonly int houghThreshold = 20;

        /// <summary>
        /// Resolution of rho in pixels for the Hough Lines function.
        /// </summary>
        private static double houghRhoResolution = 2;

        /// <summary>
        /// Resolution of theta in degrees for the Hough Lines function.
        /// </summary>
        private static double houghThetaResolution = 5;

        /// <summary>
        /// Resolution of the image used for the eyelid detection. Number of pixels
        /// that correspond to the eyeglobe radius.
        /// </summary>
        private static int pixelsPerEyeRadius = 80;

        /// <summary>
        /// Initializes a new instance of the EyeLidTrackerHoughLines class.
        /// </summary>
        public EyeLidTracking()
        {
            this.filteredEyelids = new EyelidData();
        }

        /// <summary>
        /// Running average of the position of the eyelids.
        /// </summary>
        private EyelidData filteredEyelids;

        private Image<Bgr, byte>? debugImage = null;

        /// <summary>
        /// Variable to keep track of the eyeGlobe used for the eyelids.
        /// </summary>
        private EyePhysicalModel latEyeGlobe;

        /// <summary>
        /// Find the eyelids.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="pupil"></param>
        /// <param name="eyeGlobe"></param>
        /// <param name="trackingSettings"></param>
        /// <returns></returns>
        public EyelidData FindEyelids(ImageEye imageEye, PupilData pupil, EyePhysicalModel eyeGlobe, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            switch (trackingSettings.EyelidTrackingMethod)
            {
                case EyeLidTrackingMethod.None:
                    return new EyelidData();
                case EyeLidTrackingMethod.HoughLines:
                    return FindEyelidsHoughLines(debugImage, imageEye, pupil, eyeGlobe, trackingSettings);
                case EyeLidTrackingMethod.Fixed:
                    return FindEyelidsFixed(imageEye, pupil, eyeGlobe, trackingSettings);
                default:
                    break;
            }

            return new EyelidData();
        }

        /// <summary>
        /// Finds the eyelids.
        /// </summary>
        /// <param name="debugImage"></param>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="pupil">Iris position and radius.</param>
        /// <param name="eyeGlobe">Eye globe position and size in the geometry corrected image.</param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>Eyelids as an array of 8 points. First 4 points are the top eyelid from left to right,
        /// the next 4 are the bottom eyelid from left to right.</returns>
        public static EyelidData FindEyelidsHoughLines(Image<Bgr, byte>? debugImage, ImageEye imageEye, PupilData pupil, EyePhysicalModel eyeGlobe, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            var DEBUG = EyeTracker.DEBUG;

            float scale = (float)(eyeGlobe.Radius / (float)pixelsPerEyeRadius);
            if (scale == 0)
            {
                scale = 1;
            }

                var smallSize = new Size((int)Math.Round((double)imageEye.Image.ROI.Width / scale), (int)Math.Round((double)imageEye.Image.ROI.Height / scale));
            var scaleX = (float)smallSize.Width / imageEye.Image.ROI.Width;
            var scaleY = (float)smallSize.Height / imageEye.Image.ROI.Height;


            // Resize the image to a fixed size. That way processing time is independent on the original
            // image size and the parameters don't need to change.
            // Approximate the iris using the eyeglobe radius
            var irisResize = new CircleF(new PointF(pupil.Center.X * scaleX, pupil.Center.Y * scaleY), eyeGlobe.Radius / 2.0f * scaleX);
            var eyeGlobeResize = new CircleF(new PointF(eyeGlobe.Center.X * scaleX, eyeGlobe.Center.Y * scaleY), eyeGlobe.Radius * scaleX);

            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;
            var pupilMask = imageEye.ThresholdDarkResized(thresholdDark, smallSize, imageEye.Image.ROI);


            var imageResize = imageEye.Image.Resize(smallSize.Width, smallSize.Height, Emgu.CV.CvEnum.Inter.Cubic);

            if (DEBUG)
            {
                debugImage = imageResize.Convert<Bgr, byte>();
            }

            //////////////////////////////////////////////////////////////////////////////////////
            // Define the search parameters depending on the corner
            //
            // The 4 boxes are positioned relative to the pupil center and the eye globe.
            // The horizontal position of the boxes is completely determined by the pupil.
            // The vertical position of the top eyelid is also determined mainly by the pupil but the bottom one
            // is determined mainly by the eyeglobe.
            //  _____    _ . - = - . _
            // |top  | . "  \  \   /  /  " .
            // |left,|  \                 /  .
            // |  . \|  _,.--~=~"~=~--..Top eyelid
            // | ;  _|-/ \ !   ! / \     "-._  .
            // |/ ," |/ ,` .---. `, \        ". \
            //||_____|   /:::Pupil center     '.\
            //\`.`~  |   \:::::/+Eye globe center
            //  _____\ `, `~~~' ,` /      ~`.' /
            // |bottom\ / !   ! \ /     _.-"  .
            // |left |  "=~~..____..~~=`"Bottom eyelid
            // |    ,|                     \,
            // |_____|. _/             \_ . 
            //          " - ./. .\. - "
            //////////////////////////////////////////////////////////////////////////////////////

            // The width of the search areas and the gap from the center of the pupil
            // are relative to the size of the iris
            var maxAreaWidth = (int)(irisResize.Radius * 0.8);
            var centerGap = (int)(irisResize.Radius * 0.3);

            var topY = (int)Math.Max(irisResize.Center.Y - irisResize.Radius * 1.1f, eyeGlobeResize.Center.Y - irisResize.Radius * 1.1f);
            var topHeight = (int)(irisResize.Radius * 1f);
            var bottomY = (int)(eyeGlobeResize.Center.Y + irisResize.Radius / 2.0);
            var bottomHeight = (int)(irisResize.Radius * 1.3f);

            var xLeft = (int)Math.Max(
                eyeGlobeResize.Center.X - eyeGlobeResize.Radius,
                Math.Min(
                    eyeGlobeResize.Center.X - irisResize.Radius / 2,
                    irisResize.Center.X - maxAreaWidth - centerGap));

            var xRight = (int)Math.Min(
                eyeGlobeResize.Center.X + eyeGlobeResize.Radius - maxAreaWidth,
                Math.Max(
                    eyeGlobeResize.Center.X + irisResize.Radius / 2,
                    irisResize.Center.X + centerGap));

            xRight = (int)Math.Min(imageResize.Size.Width - maxAreaWidth, xRight);

            /////////////////////////////////////////////////////////////
            // Find the eyelid segments
            //
            // Find first the bottom because they are less mobile. Then find the top.
            /////////////////////////////////////////////////////////////

            // Get the ROI that corresponds with the corner
            var bottomLeftRoi = new Rectangle(xLeft, bottomY, maxAreaWidth, bottomHeight);
            var bottomRightRoi = new Rectangle(xRight, bottomY, maxAreaWidth, bottomHeight);

            bottomLeftRoi.Intersect(imageResize.ROI);
            bottomRightRoi.Intersect(imageResize.ROI);

            // Get the default lines for each corner
            var bottomLeftDefaultEyeLidLine = new LineSegment2DF(
                        new PointF(bottomLeftRoi.X, bottomLeftRoi.Y),
                        new PointF(bottomLeftRoi.X + bottomLeftRoi.Width, bottomLeftRoi.Y + bottomLeftRoi.Height / 2f));
            var bottomRightDefaultEyeLidLine = new LineSegment2DF(
                        new PointF(bottomRightRoi.X, bottomRightRoi.Y + bottomRightRoi.Height / 2f),
                        new PointF(bottomRightRoi.X + bottomRightRoi.Width, bottomRightRoi.Y));

            var lineLeftBottom = FindEyelidSegment(debugImage, imageResize, pupilMask, EyeLidCorners.BottomLeft, bottomLeftRoi, bottomLeftDefaultEyeLidLine);
            var lineRightBottom = FindEyelidSegment(debugImage, imageResize, pupilMask, EyeLidCorners.BottomRight, bottomRightRoi, bottomRightDefaultEyeLidLine);

            // Make the top eyelid search, relative to the bottom one
            var meanBottomEyelidY = (int)Math.Max(lineLeftBottom.P1.Y, lineRightBottom.P2.Y);
            topHeight = (int)Math.Max(topHeight, meanBottomEyelidY - 0.5 * irisResize.Radius - topY);

            // Get the ROI that corresponds with the corner
            var topLeftRoi = new Rectangle(xLeft, topY, maxAreaWidth, topHeight);
            var topRightRoi = new Rectangle(xRight, topY, maxAreaWidth, topHeight);

            topLeftRoi.Intersect(imageResize.ROI);
            topRightRoi.Intersect(imageResize.ROI);

            // Get the default lines for each corner
            var topLeftDefaultEyeLidLine = new LineSegment2DF(
                        new PointF(topLeftRoi.X, topLeftRoi.Y + topLeftRoi.Height),
                        new PointF(topLeftRoi.X + topLeftRoi.Width, topLeftRoi.Y + topLeftRoi.Height / 2f));
            var topRightDefaultEyeLidLine = new LineSegment2DF(
                        new PointF(topRightRoi.X, topRightRoi.Y + topRightRoi.Height / 2f),
                        topRightRoi.Location + topRightRoi.Size);

            var lineLeftTop = FindEyelidSegment(debugImage, imageResize, pupilMask, EyeLidCorners.TopLeft, topLeftRoi, topLeftDefaultEyeLidLine);
            var lineRightTop = FindEyelidSegment(debugImage, imageResize, pupilMask, EyeLidCorners.TopRight, topRightRoi, topRightDefaultEyeLidLine);

            var pointsUpper = new PointF[]
            {
                lineLeftTop.P1,
                lineLeftTop.P2,
                lineRightTop.P1,
                lineRightTop.P2,
            };

            var pointsLower = new PointF[]
            {
                lineLeftBottom.P1,
                lineLeftBottom.P2,
                lineRightBottom.P1,
                lineRightBottom.P2,
            };

            var eyelidData = new EyelidData
            {
                // Fix the scale of points
                Upper = Array.ConvertAll(pointsUpper, point => new PointF(point.X * scaleX, point.Y / scaleY)),
                Lower = Array.ConvertAll(pointsLower, point => new PointF(point.X * scaleX, point.Y / scaleY))
            };


            if (DEBUG)
            {
                if (debugImage != null)
                {
                    debugImage.Draw(lineLeftTop, new Bgr(Color.Orange), 2);
                    debugImage.Draw(lineRightTop, new Bgr(Color.Orange), 2);
                    debugImage.Draw(lineLeftBottom, new Bgr(Color.Orange), 2);
                    debugImage.Draw(lineRightBottom, new Bgr(Color.Orange), 2);
                    EyeTrackerDebug.AddImage("EyelidLines", imageEye.WhichEye, debugImage);
                }

                var imageDebugParabola = imageEye.Image.Convert<Bgr, byte>();
                EyeData data = new EyeData(imageEye, ProcessFrameResult.Good)
                {
                    Pupil = new PupilData(pupil.Center, new SizeF(), 0),
                    Iris = new IrisData(pupil.Center, eyeGlobe.Radius / 2.0f),
                    Eyelids = eyelidData,
                };

                ImageEyeBox.DrawEyelids(imageDebugParabola, data, eyeGlobe);
                EyeTrackerDebug.AddImage("EyelidParabolas", imageEye.WhichEye, imageDebugParabola);
            }

            return eyelidData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eyeLids"></param>
        /// <param name="eyeGlobe"></param>
        /// <returns></returns>
        public EyelidData UpdateFilteredEyelids(EyelidData eyeLids, EyePhysicalModel eyeGlobe)
        {
            // If the eyeGlobe position has changed reset the eyelids
            if (eyeGlobe.IsEmpty || !eyeGlobe.Equals(this.latEyeGlobe))
            {
                this.filteredEyelids = new EyelidData();
            }

            this.latEyeGlobe = eyeGlobe;

            double alfa = 0.5;

            for (int i = 0; i < 4; i++)
            {
                if (this.filteredEyelids.Upper[i].IsEmpty)
                {
                    this.filteredEyelids.Upper[i] = eyeLids.Upper[i];
                }
                else
                {
                    this.filteredEyelids.Upper[i].X = (int)Math.Round(this.filteredEyelids.Upper[i].X * alfa + eyeLids.Upper[i].X * (1 - alfa));
                    this.filteredEyelids.Upper[i].Y = (int)Math.Round(this.filteredEyelids.Upper[i].Y * alfa + eyeLids.Upper[i].Y * (1 - alfa));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (this.filteredEyelids.Lower[i].IsEmpty)
                {
                    this.filteredEyelids.Lower[i] = eyeLids.Lower[i];
                }
                else
                {
                    this.filteredEyelids.Lower[i].X = (int)Math.Round(this.filteredEyelids.Lower[i].X * alfa + eyeLids.Lower[i].X * (1 - alfa));
                    this.filteredEyelids.Lower[i].Y = (int)Math.Round(this.filteredEyelids.Lower[i].Y * alfa + eyeLids.Lower[i].Y * (1 - alfa));
                }
            }

            return this.filteredEyelids;
        }

        /// <summary>
        /// Finds one of the segments of the eyelid.
        /// </summary>
        /// <param name="debugImage">Image of the eye for debugging.</param>
        /// <param name="image">Image of the eye.</param>
        /// <param name="pupilMask">Image of the mask.</param>
        /// <param name="corner">Which corner.</param>
        /// <param name="roi">Region of interest where to find the segment.</param>
        /// <param name="defaultSegment">Default segment.</param>
        /// <returns>The eyelid line segment.</returns>
        private static LineSegment2DF FindEyelidSegment(Image<Bgr, byte>? debugImage, Image<Gray, byte> image, Image<Gray, byte> pupilMask, EyeLidCorners corner, Rectangle roi, LineSegment2DF defaultSegment)
        {
            var DEBUG = EyeTracker.DEBUG;

            // Get the range of angles of the segment for each piece of eyelid
            // More flexible for the top eyelids than the bottom ones.
            var cosRange = corner switch
            {
                EyeLidCorners.TopLeft => new RangeDouble(-0.2, 0.7),
                EyeLidCorners.TopRight => new RangeDouble(-0.7, 0.2),
                EyeLidCorners.BottomLeft => new RangeDouble(-0.4, 0.2),
                EyeLidCorners.BottomRight => new RangeDouble(-0.2, 0.4),
                _ => throw new InvalidOperationException("Error"),
            };

            var eyeLidLine = defaultSegment;

            // If the ROI is too small don't try to find a line
            if (roi.Size.Height < 2 || roi.Size.Width < 2)
            {
                return eyeLidLine;
            }

            //////////////////////////////////////////////////////////////////////////////////////
            // Optimize the image for line segment search
            //////////////////////////////////////////////////////////////////////////////////////

            image = image.Copy(roi);

            // Low pass filter in the horizontal direction and high pass second order filter in the 
            // vertical direction
            image = image.SmoothBlur(4, 4);
            var sob1 = image.Sobel(0, 2, 5).Convert<Gray, byte>();
            var sob2 = image.Sobel(2, 0, 5).Convert<Gray, byte>();

            var angle = Math.Asin((cosRange.Begin + cosRange.End) / 2);
            image = sob1 * Math.Cos(angle) + sob2 * Math.Sin(angle);


            // Equalize the contrast and threshold to find the brightest spots
            //image = image.SmoothBlur(5, 5);
            image._EqualizeHist();
            image = image.ThresholdBinary(new Gray(230), new Gray(255));

            pupilMask.ROI = roi;
            image = image.Mul(pupilMask.Not());
            pupilMask.ROI = new Rectangle();

            //////////////////////////////////////////////////////////////////////////////////////
            // Find lines in the segment
            //////////////////////////////////////////////////////////////////////////////////////

            // Keep track of the highest (lowest) line
            var maxy2 = (corner == EyeLidCorners.TopLeft || corner == EyeLidCorners.TopRight) ?
                0f : roi.Height;

            using (var vector = new VectorOfPointF())
            {
                // Find the lines
                CvInvoke.HoughLines(
                image,
                vector,
                houghRhoResolution,
                (houghThetaResolution * Math.PI) / 180,
                houghThreshold,
                0,
                0);

                // Loop troough the lines to find the highest (lowest) line
                int maxLines = 30;
                PolarCoordinates coords1 = new PolarCoordinates();
                coords1.Rho = 99;

                for (int i = 0; i < maxLines; i++)
                {
                    if (i >= vector.Size)
                    {
                        break;
                    }

                    PolarCoordinates coords = new PolarCoordinates(vector[i].X, vector[i].Y);

                    // Check if the angle of the line is within the range for that corner
                    if (cosRange.Contains(Math.Cos(coords.Theta)) && Math.Sin(coords.Theta) > 0.1)
                    {
                        // The lines are given in kind of polar coordinates. Rho and theta define a vector that touches the
                        // line and is perpendicular to it.
                        var y1 = coords.Rho / (float)Math.Sin(coords.Theta);
                        var y2 = -(float)(Math.Cos(coords.Theta) / (float)Math.Sin(coords.Theta) * (float)roi.Width) + y1;

                        switch (corner)
                        {
                            case EyeLidCorners.TopLeft:
                                // For top eyelids find the lowest line (highest y)
                                if (y2 > maxy2)
                                {
                                    maxy2 = y2;
                                    eyeLidLine = new LineSegment2DF(
                                        new PointF(roi.X + 0, roi.Y + y1),
                                        new PointF(roi.X + image.Width, roi.Y + y2));
                                    coords1 = coords;
                                }
                                break;
                            case EyeLidCorners.TopRight:
                                // For top eyelids find the lowest line (highest y)
                                if (y1 > maxy2)
                                {
                                    maxy2 = y1;
                                    eyeLidLine = new LineSegment2DF(
                                        new PointF(roi.X + 0, roi.Y + y1),
                                        new PointF(roi.X + image.Width, roi.Y + y2));
                                    coords1 = coords;
                                }
                                break;
                            case EyeLidCorners.BottomLeft:
                                // For top eyelids find the highest line (lowest y)
                                if (y1 < maxy2)
                                {
                                    maxy2 = y1;
                                    eyeLidLine = new LineSegment2DF(
                                        new PointF(roi.X + 0, roi.Y + y1),
                                        new PointF(roi.X + image.Width, roi.Y + y2));
                                    coords1 = coords;
                                }
                                break;
                            case EyeLidCorners.BottomRight:
                                // For top eyelids find the highest line (lowest y)
                                if (y2 < maxy2)
                                {
                                    maxy2 = y2;
                                    eyeLidLine = new LineSegment2DF(
                                        new PointF(roi.X + 0, roi.Y + y1),
                                        new PointF(roi.X + image.Width, roi.Y + y2));
                                    coords1 = coords;
                                }
                                break;
                        }
                    }
                }
            }


            if (DEBUG)
            {
                if (debugImage != null)
                {
                    // Debug stuff
                    debugImage.ROI = roi;
                    image.Convert<Bgr, byte>().CopyTo(debugImage);
                    debugImage.ROI = new Rectangle();
                }
            }

            return eyeLidLine;
        }


        /// <summary>
        /// Finds the eyelids.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="pupil">Pupil Data.</param>
        /// <param name="eyeGlobe"></param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>Eyelids as an array of 8 points. First 4 points are the top eyelid from left to right,
        /// the next 4 are the bottom eyelid from left to right.</returns>
        public static EyelidData FindEyelidsFixed(ImageEye imageEye, PupilData pupil, EyePhysicalModel eyeGlobe, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            var pupilRadius = (pupil.Size.Width + pupil.Size.Height) / 2.0;

            var ytop = (int)(pupil.Center.Y - pupilRadius);
            var ybottom = (int)(pupil.Center.Y + pupilRadius);

            var eyelidData = new EyelidData
            {
                Upper = new PointF[]
                {
                    // left top to right top
                    new PointF(pupil.Center.X - (eyeGlobe.Radius / 2.0f * 1.5f), ytop),
                    new PointF(pupil.Center.X - (eyeGlobe.Radius / 2.0f * 1f), ytop),
                    new PointF(pupil.Center.X + (eyeGlobe.Radius / 2.0f * 1f), ytop),
                    new PointF(pupil.Center.X + (eyeGlobe.Radius / 2.0f * 1.5f), ytop),
                },

                Lower = new PointF[]
                {
                    // left bottom to right bottom
                    new PointF(pupil.Center.X - (eyeGlobe.Radius / 2.0f * 1.5f), ybottom),
                    new PointF(pupil.Center.X - (eyeGlobe.Radius / 2.0f * 1f), ybottom),
                    new PointF(pupil.Center.X + (eyeGlobe.Radius / 2.0f * 1f), ybottom),
                    new PointF(pupil.Center.X + (eyeGlobe.Radius / 2.0f * 1.5f), ybottom),
                },
            };

            return eyelidData;
        }


        /// <summary>
        /// Polar coordinates structure.
        /// </summary>
        internal struct PolarCoordinates
        {
            /// <summary>
            /// Magnitude of the vector.
            /// </summary>
            internal float Rho;

            /// <summary>
            /// Angle of the vector.
            /// </summary>
            internal float Theta;

            /// <summary>
            /// Initializes a new instance of the PolarCoordinates struct.
            /// </summary>
            /// <param name="rho">Magnitude of the vector.</param>
            /// <param name="theta">Angle of the vector.</param>
            internal PolarCoordinates(float rho, float theta)
            {
                this.Rho = rho;
                this.Theta = theta;
            }
        }

    }
}
