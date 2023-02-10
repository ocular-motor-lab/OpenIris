//-----------------------------------------------------------------------
// <copyright file="PositionTrackerEllipseFitting.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Class for eye position tracking using the convex hull surrounding the pupil and then fitting
    /// an ellipse to it.
    /// </summary>
    public class PositionTrackerEllipseFitting
    {
        /// <summary>
        /// Posible position tracking methods.
        /// </summary>
        public enum PositionTrackingMethod
        {
            /// <summary>
            /// None, just go with the quick pupil search.
            /// </summary>
            None,

            /// <summary>
            /// Centroid of the blob.
            /// </summary>
            Centroid,

            /// <summary>
            /// Centroid of the convex hull.
            /// </summary>
            ConvexHull,

            /// <summary>
            /// Center of the ellipse that best fits the contour.
            /// </summary>
            EllipseFitting
        }

        /// <summary>
        /// Calculates the position of the eye (vertical and horizontal).
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="mask"></param>
        /// <param name="pupilAprox"></param>
        /// <param name="pupilReference"></param>
        /// <param name="trackingSettings"></param>
        /// <returns></returns>
        public PupilData CalculatePosition(ImageEye imageEye, Image<Gray, byte>? mask, PupilData pupilAprox, PupilData pupilReference, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            var desiredMaxSize = 200;
            var threshold = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;

            switch (trackingSettings.PositionTrackingMethod)
            {
                case PositionTrackingMethod.None:
                    return pupilAprox;
                case PositionTrackingMethod.Centroid:
                    return CalculatePositionCentroid(imageEye, pupilAprox, desiredMaxSize, threshold);
                case PositionTrackingMethod.ConvexHull:
                    return CalculatePositionConvexHull(imageEye, pupilAprox, desiredMaxSize, threshold);
                case PositionTrackingMethod.EllipseFitting:
                    return CalculatePositionEllipseFitting(imageEye, mask, pupilAprox, pupilReference, desiredMaxSize, threshold);
                default:
                    break;
            }

            return new PupilData();
        }


        /// <summary>
        /// Calculates the position of the eye (vertical and horizontal).
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="mask">Mask of the eyelids, reflections, etc.</param>
        /// <param name="pupilAprox">Pupil ellipse, rough aproximation.</param>
        /// <param name="pupilReference">Pupil ellipse reference from calibration.</param>
        /// <param name="desiredMaxSize"></param>
        /// <param name="threshold"></param>
        /// <returns>The pupil ellipse.</returns>
        public static PupilData CalculatePositionEllipseFitting(ImageEye imageEye, Image<Gray, byte>? mask, PupilData pupilAprox, PupilData pupilReference, int desiredMaxSize, int threshold)
        {
            // Do it at the begining so it does not change during the processing
            var DEBUG = EyeTracker.DEBUG;

            (var imgPupilBinary, var imgTemp, var roiPupil) = GetBinaryPupilImage(imageEye, pupilAprox, desiredMaxSize, threshold);

            if (imgPupilBinary is null) return pupilAprox;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Contour analysis -- Find the contour with the largest area and select the points
            // that are more likely to be part of the pupil and not artifacts like eyelashes or reflections.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Factor for the polynomial aproximation of the countour. The larger the number the
            // smoother the counter will be.
            var aproxPolyFactor = 0.8;

            var ELLIPSE_FITTING_DO_CURVATURE_FILTER = true;
            var ELLIPSE_FITTING_DO_MASK = true;
            var ELLIPSE_FITTING_DO_APROX_POLY = true;
            var ELLIPSE_FITTING_DO_HEURISTIC_INTERPOLATION = true;

            (var pupilCountour, var imgPupilBinaryDebug1) = GetPupilContour(imageEye, imgPupilBinary);

            if (pupilCountour is null) return pupilAprox;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Smooth the contour -- Aproximate the countour by a polinomial to smooth it.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (ELLIPSE_FITTING_DO_APROX_POLY)
            {
                // Simplify the contour
                CvInvoke.ApproxPolyDP(pupilCountour, pupilCountour, aproxPolyFactor, true);
            }

            if (DEBUG)
            {
                foreach (var point in pupilCountour.ToArray())
                {
                    // Draw all the remaining points in the contour in green.
                    imgPupilBinaryDebug1?.Draw(new CircleF(new PointF(point.X, point.Y), 1f), new Bgr(0, 255, 0), 2);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Curvature analysis -- Remove points whith large changes in curvature or two far
            // from the center of mass. 
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (ELLIPSE_FITTING_DO_CURVATURE_FILTER)
            {
                // Get an approxiamte center of the ellipse using the center of mass of the contour
                var gravCenter = CvInvoke.Moments(pupilCountour).GravityCenter;

                var center = new PointF((float)gravCenter.X, (float)gravCenter.Y);

                Image<Bgr, byte>? imgPupilBinaryDebug2 = null;
                if (DEBUG)
                {
                    imgPupilBinaryDebug2 = imgPupilBinary.Convert<Bgr, byte>();

                    // Save stuff for debugging
                    if (pupilCountour.Size > 2)
                    {
                        foreach (var point in pupilCountour.ToArray())
                        {
                            imgPupilBinaryDebug2.Draw(new CircleF(new PointF(point.X, point.Y), 1f), new Bgr(Color.Green), 2);
                        }
                    }
                    imgPupilBinaryDebug1?.Draw(new Cross2DF(center, 10, 10), new Bgr(Color.Red), 2);
                }

                // /// Calculate curvature associated with each contour point ///
                var curvature = new double[pupilCountour.Size];
                var curvature2 = new double[pupilCountour.Size];
                var distanceToCenter = new double[pupilCountour.Size];
                var pupilContourSize = pupilCountour.Size;
                for (int i = 0; i < pupilContourSize; i++)
                {
                    // Circular index
                    var inext = i + 1;
                    if (inext >= pupilContourSize)
                    {
                        inext = 0;
                    }

                    // Get two points of the countour
                    var p1 = pupilCountour[i];
                    var p2 = pupilCountour[inext];

                    // Define the triangle formed by those two points and the center
                    var a = Math.Sqrt(Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.X - p2.X, 2));
                    var b = Math.Sqrt(Math.Pow(p1.Y - center.Y, 2) + Math.Pow(p1.X - center.X, 2));
                    var c = Math.Sqrt(Math.Pow(p2.Y - center.Y, 2) + Math.Pow(p2.X - center.X, 2));

                    // Angle between the line that connects Pi with the center and Pi with P+1
                    curvature[i] = Math.Acos((Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b)) * 180 / Math.PI;

                    // Angle between the line that connects Pi with the center and Pi with P-1
                    curvature2[inext] = Math.Acos((Math.Pow(a, 2) + Math.Pow(c, 2) - Math.Pow(b, 2)) / (2 * a * c)) * 180 / Math.PI;

                    // Distance between Pi and the centroid of the contour
                    distanceToCenter[i] = b;
                }

                var area = CvInvoke.ContourArea(pupilCountour, false);

                // Radius of the circle with an equivalent area to the contour
                var radiusEquivCircle = Math.Sqrt(area / Math.PI);

                // Variable to indicate if a point is good or not and if it should be kept
                var goodPoint = new bool[pupilCountour.Size];

                pupilContourSize = pupilCountour.Size;
                var nBadPoints = 0;
                for (int i = 0; i < pupilContourSize; i++)
                {
                    // Make it circular
                    var inext = i + 1;
                    if (inext >= pupilContourSize)
                    {
                        inext = 0;
                    }
                    var iprev = i - 1;
                    if (iprev < 0)
                    {
                        inext = pupilContourSize - 1;
                    }

                    goodPoint[i] = true;

                    // If the curvature of that point is not good the point is not good
                    if (curvature[i] < 50 || curvature[i] > 140 || curvature2[i] < 50 || curvature2[i] > 140)
                    {
                        goodPoint[i] = false;
                        nBadPoints++;
                        continue;
                    }

                    // If the change in curvature between one point and the next is not good
                    if (Math.Abs(curvature[i] - curvature2[i]) / Math.Abs(curvature[i] + curvature2[i]) > 0.3)
                    {
                        goodPoint[i] = false;
                        nBadPoints++;
                        continue;
                    }

                    // If the distance to the center is to large
                    if (Math.Abs(distanceToCenter[i] - radiusEquivCircle) > radiusEquivCircle * 0.8)
                    {
                        goodPoint[i] = false;
                        nBadPoints++;
                        continue;
                    }
                }
                if (nBadPoints > 0)
                {
                    for (int i = 0; i < pupilContourSize; i++)
                    {
                        var nPreviousGood = 0;
                        var nNextGood = 0;

                        if (!goodPoint[i])
                        {
                            // Go forward finding the next bad point
                            for (int j = i + 1; j != i; j++)
                            {
                                if (j >= pupilContourSize)
                                {
                                    j = -1;
                                    continue;
                                };

                                if (goodPoint[j])
                                {
                                    nNextGood++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // Go backward finding the previous bad point
                            for (int j = i - 1; j != i; j--)
                            {
                                if (j < 0)
                                {
                                    j = pupilContourSize;
                                    continue;
                                };

                                if (goodPoint[j])
                                {
                                    nPreviousGood++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (nNextGood > 2 && nPreviousGood > 2)
                            {
                                goodPoint[i] = true;
                            }
                        }
                    }

                    for (int i = 0; i < pupilContourSize; i++)
                    {
                        var nPreviousGood = 0;
                        var nNextGood = 0;

                        if (goodPoint[i])
                        {
                            // Go forward finding the next bad point
                            for (int j = i + 1; j != i; j++)
                            {
                                if (j >= pupilContourSize)
                                {
                                    j = -1;
                                    continue;
                                };

                                if (goodPoint[j])
                                {
                                    nNextGood++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // Go backward finding the previous bad point
                            for (int j = i - 1; j != i; j--)
                            {
                                if (j < 0)
                                {
                                    j = pupilContourSize;
                                    continue;
                                };

                                if (goodPoint[j])
                                {
                                    nPreviousGood++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // Remove all groups of points that have a neighborhood of less than 4
                            // good points.
                            if (nNextGood + nPreviousGood < 5)
                            {
                                goodPoint[i] = false;
                            }
                        }
                    }

                    var pointList = new List<PointF>(pupilCountour.ToArray());
                    for (int i = pupilContourSize - 1; i >= 0; i--)
                    {
                        if (!goodPoint[i])
                        {
                            pointList.RemoveAt(i);
                            continue;
                        }
                    }

                    pupilCountour = new VectorOfPointF(pointList.ToArray());
                }

                // If there are too few points do not try to fit the ellipse, just exit
                if (pupilCountour.Size < 8)
                {
                    return new PupilData();
                }
            }

            Image<Bgr, byte>? imageDebug = null;
            if (DEBUG)
            {
                // Save stuff for debugging
                if (pupilCountour.Size > 2)
                {
                    foreach (var point in pupilCountour.ToArray())
                    {
                        imgPupilBinaryDebug1?.Draw(new CircleF(new PointF(point.X, point.Y), 1f), new Bgr(0, 0, 255), 2);
                    }
                }

                imageDebug = imgTemp?.Convert<Bgr, byte>();
            }


            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Mask -- Remove points that are too close to the mask because they are probably not
            // edges of the pupil
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (ELLIPSE_FITTING_DO_MASK && mask != null)
            {
                var maskROIpupil = mask.Copy(roiPupil).Resize(imgPupilBinary.Width, imgPupilBinary.Height, Emgu.CV.CvEnum.Inter.Nearest);
                int maskErodeSize = imgPupilBinary.Width / 10;
                int maskErodeCenter = (int)Math.Floor(maskErodeSize / 2.0);
                var maskErodeKernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Ellipse, new Size(maskErodeSize, maskErodeSize), new Point(maskErodeCenter, maskErodeCenter));
                CvInvoke.Erode(maskROIpupil, maskROIpupil, maskErodeKernel, new Point(maskErodeCenter, maskErodeCenter), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));

                // Go trhough the points in the countour in reverese order to avoid problems when deleting

                var pointList = new List<PointF>(pupilCountour.ToArray());
                for (int i = pointList.Count - 1; i >= 0; i--)
                {
                    if (maskROIpupil.Data[(int)Math.Round(pointList[i].Y), (int)Math.Round(pointList[i].X), 0] == 0)
                    {
                        pointList.RemoveAt(i);
                    }
                }

                pupilCountour = new VectorOfPointF(pointList.ToArray());

                if (DEBUG)
                {
                    imageDebug?.SetValue(new Bgr(Color.Yellow), maskROIpupil.Not());
                }
            }

            if (DEBUG)
            {
                // Save stuff for debugging
                if (pupilCountour.Size > 2)
                {
                    imgPupilBinaryDebug1?.Draw(pupilCountour.ToArray().Select(p => new Point((int)p.X, (int)p.Y)).ToArray(), new Bgr(0, 255, 255), 2);
                }
            }

            // If there are too few points do not try to fit the ellipse, or if the shape is not
            // round enough, just exit
            var boundingRect = CvInvoke.BoundingRectangle(pupilCountour);
            if (pupilCountour.Size < 8 || boundingRect.Height < boundingRect.Width / 2) return pupilAprox;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Heuristic interpolation -- Fill in points to interpolate the top part of the pupil.
            // The top part of the pupil is usually cover by the eyelid. In that case it helps to add
            // a few points so the ellipse fitting is more accurate and less noise. One good guess of
            // the position of the top of the pupil is to keep a memory of the aspect ratio of the
            // pupil and then taking into account the current position of the bottom and the current
            // width of the pupil calculate the corresponding top.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            bool contourComplete = true;

            if (ELLIPSE_FITTING_DO_HEURISTIC_INTERPOLATION)
            {
                // TODO: If the current position is too far from the reference position don't do this
                // unless there is a geometric correction

                var boundingBox = pupilCountour.GetCountourBoundingBox();

                var pointMostLeft = new PointF(int.MaxValue, 0);
                var pointMostRight = new PointF(int.MinValue, 0);
                var pointMostUp = new PointF(0, int.MaxValue);
                var pointMostDown = new PointF(0, int.MinValue);

                // Middle of the bounding box
                var middleX = boundingRect.X + boundingBox.Width / 2.0;

                var Npoints = pupilCountour.Size;

                // Check if the counter is complete
                for (int i = 0; i < pupilCountour.Size; i++)
                {
                    if (pupilCountour[i].X < pointMostLeft.X)
                    {
                        pointMostLeft = pupilCountour[i];
                    }
                    if (pupilCountour[i].X > pointMostRight.X)
                    {
                        pointMostRight = pupilCountour[i];
                    }
                    if (pupilCountour[i].Y < pointMostUp.Y)
                    {
                        pointMostUp = pupilCountour[i];
                    }
                    if (pupilCountour[i].Y > pointMostDown.Y)
                    {
                        pointMostDown = pupilCountour[i];
                    }

                    // If there is a gap and the gap includes the middle of the box
                    var gapIncludesMiddle = ((pupilCountour[i].X - middleX) * (pupilCountour[(i + 1) % Npoints].X - middleX) < 0);
                    var gapTooLarge = Math.Abs(pupilCountour[i].X - pupilCountour[(i + 1) % Npoints].X) > Math.Max(imgPupilBinary.Width, imgPupilBinary.Height) / 5;
                    if (gapTooLarge && gapIncludesMiddle)
                    {
                        contourComplete = false;
                    }
                }

                var pointList = new List<PointF>(pupilCountour.ToArray());
                if (!contourComplete && !pupilReference.IsEmpty)
                {
                    // Get an approxiamte center of the ellipse using the center of mass of the contour
                    var gravCenter = CvInvoke.Moments(pupilCountour).GravityCenter;

                    // Add the extra points to interpolate the top part of the countour
                    var currentWidth = pointMostRight.X - pointMostLeft.X;
                    var currentHeight = boundingBox.Height;

                    var box = new Ellipse(pupilReference.Center, pupilReference.Size, pupilReference.Angle + 90);
                    var r = box.GetEllipseBoundingBox();

                    var pupilSizeRatio = r.Width / r.Height;

                    var pupilHeight = currentWidth / pupilSizeRatio;

                    pointList.Insert(0, new Point(
                        (int)gravCenter.X,
                        (int)(boundingBox.Y + currentHeight - pupilHeight)));
                }

                pupilCountour = new VectorOfPointF(pointList.ToArray());
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Ellipse fitting -- Fit an ellipse to the points of the contour.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            // If there are too few points do not try to fit the ellipse, just exit
            if (pupilCountour.Size < 8) return pupilAprox;

            var pupilEllipse = CvInvoke.FitEllipse(pupilCountour);
            
            // Change the ellipse information to the coordinates of the original image

            PupilData pupil = GetPupilData(imgPupilBinary, roiPupil, pupilEllipse);

            if (DEBUG)
            {
                if (imageDebug != null)
                {
                    imageDebug.Draw(new Ellipse(pupilEllipse.Center, pupilEllipse.Size, pupilEllipse.Angle - 90), new Bgr(Color.Red), 1);
                    if (pupilCountour.Size > 2)
                    {
                        foreach (var point in pupilCountour.ToArray())
                        {
                            imageDebug.Draw(new CircleF(new PointF(point.X, point.Y), 1f), new Bgr(Color.Orange), 2);
                        }
                    }
                    EyeTrackerDebug.AddImage("position", imageEye.WhichEye, imageDebug);
                }
            }

            return pupil;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="pupilAprox"></param>
        /// <param name="desiredMaxSize"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static (Image<Gray, byte>?, Image<Gray, byte>?, Rectangle) GetBinaryPupilImage(ImageEye imageEye, PupilData pupilAprox, int desiredMaxSize, int threshold)
        {
            // Size of the image of the pupil that will be used to run the ellipse detection. To
            // avoid bigger pupils taking longer to process than small pupils resize the image to a
            // fixed size that works well. Since the ROI is not necessarily square the new image will
            // keep the proportions but the larger dimension will be fixed.
            var DO_IMAGE_CLOSING = false;
            var DO_IMAGE_OPENNING = false;

            // Add some padding to the pupil ROI just in case make the ROI always square. This may
            // help in some occasions.
            var squareSize = Math.Max(pupilAprox.Size.Width, pupilAprox.Size.Height);
            var roiPupil = new Rectangle(
                 (int)Math.Round(pupilAprox.Center.X - (squareSize / 2.0) - squareSize / 4),
                 (int)Math.Round(pupilAprox.Center.Y - (squareSize / 2.0) - squareSize / 4),
                 (int)Math.Round(squareSize + squareSize / 4 * 2),
                 (int)Math.Round(squareSize + squareSize / 4 * 2));
            roiPupil.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));

            if (roiPupil.IsEmpty) return (null, null, roiPupil);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Resizing -- To avoid bigger pupils taking longer to process than small pupils
            // resize the image to a fixed size that works well. Since the ROI is not necessarily
            // square the new image will keep the proportions but the larger dimension will be fixed.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            float scaleFactor = (float)desiredMaxSize / Math.Max(roiPupil.Width, roiPupil.Height);
            int resizeWidth = (int)Math.Round(scaleFactor * (float)roiPupil.Width);
            int resizeHeight = (int)Math.Round(scaleFactor * (float)roiPupil.Height);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Thresholding -- Get the binary image with the dark pixels
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            int blurSize = (int)Math.Round(2 * scaleFactor);
            var imgTemp = imageEye.Image.Copy(roiPupil).Resize(resizeWidth, resizeHeight, Emgu.CV.CvEnum.Inter.Cubic).SmoothBlur(blurSize, blurSize);
            var imgPupilBinary = imgTemp.SmoothBlur(10, 10).ThresholdBinaryInv(new Gray(threshold), new Gray(255));

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Image Closing and opening -- Remove little dots over threshold or little gaps under
            // the treshold.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (DO_IMAGE_CLOSING)
            {
                int size = 2 * (int)Math.Round(2 * (desiredMaxSize / 50.0)) + 1;
                int center = (int)Math.Floor(size / 2.0);
                var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Ellipse, new Size(size, size), new Point(center, center));
                CvInvoke.Erode(imgPupilBinary, imgPupilBinary, kernel, new Point(center, center), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));
                CvInvoke.Dilate(imgPupilBinary, imgPupilBinary, kernel, new Point(center, center), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));
            }

            if (DO_IMAGE_OPENNING)
            {
                int size = 2 * (int)Math.Round(2 * (desiredMaxSize / 50.0)) + 1;
                int center = (int)Math.Floor(size / 2.0);
                var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Ellipse, new Size(size, size), new Point(center, center));
                CvInvoke.Dilate(imgPupilBinary, imgPupilBinary, kernel, new Point(center, center), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));
                CvInvoke.Erode(imgPupilBinary, imgPupilBinary, kernel, new Point(center, center), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));
            }

            return (imgPupilBinary, imgTemp, roiPupil);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="imgPupilBinary"></param>
        /// <returns></returns>
        public static (VectorOfPointF?, Image<Bgr, byte>?) GetPupilContour(ImageEye imageEye, Image<Gray, byte> imgPupilBinary)
        {
            var DEBUG = EyeTracker.DEBUG;

            VectorOfPointF? pupilCountour = null;
            Image<Bgr, byte>? imgPupilBinaryDebug1 = null;

            using var contours = new VectorOfVectorOfPoint();
            using Mat hierachy = new Mat();

            double maxArea = 0;
            int pupilContourIndex = -1;
            CvInvoke.FindContours(imgPupilBinary, contours, hierachy, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            for (int i = 0; i < contours.Size; i++)
            {
                var contourArea = CvInvoke.ContourArea(contours[i], false);
                if (contourArea < maxArea) continue;

                maxArea = contourArea;
                pupilContourIndex = i;
            }

            if (pupilContourIndex < 0)
            {
                return (null, null);
            }
            else
            {
                pupilCountour = new VectorOfPointF(Array.ConvertAll(contours[pupilContourIndex].ToArray(), p => new PointF(p.X, p.Y)));
            }

            if (DEBUG)
            {
                imgPupilBinaryDebug1 = imgPupilBinary.Convert<Bgr, byte>();
                EyeTrackerDebug.AddImage("position1", imageEye.WhichEye, imgPupilBinaryDebug1);

                // Save stuff for debugging
                foreach (var point in pupilCountour.ToArray())
                {
                    // Draw all original points in the contour in blue.
                    imgPupilBinaryDebug1.Draw(new CircleF(new PointF(point.X, point.Y), 1f), new Bgr(255, 0, 0), 2);
                }
            }

            // If there are too few points do not try to fit the ellipse, just exit
            if (pupilCountour is null || pupilCountour.Size < 10)
            {
                return (null, null);
            }

            return (pupilCountour, imgPupilBinaryDebug1);
        }

        private static PupilData GetPupilData(Image<Gray, byte> imgPupilBinary, Rectangle roiPupil, RotatedRect pupilEllipse)
        {
            float scaleFactorX = imgPupilBinary.Width / (float)roiPupil.Width;
            float scaleFactorY = imgPupilBinary.Height / (float)roiPupil.Height;

            var pupil = new PupilData(
                new PointF((pupilEllipse.Center.X / scaleFactorX) + (float)roiPupil.X, (pupilEllipse.Center.Y / scaleFactorY) + (float)roiPupil.Y),
                new SizeF(pupilEllipse.Size.Width / scaleFactorX, pupilEllipse.Size.Height / scaleFactorY),
                pupilEllipse.Angle - 90);
            return pupil;
        }

        /// <summary>
        /// Calculates the position of the eye (vertical and horizontal).
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="pupilAprox">Pupil ellipse, rough aproximation.</param>
        /// <param name="desiredMaxSize">Desired magnification in pixels.</param>
        /// <param name="threshold">Treshold for dark pixels.</param>
        /// <returns>The pupil ellipse.</returns>
        public static PupilData CalculatePositionConvexHull(ImageEye imageEye, PupilData pupilAprox, int desiredMaxSize, int threshold)
        {
            (var imgPupilBinary, _, var roiPupil) = GetBinaryPupilImage(imageEye, pupilAprox, desiredMaxSize, threshold);
            if (imgPupilBinary is null) return pupilAprox;

            (var pupilContour, _) = GetPupilContour(imageEye, imgPupilBinary);

            if (pupilContour is null) return pupilAprox;

            var centerMass = CvInvoke.Moments(pupilContour).GravityCenter;

            if (double.IsNaN(centerMass.X)) return pupilAprox;

            var boundingRectangle = CvInvoke.BoundingRectangle(pupilContour);
            var pupilEllipse = new RotatedRect(
                new PointF((float)centerMass.X, (float)centerMass.Y), 
                new SizeF((float)boundingRectangle.Width, (float)boundingRectangle.Height),
                angle:90f);
            pupilAprox = GetPupilData(imgPupilBinary, roiPupil, pupilEllipse);

            // Save stuff for debugging
            EyeTrackerDebug.AddImage("position", imageEye.WhichEye, imgPupilBinary);

            return pupilAprox; 
        }

        /// <summary>
        /// Calculates the position of the eye (vertical and horizontal).
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="pupilAprox">Pupil ellipse, rough aproximation.</param>
        /// <param name="desiredMaxSize">Desired magnification in pixels.</param>
        /// <param name="threshold">Treshold for dark pixels.</param>
        /// <returns>The pupil ellipse.</returns>
        public static PupilData CalculatePositionCentroid(ImageEye imageEye, PupilData pupilAprox, int desiredMaxSize, int threshold)
        {
            (var imgPupilBinary, _, var roiPupil) = GetBinaryPupilImage(imageEye, pupilAprox, desiredMaxSize, threshold);
            if (imgPupilBinary is null) return pupilAprox;
            
            var centerMass = imgPupilBinary.GetMoments(true).GravityCenter;

            var pupilEllipse = new RotatedRect(
                new PointF((float)centerMass.X, (float)centerMass.Y),
                new SizeF(pupilAprox.Size.Width/roiPupil.Width*imgPupilBinary.Width, pupilAprox.Size.Height / roiPupil.Height * imgPupilBinary.Height),
                angle: 90f);
            pupilAprox = GetPupilData(imgPupilBinary, roiPupil, pupilEllipse);
            
            // Save stuff for debugging
            EyeTrackerDebug.AddImage("position", imageEye.WhichEye, imgPupilBinary);

            return pupilAprox;
        }
    }
}