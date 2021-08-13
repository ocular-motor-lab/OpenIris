//-----------------------------------------------------------------------
// <copyright file="HeadSensorMicromedicalMeasurmentComputing.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.HeadTracking
{
    using System;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Analog accelerometer connected to a measurement computing DAQ used together with the micromedical googles.
    /// </summary>
    [Obsolete]
    internal class HeadSensorMicromedicalMeasurmentComputing
    {
        /// <summary>
        /// Pointgrey camera used to get timestamps to sync the head data and the eye data.
        /// </summary>
        private CameraEyeFlyCapture cameraSync;

        /// <summary>
        /// Data aquisition board.
        /// </summary>
        private DataAcquisitionMeasurementComputing daq;

        /// <summary>
        /// Initializes a new instance of HeadSensorMicromedicalMeasurmentComputing the class.
        /// </summary>
        /// <param name="cameraSync">The associated point grey camera.</param>
        internal HeadSensorMicromedicalMeasurmentComputing(CameraEyeFlyCapture cameraSync)
        {
            this.cameraSync = cameraSync;
            this.daq = DataAcquisitionMeasurementComputing.Instance;
        }

        /// <summary>
        /// Starts tracking.
        /// </summary>
        public void StartTracking()
        {

        }

        /// <summary>
        /// Stops tracking.
        /// </summary>
        public void StopTracking()
        {

        }
        
        /// <summary>
        /// Interface for head tracking sensors
        /// </summary>
        public HeadData GetHeadData(EyeCollection<ImageEye> images)
        {
            var headData = new HeadData();
            headData.AccelerometerX = this.daq.ReadAnalog(1);
            headData.AccelerometerY = this.daq.ReadAnalog(0);
            headData.AccelerometerZ = this.daq.ReadAnalog(3);
            if (this.cameraSync != null)
            {
                var timeStamp = new ImageEyeTimestamp();
                timeStamp.Seconds = this.cameraSync.GetCurrentSeconds();
                headData.TimeStamp = timeStamp;
            }

            return headData;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if ( disposing)
            {

            }
        }
    }
}
