// <copyright file="PupilTrackerBlob.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Cvb;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;

    /// <summary>
    /// Finds the pupil using blob detection. The blobs are scored according to size and compacity.
    /// </summary>
    public sealed class PupilTracking : IDisposable
    {
        /// <summary>
        /// Posible mehtods for pupil tracking.
        /// </summary>
        public enum PupilTrackingMethod
        {
            /// <summary>
            /// Centroid of all the pixels in the image below threshold.
            /// </summary>
            Centroid,

            /// <summary>
            /// Blob search.
            /// </summary>
            Blob,
        }

        private readonly CvBlobDetector detector = new CvBlobDetector();
        private readonly CvBlobs blobs = new CvBlobs();

        /// <summary>
        /// Dispose objects
        /// </summary>
        public void Dispose()
        {
            blobs.Dispose();
            detector.Dispose();
        }

        /// <summary>
        /// Finds the pupil.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeRoi"></param>
        /// <param name="trackingSettings"></param>
        /// <returns></returns>
        public PupilData FindPupil(ImageEye imageEye, Rectangle eyeRoi, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            var maxPupRad = trackingSettings.MaxPupRadPix;
            var minPupArea = Math.PI * Math.Pow(trackingSettings.MinPupRadPix, 2);
            var thresholdDark = (imageEye.WhichEye == Eye.Left) ? trackingSettings.DarkThresholdLeftEye : trackingSettings.DarkThresholdRightEye;
            var imageSize = 200;

            return trackingSettings.PupilTrackingMethod switch
            {
                PupilTrackingMethod.Blob => FindPupilBlob(detector, blobs, imageEye, eyeRoi, maxPupRad, minPupArea, imageSize, thresholdDark),
                PupilTrackingMethod.Centroid => FindPupilCentroid(imageEye, eyeRoi, thresholdDark),
                _ => new PupilData(),
            };
        }

        /// <summary>
        /// Finds the pupil. Careful with multithreading and the prealocation of detector, blobs and kernel. 
        /// </summary>
        /// <param name="detector"></param>
        /// <param name="blobs"></param>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyeRoi">Roi containing the pupil.</param>
        /// <param name="maxPupRad">Maximum radious of the pupil in pixels.</param>
        /// <param name="minPupArea">Minimum pupil area in pixels.</param>
        /// <param name="imageSize">Size of the image used for searching blobs.</param>
        /// <param name="thresholdDark">Threshold to find dark pixels.</param>
        /// <returns>Pupil ellipse.</returns>
        public static PupilData FindPupilBlob(CvBlobDetector detector, CvBlobs blobs, ImageEye imageEye, Rectangle eyeRoi, double maxPupRad, double minPupArea, int imageSize, int thresholdDark)
        {
            if (detector is null) throw new ArgumentNullException(nameof(detector));
            if (blobs is null) throw new ArgumentNullException(nameof(blobs));
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            // Reduce the image to incrase processing speed because precision at this point is not
            // very important. 
            var smallSize = new Size(imageSize, (int)Math.Round((double)eyeRoi.Height / eyeRoi.Width * imageSize));
            var scaleDownX = (float)smallSize.Width / eyeRoi.Width;
            var scaleDownY = (float)smallSize.Height / eyeRoi.Height;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Thresholding --
            // Get the binary image with the dark pixels
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            var imageThreshold = imageEye.ThresholdDarkResized(thresholdDark, smallSize, eyeRoi);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Morphological erosion and dilation --
            // Optimize the blobs by removing small white or black spots
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            var kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), new Point(1, 1));
            var center = new Point(-1, -1);
            var one = new MCvScalar(1);

            // Image opening. Morpholigical operation to remove small spots that are above treshold.
            // First it erodes the image. That is reduce the size of the white blobs. Next it dilates the
            // remaning blobs. This will eliminate the blobs that are smaller than the kernel size.
            CvInvoke.Erode(imageThreshold, imageThreshold, kernel, center, 1, BorderType.Default, one);
            CvInvoke.Dilate(imageThreshold, imageThreshold, kernel, center, 1, BorderType.Default, one);

            // Image closing. Morpholigical operation to remove small spots that are below treshold.
            // First it dilates the image. That is increase the size of the white blobs. Next it erods the
            // remaning blobs. This will eliminate the spots within the blobs that are smaller than the kernel size.
            CvInvoke.Dilate(imageThreshold, imageThreshold, kernel, center, 1, BorderType.Default, one);
            CvInvoke.Erode(imageThreshold, imageThreshold, kernel, center, 1, BorderType.Default, one);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // --  Blob selection --
            // Find the blob that is most likely to be the pupil.
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            var pupil = new PupilData();

            // Score of the blob with the maximum score.
            var maxScore = 0.0;

            detector.Detect(imageThreshold, blobs);
            foreach (var item in blobs)
            {
                CvBlob blob = item.Value;

                // If area is too small
                if (blob.Area < minPupArea * scaleDownX)
                {
                    continue;
                }

                // If the bounding box is too large
                if (Math.Max(blob.BoundingBox.Width, blob.BoundingBox.Height) > (maxPupRad * 2 * scaleDownX))
                {
                    continue;
                }

                var contour = blob.GetContour();

                // the score is the ratio between area and perimeter
                var score = blob.Area / (double)(blob.BoundingBox.Width * blob.BoundingBox.Height);

                // If touching the edges of the image lower the score
                if (blob.BoundingBox.X < 2 ||
                    blob.BoundingBox.Y < 2 ||
                    imageThreshold.Width - (blob.BoundingBox.X + blob.BoundingBox.Width) < 2 ||
                    imageThreshold.Height - (blob.BoundingBox.Y + blob.BoundingBox.Height) < 2)
                {
                    score /= 2;
                }

                if (score < maxScore) continue;

                // keep track of the pupil with highest score
                if (score > 0.1)
                {
                    maxScore = score;
                    pupil = new PupilData(blob.Centroid, blob.BoundingBox.Size, 90f);
                }
            }

            if (EyeTracker.DEBUG)
            {
                var imgDebug = imageThreshold.Convert<Bgr, byte>();
                imgDebug.Draw(new CircleF(pupil.Center, pupil.Size.Width / 2), new Bgr(Color.Red), 2);
                EyeTrackerDebug.AddImage("pupil", imageEye.WhichEye, imgDebug);
            }

            pupil = new PupilData(
                new PointF((pupil.Center.X / scaleDownX) + eyeRoi.X, (pupil.Center.Y / scaleDownY) + eyeRoi.Y),
                new SizeF(pupil.Size.Width / scaleDownX, pupil.Size.Height / scaleDownY),
                (float)90.0);



            return pupil;
        }

        /// <summary>
        /// Finds the pupil.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyeRoi">Roi containing the pupil.</param>
        /// <param name="thresholdDark">Threshold to find dark pixels.</param>
        /// <returns>Pupil ellipse.</returns>
        public static PupilData FindPupilCentroid(ImageEye imageEye, Rectangle eyeRoi, int thresholdDark)
        {
            //// Threshold the image to find the dark parts (pupil)
            var imageThreshold = imageEye.ThresholdDark(thresholdDark);

            imageThreshold.ROI = eyeRoi;
            var moments = imageThreshold.GetMoments(true);
            imageThreshold.ROI = new Rectangle();

            var centerPupilThreshold = new PointF((float)(moments.GravityCenter.X + eyeRoi.Location.X), (float)(moments.GravityCenter.Y + eyeRoi.Location.Y));
            var radius = (float)Math.Sqrt(imageThreshold.GetAverage().Intensity / 255.0 * imageThreshold.Width * imageThreshold.Height / Math.PI);

            // Save stuff for debugging
            EyeTrackerDebug.AddImage("pupil", imageEye.WhichEye, imageThreshold);

            return new PupilData(centerPupilThreshold, new SizeF(radius * 2, radius * 2), (float)0.0);
        }


        /// <summary>
        /// Finds the pupil.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyeRoi">Roi containing the pupil.</param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>Pupil ellipse.</returns>
        [Obsolete]
        public static PupilData FindPupilHoughCircles(ImageEye imageEye, Rectangle eyeRoi, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            var minPupRad = trackingSettings.MinPupRadPix;
            var maxPupRad = (imageEye.WhichEye == Eye.Left) ? trackingSettings.IrisRadiusPixLeft : trackingSettings.IrisRadiusPixRight;

            var thresholdDark = (imageEye.WhichEye == Eye.Left) ?
                trackingSettings.DarkThresholdLeftEye :
                trackingSettings.DarkThresholdRightEye;

            var imageSize = (int)Math.Round(imageEye.Size.Width / 2.0);
            var smallSize = new Size(imageSize, (int)Math.Round((double)imageEye.Size.Height / imageEye.Size.Width * imageSize));


            var imageThreshold = imageEye.ThresholdDarkResized(thresholdDark, smallSize, eyeRoi);

            //var pupil = PupilTrackerBlob.FindPupilBlob(imageThreshold, eyeRoi, 1 / (float)2.0, maxPupRad);

            //eyeRoi = new Rectangle(
            //    (int)pupil.Center.X - 10,
            //    (int)pupil.Center.Y - 10,
            //    (int)pupil.Size.Width + 20,
            //    (int)pupil.Size.Height + 20);
            //eyeRoi.Intersect(imageEye.Image.ROI);

            //var center1 = pupil.Center;
            //center1.X -= eyeRoi.X;
            //center1.Y -= eyeRoi.Y;
            ////var imgBinarTemp = PupilTrackerCrossCorrelation.PolarSobelTangent(imageEye.Image.Copy(roiPupil).Convert<Gray, float>(), center1).Convert<Gray, byte>();

            Image<Gray, byte> imgBinarTemp = imageEye.Image.Copy(eyeRoi).PyrDown().PyrUp();
            CircleF[] pupilHoughCircles = imgBinarTemp.HoughCircles(new Gray(100), new Gray(30), 1, 50, 150, 400)[0];

            ////CircleF[] pupilHoughCircles = m.HoughCircles(new Gray(40), new Gray(10), 2, 50, (int)minPupRad, (int)maxPupRad)[0];

            PupilData pupil = new PupilData();
            if (pupilHoughCircles.Length > 0)
            {
                PointF center = pupilHoughCircles[0].Center;
                center.X += eyeRoi.X;
                center.Y += eyeRoi.Y;

                pupil = new PupilData(center, new SizeF(pupilHoughCircles[0].Radius * 2, pupilHoughCircles[0].Radius * 2), 0.0f);
            }

            // Save stuff for debugging
            for (int i = 0; i < pupilHoughCircles.Length; i++)
            {
                imgBinarTemp.Draw(pupilHoughCircles[i], new Gray(200), 2);
            }

            EyeTrackerDebug.AddImage("pupil", imageEye.WhichEye, imgBinarTemp);

            return pupil;
        }
    }
}
