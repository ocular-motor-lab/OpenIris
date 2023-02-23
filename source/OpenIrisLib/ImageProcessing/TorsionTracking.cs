//-----------------------------------------------------------------------
// <copyright file="TorsionTrackerCrossCorrelation.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Calculates the torsion angle using the cross correlation of the iris pattern.
    /// </summary>
    public class TorsionTracking
    {
        /// <summary>
        /// Calculate the angle between the current iris and the reference.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyeModel">Physical model of the eye.</param>
        /// <param name="imageTorsionReference">Iris image reference.</param>
        /// <param name="mask">Mask of the eyelids, reflections, etc.</param>
        /// <param name="pupil">Pupil ellipse.</param>
        /// <param name="iris">Iris circle.</param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <param name="imageTorsion">The image used to calculate the torsion angle.</param>
        /// <param name="dataQuality">Index of data quality [0-100]</param>
        /// <returns>The torsion angle.</returns>
        public double CalculateTorsionAngle(ImageEye imageEye, EyePhysicalModel eyeModel, Image<Gray, byte>? imageTorsionReference, Image<Gray, byte>? mask, PupilData pupil, IrisData iris, EyeTrackingPipelineJOMSettings trackingSettings, out Image<Gray, byte>? imageTorsion, out double dataQuality)
        {
            if (trackingSettings is null) throw new ArgumentNullException(nameof(trackingSettings));

            var maxTorsion = trackingSettings.MaxTorsion;
            var torsionAngularResolution = trackingSettings.TorsionAngularResolution;
            var torsionInterpolation = trackingSettings.TorsionInterpolation;

            if (!trackingSettings.CalculateTorsion)
            {
                imageTorsion = new Image<Gray, byte>(4, 4);
                dataQuality = 100.0;
                return 0.0;
            }

            imageTorsion = GetTorsionImage(imageEye, eyeModel, mask, pupil, iris, trackingSettings);

            dataQuality = 0;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // -- Torsion calculation --
            // Calculate the cross correlation between the reference image and the current image
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (imageTorsion is null || imageTorsionReference is null || imageTorsionReference.Size != imageTorsion.Size)
            {
                return double.NaN;
            }

            // Calculate the cross correlation between the reference and the current iris
            imageTorsion.ROI = new Rectangle(0, (int)(maxTorsion * torsionAngularResolution), imageTorsion.Width, (int)(360 * torsionAngularResolution));
            Image<Gray, float> imageCorr = imageTorsionReference.MatchTemplate(imageTorsion, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            //UMat imR = imageTorsionReference.ToUMat();
            //var imageCorr = new Image<Gray, float>(1, imR.Rows - imT.Rows + 1);
            //CvInvoke.MatchTemplate(imR, imT, imageCorr, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            imageTorsion.ROI = Rectangle.Empty;

            // Check the shape of the correlation. We want a correlation with a very sharp peak
            // so the mean should be low
            dataQuality = (255 - imageCorr.Convert<Gray, byte>().GetAverage().Intensity) / 255.0 * 100;

            // Interpolate the correlation to get subpixel resolution
            // and get the maximum of the correlation
            imageCorr = imageCorr.Resize(1, imageCorr.Height * torsionInterpolation, Emgu.CV.CvEnum.Inter.Lanczos4);
            imageCorr.MinMax(out _, out _, out _, out var maxLocs);
            var torsionAngle = (maxLocs[0].Y - (maxTorsion * torsionAngularResolution * torsionInterpolation) - (torsionInterpolation / 2)) / torsionAngularResolution / torsionInterpolation;

            // Save stuff for debugging
            EyeTrackerDebug.AddImage("crosscorrelation", imageEye.WhichEye, imageCorr);
            EyeTrackerDebug.AddImage("torsion", imageEye.WhichEye, imageTorsion);

            return torsionAngle;
        }

        /// <summary>
        /// Gets the torsion image from a given eye image. This function is necessary for the calibration.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyeModel">Physical properties of the eye globe.</param>
        /// <param name="mask">Mask of the image indicating the pixels of the globe.</param>
        /// <param name="pupil">Pupil ellipse.</param>
        /// <param name="iris">Iris circle.</param>
        /// <param name="trackingSettings">Configuration parameters.</param>
        /// <returns>The iris image used to calculate torsion.</returns>
        public static Image<Gray, byte>? GetTorsionImage(ImageEye imageEye, EyePhysicalModel eyeModel, Image<Gray, byte>? mask, PupilData pupil, IrisData iris, EyeTrackingPipelineJOMSettings trackingSettings)
        {
            try
            {
                var torsionSobelFilterSize = trackingSettings.TorsionSobelFilterSize;
                var torsionAngularResolution = trackingSettings.TorsionAngularResolution;
                var maxTorsion = trackingSettings.MaxTorsion;
                var useGeometricCorrection = trackingSettings.UseGeometricTransformation;

                var torsionImageSize = new Size(trackingSettings.TorsionImageIrisWidth, (int)Math.Round(360 * trackingSettings.TorsionAngularResolution));

                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                // -- Iris pattern segmentation and polar transformation --
                // Get the iris pattern in polar coordinates and optimize the signature and the contrast
                ////////////////////////////////////////////////////////////////////////////////////////////////////////

                // Define the ROI containing the iris, leaving some margin to avoid edge effects
                var roiIris = new Rectangle(
                    (int)Math.Round(iris.Center.X - iris.Radius),
                    (int)Math.Round(iris.Center.Y - iris.Radius),
                    (int)Math.Round(iris.Radius * 2),
                    (int)Math.Round(iris.Radius * 2));

                // Limit the ROI by the size of the image
                roiIris.Intersect(new Rectangle(0, 0, imageEye.Size.Width, imageEye.Size.Height));

                // Check if the ROI is not empty
                if (roiIris.Width * roiIris.Height == 0) return null;

                // Mask the image, setting to zero the masked pixels
                Image<Gray, byte> imageIris;

                if (mask is null)
                {
                    imageIris = imageEye.Image.Copy(roiIris);
                }
                else
                { 
                    mask.ROI = roiIris;
                    imageEye.Image.ROI = roiIris;
                    imageIris = imageEye.Image.Mul(mask);
                    imageEye.Image.ROI = Rectangle.Empty;
                    mask.ROI = Rectangle.Empty;
                    EyeTrackerDebug.AddImage("torsionMask", imageEye.WhichEye, mask);
                }
                EyeTrackerDebug.AddImage("torsion0", imageEye.WhichEye, imageIris);

                // Transform the eye to polar coordinates (unwrap iris)
                using var imageEyePolar = GetImageIrisPolar(imageIris, eyeModel, pupil, iris, roiIris, torsionImageSize, maxTorsion, useGeometricCorrection);
                EyeTrackerDebug.AddImage("torsion1", imageEye.WhichEye, imageEyePolar);

                // Extract the polar mask (pixels that were set to zero)
                using var imageIrisPolarMask = imageEyePolar.Convert<Gray, byte>().ThresholdBinary(new Gray(5), new Gray(255));

                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                // -- Iris pattern optimization --
                // Sobel filtering, smoothing and masking
                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                CvInvoke.Blur(imageEyePolar, imageEyePolar, new Size(torsionSobelFilterSize * 2, 1), new Point(-1, -1));
                CvInvoke.Sobel(imageEyePolar, imageEyePolar, Emgu.CV.CvEnum.DepthType.Cv32F, 0, 1, (int)Math.Min(31, (torsionSobelFilterSize * torsionAngularResolution) + 1));
                EyeTrackerDebug.AddImage("torsion2", imageEye.WhichEye, imageEyePolar);

                // Apply the zero mask and add saturations to keep the range symmetric around zero
                // TODO: this does not work very well for different sizes of sobel filter.
                var imageTorsion = imageEyePolar.Max(-100).Min(100).Convert<Gray, byte>();
                EyeTrackerDebug.AddImage("torsion3", imageEye.WhichEye, imageTorsion);

                // Apply the noise mask
                ApplyMaskNoise(imageTorsion, imageIrisPolarMask);
                EyeTrackerDebug.AddImage("torsion4", imageEye.WhichEye, imageTorsion);

                return imageTorsion;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing torsion. " + ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the polar image of the eye around the pupil center.
        /// </summary>
        /// <param name="imageIris">Current image of the eye.</param>
        /// <param name="eyeModel">Physical model of the eye.</param>
        /// <param name="pupil">Current position of the pupil.</param>
        /// <param name="iris">Iris circle.</param>
        /// <param name="roiIris">ROI of the iris image in the original image.</param>
        /// <param name="outputSize">Size of the torsion image.</param>
        /// <param name="maxTorsion">Maximum torsion</param>
        /// <param name="useGeometricCorrection">Use geoemetric correction yes or no.</param>
        /// <returns>Image of the eye in polar coordinates.</returns>
        internal static Image<Gray, float> GetImageIrisPolar(Image<Gray, byte> imageIris, EyePhysicalModel eyeModel, PupilData pupil, IrisData iris, Rectangle roiIris, Size outputSize, double maxTorsion, bool useGeometricCorrection)
        {
            try
            {
                var pupilRadius = Math.Max(pupil.Size.Width / 2, pupil.Size.Height / 2) * 0.9;

                // Get the portion of the image containing the iris.
                var imageEyePolarTemp = new Image<Gray, float>(outputSize);

                // Do the polar transformation
                var polarCenter = iris.Center - new Size(roiIris.Location);

                var src = imageIris;

                // Make the map 4 times smaller and then resize with interpolation. This is faster.
                var mapSize = new Size((int)(outputSize.Width / 4.0), outputSize.Height);
                var mapx = new Image<Gray, float>(mapSize);
                var mapy = new Image<Gray, float>(mapSize);


                // quaternion defining the rotation of the eyeball (ignoring torsion) just the center of the pupil
                var angle = Math.Atan2((iris.Center.Y - eyeModel.Center.Y), (iris.Center.X - eyeModel.Center.X));
                var yC = iris.Center.Y - eyeModel.Center.Y;
                var xC = iris.Center.X - eyeModel.Center.X;
                var ecc = Math.Asin(Math.Sqrt(yC * yC + xC * xC) / eyeModel.Radius);
                var q = new Quaternions(Math.Cos(ecc / 2), -Math.Sin(angle) * Math.Sin(ecc / 2), Math.Cos(angle) * Math.Sin(ecc / 2), 0);


                // Intermideate variables neccesary for the quaternion rotation
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


                var w = mapSize.Width;
                var h = mapSize.Height;
                float er = eyeModel.Radius;

                float ixMinusRoirisX = iris.Center.X - roiIris.X;
                float iyMinusRoirisY = iris.Center.Y - roiIris.Y;

                if (useGeometricCorrection)
                {
                    ixMinusRoirisX = eyeModel.Center.X - roiIris.X;
                    iyMinusRoirisY = eyeModel.Center.Y - roiIris.Y;
                }

                // The point of this loop is to find for each pixel in the torsion image
                // with rows being different angles and columns being distance from pupil to limbus
                // what is the corresponding x and y coordinate of the pixel in the original image.

                double cp, sp, x, y, rr, z,r;
                double hfactor = 2 * Math.PI / h;
                double wfactor = (iris.Radius - pupilRadius)/w;

                unsafe
                {
                    // Fix the pointers to the begining of the data
                    fixed (float* px = mapx.Data)
                    fixed (float* py = mapy.Data)
                    {
                        for (int phi = 0; phi < h; phi++)
                        {
                            cp = Math.Cos(phi * hfactor);
                            sp = Math.Sin(phi * hfactor);

                            float* mx = px + phi * w;
                            float* my = py + phi * w;

                            for (int rho = 0; rho < w; rho++)
                            {
                                r = rho * wfactor + pupilRadius;

                                // x and y are the coordinates relative to the center of the pupil that correspond with rho and phi
                                x = r * cp;
                                y = r * sp;

                                if (useGeometricCorrection)
                                {
                                    // now we need to rotate those to the actual position

                                    rr = Math.Sqrt(x * x + y * y) / er;

                                    if (rr <= 1)
                                    {
                                        z = Math.Sqrt(1 - rr * rr) * er;

                                        // var p2 = q.RotatePoint(p);
                                        x = 2.0 * ((t8 + t10) * x + (t6 - t4) * y + (t3 + t7) * z) + x;
                                        y = 2.0 * ((t4 + t6) * x + (t5 + t10) * y + (t9 - t2) * z) + y;
                                        //double zz = 2.0 * ((t7 - t3) * x + (t2 + t9) * y + (t5 + t8) * z) + z;
                                    }
                                }

                                mx[rho] = (float)x + ixMinusRoirisX;
                                my[rho] = (float)y + iyMinusRoirisY;
                            }
                        }
                    }
                }

                EyeTrackerDebug.AddImage("MAPX", Eye.Left, mapx.Convert<Gray, byte>());
                EyeTrackerDebug.AddImage("MAPY", Eye.Left, mapx.Convert<Gray, byte>());

                mapx = mapx.Resize(imageEyePolarTemp.Width, imageEyePolarTemp.Height, Emgu.CV.CvEnum.Inter.Linear);
                mapy = mapy.Resize(imageEyePolarTemp.Width, imageEyePolarTemp.Height, Emgu.CV.CvEnum.Inter.Linear);
                CvInvoke.Remap(imageIris.Convert<Gray, float>(), imageEyePolarTemp, mapx, mapy, Emgu.CV.CvEnum.Inter.Linear, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));

                // Shift the iris pattern by 270 degrees so the better part is not cut at the bottom and the top.
                // This will reduce edge effects.
                // At the same time extended maxTorsion degrees in both directions to do the cross correlations
                int totalHeight = outputSize.Height;
                int totalWidth = outputSize.Width;
                int totalHeightWithExtra = (int)Math.Round((360 + maxTorsion * 2) / 360.0 * outputSize.Height);
                int topSectionHeight = (int)Math.Round((90 + maxTorsion) / 360.0 * outputSize.Height);
                int bottomSectionHeight = (int)Math.Round((270 + maxTorsion) / 360.0 * outputSize.Height);

                var imageEyePolar = new Image<Gray, float>(totalWidth, totalHeightWithExtra);

                // Copy the top section
                imageEyePolar.ROI = new Rectangle(0, 0, totalWidth, topSectionHeight);
                imageEyePolarTemp.ROI = new Rectangle(0, totalHeight - topSectionHeight, totalWidth, topSectionHeight);
                imageEyePolarTemp.CopyTo(imageEyePolar);

                // Copy the bottom section
                imageEyePolar.ROI = new Rectangle(0, topSectionHeight, totalWidth, bottomSectionHeight);
                imageEyePolarTemp.ROI = new Rectangle(0, 0, totalWidth, bottomSectionHeight);
                imageEyePolarTemp.CopyTo(imageEyePolar);

                imageEyePolar.ROI = Rectangle.Empty;
                return imageEyePolar;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR in cross correlation : " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Applies the noise mask to the image of the iris.
        /// </summary>
        /// <param name="image">Current image.</param>
        /// <param name="mask">Current mask.</param>
        /// <returns>Masked image.</returns>
        internal static void ApplyMaskNoise(Image<Gray, byte> image, Image<Gray, byte> mask)
        {
            // Increase the mask by eroding the mask image. This will reduce the edge effects of the filter.
            var se21 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Ellipse, new Size(15, 31), new Point(8, 16));
            CvInvoke.Erode(mask, mask, se21, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));

            // Random generator for noise mask
            var r = new Random();
            var wh = mask.Width * mask.Height;

            unsafe
            {
                fixed (byte* px = mask.Data)
                fixed (byte* pi = image.Data)
                {
                    for (int i = 0; i < wh; i++)
                    {
                        if (px[i] == 0)
                        {
                            // Make the value of the masked pixels equal to the value of some random pixel in the image
                            pi[i] = pi[r.Next(wh)];
                        }
                    }
                }
            }
        }
    }
}
