using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlyCapture2Managed;

namespace OpenIris.ImageGrabbing
{
    public partial class CameraEyeFlyCapture
    {
        /// <summary>
        /// Flags for the set up of the serial port.
        /// </summary>
        [Flags]
        private enum SerialFlags : uint
        {
            TxEnable = 0x40000000,
            RxEnable = 0x80000000,
        }

        uint SIO_CONTROL_CSR_INQ = 0x488;
        uint SERBASE;
        uint SERIAL_MODE_REG => SERBASE + 0x000;
        uint SERIAL_CONTROL_REG => SERBASE + 0x004;
        uint SERIAL_STATUS_REG => SERBASE + 0x004;
        uint RECEIVE_BUFFER_STATUS_CONTROL => SERBASE + 0x008;
        uint TRANSMIT_BUFFER_STATUS_CONTROL => SERBASE + 0x00C;
        uint SIO_DATA_REGISTER => SERBASE + 0x100;

        public void InitSerialCom()
        {
            // Get the base of the camera's serial interface registers.
            // The "absolute" address is required for the ReadRegisterBlock() call.
            var SERBASE_absolute = ReadRegister(SIO_CONTROL_CSR_INQ) * 4;
            SERBASE = SERBASE_absolute & 0x000fffff;

            // Set baud rate to highest, 230400 (setting 10).
            uint MODE = ReadRegister(SERBASE);
            WriteRegister(SERBASE, (MODE & 0x00ffffff) | (10 << 24));

            // Make sure serial reading is enabled.
            WriteRegister(SERIAL_CONTROL_REG, (uint)(SerialFlags.RxEnable | SerialFlags.TxEnable));
        }

        // Write a single byte to PtGrey serial port.
        public void WriteSerialByte(uint val)
        {
            // Write value out to serial holding queue.
            WriteRegister(SIO_DATA_REGISTER, val << 24);
            // Tell camera we want to send 1 byte.
            WriteRegister(TRANSMIT_BUFFER_STATUS_CONTROL, 0x00010000 /*1 << 16*/);
        }

        // Read 4 byte unsigned integer from Point Grey serial port, and swap the bytes.
        public UInt32 ReadUInt32()
        {
            // Tell the camera to transfer 4 bytes into output queue,
            // then read the 4 bytes.
            WriteRegister(RECEIVE_BUFFER_STATUS_CONTROL, 0x00040000);
            UInt32 val = ReadRegister(SIO_DATA_REGISTER);
            return ((val & 0x000000ff) << 24) +
                   ((val & 0x0000ff00) << 8) +
                   ((val & 0x00ff0000) >> 8) +
                   ((val & 0xff000000) >> 24);
        }

        // Read 2 byte signed integer from Point Grey serial port, and swap the bytes.
        public Int16 ReadInt16()
        {
            // Tell the camera to transfer 2 bytes into output queue.
            WriteRegister(RECEIVE_BUFFER_STATUS_CONTROL, 0x00020000);
            UInt16 val = (UInt16)(ReadRegister(SIO_DATA_REGISTER) >> 16);
            return (Int16)(
                (val >> 8) +
                (val << 8));
        }

        public uint BytesAvailable()
        {
            return ReadRegister(SERBASE + 8) >> 24;
        }

        public static UInt32 SwapUInt32(UInt32 val)
        {
            return ((val & 0x000000ff) << 24) +
                   ((val & 0x0000ff00) << 8) +
                   ((val & 0x00ff0000) >> 8) +
                   ((val & 0xff000000) >> 24);
        }

        public static UInt32 SwapUInt32Bytes(byte[] bytes, int idx)
        {
            return SwapUInt32(BitConverter.ToUInt32(bytes, idx));
        }
        public static Int16 SwapInt16Bytes(byte[] bytes, int idx)
        {
            return SwapInt16(BitConverter.ToUInt16(bytes, idx));
        }

        // Swap the two bytes, and return a signed 16-bit integer.
        public static Int16 SwapInt16(UInt16 val)
        {
            return (Int16)((val >> 8) + (val << 8));
        }

        // This will read multiple 32-bit registers at once.
        // The number of registers is infered from the length of the buffer.
        // The "address" should be just the low 20 bits. The proper offset
        // is added for the block copy.
        public void ReadRegBlock(uint address, ref uint[] vals)
        {
            ReadRegisterBlock(0xffff, 0xf0f00000 + address, vals);
        }

