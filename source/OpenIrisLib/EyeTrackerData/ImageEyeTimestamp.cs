//-----------------------------------------------------------------------
// <copyright file="ImageEyeTimestamp.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;

    /// <summary>
    /// Timestamp of the frame.
    /// </summary>
    [Serializable]
    public class ImageEyeTimestamp : IEquatable<ImageEyeTimestamp>
    {
        /// <summary>
        /// Initializes a new instance of timestamp.
        /// </summary>
        public ImageEyeTimestamp()
        {
            DateTimeGrabbed = DateTime.Now;
            TimeGrabbed = EyeTrackerDebug.TimeElapsed.TotalSeconds;
        }

        /// <summary>   
        /// Gets or sets the Seconds from some reference. I.e. connecting to the camera.
        /// </summary>
        public double Seconds { get; set; }

        /// <summary>   
        /// Gets or sets the frame number. Starting at zero at the begining of the recording.
        /// </summary>
        public ulong FrameNumber { get; set; }

        /// <summary>   
        /// Gets or sets the frame number. The starting number will depend on the status of 
        /// the camera at the begining of the recording.
        /// </summary>
        public ulong FrameNumberRaw { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp from the time the image is grabbed on the computer.
        /// </summary>
        public DateTime DateTimeGrabbed { get; set; }

        /// <summary>
        /// Gets or sets the ammount of time in seconds since the eye tracker started.
        /// Note that this is not a reliable timestamp. It is used for debuging purposes.
        /// </summary>
        public double TimeGrabbed { get; set; }

        /// <summary>
        /// Compares two timestamps.
        /// </summary>
        /// <param name="timestamp1">First timestamps.</param>
        /// <param name="timestamp2">Second timestamps.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(ImageEyeTimestamp timestamp1, ImageEyeTimestamp timestamp2)
        {
            return timestamp1.Equals(timestamp2);
        }

        /// <summary>
        /// Compares two timestamps.
        /// </summary>
        /// <param name="timestamp1">First timestamps.</param>
        /// <param name="timestamp2">Second timestamps.</param>
        /// <returns>True if different.</returns>
        public static bool operator !=(ImageEyeTimestamp timestamp1, ImageEyeTimestamp timestamp2)
        {
            return !timestamp1.Equals(timestamp2);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (int)this.FrameNumber;
        }

        /// <summary>
        /// Compares to objects.
        /// </summary>
        /// <param name="obj">Second object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (!(obj is ImageEyeTimestamp))
            {
                return false;
            }

            return this.Equals((ImageEyeTimestamp)obj);
        }

        /// <summary>
        /// Compares two timestamps.
        /// </summary>
        /// <param name="timestamp">Second timestamp.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(ImageEyeTimestamp timestamp)
        {
            return DateTimeGrabbed.Equals(timestamp.DateTimeGrabbed);
        }

        /// <summary>
        /// Copys the timestamp
        /// </summary>
        /// <returns></returns>
        public ImageEyeTimestamp Copy()
        {
            return new ImageEyeTimestamp
            {
                FrameNumber = FrameNumber,
                FrameNumberRaw = FrameNumberRaw,
                Seconds = Seconds,
                DateTimeGrabbed = DateTimeGrabbed,
                TimeGrabbed = TimeGrabbed,
            };
        }
    }
}
