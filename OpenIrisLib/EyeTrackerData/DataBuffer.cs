//-----------------------------------------------------------------------
// <copyright file="DataBuffer.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    /// <summary>
    /// Class to contain a buffer of data use, for example, for plotting of traces.
    /// </summary>
    public class EyeTrackerDataBuffer
    {
#nullable enable
        private readonly EyeTrackerData?[] bufferData;

        /// <summary>
        /// Initializes an instance of the class EyeTrackerDataBuffer.
        /// </summary>
        public EyeTrackerDataBuffer()
        {
            bufferData = new EyeTrackerData[10000];
        }

        /// <summary>
        /// Gets the number of the last frame updated in the buffer.
        /// </summary>
        public long LastFrameUpdated { get; private set; }

        /// <summary>
        /// Gets the index of the last element updated in the buffer.
        /// </summary>
        public long LastIdxUpdated { get; private set; }
        
        /// <summary>
        /// Gets the current frame rate. Necessary to set the timescale of the plots.
        /// </summary>
        public double CurrentFrameRate { get; private set; }

        /// <summary>
        /// Length of the buffer.
        /// </summary>
        public long Length { get { return bufferData.Length; } }

        /// <summary>
        /// Adds data from a new frame to the buffer.
        /// </summary>
        /// <param name="data">The new data.</param>
        public void Add(EyeTrackerData? data)
        {
            if (data is null) return;

            var currentFrameNumber = data.FrameNumber;
            var currentIdx = currentFrameNumber % bufferData.Length;

            // No point on updating more than a whole buffer. This may happen on scrolling or some weird errors
            LastFrameUpdated = System.Math.Max(LastFrameUpdated, currentFrameNumber - bufferData.Length);

            // Clear the data for possible dropped frames from the last frame saved to the current one.
            for (long frameNumber = LastFrameUpdated + 1; frameNumber < currentFrameNumber; frameNumber++)
            {
                bufferData[frameNumber % bufferData.Length] = null;
            }

            CurrentFrameRate = data.FrameRate;
            bufferData[currentIdx] = data;
            LastFrameUpdated = currentFrameNumber;
            LastIdxUpdated = currentIdx;
        }

        /// <summary>
        /// Gets the data for a given index of the buffer.
        /// </summary>
        /// <param name="idx">Index.</param>
        /// <returns>The data.</returns>
        public EyeTrackerData? this[long idx] { get { return bufferData[idx]; } }
         
        /// <summary>
        /// Gets the value of a given signal for an idx and for a given eye.
        /// </summary>
        /// <param name="idx">The index of the sample within the buffer.</param>
        /// <param name="whichEye">The eye, left or right.</param>
        /// <param name="signal">Which signal.</param>
        /// <returns></returns>
        public double this[long idx, Eye whichEye, string signal]
        {
            get
            {
                var data = bufferData[idx];

                if (data is null) return double.NaN;

                var eyeData = data.EyeDataCalibrated?[whichEye] ?? new CalibratedEyeData();
                var headData = data.HeadDataCalibrated;

                return signal switch
                {
                    "H" => eyeData.HorizontalPosition,

                    "V" => eyeData.VerticalPosition,

                    "T" => eyeData.TorsionalPosition,

                    "P" => eyeData.PupilArea,

                    "E" => eyeData.PercentOpening,

                    "HR" => headData.Roll,

                    "HP" => headData.Yaw,

                    "HY" => headData.Pitch,

                    "HVR" => headData.RollVelocity,

                    "HVP" => headData.YawVelocity,

                    "HVY" => headData.PitchVelocity,

                    _ => double.NaN,
                };
            }
        }
    }
}