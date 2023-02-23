//-----------------------------------------------------------------------
// <copyright file="HeadSensorMPUFTDI.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.HeadTracking
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using OpenIris.ImageGrabbing;
    using FTD2XX_NET;
    using MPU;

    /// <summary>
    /// USB sensor with 9 degrees of freedom
    /// </summary>
    [Obsolete]
    internal class HeadSensorMPUFTDI
    {

        class FTDI_MPU
        {
            public bool statusOK = false;

            private FTDI myFTDI = new FTDI();
            private byte[] cmdBuffer = new byte[5000];
            private int N_FIFO_Channels = 6;

            private void FTDI_init()
            {
                FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

                ftStatus = myFTDI.OpenByIndex(0);
                ftStatus |= myFTDI.ResetDevice();
                ftStatus |= myFTDI.SetBaudRate(5000000);
                ftStatus |= myFTDI.SetLatency(1);
                ftStatus |= myFTDI.SetBitMode(0, 0);
                ftStatus |= myFTDI.SetBitMode(0, 2);


                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    Trace.WriteLine("Error in FTDI_init");
                }
                else
                {
                    Trace.WriteLine("FTDI initialize succes!");
                    statusOK = true;
                }
            }

            public void MPU_init()
            {
                FTDI_init();

                //To clear out the FTDI receive buffer before calling any commands
                uint queueReceive = 0;
                myFTDI.GetRxBytesAvailable(ref queueReceive);

                byte[] junkBuf = new byte[queueReceive];
                uint numBytesRead = 0;
                myFTDI.Read(junkBuf, queueReceive, ref numBytesRead);


                //start MPU initialization
                MPU_write(MPU9250ConstantClass.MPU9250_PWR_MGMT_1, 0x81);

                // Must leave Clock Select (lowest 3 bits) set to 001.
                //   MPU_write(MPU9250_PWR_MGMT_1, 0x01);

                // Disable I2C mode - set I2C_IF_DIS bit, reset FIFO, and reset signal paths..
                MPU_write(MPU9250ConstantClass.MPU9250_USER_CTRL, 0x15);
                MPU_write(MPU9250ConstantClass.MPU9250_USER_CTRL, 0x00);
                Thread.Sleep(5); //let sleep so that the chip can reset

                //initialize MPU - take out of sleep mode and set full-scale range for gyros and accel
                //	MPU_write(MPU9250_PWR_MGMT_1, 0x00);               // Take out of sleep mode
                //	MPU_write(MPU9250_SIGNAL_PATH_RESET, 0x07);        // reset signal paths for gyro, accel, and temp

                // Set the A/D full-scale ranges.
                byte MPU_ACCEL_FS_RANGE = (MPU9250ConstantClass.AFS_SEL << 3) & 0xFF; //accel FS 2bit value goes in Bits 3 and 4 of ACCEL_CONFIG register
                MPU_write(MPU9250ConstantClass.MPU9250_ACCEL_CONFIG, MPU_ACCEL_FS_RANGE);//set fullscale range of accel to 2g

                byte MPU_GYRO_FS_RANGE = (MPU9250ConstantClass.FS_SEL << 3) & 0xFF;   //gyro FS 2bit value goes in Bits 3 and 4 of GYRO_CONFIG register
                MPU_write(MPU9250ConstantClass.MPU9250_GYRO_CONFIG, MPU_GYRO_FS_RANGE);  //set fullscale range of gyro to 500dps

                // Set the Gyro low-pass filter to second highest setting, so that we get a 1KHz sample rate.
                MPU_write(MPU9250ConstantClass.MPU9250_CONFIG, 0x01 | (5 << 3));

                // Set sample rate divisor.
                MPU_write(MPU9250ConstantClass.MPU9250_SMPLRT_DIV, 7);
            }

            private bool MPU_write(byte addr, byte val)
            {
                byte[] buf = new byte[2] { addr, val };
                return my_SPI_Write(ref buf, 2) > 0;
            }

            public byte MPU_read(int addr)
            {
                byte[] buf = new byte[1];
                byte readAddr = (byte)((1 << 7) | addr);

                my_SPI_Read(readAddr, buf, 1);
                return buf[0];
            }

            private UInt32 MPU_read(int addr, ref byte[] buf, byte NBytesToRead)
            {
                if (NBytesToRead <= 0)
                    return 0;

                byte readAddr = (byte)((1 << 7) | addr);

                return my_SPI_Read(readAddr, buf, NBytesToRead);
            }

            private UInt32 my_SPI_Read(byte RegAddress, byte[] inBuffer, UInt32 sizeToTransfer)
            {
                FTDI.FT_STATUS status;
                int cmdidx = 0, i;
                UInt32 NBytesTransferred = 0;

                // Chip Select LOW.
                cmdBuffer[cmdidx++] = 0x80; //MPSSE_CMD_SET_DATA_BITS_LOWBYTE;
                cmdBuffer[cmdidx++] = 0x00;   // Value set CS low.
                cmdBuffer[cmdidx++] = 0x0b;	// Direction bits. All output except for SDO from MPU-9250.

                // Send the first byte w/out reading. This is the MPU-9250 register address, which
                // should also have the high bit set for "read".
                cmdBuffer[cmdidx++] = 0x11; //MPSSE_CMD_DATA_OUT_BYTES_NEG_EDGE;
                cmdBuffer[cmdidx++] = 0;  // Length-1 low
                cmdBuffer[cmdidx++] = 0;  // Length-1 high
                cmdBuffer[cmdidx++] = RegAddress; // data byte

                // Command to write and read bytes.
                cmdBuffer[cmdidx++] = 0x31; //MPSSE_CMD_DATA_BYTES_IN_POS_OUT_NEG_EDGE;
                cmdBuffer[cmdidx++] = (byte)((sizeToTransfer - 1) & 0x000000FF);      // lengthL
                cmdBuffer[cmdidx++] = (byte)(((sizeToTransfer - 1) & 0x0000FF00) >> 8); // lenghtH

                // Fill with "dummy" zero bytes, which must be sent out in order for the read
                // to occur.
                for (i = 0; i < sizeToTransfer; ++i)
                    cmdBuffer[cmdidx++] = 0;

                // Chip Select HIGH.
                cmdBuffer[cmdidx++] = 0x80; //MPSSE_CMD_SET_DATA_BITS_LOWBYTE;
                cmdBuffer[cmdidx++] = 0x08;   // Value set CS high, and clock bit high.
                cmdBuffer[cmdidx++] = 0x0b;	// Direction bits. All output except for SDO from MPU-9250.

                // Write all commands and data all at once.
                status = myFTDI.Write(cmdBuffer, cmdidx, ref NBytesTransferred);

                // Read from buffer.
                NBytesTransferred = 0;
                status = myFTDI.Read(inBuffer, sizeToTransfer, ref NBytesTransferred);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    Trace.WriteLine("Error in Read");
                }

                return NBytesTransferred;
            }


            private UInt32 my_SPI_Write(ref byte[] outBuffer, UInt32 sizeToTransfer)
            {
                FTDI.FT_STATUS status;
                int cmdidx = 0, i;
                UInt32 NBytesTransferred = 0;

                //Chip Select LOW
                cmdBuffer[cmdidx++] = 0x80; //MPSSE_CMD_SET_DATA_BITS_LOWBYTE
                cmdBuffer[cmdidx++] = 0x00;   // Value set CS low.
                cmdBuffer[cmdidx++] = 0x0b;	// Direction bits. All output except for SDO from MPU-9250.

                // Command to write and read bytes.
                cmdBuffer[cmdidx++] = 0x11; //MPSSE_CMD_DATA_OUT_BYTES_NEG_EDGE;
                cmdBuffer[cmdidx++] = (byte)((sizeToTransfer - 1) & 0x000000FF); // lengthL 
                cmdBuffer[cmdidx++] = (byte)(((sizeToTransfer - 1) & 0x0000FF00) >> 8);//lenghtH

                // Copy bytes from inBuffer to cmdBuffer.
                for (i = 0; i < sizeToTransfer; ++i)
                    cmdBuffer[cmdidx++] = outBuffer[i];

                // Chip Select HIGH.
                cmdBuffer[cmdidx++] = 0x80; //MPSSE_CMD_SET_DATA_BITS_LOWBYTE
                cmdBuffer[cmdidx++] = 0x08;   // Value set CS high, and clock bit high.
                cmdBuffer[cmdidx++] = 0x0b;	// Direction bits. All output except for SDO from MPU-9250.

                // Write all commands and data all at once.
                status = myFTDI.Write(cmdBuffer, cmdidx, ref NBytesTransferred);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    Trace.WriteLine("Error in Write");
                }

                return NBytesTransferred;
            }


            public void MPU_close()
            {
                myFTDI.Close();
            }


            public bool enableFIFO()
            {
                //FIFO Enable Register Bits: Register 0x23 (hex). Set to 1 to enable
                //Bit 7:Temp_FIFO_EN
                //Bits 6-4: 6=GyroX, 5=GyroY, 4=GyroZ
                //Bit 3: ACCEL_FIFO_EN
                //Bits 2-0: 2=SVL2, 1=SLV1, 0=SLV0
                byte FIFO_ENABLE_OPTIONS = 0x78; //enables all gyros and accels
                MPU_write(MPU9250ConstantClass.MPU9250_FIFO_EN, FIFO_ENABLE_OPTIONS);

                // This MUST STAY IN SYNC with the number of channels selected above.
                N_FIFO_Channels = 6;

                byte FIFO_ENABLE = 0x04; //first write to FIFO_EN register to reset FIFO buffer
                MPU_write(MPU9250ConstantClass.MPU9250_USER_CTRL, FIFO_ENABLE);

                Thread.Sleep(5);//Let sleep for 5 sec to let FIFO reset

                //Register 106 (0x6A) - The enable the fifo buffer
                FIFO_ENABLE = 0x40;

                return MPU_write(MPU9250ConstantClass.MPU9250_USER_CTRL, FIFO_ENABLE);
            }

            private int NBadReads = 0;
            private int NBytesLast = 0;
            int FIFO_count_trys = 0;

            public int MPU_readFIFO(ref Int16[,] buf)
            {
                int N_BYTES_PER_SAMPLE = N_FIFO_Channels * 2;

                int N_FIFO_bytes;
                int ExtraBytes;

                // Read number of bytes in MPU-9250 FIFO.
                N_FIFO_bytes = MPU_read16(MPU9250ConstantClass.MPU9250_FIFO_COUNTH);

                //Debug.WriteLine("FIFO Count: {0}", N_FIFO_bytes);

                if (N_FIFO_bytes < N_BYTES_PER_SAMPLE)
                {
                    return 0;
                }

                // For debugging.
                NBytesLast = N_FIFO_bytes;

                // ExtraBytes will usually be zero, but sometimes we may happen to
                // catch it in the middle of being updated with sensor data, then
                // we will have a few "extra" bytes in the count that are not
                // an exact multiple of the sample size.
                //
                // If we have any Extra bytes 3 times in a row, then it is almost
                // certain that we are out of sync. So remove the extra bytes
                // from the FIFO before reading the "good"
                // samples into the caller's buffer.
                //
                ExtraBytes = N_FIFO_bytes % N_BYTES_PER_SAMPLE;
                if (ExtraBytes > 0)
                    ++FIFO_count_trys;
                else
                    FIFO_count_trys = 0;

                byte[] interBuf = new byte[100];
                // If we are out of sync, then remove extra bytes
                // to get us back in sync. If the FIFO is full,
                // then automatically re-sync.
                if ((FIFO_count_trys >= 3) || (N_FIFO_bytes == 512))
                {
                    ++NBadReads;
                    MPU_read(MPU9250ConstantClass.MPU9250_FIFO_R_W, ref interBuf, (byte)ExtraBytes);
                    N_FIFO_bytes -= ExtraBytes;
                    for (int i = 0; i < N_FIFO_Channels * 2; ++i)
                        interBuf[i] = (byte)0x7f;
                    FIFO_count_trys = 0;
                }

                N_FIFO_bytes = (N_FIFO_bytes / N_BYTES_PER_SAMPLE) * N_BYTES_PER_SAMPLE;

                // After this point, the FIFO should be "in sync", with an exact number
                // of samples remaining in the FIFO.
                //

                int NSampsRead = MPU_read16(MPU9250ConstantClass.MPU9250_FIFO_R_W, ref buf, N_FIFO_bytes / 2);

                // Return the number of samples read in.
                return NSampsRead;

            }

            // Read a 16-bit register.
            public UInt16 MPU_read16(int addr)
            {
                byte[] buf = { 0, 0 };
                byte readAddr = (byte)((1 << 7) | addr);

                my_SPI_Read(readAddr, buf, 2);

                UInt16 total = 0;
                total = (UInt16)((total << 8) | buf[0]);
                total = (UInt16)((total << 8) | buf[1]);

                int who = MPU_read(MPU9250ConstantClass.MPU9250_WHO_AM_I);

                return total;
            }

            // 
            // "Overloaded" version of MPU_read16() for multiple uint16's into a
            // buffer.
            // 
            // Pass the number of 16-bit ints to read, NOT the number of bytes!!
            // 
            // Returns number of 16-bit int's read, NOT the number of bytes.
            //
            private int MPU_read16(int addr, ref Int16[,] buf, int N16bitValsToRead)
            {
                int N8bitValsToRead = N16bitValsToRead * 2;

                if (N16bitValsToRead <= 0)
                    return 0;

                byte readAddr = (byte)((1 << 7) | addr);

                byte[] interBuf = new byte[N8bitValsToRead];

                UInt32 number8bitsValsRead = my_SPI_Read(readAddr, interBuf, (UInt32)N8bitValsToRead);

                for (int i = 0; i < number8bitsValsRead / 12; ++i)
                {
                    for (int j = 0; j <= 5; j++)
                    {
                        buf[i, j] = 0;
                        buf[i, j] = (Int16)((buf[i, j] << 8) | interBuf[(12 * i) + (j * 2)]);
                        buf[i, j] = (Int16)((buf[i, j] << 8) | interBuf[(12 * i) + (j * 2) + 1]);
                    }
                }

                int number16bitSampsRead = ((int)number8bitsValsRead / 2) / N_FIFO_Channels;

                return number16bitSampsRead;
            }
        }
        /// <summary>
        /// Pointgrey camera used to get timestamps to sync the head data and the eye data.
        /// </summary>
        private CameraEyeFlyCapture cameraSync;

        /// <summary>
        /// Data aquisition board.
        /// </summary>
        private FTDI_MPU ftdi_mpu;

        /// <summary>
        /// Initializes a new instance of HeadSensorMicromedicalMeasurmentComputing the class.
        /// </summary>
        /// <param name="cameraSync">The associated point grey camera.</param>
        internal HeadSensorMPUFTDI(CameraEyeFlyCapture cameraSync)
        {
            this.cameraSync = cameraSync;
            this.ftdi_mpu = new FTDI_MPU();
        }

        /// <summary>
        /// Starts tracking.
        /// </summary>
        public void StartTracking()
        {
            this.ftdi_mpu.MPU_init();
            this.ftdi_mpu.enableFIFO();
            this.initialized = true;
        }
        private bool initialized = false;
        /// <summary>
        /// Stops tracking.
        /// </summary>
        public void StopTracking()
        {

        }

        Int16[,] SampleBuf = new Int16[1000, 6];

        /// <summary>
        /// Interface for head tracking sensors
        /// </summary>
        public HeadData GetHeadData(EyeCollection<ImageEye> images)
        {
            var headData = new HeadData();
            if (this.ftdi_mpu.statusOK && this.initialized)
            {
                int nSamplesRead = this.ftdi_mpu.MPU_readFIFO(ref SampleBuf);

                //Debug.WriteLine("Number of Samples Read: {0}", nSamplesRead);
                if (nSamplesRead > 0)
                {
                    int plot_out_loc = 0;
                    headData.AccelerometerX = (double)SampleBuf[plot_out_loc, 0];
                    headData.AccelerometerY = (double)SampleBuf[plot_out_loc, 1];
                    headData.AccelerometerZ = (double)SampleBuf[plot_out_loc, 2];

                    headData.GyroX = (double)SampleBuf[plot_out_loc, 3];
                    headData.GyroY = (double)SampleBuf[plot_out_loc, 4];
                    headData.GyroZ = (double)SampleBuf[plot_out_loc, 5];
                }
            }

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
            if (disposing)
            {

            }
        }
    }
}
