//-----------------------------------------------------------------------
// <copyright file="CornealReflectionTrackerBlob.cs">
//     Copyright (c) 2018 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
    using System;
    using System.Drawing;
    using Emgu.CV.Cvb;
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
            BlobRS,
        }

        private readonly CvBlobDetector detector = new CvBlobDetector();
        private readonly CvBlobs blobs = new CvBlobs();

        /// <summary>
        /// Find the corneal reflections.
        /// </summary>
        /// <param name="imageEye">The image of the eye.</param>
        /// <param name="pupilAprox">Current position of the pupil.</param>
        /// <param name="referencePupil">Position of the pupil in the reference image.</param>
        /// <param name="trackingSettings">Settings for tracking.</param>
        /// <returns></returns>
        public CornealReflectionData[] FindCornealReflections(ImageEye imageEye, PupilData pupilAprox, PupilData referencePupil, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            return trackingSettings.CornealReflectionTrackingMethod switch
            {
                CornealReflectionTrackingMethod.Blob => FindCornealReflectionsBlob(detector, blobs, imageEye, pupilAprox, trackingSettings),
                CornealReflectionTrackingMethod.BlobRS => FindCornealReflectionsBlobRS(detector, blobs, imageEye, pupilAprox, trackingSettings),
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
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>Pupil ellipse.</returns>
        public static CornealReflectionData[] FindCornealReflectionsBlob(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, PupilData pupilAprox, EyeTrackingPipelinePupilCRSettings trackingSettings)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            int blurSize = (int)Math.Ceiling(trackingSettings.MinCRRadPix / 2);
            var threshold = (imageEye.WhichEye == Eye.Left) ? trackingSettings.BrightThresholdLeftEye : trackingSettings.BrightThresholdRightEye;
            var maxBlobArea = trackingSettings.MaxCRRadPix * Math.PI * Math.PI;
            var minBlobArea = trackingSettings.MinCRRadPix * Math.PI * Math.PI;

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


        /// <summary>
        /// Finds the corneal reflections.
        /// </summary>
        /// <param name="detector"></param>
        /// <param name="blobs"></param>
        /// <param name="imageEye">Image of the eye.</param> 
        /// <param name="pupilAprox"></param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>Pupil ellipse.</returns>
        public static CornealReflectionData[] FindCornealReflectionsBlobRS(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, PupilData pupilAprox, EyeTrackingPipelinePupilCRSettings trackingSettings)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            int blurSize = (int)Math.Round(trackingSettings.MinCRRadPix / 2);
            var threshold = (imageEye.WhichEye == Eye.Left) ? trackingSettings.BrightThresholdLeftEye : trackingSettings.BrightThresholdRightEye;
            var maxBlobArea = trackingSettings.MaxCRRadPix * Math.PI * Math.PI;
            var minBlobArea = trackingSettings.MinCRRadPix * Math.PI * Math.PI;

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
