//-----------------------------------------------------------------------
// <copyright file="EyeData.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
#nullable enable

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    using System;
    using System.Drawing;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the data about head movement.
    /// </summary>
    [Serializable]
    public class HeadData
    {
        /// <summary>
        /// Timestamp of the head data.
        /// </summary>
        public ImageEyeTimestamp TimeStamp { get; set; } = new ImageEyeTimestamp();

        public double AccelerometerX { get; set; }
        public double AccelerometerY { get; set; }
        public double AccelerometerZ { get; set; }
        public double GyroX { get; set; }
        public double GyroY { get; set; }
        public double GyroZ { get; set; }
        public double MagnetometerX { get; set; }
        public double MagnetometerY { get; set; }
        public double MagnetometerZ { get; set; }

        public bool IsEmpty
        {
            get { return this.TimeStamp.FrameNumber == 0 && this.TimeStamp.FrameNumberRaw == 0; }
        }
    }
}
