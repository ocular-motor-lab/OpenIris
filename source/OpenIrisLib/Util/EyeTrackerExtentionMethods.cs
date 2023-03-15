﻿//-----------------------------------------------------------------------
// <copyright file="EyeTrackerExtentionMethods.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;
    using System.Numerics;

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
            return images[0]?.Size ?? images[1]?.Size ?? throw new InvalidOperationException("No images");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static Size GetFrameSize(this EyeCollection<(Eye, Image<Gray, byte>?)> images)
        {
            return images[0].Item2?.Size ?? images[1].Item2?.Size ?? throw new InvalidOperationException("No images");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static long GetFrameNumber(this EyeCollection<ImageEye?> images)
        {
            return (long)(images[0]?.TimeStamp.FrameNumber ?? images[1]?.TimeStamp.FrameNumber ?? throw new InvalidOperationException("No images"));
        }


        public static Vector2 Round(Vector2 V) => new Vector2((float)Math.Round(V.X), (float)Math.Round(V.Y));
        public static Vector2 Max(Vector2 V1, Vector2 V2) => Vector2.Max(V1, V2);
        public static Vector2 Min(Vector2 V1, Vector2 V2) => Vector2.Min(V1, V2);
    }
}