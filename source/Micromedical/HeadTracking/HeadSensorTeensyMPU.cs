//-----------------------------------------------------------------------
// <copyright file="HeadSensorMPUFTDI.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Class to control a motion sensor used together with the micromedical system. The sensor is connected to the camera 
    /// using a teensy microcontroller (arduino) and they communicate using the serial capabilities of the camera.
    /// </summary>
    public sealed class HeadSensorTeensyMPU : IHeadDataSource
    {
        /// <summary>
        /// Eye tracking system settings.
        /// </summary>
        private EyeTrackingSystemSettingsMicromedical settings;

        /// <summary>
        /// Constructor for Head sensors. 
        /// </summary>
        /// <param name="settings"></param>
        public HeadSensorTeensyMPU(EyeTrackingSystemSettingsMicromedical settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// This is the data sent by the TEENSY to the camera, minus the 4-byte header "flag" value of 0xffffffff.
        /// Sensors are: Accel X/Y/Z, +/-32768 == +/-2G
        ///              Temperature
        ///              Gyro X/Y/Z, +/-32768 == +/- 500dps
        ///              Magnetometer
        /// </summary>
        class HeadDataPacket
        {
            EyeTrackingSystemSettingsMicromedical settings;

            /// <summary>
            /// Number of bytes sent by Teensy for each packet.
            /// 4-byte header "flag" value of 0xffffffff (not saved to SerDat),
            /// 4-byte Frame counter, then 6 2-byte integers.
            /// </summary>
            public static int PktSize = 4 + 4 + 2 * 10;  // The 10 here MUST match Sensor[] length.

            /// <summary>
            /// Header of the packet
            /// </summary>
            public static uint Header = 0xffffffff;

            /// <summary>
            /// Hardware frame counter
            /// </summary>
            public UInt32 FrameCount;

            /// <summary>
            /// TRUE when sync pulse present, every 16 frames.
            /// </summary>
            public bool SYNC;

            /// <summary>
            /// 
            /// </summary>
            // Note there is one extra Int16 here, so the length is a multiple of 2, so swap() works correctly.
            public readonly Int16[] SensorData = new Int16[10];

            public HeadDataPacket(byte[] pktbytes, EyeTrackingSystemSettingsMicromedical settings)
            {

                this.settings = settings;

                // Now just read the packet data. The packet's FrameCount
                // increments by 2, and contains the SYNC bit as the lowest bit.
                // Here, we separate them out, and divide the count by 2.
                FrameCount = CameraEyeFlyCapture.SwapUInt32Bytes(pktbytes, 4);
                SYNC = (FrameCount & 1) == 1;
                FrameCount >>= 1;

                // If Sensor.Length is not a multiple of 2, this will crash,
                // since it takes 2 values at a time.
                for (int i = 0; i < SensorData.Length; i += 2)
                {
                    // Remember, the 4 bytes are backwards in each 4-byte group,
                    // so for instance, Sensor[1] comes before Sensor[0].
                    SensorData[i] = CameraEyeFlyCapture.SwapInt16Bytes(pktbytes, 10 + 2 * i);
                    SensorData[i + 1] = CameraEyeFlyCapture.SwapInt16Bytes(pktbytes, 8 + 2 * i);
                }
            }

            /// <summary>
            /// Converts the packet to HeadData
            /// </summary>
            /// <param name="initialSensorFrameNumber"></param>
            /// <param name="initialCameraFrameNumber"></param>
            /// <returns></returns>
            public HeadData ConvertPacketToHeadData(ulong initialSensorFrameNumber, ulong initialCameraFrameNumber)
            {
                var AccelerometerX = (double)SensorData[0] / short.MaxValue * 8.0; // 2019-10-30 Changed to 8.0 from 2.0 after adding the magnetometer to match the arduino code
                var AccelerometerY = (double)SensorData[1] / short.MaxValue * 8.0; // 2019-10-30 Changed to 8.0 from 2.0 after adding the magnetometer to match the arduino code
                var AccelerometerZ = (double)SensorData[2] / short.MaxValue * 8.0; // 2019-10-30 Changed to 8.0 from 2.0 after adding the magnetometer to match the arduino code

                if (settings.UseHeadSensorRotation)
                {
                    var m = settings.HeadSensorRotation;
                    var AccelerometerX_temp = AccelerometerX * m[0][0] + AccelerometerY * m[0][1] + AccelerometerZ * m[0][2];
                    var AccelerometerY_temp = AccelerometerX * m[1][0] + AccelerometerY * m[1][1] + AccelerometerZ * m[1][2];
                    var AccelerometerZ_temp = AccelerometerX * m[2][0] + AccelerometerY * m[2][1] + AccelerometerZ * m[2][2];
                    AccelerometerX = AccelerometerX_temp;
                    AccelerometerY = AccelerometerY_temp;
                    AccelerometerZ = AccelerometerZ_temp;
                }

                return new HeadData
                {
                    TimeStamp = new ImageEyeTimestamp
                    {
                        FrameNumberRaw = FrameCount,
                        FrameNumber = FrameCount - initialSensorFrameNumber + initialCameraFrameNumber
                    },

                    GyroX = (double)SensorData[3] / short.MaxValue * 500.0,
                    GyroY = (double)SensorData[4] / short.MaxValue * 500.0,
                    GyroZ = (double)SensorData[5] / short.MaxValue * 500.0,

                    // The way the sensor is currently placed int he goggles.
                    // The x axes points down
                    // The y axes points forward
                    // The z axes points out to the right
                    // All from the point of view of the subject wearing the goggles
                    // The values correspond with the fraction of 1G that is projected along each axis
                    // When the subject is upright X=-1, y=0, Z=0

                    AccelerometerX = AccelerometerX,
                    AccelerometerY = AccelerometerY,
                    AccelerometerZ = AccelerometerZ,

                    MagnetometerX = (double)SensorData[6] / 1000.0,
                    MagnetometerY = (double)SensorData[7] / 1000.0,
                    MagnetometerZ = (double)SensorData[8] / 1000.0,
                };
            }
        }

        private CameraEyeFlyCapture? camera;

        private ulong initialCameraFrameNumber;
        private ulong initialSensorFrameNumber;

        public void StartHeadSensorAndSyncWithCamera(CameraEyeFlyCapture camera)
        {
            if (camera is null) throw new ArgumentNullException(nameof(camera));

            this.camera = camera;

            // Set GPIO 0 and 1 to outputs
            // TODO: check if this is necessary. Doubt it.
            camera.SetGPIO(0, CameraEyeFlyCapture.GPIOMode.output);
            camera.SetGPIO(1, CameraEyeFlyCapture.GPIOMode.output);

            camera.InitSerialCom();
            camera.InitStrobe();

            //
            // Sync camera and head sensor
            //
            // Run two threads, one getting packets from the head sensor and one
            // getting images from the camera. Waiting until both are the ones
            // that corresponds with the strobe pattern. Then save the frame numbers for those two for future syncronization.
            // The two frames must also have been captured within a small time period to make sure we are not looking past
            // one strobe cycle.
            //
            HeadDataPacket syncPacket = null;
            uint strobePattern = 1;
            ImageEye syncImage = null;
            bool cancel = false;
            double timePacket = double.MaxValue;
            double timeImage = 0;

            Trace.WriteLine("Syncing camera and head sensor.");
            var task1 = Task.Run(() =>
            {
                while (!cancel && Math.Abs(timeImage - timePacket) > 2 / camera.FrameRate)
                {
                    var result = this.camera.GetPacketWithHeader(out byte[] pktbytes, HeadDataPacket.Header, HeadDataPacket.PktSize);

                // If there was no packet sleep a bit and come back later because
                // the call to GetPacket is nto blocking
                if (!result)
                    {
                        Thread.Sleep(2);
                        continue;
                    }

                    var packet = new HeadDataPacket(pktbytes, settings);
                    if (packet.SYNC)
                    {
                        syncPacket = packet;
                        timePacket = EyeTrackerDebug.TimeElapsed.TotalSeconds;
                    }
                }
            });
            var task2 = Task.Run(() =>
            {
                while (!cancel && Math.Abs(timeImage - timePacket) > 2 / camera.FrameRate)
                {
                    var image = camera.GrabImageEye();
                    strobePattern = CameraEyeFlyCapture.GetEmbeddedStrobePattern(image);

                    if (strobePattern == 0)
                    {
                        syncImage = image;
                        timeImage = EyeTrackerDebug.TimeElapsed.TotalSeconds;
                    }
                }
            });

            var finishedBeforeTimeout = Task.WaitAll(new Task[] { task1, task2 }, 2000);

            Trace.WriteLine($"Finished syncing camera and head sensor. Diff time= {Math.Abs(timeImage - timePacket)}");

            if (finishedBeforeTimeout && !task1.IsFaulted && !task2.IsFaulted)
            {
                initialCameraFrameNumber = syncImage.TimeStamp.FrameNumberRaw;
                initialSensorFrameNumber = syncPacket.FrameCount;
            }
            else
            {
                cancel = true;
                if (task1.IsFaulted) throw task1.Exception;
                if (task2.IsFaulted) throw task2.Exception;

                throw new OpenIrisException("Head sensor does not seem to be present. Change the settings.");
            }
        }

        /// <summary>
        /// Grabs head data from the sensor.
        /// </summary>
        /// <returns></returns>
        public HeadData? GrabHeadData()
        {
            if (camera is null) return null;

            var result = camera.GetPacketWithHeader(out byte[] pktbytes, HeadDataPacket.Header, HeadDataPacket.PktSize);

            if (!result) return null;

            var packet = new HeadDataPacket(pktbytes, settings);
            return packet.ConvertPacketToHeadData(initialSensorFrameNumber, initialCameraFrameNumber);
        }

        public void Stop()
        {
            camera = null;
        }
    }

}
