//-----------------------------------------------------------------------
// <copyright file="EyeTrackerMask.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Class that creates the mask of the non-important parts of the eye: eyelids, reflections, etc.
    /// </summary>
    public class EyeTrackerMask
    {
        /// <summary>
        /// Gets the mask of the non-important parts of the eye: eyelids, reflections, etc.
        /// </summary>
        /// <param name="imageEye">Image of the eye.</param>
        /// <param name="eyelid">Eyelid points.</param>
        /// <param name="eyeGlobe">Eye globe position and size.</param>
        /// <param name="setttings">Configuration parameters.</param>
        /// <returns>The mask, ones in the good pixels zeros in masked ones.</returns>
        public Image<Gray, byte> GetMask(ImageEye imageEye, EyelidData eyelid, EyePhysicalModel eyeGlobe, EyeTrackingJOMalgorithmSettings setttings)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));
            if (eyelid is null) throw new ArgumentNullException(nameof(eyelid));
            if (setttings is null) throw new ArgumentNullException(nameof(setttings));

            // Find the brightest pixels (reflections)
            var thresholdBright = (imageEye.WhichEye == Eye.Left) ? setttings.BrightThresholdLeftEye : setttings.BrightThresholdRightEye;
            var imageMask = imageEye.ThresholdBright(thresholdBright);

            if (setttings.EyelidTrackingMethod != EyeLidTracking.EyeLidTrackingMethod.None)
            {
                using var eyelidMask = GetEyelidMask(imageEye.Size, eyelid, eyeGlobe);
                imageMask = imageMask.Mul(eyelidMask);
            }

            if (EyeTracker.DEBUG)
            {
                EyeTrackerDebug.AddImage("mask", imageEye.WhichEye, imageMask * 255);
            }

            return imageMask;
        }


        /// <summary>
        /// Gets the eyelid mask.
        /// </summary>
        /// <param name="size">Size of the image.</param>
        /// <param name="eyeLids">Eyelid position.</param>
        /// <param name="eyeModel">Eye globe position and size.</param>
        /// <returns>The eyelid mask.</returns>
        internal Image<Gray, byte> GetEyelidMask(Size size, EyelidData eyeLids, EyePhysicalModel eyeModel)
        {
            //// It is necessary to clone the arrays because the points will be modified later to add the offset
            //// and we want to keep the raw eyelid data.

            var backgroundColor = new Gray(1);
            var maskColor = new Gray(0);

            var upperEyelid = (Point[])Array.ConvertAll(eyeLids.Upper, point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y+10)));
            var lowerEyelid = (Point[])Array.ConvertAll(eyeLids.Lower, point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y-10)));

            var c1 = new Point(
                    (int)(eyeModel.Center.X - eyeModel.Radius),
                    (int)((eyeLids.Upper[0].Y + eyeLids.Upper[1].Y + 1.5 * eyeLids.Lower[0].Y + 1.5 * eyeLids.Lower[1].Y) / 5.0));

            var c2 = new Point(
                (int)(eyeModel.Center.X + eyeModel.Radius),
                (int)((eyeLids.Upper[3].Y + 1.5 * eyeLids.Lower[3].Y + eyeLids.Upper[2].Y + 1.5 * eyeLids.Lower[2].Y) / 5.0));


            var top = new Point[8];
            top[0] = c1;
            top[5] = c2;
            top[6] = c1;
            top[7] = c2;
            Array.Copy(upperEyelid, 0, top, 1, 4);


            var bottom = new Point[8];
            bottom[0] = c1;
            bottom[5] = c2;
            bottom[6] = c1;
            bottom[7] = c2;
            Array.Copy(lowerEyelid, 0, bottom, 1, 4);


            var top2 = top.FitParabola((EyePhysicalModel)eyeModel);
            var bottom2 = bottom.FitParabola((EyePhysicalModel)eyeModel);

            var points = new List<Point>();
            points.AddRange(top2);
            Array.Reverse(bottom2);
            points.AddRange(bottom2);

            var imageMaskTemp = new Image<Gray, byte>(size.Width, size.Height, maskColor);
            imageMaskTemp.FillConvexPoly(points.ToArray(), backgroundColor);
            return imageMaskTemp;
        }
    }
}