        /// <summary>
        /// For efficiency on the FireWire bus, read both the serial status
        /// register, and the bytes available, all at once.
        /// </summary>
        /// <param name="serialError"></param>
        /// <param name="bytesAvail"></param>
        public void SerialStatusAndCount(out bool serialError, out uint bytesAvail)
        {
            uint[] vals = new uint[2];
            ReadRegBlock(SERIAL_STATUS_REG, ref vals);

            var SerialStatus = SwapUInt32(vals[0]);
            // Check the overflow, framing, and parity error bits.
            serialError = (SerialStatus & 0x000E0000) != 0;

            bytesAvail = SwapUInt32(vals[1]);
        }

        /// <summary>
        /// Simply disable, then re-enable the PointGrey serial receive buffer.
        /// This automatically flushes the buffer, and clears the overflow bit.
        /// </summary>
        public void FlushSerialBytes()
        {
            WriteRegister(SERIAL_CONTROL_REG, 0);
            WriteRegister(SERIAL_CONTROL_REG, (uint)(SerialFlags.RxEnable | SerialFlags.TxEnable));
        }

        /// <summary>
        /// Read a whole packet worth of bytes into a buffer. The
        /// passed buffer must be EXACTLY the packet size, plus the 4-byte
        /// header size. It should be already determined that there are
        /// at least this many bytes available in the Camera's serial queue.
        /// </summary>
        /// <param name="packetSize">Size of the packet.</param>
        /// <returns>The array of bytes read.</returns>
        public byte[] ReadSerialPacketBytes(int packetSize)
        {
            // ByteBuf MUST be a multiple of 4 in length.
            byte[] byteBuf = new byte[packetSize];

            // Transfer a packet's worth of bytes into camera's serial read queue.
            WriteRegister(RECEIVE_BUFFER_STATUS_CONTROL, (uint)byteBuf.Length << 16);

            // CONFUSING!! We read from the camera in 4-byte (32-bit) chunks into UIntBuf.
            // THEN we COPY the BYTES into ByteBuf.
            // So UIntBuf must be ByteBuf.Length/4, rounded up.
            uint[] UIntBuf = new uint[7]; // ByteBuf.Length should be 28, so this is length 7*4=28.
            ReadRegBlock(SIO_DATA_REGISTER, ref UIntBuf);
            Buffer.BlockCopy(UIntBuf, 0, byteBuf, 0, byteBuf.Length);

            return byteBuf;
        }


        /// <summary>
        /// Gets a data packet from the serial port.
        /// </summary>
        /// <remarks>
        /// "Auto-align" to the packets of serial data.
        /// Return TRUE if we get a good packet, FALSE otherwise.
        /// Simple strategy to maintain packet alignment.
        ///  - If there is less than one packet of bytes, return false.
        ///  - If buffer has filled up, flush buffer and return false.
        ///  - Read one whole packet of bytes.
        ///  - If we do not have an 0xffffffff header, then flush buffer
        ///    and return false.
        ///  - Otherwise, we have a good packet, so return true.
        ///
        /// </remarks>
        /// <param name="pktbytes">The packet that will be received.</param>
        /// <param name="expectedHeader">Expetec header at the begining of the packet.</param>
        /// <param name="packetSize">Size of the packet.</param>
        /// <returns>True if a packet was obtained.</returns>
        public bool GetPacketWithHeader(out byte[] pktbytes, uint expectedHeader, int packetSize)
        {
            pktbytes = null;
            try
            {
                // Get the serial overflow status, and byte count, all at once.
                SerialStatusAndCount(out bool serialERROR, out uint bytesAvail);

                // Not enough bytes available to read a packet. Return false and try again later.
                if (bytesAvail < packetSize)
                {
                    return false;
                }

                // Overflow of camera's serial input buffer?
                //if ((bytesAvail >= 64) || serialERROR)
                // JORGE: changed this to still try to read a packet even if bytesAvailalble is 64
                // If the packet is no good it will be trashed in the next step with the whole buffer.
                if (serialERROR)
                {
                    Trace.WriteLine($"Bytes available: {bytesAvail}  -  serialERROR: {serialERROR}");
                    FlushSerialBytes();
                    return false;
                }

                pktbytes = ReadSerialPacketBytes(packetSize);

                // If the first 4 bytes are not 0xff something went wrong. Flush and try again later.
                if (BitConverter.ToUInt32(pktbytes, 0) != expectedHeader)
                {
                    FlushSerialBytes();
                    return false;
                }

                //for (int i = 0; i < pktbytes.Length; i++)
                //{
                //    if (i % 2 == 0)
                //        Console.Write("   ");
                //    Console.Write("{0,3:d} ", pktbytes[i]);
                //}
                //Console.WriteLine("");

                return true;
            }
            catch (FC2Exception ex)
            {
                throw new OpenIrisException("ERROR getting head data packet. " + ex.Message, ex);
            }
        }
    }
}
