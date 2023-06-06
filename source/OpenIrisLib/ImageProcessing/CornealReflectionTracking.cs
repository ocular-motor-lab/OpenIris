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
            //Blob_Resize,
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

            int blurSize = (int)Math.Ceiling(trackingSettings.MinCRRadPix/2); //if not divided by 2, it only picks very small ones and by increasing the minCRRadPix it will make the cr vanish in opening and closing
            if (blurSize == 0) { blurSize = 1; }
            var threshold = (imageEye.WhichEye == Eye.Left) ? trackingSettings.BrightThresholdLeftEye : trackingSettings.BrightThresholdRightEye;
            var maxBlobArea = trackingSettings.MaxCRRadPix * Math.PI * Math.PI;
            var minBlobArea = trackingSettings.MinCRRadPix * Math.PI * Math.PI;
            var irisRadiusPix =  (int) Math.Round( (imageEye.WhichEye == Eye.Left) ? trackingSettings.IrisRadiusPixLeft : trackingSettings.IrisRadiusPixRight);

            //for low resolution camera image the downsample will remove the corneal reflection
            var resizeRate_findCRBlob = (double)0.5;
            var resizeRate_findCRCenter = (double)2;
            var openImage = true;
            var closeImage = true;

            return trackingSettings.CornealReflectionTrackingMethod switch
            {
                CornealReflectionTrackingMethod.Blob => FindCornealReflectionsBlob_Resize(detector, blobs, imageEye, pupilAprox, blurSize, threshold, maxBlobArea, minBlobArea, irisRadiusPix, resizeRate_findCRBlob, resizeRate_findCRCenter, openImage, closeImage),
                _ => Array.Empty<CornealReflectionData>(),
            };
        }

        public static CornealReflectionData[] FindCornealReflectionsBlob_Resize(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, PupilData pupilAprox, int blurSize, int threshold, double maxBlobArea, double minBlobArea, int irisRadiusPix, double scaleFactor_findBlob = 1.0, double scaleFactor_findCenter = 1.0, bool openImage = false, bool closeImage = false)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            //using iris radius for the ROI to look for CR
            double squareSize = irisRadiusPix * 2;
            var irisROI = new Rectangle(
                (int)Math.Round(pupilAprox.Center.X - squareSize / 2),
                (int)Math.Round(pupilAprox.Center.Y - squareSize / 2),
                (int)Math.Round(squareSize),
                (int)Math.Round(squareSize)
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

            //int resizeBlur = (int) Math.Round(blurSize * scaleFactor_findBlob);
            //resizeBlur = resizeBlur == 0 ? 1 : resizeBlur;
            double resizeWidth = irisROI.Width * scaleFactor_findBlob;
            double resizeHeight = irisROI.Height * scaleFactor_findBlob;

            if (irisROI.Width == 0 || irisROI.Height == 0) return null;

            Image<Gray, byte> imgBlurred = imageEye.Image.Copy(irisROI).SmoothBlur(blurSize, blurSize);
            Image<Gray, byte> imgResized;
            //resize the image
            if (scaleFactor_findBlob == 1)
            { imgResized = imgBlurred; }
            else
            {
                imgResized = imgBlurred.Resize((int)Math.Round(resizeWidth), (int)Math.Round(resizeHeight), Emgu.CV.CvEnum.Inter.Cubic);
            }

            var imgCRBinary = imgResized.ThresholdBinary(new Gray(threshold), new Gray(255));

            //draw debug binary image
            if (EyeTracker.DEBUG)
            {
                var imgDebug = imgCRBinary.Convert<Bgr, byte>();

                EyeTrackerDebug.AddImage("CR_Binary", imageEye.WhichEye, imgDebug);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Morphological erosion and dilation --
            // Optimize the blobs by removing small white or black spots
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            //adjust the kernel size as the image resized
            int kernelSize = (int) Math.Round(blurSize * scaleFactor_findBlob);
            kernelSize = kernelSize == 0 ? 1 : kernelSize;
            //var kernelSize = resizeBlur;
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(kernelSize, kernelSize), new Point(-1, -1));
            var center = new Point(-1, -1);
            var one = new MCvScalar(1);
            if (openImage)
            {
                // Image opening. Morpholigical operation to remove small spots that are above treshold.
                // First it erodes the image. That is reduce the size of the white blobs. Next it dilates the
                // remaning blobs. This will eliminate the blobs that are smaller than the kernel size.
                CvInvoke.Erode(imgCRBinary, imgCRBinary, kernel, center, 1, BorderType.Default, one);
                CvInvoke.Dilate(imgCRBinary, imgCRBinary, kernel, center, 1, BorderType.Default, one);
            }
            if (closeImage)
            {
                // Image closing. Morpholigical operation to remove small spots that are below treshold.
                // First it dilates the image. That is increase the size of the white blobs. Next it erods the
                // remaning blobs. This will eliminate the spots within the blobs that are smaller than the kernel size.
                CvInvoke.Dilate(imgCRBinary, imgCRBinary, kernel, center, 1, BorderType.Default, one);
                CvInvoke.Erode(imgCRBinary, imgCRBinary, kernel, center, 1, BorderType.Default, one);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Blob selection --
            // Find the blob that is most likely to be the CR.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            //draw debug binary image after opening and closing
            if (EyeTracker.DEBUG)
            {
                var imgDebug = imgCRBinary.Convert<Bgr, byte>();
                EyeTrackerDebug.AddImage("CR_BinaryOpenClose", imageEye.WhichEye, imgDebug);
            }

            var crs = new List<CornealReflectionData>();

            detector.Detect(imgCRBinary, blobs);

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

                // the score is the ration between area and squared perimeter 
                double blobBoundingBoxArea = (double) Math.Pow(Math.Max(blob.BoundingBox.Width, blob.BoundingBox.Height) , 2);  
                var score = blob.Area / blobBoundingBoxArea;

                if (scaleFactor_findCenter == 1)
                {
                    //score it based on a perfect circle within a square (area of a circle within a square = pi/4) - 0.38 
                    if (score > 0.4)
                    {
                        crs.Add(new CornealReflectionData(
                            new PointF((float) (blob.Centroid.X / scaleFactor_findBlob) + irisROI.X, (float) (blob.Centroid.Y / scaleFactor_findBlob) + irisROI.Y),
                            new SizeF(blob.BoundingBox.Size),
                            (float)90.0));
                    }
                }
                else
                {
                    //score it based on a perfect circle within a square (area of a circle within a square = pi/4), the square is enlarged by variable k. Therefore the score is (pi/4)/k^2
                    if (score > 0.4)
                    {
                        // up sample to find the center of the crs
                        resizeWidth = blob.BoundingBox.Width / scaleFactor_findBlob;
                        resizeHeight = blob.BoundingBox.Height / scaleFactor_findBlob;
                        squareSize = Math.Max(resizeWidth, resizeHeight) * 1.8; // expand the square size to upsample the blob 

                        crROI = new Rectangle(
                            (int)Math.Round(blob.Centroid.X / scaleFactor_findBlob + irisROI.X - squareSize),
                            (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob + irisROI.Y - squareSize),
                            (int)Math.Round(squareSize * 2),
                            (int)Math.Round(squareSize * 2)
                        );
                        crROI.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));

                        var crROI2 = new Rectangle(
                            (int)Math.Round(blob.Centroid.X / scaleFactor_findBlob + irisROI.X - squareSize),
                            (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob + irisROI.Y - squareSize),
                            (int)Math.Round(squareSize * 2 / 1.2),
                            (int)Math.Round(squareSize * 2 / 1.2)
                        );
                        crROI2.Intersect(new Rectangle(new Point(0, 0), imageEye.Size));


                        if (EyeTracker.DEBUG)
                        {
                            var imgDebug = imageEye.Image.Convert<Bgr, byte>();
                            imgDebug.Draw(crROI, new Bgr(Color.Red), 2);


                            EyeTrackerDebug.AddImage("CR - crROI", imageEye.WhichEye, imgDebug);
                        }

                        //resizeBlur = (int)Math.Round(blurSize * scaleFactor_findCenter);
                        //resizeBlur = resizeBlur == 0 ? 1 : resizeBlur;
                        resizeWidth = (int)Math.Round(crROI.Width * scaleFactor_findCenter);
                        resizeHeight = (int)Math.Round(crROI.Height * scaleFactor_findCenter);

                        imgResized = imageEye.Image.Copy(crROI).SmoothBlur(blurSize, blurSize).Resize((int)Math.Round(resizeWidth), (int)Math.Round(resizeHeight), Emgu.CV.CvEnum.Inter.Cubic);
                        var upSampled_imgCRBinary = imgResized.ThresholdBinary(new Gray(threshold), new Gray(255));
                        //crs.Add(new CornealReflectionData(
                        //            new PointF((int)Math.Round(blob.Centroid.X / scaleFactor_findBlob) + irisROI.X, (int)Math.Round(blob.Centroid.Y / scaleFactor_findBlob) + irisROI.Y),
                        //            new SizeF(blob.BoundingBox.Size),
                        //            (float)90.0));
                        

                        if (EyeTracker.DEBUG)
                        {
                            EyeTrackerDebug.AddImage("CRTEST", imageEye.WhichEye, upSampled_imgCRBinary);
                            
                            var imgDebug = imageEye.Image.Copy(crROI).Resize((int)Math.Round(resizeWidth), (int)Math.Round(resizeHeight), Emgu.CV.CvEnum.Inter.Cubic);
                            EyeTrackerDebug.AddImage("CRTEST2", imageEye.WhichEye, imgDebug);

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
                var imgDebug = imgCRBinary.Convert<Bgr, byte>();
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
