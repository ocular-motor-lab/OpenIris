//-----------------------------------------------------------------------
// <copyright file="VideoEyeFlyCapture.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System;
    using FlyCapture2Managed;

    /// <summary>
    /// Captures images from video files recorded from a point grey camera.
    /// </summary>
    /// <remarks>
    /// The main features of the class are to recreate the timestamps from the videos.
    /// </remarks>
    public class VideoEyeFlyCapture : VideoEye
    {
        /// <summary>
        /// Possible positions of the embedded info in the video.
        /// </summary>
        public enum PositionOfEmbeddedInfo
        {
            /// <summary>
            /// Top Left Horizontal.
            /// </summary>
            TopLeftHorizontal,

            /// <summary>
            /// Top Right Horizontal.
            /// </summary>
            TopRightHorizontal,

            /// <summary>
            /// Bottom Left Horizontal.
            /// </summary>
            BottomLeftHorizontal,

            /// <summary>
            /// Bottom Right Horizontal.
            /// </summary>
            BottomRightHorizontal,

            /// <summary>
            /// Top Left Vertical.
            /// </summary>
            TopLeftVertical,

            /// <summary>
            /// Top Right Vertical.
            /// </summary>
            TopRightVertical,

            /// <summary>
            /// Bottom Left Vertical.
            /// </summary>
            BottomLeftVertical,

            /// <summary>
            /// Bottom Right Vertical.
            /// </summary>
            BottomRightVertical,
        }

        /// <summary>
        /// Timestamp in seconds of the last frame.
        /// </summary>
        private double lastSeconds = 0;

        /// <summary>
        /// Counter of how many 128 cycles have passed since capturing the first frame.
        /// </summary>
        private long cycles128sec = 0;

        /// <summary>
        /// Pposition of the embedded info in the video.
        /// </summary>
        private PositionOfEmbeddedInfo positionOfEmbeddedInfo;

        /// <summary>
        /// Poisition in bytes of the frame number inside the image.
        /// </summary>
        private int offsetFrameNumber;

        /// <summary>
        /// Initializes a new instance of the VideoEyeFlyCapture class.
        /// </summary>
        /// <param name="whichEye">Left or right eye.</param>
        /// <param name="fileName">Full path and name of the file.</param>
        /// <param name="positionOfEmbeddedInfo">Pposition of the embedded info in the video.</param>
        /// <param name="offsetFrameNumber">Position in bytes of the frame number</param>
        public VideoEyeFlyCapture(Eye whichEye, string fileName, PositionOfEmbeddedInfo positionOfEmbeddedInfo, int offsetFrameNumber=4)
            : base(whichEye, fileName)
        {
            this.positionOfEmbeddedInfo = positionOfEmbeddedInfo;
            this.offsetFrameNumber = offsetFrameNumber;
        }

        /// <summary>
        /// Retrieves an image from the video file. Extracts the timestamp and other info from the image. Specific for point grey cameras.
        /// </summary>
        /// <returns>Image of the eye grabbed.</returns>
        public override ImageEye GrabImageEyeFromVideo()
        {
            var image = base.GrabImageEyeFromVideo();
            var videoTimestamp = image.TimeStamp;

            if ( image is null)   return null;

            // Update the timestamp with the info from the pixels
            ImageEyeTimestamp timestamp = VideoEyeFlyCapture.GetTimeStampFromImage(image, positionOfEmbeddedInfo, this.offsetFrameNumber);

            if (lastSeconds > timestamp.Seconds) cycles128sec++;
            timestamp.Seconds += cycles128sec * 128;
            // Needs to use the video frame number otherwise the video player will not work properly.
            timestamp.FrameNumber = videoTimestamp.FrameNumber;
            lastSeconds = timestamp.Seconds;

            image.TimeStamp = timestamp;

            return image;
        }
        
        /// <summary>
        /// Gets the eye tracker time stamp from the embedded time stamp in the pixels of the image.
        /// </summary>
        /// <param name="image">Image of the eye.</param>
        /// <param name="positionOfEmbeddedInfo">Positions of the embedded info in the video.</param>
        /// <param name="offsetFrameNumber"></param>
        /// <returns>The time stamp.</returns>
        internal static ImageEyeTimestamp GetTimeStampFromImage(ImageEye image, PositionOfEmbeddedInfo positionOfEmbeddedInfo, int offsetFrameNumber)
        {
            // get the PointGrey timestamp
            var timeStampBytes = VideoEyeFlyCapture.GetEmbbeddedField(image, positionOfEmbeddedInfo, 0);
            TimeStamp timeStamp = VideoEyeFlyCapture.GetTimestampFromBytes(timeStampBytes);

            // Convert the timestamp to seconds
            double seconds = (double)timeStamp.cycleSeconds + (((double)timeStamp.cycleCount + ((double)timeStamp.cycleOffset / 3072.0)) / 8000.0);

            // get the PointGrey frameCounter
            var frameCounterBytes = VideoEyeFlyCapture.GetEmbbeddedField(image, positionOfEmbeddedInfo, offsetFrameNumber);
            var frameCounter = BitConverter.ToUInt32(frameCounterBytes, 0);

            // Create the EyeTrackerTimestamp
            ImageEyeTimestamp eyeTrackerTimeStamp = new ImageEyeTimestamp();
            eyeTrackerTimeStamp.Seconds = seconds;
            eyeTrackerTimeStamp.FrameNumber = (ulong)frameCounter;
            eyeTrackerTimeStamp.FrameNumberRaw = (ulong)frameCounter;

            return eyeTrackerTimeStamp;
        }

        /// <summary>
        /// Gets the embedded field after in the frame after offset pixels.
        /// </summary>
        /// <param name="image">Image from the frame.</param>
        /// <param name="positionOfEmbeddedInfo">Positions of the embedded info in the video.</param>
        /// <param name="offset">Starting pixel number of the field.</param>
        /// <returns>Returns the bytes containing the field.</returns>
        internal static byte[] GetEmbbeddedField(ImageEye image, PositionOfEmbeddedInfo positionOfEmbeddedInfo, int offset)
        {
            byte[] bytes = new byte[4];

            switch (positionOfEmbeddedInfo)
            {
                case PositionOfEmbeddedInfo.TopLeftHorizontal:
                    bytes[0] = image.GetData(0, offset + 3, 0);
                    bytes[1] = image.GetData(0, offset + 2, 0);
                    bytes[2] = image.GetData(0, offset + 1, 0);
                    bytes[3] = image.GetData(0, offset + 0, 0);
                    break;
                case PositionOfEmbeddedInfo.TopRightHorizontal:
                    bytes[0] = image.GetData(0, image.Size.Width - (offset + 4), 0);
                    bytes[1] = image.GetData(0, image.Size.Width - (offset + 3), 0);
                    bytes[2] = image.GetData(0, image.Size.Width - (offset + 2), 0);
                    bytes[3] = image.GetData(0, image.Size.Width - (offset + 1), 0);
                    break;
                case PositionOfEmbeddedInfo.BottomLeftHorizontal:
                    bytes[0] = image.GetData(image.Size.Height - 1, offset + 3, 0);
                    bytes[1] = image.GetData(image.Size.Height - 1, offset + 2, 0);
                    bytes[2] = image.GetData(image.Size.Height - 1, offset + 1, 0);
                    bytes[3] = image.GetData(image.Size.Height - 1, offset + 0, 0);
                    break;
                case PositionOfEmbeddedInfo.BottomRightHorizontal:
                    bytes[0] = image.GetData(image.Size.Height - 1, image.Size.Width - (offset + 4), 0);
                    bytes[1] = image.GetData(image.Size.Height - 1, image.Size.Width - (offset + 3), 0);
                    bytes[2] = image.GetData(image.Size.Height - 1, image.Size.Width - (offset + 2), 0);
                    bytes[3] = image.GetData(image.Size.Height - 1, image.Size.Width - (offset + 1), 0);
                    break;
                case PositionOfEmbeddedInfo.TopLeftVertical:
                    bytes[0] = image.GetData(offset + 3, 0, 0);
                    bytes[1] = image.GetData(offset + 2, 0, 0);
                    bytes[2] = image.GetData(offset + 1, 0, 0);
                    bytes[3] = image.GetData(offset + 0, 0, 0);
                    break;
                case PositionOfEmbeddedInfo.TopRightVertical:
                    bytes[0] = image.GetData(offset + 3, image.Size.Width, 0);
                    bytes[1] = image.GetData(offset + 2, image.Size.Width, 0);
                    bytes[2] = image.GetData(offset + 1, image.Size.Width, 0);
                    bytes[3] = image.GetData(offset + 0, image.Size.Width, 0);
                    break;
                case PositionOfEmbeddedInfo.BottomLeftVertical:
                    bytes[0] = image.GetData(image.Size.Height - (offset + 4), 0, 0);
                    bytes[1] = image.GetData(image.Size.Height - (offset + 3), 0, 0);
                    bytes[2] = image.GetData(image.Size.Height - (offset + 2), 0, 0);
                    bytes[3] = image.GetData(image.Size.Height - (offset + 1), 0, 0);
                    break;
                case PositionOfEmbeddedInfo.BottomRightVertical:
                    bytes[0] = image.GetData(image.Size.Height - (offset + 4), image.Size.Width, 0);
                    bytes[1] = image.GetData(image.Size.Height - (offset + 3), image.Size.Width, 0);
                    bytes[2] = image.GetData(image.Size.Height - (offset + 2), image.Size.Width, 0);
                    bytes[3] = image.GetData(image.Size.Height - (offset + 1), image.Size.Width, 0);
                    break;
                default:
                    break;
            }

            return bytes;
        }

        /// <summary>
        /// Get the timestamp structure from the bytes.
        /// </summary>
        /// <remarks>
        /// INFO ABOUT TIMESTAMPS: 
        /// <para>Article 99: Imaging Products timestamping and different timestamp mechanisms.
        /// SUMMARY: 
        /// This article describes the different timestamps available to the user used in determining when an image was captured. 
        /// APPLICABLE PRODUCTS :
        /// All Imaging Products •  FlyCapture 1.0 SDK •  FlyCapture 2.0 SDK • 
        /// ANSWER:
        /// With the FlyCapture SDK, users have access to the following different timestamps:</para>
        /// <para>
        /// PC system clock - This timestamp is generated from the PC's clock; captured images are timestamped once the last image 
        /// packet arrives at the PC. This gives a rough time estimate and is generally not recommended for precision purposes. In 
        /// the FlyCapture 1.x interface, this timestamp can be accessed through the FlyCaptureTimeStamp.ulSeconds and 
        /// FlyCaptureTimeStamp.ulMicroSeconds members of the FlyCaptureImage structure. Using FlyCapture 2.x, reference the seconds 
        /// and microseconds attributes of the TimeStamp structure, where seconds is UNIX time in seconds. For more information on 
        /// these structures, consult the FlyCapture SDK Help.</para>
        /// <para>
        /// 1394 cycle time - This timestamp is based on the 1394 cycle timer (which increments at 8kHz); captured images are 
        /// timestamped at the time that the shutter was closed . In the FlyCapture 1.x interface, this timestamp can be accessed 
        /// through the FlyCaptureTimeStamp.ulCycleSeconds and FlyCaptureTimeStamp.ulCycleCount members of the FlyCaptureImage structure. 
        /// Using FlyCapture 2.x, reference the cycleSeconds and cycleCount attributes of the TimeStamp structure. For more information 
        /// on these structures, consult the FlyCapture SDK Help.</para>
        /// <para>
        /// Embedded image timestamp - This timestamp takes the 1394 cycle timer at the time that the shutter was closed and embeds the 
        /// information in the image pixels. This is the most accurate of the timestamps. In order to access this, you need to set the 
        /// appropriate bit of the FRAME_INFO register 12F8h or use flycaptureSetImageTimestamping() (PGR FlyCapture v1.5.x.x only), and 
        /// then use a function to read the first 4 bytes of the image (the timestamp is located in the first 4 pixels of the image). For 
        /// an example of how this is done in the FlyCapture interface, refer to the MultipleCameraEx sample program. For a FlyCapture 1.x 
        /// example of how to manually parse the image data to retrieve this timestamp, download this sample program. </para>
        /// <code>
        /// unsigned int stamps1[ NUM_GRABS ];
        /// unsigned char* pStamp1 = (unsigned char*)&amp;stamps1;
        /// 
        /// ...
        /// 
        ///  pStamp1[ (i * 4) + 0 ] = image1.pData[3];
        ///  pStamp1[ (i * 4) + 1 ] = image1.pData[2];
        ///  pStamp1[ (i * 4) + 2 ] = image1.pData[1];
        ///  pStamp1[ (i * 4) + 3 ] = image1.pData[0];
        ///  
        ///  double dTime1 = imageTimeStampToSeconds(stamps1[i]);
        /// 
        /// ...
        /// 
        /// inline double 
        /// imageTimeStampToSeconds(unsigned int uiRawTimestamp)
        /// {
        /// 
        ///      int nSecond      = (uiRawTimestamp >> 25) &amp; 0x7F;   // get rid of cycle_* - keep 7 bits
        ///      int nCycleCount  = (uiRawTimestamp >> 12) &amp; 0x1FFF; // get rid of offset
        ///      int nCycleOffset = (uiRawTimestamp >>  0) &amp; 0xFFF;  // get rid of *_count
        /// 
        ///      return (double)nSecond + (((double)nCycleCount+((double)nCycleOffset/3072.0))/8000.0);
        /// }
        /// </code>
        /// <para>
        /// The camera firmware is designed to grab at selected intervals of the 1394 cycle time. To compare any of these timestamps with 
        /// the absolute cycle time information on the 1394 bus, query CYCLE_TIME register FF100200h.</para>
        /// </remarks>
        /// <param name="bytes">Bytes containing the timestamp.</param>
        /// <returns>Timestamp structure.</returns>
        internal static TimeStamp GetTimestampFromBytes(byte[] bytes)
        {
            var uiRawTimestamp = BitConverter.ToUInt32(bytes, 0);

            TimeStamp timestamp = new TimeStamp();

            timestamp.cycleSeconds = (uint)((uiRawTimestamp >> 25) & 0x7F); // get rid of cycle_* - keep 7 bits
            timestamp.cycleCount = (uint)((uiRawTimestamp >> 12) & 0x1FFF); // get rid of offset
            timestamp.cycleOffset = (uint)((uiRawTimestamp >> 0) & 0xFFF);  // get rid of *_count

            return timestamp;
        }
    }
}
