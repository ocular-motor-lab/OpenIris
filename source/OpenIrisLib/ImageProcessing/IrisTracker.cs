//-----------------------------------------------------------------------
// <copyright file="IrisTracker.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Class for pupil tracking. Follows Template Method Pattern.
    /// </summary>
    public class IrisTracker
    {
        /// <summary>
        /// Initializes a new instance of the IrisTracker class.
        /// </summary>
        internal IrisTracker()
        {
        }

        ///// <summary>
        ///// Crop the polar image of the eye to get the iris band.
        ///// </summary>
        ///// <typeparam name="TDeph">Type of the pixels of the image.</typeparam>
        ///// <param name="imagePolar">Image of the eye in polar coordinates.</param>
        ///// <param name="irisRadius">Radius of the iris.</param>
        ///// <param name="pupilRadius">Radius of the pupil.</param>
        ///// <returns>Image of the iris band.</returns>
        //internal static Image<Gray, TDeph> CropIrisImage<TDeph>(Image<Gray, TDeph> imagePolar, double irisRadius, double pupilRadius)
        //    where TDeph : new()
        //{
        //    int innerLimit = (int)Math.Min(Math.Ceiling(pupilRadius), imagePolar.Width - 1);
        //    int irisWidth = (int)Math.Max(irisRadius - pupilRadius, 1);

        //    // Extract the iris band
        //    imagePolar.ROI = new Rectangle(
        //        innerLimit,
        //        0,
        //        irisWidth,
        //        imagePolar.Height);

        //    return imagePolar;
        //}

        /// <summary>
        /// Finds the limit of the iris.
        /// </summary>                                            
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="pupil">Pupil information.</param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>The number of pixels of the iris radius.</returns>
        public IrisData FindIris(ImageEye imageEye, PupilData pupil, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            ////TODO: this should return more properties of the iris. Probably an ellipse or a custom object

            // Use the value of the radius in the settings (coming from the UI)
            var irisRadius = (imageEye.WhichEye == Eye.Left) ? trackingSettings.IrisRadiusPixLeft : trackingSettings.IrisRadiusPixRight;
            return new IrisData(pupil.Center, (float)irisRadius);

            ////// Return the same radius as the reference unless it is being resetted
            ////if (!this.EyeTrackerProcess.GetCalibration(whichEye).Calibrating)
            ////{
            ////    return new CircleF(pupil.MCvBox2D.center, this.EyeTrackerProcess.GetCalibration(whichEye).IrisReference.Radius);
            ////}

            ////// Find limit of the iris by finding the column with a maximum average value
            ////// after vertical edge detection
            ////Image<Gray, float> imageEdgePolar = imagePolar.Copy();
            ////float[] colsum = new float[imageEdgePolar.Width];

            ////for (int i = 0; i < imageEdgePolar.Height; i++)
            ////{
            ////    for (int j = (int)pupil.MCvBox2D.size.Width / 2 + 10; j < imageEdgePolar.Width; j++)
            ////    {
            ////        float temppix = 0;
            ////        for (int k = Math.Max(j - 3, 0); k < Math.Min(j + 3, imageEdgePolar.Width); k++)
            ////        {
            ////            if (k >= j)
            ////            {
            ////                temppix += imagePolar.Data[i, k, 0];
            ////            }
            ////            else
            ////            {
            ////                temppix -= imagePolar.Data[i, k, 0];
            ////            }
            ////        }

            ////        imageEdgePolar.Data[i, j, 0] = 128 + (temppix / 6);

            ////        colsum[j] += imageEdgePolar.Data[i, j, 0];
            ////    }
            ////}

            ////double maxval = 0;
            ////int colmax2 = -1;
            ////for (int j = (int)pupil.MCvBox2D.size.Width / 2 + 10; j < imageEdgePolar.Width; j++)
            ////{
            ////    if (maxval < colsum[j])
            ////    {
            ////        colmax2 = j;
            ////        maxval = colsum[j];
            ////    }
            ////}

            ////return new CircleF(pupil.MCvBox2D.center, (float)colmax2);
        }
    }
}