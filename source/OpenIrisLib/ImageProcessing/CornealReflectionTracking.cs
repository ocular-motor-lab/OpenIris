//-----------------------------------------------------------------------
// <copyright file="CornealReflectionTrackerBlob.cs">
//     Copyright (c) 2018 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Cvb;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// Position tracker that does not additional calculations. It just outputs the same pupil position as it gets.
    /// </summary>
    public sealed class CornealReflectionTracking : IDisposable
    {
        /// <summary>
        /// Posible methods for Corneal reflection tracking.
        /// </summary>
        public enum CornealReflectionTrackingMethod
        {
            /// <summary>
            /// Do not track the corneal reflections.
            /// </summary> 
            None,

            /// <summary>
            /// Use blob detection.
            /// </summary>
            Blob,

            /// <summary>
            /// Use blob detection.
            /// </summary>
            Blob_Resize,
        }

        private readonly CvBlobDetector detector = new CvBlobDetector();
        private readonly CvBlobs blobs = new CvBlobs();

        /// <summary>
        /// Find the corneal reflections.
        /// </summary>
        /// <param name="imageEye">The image of the eye.</param>
        /// <param name="pupilAprox">Current position of the pupil.</param>
        /// <param name="trackingSettings">Settings for tracking.</param>
        /// <returns></returns>
        public CornealReflectionData[] FindCornealReflections(ImageEye imageEye, PupilData pupilAprox, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            int blurSize = (int)Math.Ceiling(trackingSettings.MinCRRadPix / 2);
            var threshold = (imageEye.WhichEye == Eye.Left) ? trackingSettings.BrightThresholdLeftEye : trackingSettings.BrightThresholdRightEye;
            var maxBlobArea = trackingSettings.MaxCRRadPix * Math.PI * Math.PI;
            var minBlobArea = trackingSettings.MinCRRadPix * Math.PI * Math.PI;
            var irisRadiusPix =  (int) Math.Round( (imageEye.WhichEye == Eye.Left) ? trackingSettings.IrisRadiusPixLeft : trackingSettings.IrisRadiusPixRight);
            var resizeRate_findCRBlob = (double) trackingSettings.ResizeRate_findCRBlob;
            var resizeRate_findCRCenter = (double)trackingSettings.ResizeRate_findCRCenter;
            var openImage = trackingSettings.OpenImage;
            var closeImage = trackingSettings.CloseImage;
            
            return trackingSettings.CornealReflectionTrackingMethod switch
            {
                CornealReflectionTrackingMethod.Blob => FindCornealReflectionsBlob(detector, blobs, imageEye, pupilAprox, blurSize, threshold, maxBlobArea, minBlobArea),
                CornealReflectionTrackingMethod.Blob_Resize => FindCornealReflectionsBlobRS_Resize(detector, blobs, imageEye, pupilAprox, blurSize, threshold, maxBlobArea, minBlobArea, irisRadiusPix, resizeRate_findCRBlob, resizeRate_findCRCenter, openImage, closeImage),
                _ => Array.Empty<CornealReflectionData>(),
            };
        }

        /// <summary>
        /// Finds the corneal reflections.
        /// </summary>
        /// <param name="detector"></param>
        /// <param name="blobs"></param>
        /// <param name="imageEye">Image of the eye.</param> 
        /// <param name="pupilAprox"></param>
        /// <param name="blurSize"></param>
        /// <param name="maxBlobArea"></param>
        /// <param name="minBlobArea"></param>
        /// <param name="threshold"></param>
        /// <returns>Pupil ellipse.</returns>
        public static CornealReflectionData[] FindCornealReflectionsBlob(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, PupilData pupilAprox, int blurSize, int threshold, double maxBlobArea, double minBlobArea)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            // Add some padding to the pupil ROI just in case make the ROI always square. This may
            // help in some occasions.
            var squareSize = Math.Max(pupilAprox.Size.Width, pupilAprox.Size.Height);
            var roiPupil = new Rectangle(
                 (int)Math.Round(pupilAprox.Center.X - (squareSize) - squareSize / 2),
                 (int)Math.Round(pupilAprox.Center.Y - (squareSize / 2.0) - squareSize / 4),
                 (int)Math.Round(squareSize * 2 + squareSize / 2 * 2),
                 (int)Math.Round(squareSize + squareSize / 4 * 2));
            roiPupil.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Thresholding -- Get the binary image with the dark pixels
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            var imgTemp = imageEye.Image.Copy(roiPupil).SmoothBlur(blurSize, blurSize);
            var imgPupilBinary = imgTemp.ThresholdBinary(new Gray(threshold), new Gray(255));
            // TODO: image open and close


            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Blob selection --
            // Find the blob that is most likely to be the CR.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            var crs = new System.Collections.Generic.List<CornealReflectionData>();

            detector.Detect(imgPupilBinary, blobs);

            foreach (var item in blobs)
            {
                CvBlob blob = item.Value;

                // If area is too small
                if (blob.Area > maxBlobArea || blob.Area < minBlobArea)
                {
                    continue;
                }

                var contour = blob.GetContour();

                // the score is the ration between area and perimeter
                var score = blob.Area * blob.Area / (double)(blob.BoundingBox.Width * blob.BoundingBox.Height);


                if (score > 0.1 && crs.Count < 5)
                {
                    crs.Add(new CornealReflectionData(
                        new PointF(blob.Centroid.X + roiPupil.X, blob.Centroid.Y + roiPupil.Y),
                        new SizeF(blob.BoundingBox.Size),
                        (float)90.0));
                }
            }

            // Sort the blobs by distance to the pupil
            crs.Sort((x, y) => Math.Sqrt(Math.Pow(pupilAprox.Center.X - x.Center.X, 2) + Math.Pow(pupilAprox.Center.Y - x.Center.Y, 2)).CompareTo(Math.Sqrt(Math.Pow(pupilAprox.Center.X - y.Center.X, 2) + Math.Pow(pupilAprox.Center.Y - y.Center.Y, 2))));

            if (EyeTracker.DEBUG)
            {
                var imgDebug = imgPupilBinary.Convert<Bgr, byte>();
                foreach (var cr in crs)
                {
                    imgDebug.Draw(new CircleF(new PointF(cr.Center.X - roiPupil.X, cr.Center.Y - roiPupil.Y), cr.Size.Width * 2), new Bgr(Color.Red), 2);
                }

                EyeTrackerDebug.AddImage("CR", imageEye.WhichEye, imgDebug);
            }

            return crs.ToArray();
        }

        public static CornealReflectionData[] FindCornealReflectionsBlobRS_Resize(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, PupilData pupilAprox, int blurSize, int threshold, double maxBlobArea, double minBlobArea, int irisRadiusPix, double scaleFactor_findBlob = 1.0, double scaleFactor_findCenter = 1.0, bool openImage = false, bool closeImage = false)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            //using iris radius for the ROI to look for CR
            int squareSize = irisRadiusPix * 2;
            var irisROI = new Rectangle(
                (int)Math.Round(pupilAprox.Center.X - squareSize / 2),
                (int)Math.Round(pupilAprox.Center.Y - squareSize / 2),
                squareSize,
                squareSize
                );
            irisROI.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));

            if (EyeTracker.DEBUG)
            {
                var imgDebug = imageEye.Image.Convert<Bgr, byte>();
                imgDebug.Draw(irisROI, new Bgr(Color.Red), 2);
                

                EyeTrackerDebug.AddImage("CRroi", imageEye.WhichEye, imgDebug);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Thresholding -- Resize and Get the binary image
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            blurSize = blurSize == 1 ? 2 : blurSize;
            
            var resizeWidth =(int) Math.Round(irisROI.Width * scaleFactor_findBlob);
            var resizeHeight =(int) Math.Round(irisROI.Height * scaleFactor_findBlob);

            if (irisROI.Width == 0 || irisROI.Height == 0) return null;

            Image<Gray, byte> imgTemp;
            if (scaleFactor_findBlob == 1)
            {
                //No resizing
                imgTemp = imageEye.Image.Copy(irisROI).SmoothBlur(blurSize, blurSize);
            }
            else
            {
                imgTemp = imageEye.Image.Copy(irisROI).Resize(resizeWidth, resizeHeight, Emgu.CV.CvEnum.Inter.Cubic).SmoothBlur(blurSize, blurSize);
            }
            
            var imgPupilBinary = imgTemp.ThresholdBinary(new Gray(threshold), new Gray(255));

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Morphological erosion and dilation --
            // Optimize the blobs by removing small white or black spots
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            var kernelSize = blurSize * 2 + 1;
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(kernelSize, kernelSize), new Point(-1, -1));
            var center = new Point(-1, -1);
            var one = new MCvScalar(1);
            if (openImage)
            {
                // Image opening. Morpholigical operation to remove small spots that are above treshold.
                // First it erodes the image. That is reduce the size of the white blobs. Next it dilates the
                // remaning blobs. This will eliminate the blobs that are smaller than the kernel size.
                CvInvoke.Erode(imgPupilBinary, imgPupilBinary, kernel, center, 1, BorderType.Default, one);
                CvInvoke.Dilate(imgPupilBinary, imgPupilBinary, kernel, center, 1, BorderType.Default, one);
            }
            if (closeImage)
            {
                // Image closing. Morpholigical operation to remove small spots that are below treshold.
                // First it dilates the image. That is increase the size of the white blobs. Next it erods the
                // remaning blobs. This will eliminate the spots within the blobs that are smaller than the kernel size.
                CvInvoke.Dilate(imgPupilBinary, imgPupilBinary, kernel, center, 1, BorderType.Default, one);
                CvInvoke.Erode(imgPupilBinary, imgPupilBinary, kernel, center, 1, BorderType.Default, one);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Blob selection --
            // Find the blob that is most likely to be the CR.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            var crs = new System.Collections.Generic.List<CornealReflectionData>();

            detector.Detect(imgPupilBinary, blobs);

            irisRadiusPix = (int) Math.Round(irisRadiusPix * scaleFactor_findBlob);
            maxBlobArea = maxBlobArea * Math.Pow(scaleFactor_findBlob, 2);
            minBlobArea = minBlobArea * Math.Pow(scaleFactor_findBlob, 2);

            var crROI = new Rectangle();

            foreach (var item in blobs)
            {
                CvBlob blob = item.Value;

                var distanceToPupil = Math.Sqrt(Math.Pow(Math.Abs(blob.Centroid.X) - irisRadiusPix, 2) + Math.Pow(Math.Abs(blob.Centroid.Y) - irisRadiusPix, 2));

                // If area is too small
                if (blob.Area > maxBlobArea || blob.Area < minBlobArea || distanceToPupil > irisRadiusPix)
                {
                    continue;
                }

                var contour = blob.GetContour();

                // the score is the ration between area and perimeter
                var score = blob.Area / (double)(blob.BoundingBox.Width * blob.BoundingBox.Height);

                if (scaleFactor_findCenter == 1)
                {
                    //score it based on a perfect circle within a square +- 0.1 
                    if (score > 0.68 && score < 0.88)
                    {
                        crs.Add(new CornealReflectionData(
                            new PointF((int)Math.Round(blob.Centroid.X / scaleFactor_findBlob) + irisROI.X, (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob) + irisROI.Y),
                            new SizeF(blob.BoundingBox.Size),
                            (float)90.0));
                    }
                }
                else
                {
                    //score it based on a perfect circle within a square +- 0.13 
                    if (score > 0.65 && score < 0.91)
                    {
                        // up sample to find the center of the crs
                        resizeWidth = (int)Math.Round(blob.BoundingBox.Width / scaleFactor_findBlob);
                        resizeHeight = (int)Math.Round(blob.BoundingBox.Height / scaleFactor_findBlob);
                        squareSize = Math.Max(resizeWidth, resizeHeight);

                        crROI = new Rectangle(
                            (int)Math.Round(blob.Centroid.X / scaleFactor_findBlob + irisROI.X - squareSize),
                            (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob + irisROI.Y - squareSize),
                            squareSize * 2,
                            squareSize * 2
                        );
                        crROI.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));

                        if (EyeTracker.DEBUG)
                        {
                            var imgDebug = imageEye.Image.Convert<Bgr, byte>();
                            imgDebug.Draw(crROI, new Bgr(Color.Red), 2);


                            EyeTrackerDebug.AddImage("CR - crROI", imageEye.WhichEye, imgDebug);
                        }

                        resizeWidth = (int)Math.Round(crROI.Width * scaleFactor_findCenter);
                        resizeHeight = (int)Math.Round(crROI.Height * scaleFactor_findCenter);

                        imgTemp = imageEye.Image.Copy(crROI).Resize(resizeWidth, resizeHeight, Emgu.CV.CvEnum.Inter.Cubic).SmoothBlur(blurSize, blurSize);
                        var upSampled_imgCRBinary = imgTemp.ThresholdBinary(new Gray(threshold), new Gray(255));
                        //crs.Add(new CornealReflectionData(
                        //            new PointF((int)Math.Round(blob.Centroid.X / scaleFactor_findBlob) + irisROI.X, (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob) + irisROI.Y),
                        //            new SizeF(blob.BoundingBox.Size),
                        //            (float)90.0));

                        if (EyeTracker.DEBUG)
                        {
                            var imgDebug = imageEye.Image.Convert<Bgr, byte>();
                            imgDebug.Draw(crROI, new Bgr(Color.Red), 2);


                            EyeTrackerDebug.AddImage("CRTEST", imageEye.WhichEye, upSampled_imgCRBinary);
                        }

                        // find the center
                        var centerMass = upSampled_imgCRBinary.GetMoments(true).GravityCenter;

                        var newX = centerMass.X / scaleFactor_findCenter + crROI.X;
                        var newY = centerMass.Y / scaleFactor_findCenter + crROI.Y;

                        crs.Add(new CornealReflectionData(
                        new PointF((float)newX, (float)newY),
                        new SizeF(blob.BoundingBox.Size),
                        angle: 90f));
                    }
                }
                
            }

            // Sort the blobs by distance to the pupil
            crs.Sort((x, y) => Math.Sqrt(Math.Pow(pupilAprox.Center.X - x.Center.X, 2) +
                Math.Pow(pupilAprox.Center.Y - x.Center.Y, 2)).CompareTo(Math.Sqrt(Math.Pow(pupilAprox.Center.X - y.Center.X, 2) +
                Math.Pow(pupilAprox.Center.Y - y.Center.Y, 2))));

            if (EyeTracker.DEBUG)
            {
                var imgDebug = imgPupilBinary.Convert<Bgr, byte>();
                foreach (var cr in crs)
                {
                    imgDebug.Draw(new CircleF(new PointF((int) Math.Round((cr.Center.X - irisROI.X)*scaleFactor_findBlob),(int) Math.Round((cr.Center.Y - irisROI.Y)*scaleFactor_findBlob)), cr.Size.Width * 2), new Bgr(Color.Red), 2);
                }

                EyeTrackerDebug.AddImage("CR", imageEye.WhichEye, imgDebug);
            }

            return crs.Take(5).ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            blobs?.Dispose();
            detector?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
