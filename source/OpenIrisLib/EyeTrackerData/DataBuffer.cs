//-----------------------------------------------------------------------
// <copyright file="DataBuffer.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    /// <summary>
    /// Possible data streams to be extracted from the buffer.
    /// </summary>
    public enum DataStream
    {
        /// <summary>
        /// Horizontal eye position.
        /// </summary>
        H,
        /// <summary>
        /// Vertical eye position.
        /// </summary>
        V,
        /// <summary>
        /// Torsional eye position.
        /// </summary>
        T,
        /// <summary>
        /// Pupil size.
        /// </summary>
        P,
        /// <summary>
        /// Eyelid.
        /// </summary>
        E,
        /// <summary>
        /// Head Roll.
        /// </summary>
        HR,
        /// <summary>
        /// Head Pitch.
        /// </summary>
        HP,
        /// <summary>
        /// Head Yaw.
        /// </summary>
        HY,
        /// <summary>
        /// Head velocity roll.
        /// </summary>
        HVR,
        /// <summary>
        /// Head velocity pitch.
        /// </summary>
        HVP,
        /// <summary>
        /// Head velocity yaw.
        /// </summary>
        HVY,
        /// <summary>
        /// CR horizontal.
        /// </summary>
        CRLH,
        /// <summary>
        /// CR vertical.
        /// </summary>
        CRLV,
        /// <summary>
        /// CR horizontal.
        /// </summary>
        CRRH,
        /// <summary>
        /// CR vertical.
        /// </summary>
        CRRV,
    }

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
        /// Empty the buffer.
        /// </summary>
        public void Reset()
        {
            for(long idx = 0; idx < bufferData.Length; idx++)
            {
                bufferData[idx] = null;
            }

            CurrentFrameRate = 0;
            LastFrameUpdated = 0;
            LastIdxUpdated = 0;
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
        public double this[long idx, Eye whichEye, DataStream signal]
        {
            get
            {
                return signal switch
                {
                    DataStream.H => bufferData[idx]?.EyeDataCalibrated?[whichEye].HorizontalPosition ?? double.NaN,

                    DataStream.V => bufferData[idx]?.EyeDataCalibrated?[whichEye].VerticalPosition ?? double.NaN,

                    DataStream.T => bufferData[idx]?.EyeDataCalibrated?[whichEye].TorsionalPosition ?? double.NaN,

                    DataStream.P => bufferData[idx]?.EyeDataCalibrated?[whichEye].PupilArea ?? double.NaN,

                    DataStream.E => bufferData[idx]?.EyeDataCalibrated?[whichEye].PercentOpening ?? double.NaN,

                    DataStream.HR => bufferData[idx]?.HeadDataCalibrated?.Roll ?? double.NaN,

                    DataStream.HP => bufferData[idx]?.HeadDataCalibrated?.Yaw ?? double.NaN,

                    DataStream.HY => bufferData[idx]?.HeadDataCalibrated?.Pitch ?? double.NaN,

                    DataStream.HVR => bufferData[idx]?.HeadDataCalibrated?.RollVelocity ?? double.NaN,

                    DataStream.HVP => bufferData[idx]?.HeadDataCalibrated?.YawVelocity ?? double.NaN,

                    DataStream.HVY => bufferData[idx]?.HeadDataCalibrated?.PitchVelocity ?? double.NaN,

                    DataStream.CRLH => bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections == null | bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?.Length < 1 ? double.NaN:
                    (double) bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?[0].Center.X,

                    DataStream.CRLV => bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections == null | bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?.Length < 1 ? double.NaN :
                    (double)bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?[0].Center.Y,

                    DataStream.CRRH => bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections == null | bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?.Length < 1 ? double.NaN :
                    (double)bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?[0].Center.X,

                    DataStream.CRRV => bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections == null | bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?.Length < 1 ? double.NaN :
                    (double)bufferData[idx]?.EyeDataRaw?[whichEye]?.CornealReflections?[0].Center.Y,

                    _ => double.NaN,
                };
            }
        }
    }
}