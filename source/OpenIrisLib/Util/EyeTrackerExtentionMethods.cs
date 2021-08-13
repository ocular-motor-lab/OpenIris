//-----------------------------------------------------------------------
// <copyright file="EyeTrackerExtentionMethods.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class EyeTrackerExtentionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size Transpose(this Size size) => new Size(size.Height, size.Width);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static Size GetFrameSize(this EyeCollection<ImageEye?> images)
        {
            if (images is null) throw new ArgumentNullException(nameof(images));

            return images[0]?.Size ?? images[1]?.Size ?? throw new InvalidOperationException("No images");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static Size GetFrameSize(this EyeCollection<(Eye, Image<Gray, byte>?)> images)
        {
            if (images is null) throw new ArgumentNullException(nameof(images));

            return images[0].Item2?.Size ?? images[1].Item2?.Size ?? throw new InvalidOperationException("No images");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static long GetFrameNumber(this EyeCollection<ImageEye?> images)
        {
            if (images is null) throw new ArgumentNullException(nameof(images));

            return (long)(images[0]?.TimeStamp.FrameNumber ?? images[1]?.TimeStamp.FrameNumber ?? throw new InvalidOperationException("No images"));
        }
    }
}