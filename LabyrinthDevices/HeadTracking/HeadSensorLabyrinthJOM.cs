//-----------------------------------------------------------------------
// <copyright file="HeadSensorLabyrinth.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace VORLab.VOG.HeadTracking
{
    using System;
    using OpenIris;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Movement sensor connected to the camera in the laberynth system. It comunicates with the camera
    /// trhough the GPIO serial connection.
    /// The sensor captures data every time a frame is grabbed in the camera via a hardware connection. Then it sends
    /// the data to the camera through a serial connection and the data is placed in a buffer.
    /// </summary>
    public class HeadSensorLabyrinthJOM
    {
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unused private members
#pragma warning disable IDE0169 // Remove unused private members
        /// <summary>
        /// Pointgrey camera used to comunicate with the sensor.
        /// </summary>
        private CameraEyeFlyCapture cameraSync;

        /// <summary>
        /// Serial Port Registers
        /// </summary>
        private const uint SerSettingReg = 0x70000;
        private const uint SerSettingVal = 0x90800FF;
        private const uint SerEnableReg = 0x70004;
        private const uint SerEnableVal = 0xC0000000;
        private const uint SerTrigReg = 0x70100;
        private const uint SerTrigVal = 0x67;
        private const uint SerTransReg = 0x7000C;
        private const uint SerRecReg = 0x70008;

        private const uint SerEnableValTrans = 0x40000000;
        private const uint SerEnableValRec = 0x80000000;

        // Added by MAR for GPIO reading
        private const uint GPIO_Ctrl = 0x1100;
        private uint GPIO_Val;
        private uint GPIO_Data;

        //MAR and NSD added the following gyro related variable declarations
        private uint ReadSerSettings;
        private uint ReadSerEnable;
        private uint ReadSerTrans;
        private uint ReadSerReadReady;
        private uint ReadSerRec;
        private uint ReadSerNumBytes;
        private uint[] SerReadBuffer = new uint[255];
        private uint[] SensorBuf = new uint[3];

        /// <summary>
        /// Initializes a new instance of HeadSensorLabyrinth the class.
        /// </summary>
        /// <param name="cameraSync">The associated point grey camera.</param>
        public HeadSensorLabyrinthJOM(CameraEyeFlyCapture cameraSync)
        {
            this.cameraSync = cameraSync;
        }

        /// <summary>
        /// Starts tracking.
        /// </summary>
        public void StartTracking()
        {
            // Init camera serial comunication
            this.cameraSync.WriteRegister(SerSettingReg, SerSettingVal);
            ReadSerSettings = this.cameraSync.ReadRegister(SerSettingReg);
            this.cameraSync.WriteRegister(SerEnableReg, SerEnableValRec);

            // Set the GPIO 2 to strobe every frame
            this.cameraSync.StartStrobe(2);
        }

        /// <summary>
        /// Stops tracking.
        /// </summary>
        public void StopTracking()
        {

        }

        private bool cleared = false;
        public void Clear_Buffer()
        {
            //ReadSerEnable = this.cameraSync.ReadRegister(SerEnableReg);
            //if (ReadSerEnable == 0x80200000)
            //{
            //    while ((ReadSerEnable = this.cameraSync.ReadRegister(SerEnableReg)) == 0x80200000 && (ReadSerRec = this.cameraSync.ReadRegister(SerRecReg)) > 0)
            //    {
            //        this.cameraSync.WriteRegister(SerRecReg, ReadSerRec >> 4);
            //        ReadSerNumBytes = this.cameraSync.ReadRegister(SerRecReg);
            //        uint temp = ReadSerRec >> 24;
            //        uint result;
            //        for (uint i = 0; i < temp; i += 0x4)
            //        {
            //            result = this.cameraSync.ReadRegister(SerTrigReg + i);
            //        }
            //    }
            //    this.cleared = true;
            //}
            lock (cameraSync)
            {
                while (((ReadSerEnable = this.cameraSync.ReadRegister(SerEnableReg)) == 0x80200000 || ReadSerEnable == 0x80000000) && (ReadSerRec = this.cameraSync.ReadRegister(SerRecReg)) > 0)
                {
                    this.cameraSync.WriteRegister(SerRecReg, 0x00040000);
                    ReadSerNumBytes = this.cameraSync.ReadRegister(SerRecReg);
                    uint result = this.cameraSync.ReadRegister(SerTrigReg);
                }
            }

            //while ((ReadSerRec = this.cameraSync.ReadRegister(SerRecReg)) > 0)
            //{
            //    this.cameraSync.WriteRegister(SerRecReg, ReadSerRec >> 4);
            //    ReadSerNumBytes = this.cameraSync.ReadRegister(SerRecReg);
            //    uint temp = ReadSerRec >> 24;
            //    uint result;
            //    for (uint i = 0; i < temp; i += 0x4)
            //    {
            //        result = this.cameraSync.ReadRegister(SerTrigReg + i);
            //    }
            //}
            this.cleared = true;
        }

        /// <summary>
        /// Interface for head tracking sensors
        /// </summary>
        public HeadData GetHeadData(EyeCollection<ImageEye> images)
        {
            if (!cleared)
            {
                this.Clear_Buffer();
            }

            var headData = new HeadData();
            return headData;
            /*
            short GyroX = 0;
            short GyroY = 0;
            short GyroZ = 0;

            short AccX = 0;
            short AccY = 0;
            short AccZ = 0;

            try
            {

                while (true)
                {
                    ReadSerRec = this.cameraSync.ReadRegister(SerRecReg);
                    if ((ReadSerRec & 0xff000000) < 0xC0000)
                    {
                        //headData.DataResult = HeadData.Result.Missing;
                        // headData.TimestampSeconds = -1;
                        break;
                    }

                    ReadSerNumBytes = this.cameraSync.ReadRegister(SerRecReg);


                    if (ReadSerNumBytes>>16 < 12)
                    {
                        break;
                    }
                    //else

                    SensorBuf[0] = this.cameraSync.ReadRegister(SerTrigReg);
                    SensorBuf[1] = this.cameraSync.ReadRegister(SerTrigReg + 0x4);
                    SensorBuf[2] = this.cameraSync.ReadRegister(SerTrigReg + 0x8);

                    GyroX = (short)(SensorBuf[0] >> 16); //(short)this.lastSecondsTimestamp; NSD - I can test the synchronization by passing along the timestamp value 
                    GyroY = (short)(SensorBuf[0] & 0x0000FFFF);
                    GyroZ = (short)(SensorBuf[1] >> 16);

                    AccX = (short)(SensorBuf[1] & 0x0000FFFF);
                    AccY = (short)(SensorBuf[2] >> 16);
                    AccZ = (short)(SensorBuf[2] & 0x0000FFFF);

                    this.cameraSync.WriteRegister(SerRecReg, 0xC0000);      // Push receive buffer          FLAG - this one - 0x70008
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            
            double gyroScalar = 65.5; //NSD 5-8-14: 
            headData.GyroX = GyroX / gyroScalar;
            headData.GyroY = GyroY / gyroScalar;
            headData.GyroZ = GyroZ / gyroScalar;
            //headData.AccelerometerX = AccelX / accelScalar;
            //headData.AccelerometerY = AccelY / accelScalar;
            //headData.AccelerometerZ = AccelZ / accelScalar;
        
            double accScalar = 16384; // 8-22-2014 JB
            headData.AccelerometerX = AccX / accScalar;
            headData.AccelerometerY = AccY / accScalar;
            headData.AccelerometerZ = AccZ / accScalar;

            var timeStamp = new ImageEyeTimestamp();
            timeStamp.Seconds = this.cameraSync.GetCurrentSeconds();
            headData.TimeStamp = timeStamp;

            return headData;*/
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
                //var strobeControl = this.cameraSync.GetStrobe(2);
                //strobeControl.onOff = false;
                //this.cameraSync.SetStrobe(strobeControl);

            }
        }
    }
}
