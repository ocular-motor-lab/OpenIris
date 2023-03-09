//-----------------------------------------------------------------------
// <copyright file="CameraEyeFlyCapture.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using FlyCapture2Managed;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to control a point grey camera in the context of eye tracking.
    /// </summary>
    public partial class CameraEyeFlyCapture : CameraEye, IMovableImageEyeSource, IDisposable
    {
        public enum CameraPixelFormat { Raw8, Mono8 }
        public enum GPIOMode { input, output, strobe }

        #region fields

        /// <summary>
        /// Camera object.
        /// </summary>
        private ManagedCamera camera;
        private readonly ManagedPGRGuid cameraID;

        private long numberFramesGrabbed;

        /// <summary>
        /// How many cycles of 128 seconds (maximum raw timestamp) have happened. This is necessary
        /// to calculate good timestamps references to the beginning.
        /// </summary>
        private int cycles128sec;

        /// <summary>
        /// Last raw image metadata necessary to keep track of the cycles of seconds
        /// </summary>
        private TimeStamp lastRawTimeStamp = new TimeStamp();

        /// <summary>
        /// First timestamp
        /// </summary>
        private ImageEyeTimestamp firstTimeStamp = new ImageEyeTimestamp();

        /// <summary>
        /// Timestamp of the last frame.
        /// </summary>
        private ImageEyeTimestamp lastTimeStamp = new ImageEyeTimestamp();

        /// <summary>
        /// Last frame rate requested to the camera.
        /// </summary>
        private float lastFrameRateRequested;

        private (bool pending, PointF Position) requestedCenter;
        private (bool pending, MovementDirection Direction) requestedMoveDirection;
        private bool stopping;

        #endregion fields

        #region constructor

        /// <summary>
        /// Initializes a new instance of CameraEyeFlyCapture. With the first camera that can be found.
        /// </summary>
        /// <param name="whichEye"></param>
        /// <param name="requestedFrameRate"></param>
        /// <param name="roi"></param>
        public CameraEyeFlyCapture(Eye whichEye, float requestedFrameRate, Rectangle roi)
        {
            using var busMgr = new ManagedBusManager();

            if (busMgr.GetNumOfCameras() < 1) throw new InvalidOperationException($"No camera connected");

            WhichEye = whichEye;
            RequestedFrameRate = requestedFrameRate;
            RequestedROI = roi;

            cameraID = busMgr.GetCameraFromIndex(0u);
            AutoExposure = true;

            camera = new ManagedCamera();
        }

        /// <summary>
        /// Initializes a new instance of the CameraEyeFlyCapture class.
        /// </summary>
        /// <param name="whichEye">Left or right eye (or both).</param>
        /// <param name="cameraSerialNumber">Serial Number of the camera.</param>
        /// <param name="requestedFrameRate">Requested frame rate.</param>
        /// <param name="roi">Region of interest within the frame.</param>
        public CameraEyeFlyCapture(Eye whichEye, uint cameraSerialNumber, float requestedFrameRate, Rectangle roi)
        {
            using var busMgr = new ManagedBusManager();

            cameraID = busMgr.GetCameraFromSerialNumber(cameraSerialNumber);

            if (cameraID is null) throw new InvalidOperationException($"Camera not found for serial number {cameraSerialNumber}");

            WhichEye = whichEye;
            RequestedFrameRate = requestedFrameRate;
            RequestedROI = roi;

            AutoExposure = true;

            camera = new ManagedCamera();
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose();
                camera.Dispose();
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion constructor

        #region public properties


        /// <summary>
        /// Gets the diagnostics info.
        /// </summary>
        public override object Info
        {
            get
            {
                var ndrops = (long)(lastTimeStamp.FrameNumberRaw - firstTimeStamp.FrameNumberRaw + 1) - numberFramesGrabbed;

                string s = string.Empty;
                s += "cycles128sec".PadLeft(30) + " : " + cycles128sec.ToString() + "\r\n";

                s += "firstSeconds".PadLeft(30) + " : " + firstTimeStamp.ToString() + "\r\n";
                s += "lastSeconds".PadLeft(30) + " : " + lastTimeStamp.Seconds.ToString() + "\r\n";

                s += "firstRawFrameNumber".PadLeft(30) + " : " + firstTimeStamp.FrameNumberRaw.ToString() + "\r\n";
                s += "lastRawFrameNumber".PadLeft(30) + " : " + lastTimeStamp.FrameNumberRaw.ToString() + "\r\n";
                s += "\r\n";
                s += "FramesCounter".PadLeft(30) + " : " + numberFramesGrabbed.ToString() + "\r\n";
                s += "DroppedFramesCounter".PadLeft(30) + " : " + ndrops.ToString() + "\r\n";
                s += "\r\n";

                var delay = (lastTimeStamp.Seconds - firstTimeStamp.Seconds) - ((lastTimeStamp.FrameNumberRaw - firstTimeStamp.FrameNumberRaw) / RequestedFrameRate);

                s += "delay".PadLeft(30) + " : " + string.Format("{000:0000.0000}", delay) + "\r\n";

                s += "lastFreq".PadLeft(30) + " : " + lastFrameRateRequested.ToString() + "\r\n";
                s += "reportedFrameRate".PadLeft(30) + " : " + FrameRate.ToString() + "\r\n";

                s += "\r\n";

                return s;
            }
        }

        /// <summary>
        /// Gets or sets the option of using autoexposure for the camera.
        /// </summary>
        public bool AutoExposure { get; set; }

        /// <summary>
        /// Gain of the sensor.
        /// </summary>
        public float Gain { get; set; }

        /// <summary>
        /// Pixel format of the image. Binned or not binned, for example.
        /// </summary>
        public CameraPixelFormat PixelFormat { get; set; }

        /// <summary>
        /// Mode of the camera. 0 normal. 1 every four pixels. 2 skip half of the lines.
        /// </summary>
        public int PixelMode { get; set; }

        /// <summary>
        /// Gets the sensor resolution in pixels.
        /// </summary>
        public Size SensorResolution { get; private set; }

        /// <summary>
        /// Gets the ROI requested to the camera.
        /// </summary>
        public Rectangle RequestedROI { get; private set; }

        /// <summary>
        /// Gets the frame rate requested to the camera.
        /// </summary>
        public virtual double RequestedFrameRate { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the camera should continuously monitor and adjust
        /// the frame rate.
        /// </summary>
        public bool ShouldAdjustFrameRate { get; set; }

        /// <summary>
        /// Duration of the shutter in ms.
        /// </summary>
        public float ShutterDuration { get; set; }

        /// <summary>
        /// Region of interest within the camera frame.
        /// </summary>
        public Rectangle ROI { get; private set; }

        /// <summary>
        /// Size of the buffer of images. Zero if no bufffer should be used.
        /// </summary>
        public int BufferSize { get; set; }

        #endregion public properties

        #region Public methods


        /// <summary>
        /// Starts capturing images. It is expected that the frame numbers start at 0 and they are
        /// always monotonic. There could be dropped frames though. The <see
        /// cref="EyeTrackerImageGrabber"/> object will take care of it.
        /// </summary>
        public override void Start()
        {
            camera.Connect(cameraID);

            var cameraInfo = camera.GetCameraInfo();

            Trace.WriteLine("Initializing camera:");
            Trace.WriteLine("Driver: " + cameraInfo.driverName);
            Trace.WriteLine("Firmware version: " + cameraInfo.firmwareVersion);
            Trace.WriteLine("Firmware builtTime: " + cameraInfo.firmwareBuildTime);
            Trace.WriteLine("Max bus speed: " + cameraInfo.maximumBusSpeed);
            Trace.WriteLine("Sensor: " + cameraInfo.sensorInfo);
            Trace.WriteLine("Sensor resolution: " + cameraInfo.sensorResolution);
            Trace.WriteLine("Serial number: " + cameraInfo.serialNumber);

            string[] s = cameraInfo.sensorResolution.Split('x');
            SensorResolution = new Size(int.Parse(s[0]), int.Parse(s[1]));

            SetFormat7Settings();
            SetFrameRate(RequestedFrameRate);
            SetExposure();
            SetEmbeddedInfo();
            SetCameraBuffer(BufferSize);

            //// TODO: I would like to make this more consistent across cameras
            //// right now it is a bit messy, when do they actually start to get images

            camera.StartCapture();
            FrameRate = camera.GetProperty(PropertyType.FrameRate).absValue;
            lastFrameRateRequested = (float)RequestedFrameRate;
        }

        /// <summary>
        /// Stops capturing images.
        /// </summary>
        public override void Stop()
        {
            if (camera == null) return;

            try
            {
                stopping = true;
                camera?.StopCapture();
            }
            catch (FC2Exception ex)
            {
                Trace.WriteLine("Error Stopping capture. " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        protected override ImageEye GrabImageFromCamera()
        {
            if (camera == null) throw new InvalidOperationException("Camera is null");

            try
            {
                // Grab a new frame
                using var rawImageRetrieved = new ManagedImage();
                camera.RetrieveBuffer(rawImageRetrieved);

                numberFramesGrabbed++;

                // Not 100% why this is necessary
                var rawImage = new ManagedImage();
                rawImageRetrieved.Convert(rawImageRetrieved.pixelFormat, rawImage);

                // If the current clycle seconds is smaller than the last one it must
                // be that at least one cycle has been completed so increament the counter
                if (lastRawTimeStamp.cycleSeconds > rawImage.timeStamp.cycleSeconds) cycles128sec++;

                var timestamp = new ImageEyeTimestamp
                {
                    Seconds = GetTimestampSeconds(rawImage.timeStamp),
                    FrameNumber = rawImage.imageMetadata.embeddedFrameCounter - firstTimeStamp.FrameNumberRaw,
                    FrameNumberRaw = rawImage.imageMetadata.embeddedFrameCounter
                };

                // If it is the first frame save some info
                if (numberFramesGrabbed == 1)
                {
                    timestamp.FrameNumber = 0;
                    firstTimeStamp = timestamp;
                }

                // Save some info about the current frame
                lastTimeStamp = timestamp;
                lastRawTimeStamp = rawImage.timeStamp;

                // Change camera properties within the loop for threadsafety
                if (ShouldAdjustFrameRate) AdjustFrameRate();
                if (requestedCenter.pending) DoCenterROI();
                if (requestedMoveDirection.pending) DoMoveROI();

                unsafe
                {
                    //// TODO: make sure this is ok memorywise. I am afraid the rawImage object 
                    //// may be disposed and mess up with the image object
                    //// I actually don't understand very well why this works and never crashes. 
                    //// RawImage is inside a using so it should get disposed.
                    var newImage = new ImageEye((int)rawImage.cols, (int)rawImage.rows, (int)rawImage.stride, (IntPtr)rawImage.data, timestamp)
                    {
                        WhichEye = WhichEye,
                        ImageSourceData = rawImage
                    };

                    return newImage;
                }
            }
            catch (FC2Exception) when (stopping)
            {
                Trace.WriteLine("Exception capturing but just stopping.");
                return null;
            }
            catch(Exception ex)
            {
                Trace.WriteLine("ERROR GRABBING IMAGE: "  + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Move the camera in a given direction.
        /// </summary>
        /// <param name="direction">Direction to move.</param>
        public void Move(MovementDirection direction)
        {
            requestedMoveDirection = (true, direction);
        }

        /// <summary>
        /// Center camera (or ROI) around a point.
        /// </summary>
        /// <param name="centerPupil">Center of the eye.</param>
        public void Center(PointF centerPupil)
        {
            requestedCenter = (true, centerPupil);
        }

        /// <summary>
        /// Get the current timestamp at the camera.
        /// </summary>
        /// <returns>The seconds of the timestamp.</returns>
        public double GetCurrentSeconds()
        {
            var timeStamp = camera.GetCycleTime();
            return cycles128sec * 128 + (double)timeStamp.cycleSeconds + (((double)timeStamp.cycleCount + ((double)timeStamp.cycleOffset / 3072.0)) / 8000.0); // seconds
        }

        public void StartStrobe(int pin)
        {
            camera.SetGPIOPinDirection((uint)pin, 1);
            var strobeControl = camera.GetStrobe((uint)pin);
            if (!strobeControl.onOff)
            {
                strobeControl.onOff = true;
                strobeControl.duration = 1;
                strobeControl.polarity = 1;
                camera.SetStrobe(strobeControl);
            }
        }

        public void WriteRegister(uint address, uint value)
        {
            camera.WriteRegister(address, value);
        }

        public uint ReadRegister(uint address)
        {
            return camera.ReadRegister(address);
        }

        public void ReadRegisterBlock(ushort addressHigh, uint addressLow, uint[] buffer)
        {
            camera.ReadRegisterBlock(addressHigh, addressLow, buffer);
        }

        public virtual void WriteRegisterBlock(ushort addressHigh, uint addressLow, uint[] buffer)
        {
            camera.WriteRegisterBlock(addressHigh, addressLow, buffer);
        }

        /// <summary>
        /// Sets the GPIOs.
        /// </summary>
        public void SetGPIO(int gpioPin, GPIOMode mode)
        {
            switch (mode)
            {
                case GPIOMode.input:
                    camera.SetGPIOPinDirection((uint)gpioPin, 0);
                    break;

                case GPIOMode.output:
                    camera.SetGPIOPinDirection((uint)gpioPin, 1);
                    break;

                case GPIOMode.strobe:
                    camera.SetGPIOPinDirection((uint)gpioPin, 1);
                    break;

                default:
                    break;
            }
        }

        private void DoCenterROI()
        {
            var centerPupil = requestedCenter.Position;
            requestedCenter = (false, PointF.Empty);

            // BIG TODO: make this compatible with other pixel modes. Right now it will only work
            // with mode0

            if (centerPupil.IsEmpty) return;

            if (camera == null) throw new InvalidOperationException("Camera is null");

            var currFmt7Settings = new Format7ImageSettings();
            uint currPacketSize = 0;
            float percentage = 0.0f; // Don't need to keep this
            camera.GetFormat7Configuration(currFmt7Settings, ref currPacketSize, ref percentage);

            var xmax = (int)(SensorResolution.Width - currFmt7Settings.width);
            var ymax = (int)(SensorResolution.Height - currFmt7Settings.height);

            Point roiOrigin;

            // TODO: Deal with rotation
            // Round the center to a multiple of 4
            if (CameraOrientation.IsUpsideDown())
            {
                roiOrigin = new Point(
                    (int)(currFmt7Settings.offsetX + (Math.Round((centerPupil.X - (currFmt7Settings.width / 2)) / 4) * 4)),
                    (int)(currFmt7Settings.offsetY + (Math.Round((-centerPupil.Y + (currFmt7Settings.height / 2)) / 4) * 4)));
            }
            else
            {
                roiOrigin = new Point(
                    (int)(currFmt7Settings.offsetX + (Math.Round((-centerPupil.X + (currFmt7Settings.width / 2)) / 4) * 4)),
                    (int)(currFmt7Settings.offsetY + (Math.Round((centerPupil.Y - (currFmt7Settings.height / 2)) / 4) * 4)));
            }

            // Bound the center
            roiOrigin = new Point(
                (int)Math.Min(xmax, Math.Max(4, roiOrigin.X)),
                (int)Math.Min(ymax, Math.Max(4, roiOrigin.Y)));

            currFmt7Settings.offsetX = Convert.ToUInt32(roiOrigin.X);
            currFmt7Settings.offsetY = Convert.ToUInt32(roiOrigin.Y);

            bool isgood = false;

            try
            {
                // Validate the settings to make sure that they are valid
                camera.ValidateFormat7Settings(currFmt7Settings, ref isgood);

                if (!isgood) return;

                camera.StopCapture();

                // Get the image settings from the page
                camera.SetFormat7Configuration(currFmt7Settings, currPacketSize);

                // Restart the camera if it was running beforehand.
                camera.StartCapture();
            }
            catch (FC2Exception)
            {
                Trace.WriteLine("There was an error changing the ROI of the camera the camera.");
            }
        }

        private void DoMoveROI()
        {
            var direction = requestedMoveDirection.Direction;
            requestedMoveDirection = (false, MovementDirection.None);

            if (camera == null) throw new InvalidOperationException("Camera is null");

            // BIG TODO: make this compatible with other pixel modes. Right now it will only work
            // with mode0

            // TODO: try this one the micromedical camera
            int step = 24;

            if (CameraOrientation.IsUpsideDown())
            {
                switch (direction)
                {
                    case MovementDirection.Up:
                        direction = MovementDirection.Down;
                        break;
                    case MovementDirection.Down:
                        direction = MovementDirection.Up;
                        break;
                    case MovementDirection.Left:
                        direction = MovementDirection.Right;
                        break;

                    case MovementDirection.Right:
                        direction = MovementDirection.Left;
                        break;

                    default:
                        break;
                }
            }

            Format7ImageSettings currFmt7Settings = new Format7ImageSettings();
            uint currPacketSize = 0;
            float percentage = 0.0f; // Don't need to keep this

            camera.GetFormat7Configuration(currFmt7Settings, ref currPacketSize, ref percentage);
            switch (direction)
            {
                case MovementDirection.Up:
                    currFmt7Settings.offsetY = Convert.ToUInt32(Math.Min((int)currFmt7Settings.offsetY + step, SensorResolution.Height - RequestedROI.Height));
                    break;

                case MovementDirection.Down:
                    currFmt7Settings.offsetY = Convert.ToUInt32(Math.Max((int)currFmt7Settings.offsetY - step, 0));
                    break;

                case MovementDirection.Left:
                    currFmt7Settings.offsetX = Convert.ToUInt32(Math.Max((int)currFmt7Settings.offsetX - step, 0));
                    break;

                case MovementDirection.Right:
                    currFmt7Settings.offsetX = Convert.ToUInt32(Math.Min((int)currFmt7Settings.offsetX + step, SensorResolution.Width - RequestedROI.Width));
                    break;

                default:
                    break;
            }

            try
            {
                bool isgood = false;

                // Validate the settings to make sure that they are valid
                camera.ValidateFormat7Settings(currFmt7Settings, ref isgood);

                if (!isgood) return;

                camera.StopCapture();

                // Get the image settings from the page
                camera.SetFormat7Configuration(currFmt7Settings, currPacketSize);

                // Restart the camera if it was running beforehand.
                camera.StartCapture();
            }
            catch (FC2Exception)
            {
                Trace.WriteLine("There was an error changing the ROI.");
            }
        }

        #endregion Public methods

        #region Static Methods

        /// <summary>
        /// Synchronizes two cameras so the shutter opens more or less at the same time.
        /// </summary>
        /// <param name="cameraLeftEye">Left eye camera.</param>
        /// <param name="cameraRightEye">Right eye camera.</param>
        /// <param name="frameRate">Desired frame rate.</param>
        public static void SyncCameras(CameraEyeFlyCapture cameraLeftEye, CameraEyeFlyCapture cameraRightEye, float frameRate)
        {
            if (cameraLeftEye is null) throw new ArgumentNullException(nameof(cameraLeftEye));
            if (cameraRightEye is null) throw new ArgumentNullException(nameof(cameraRightEye));

            var cameras = new EyeCollection<CameraEyeFlyCapture>( cameraLeftEye, cameraRightEye );

            // Disable adjusting frame rate during synchronization. Save the current value
            // to set it back at the end the way it was.
            var shouldAdjustFrameRate = cameraLeftEye.ShouldAdjustFrameRate;
            cameraLeftEye.ShouldAdjustFrameRate = cameraRightEye.ShouldAdjustFrameRate = false;

            var lastImages = new EyeCollection<ImageEye>(null, null);
            var secondLastImages = new EyeCollection<ImageEye>(null, null);
            var delays = new EyeCollection<double>(double.MaxValue, double.MaxValue);
            
            //
            // Empty the buffers of the cameras
            // 
            // To do that check the current timestamp in the camera and compare it with the timestamps in
            // the frames. If the difference is large it means that the frame was captured long time ago
            // and it must come from the buffer. Wait until frames from both cameras are captured without delay
            // and then proceed with the syncrhonization.
            var cleanBufferTasks = cameras.Select(cam => Task.Run(() =>
                 {
                     while (delays[Eye.Left] > 1 / frameRate || delays[Eye.Right] > 1 / frameRate)
                     {
                         // Retrieve the image from the buffer
                         lastImages[cam.WhichEye] = cam.GrabImageEye();
                         delays[cam.WhichEye] = cam.GetCurrentSeconds() - lastImages[cam.WhichEye].TimeStamp.Seconds;
                     }
                 })).ToArray();
            Task.WaitAll(cleanBufferTasks);

            //
            // Chose common time reference
            //
            // Get a common timestamp to sync both cameras to. Right now I chose to use the maximum time
            // that way we will slow down one camera to sync. It is easier than speed it up because we may be 
            // already running at max frame rate.
            var referenceframeNumber = new EyeCollection<ulong>(lastImages.Select(im => im.TimeStamp.FrameNumberRaw));
            var referenceTime = Math.Max(lastImages[Eye.Left].TimeStamp.Seconds, lastImages[Eye.Right].TimeStamp.Seconds);

            Trace.WriteLine(String.Format(
                "CAMERA SYNC BUFFERS CLEANED: delay between cameras = {0,3:F1}ms, Reported frame rates: left={1,3:F1}Hz right={2,3:F1}Hz",
                (lastImages[Eye.Left].TimeStamp.Seconds - lastImages[Eye.Right].TimeStamp.Seconds) * 1000,
                cameras[Eye.Left].FrameRate,
                cameras[Eye.Right].FrameRate));

            //
            // Now try to sync the two cameras
            //
            // We will change the frame rate of each camera until the delay between the time grabbed and the time
            // expected given the number of frames and the common refrence is less than the allowed value.

            var MAX_DELAY_ALLOWED = 0.001;
            var MAX_CHANGE_FRAMERATE = 5;
            var MIN_NUMBER_FRAMES_SINCE_CHANGE = 5;
            var MAX_NUMBER_FRAMES_TO_TRY  = (int)Math.Round(frameRate * 10);

            var counter = 0;
            var states = new EyeCollection<FrameRateState>(FrameRateState.Unknown, FrameRateState.Unknown);
            var framesSinceChange = new EyeCollection<int>(MIN_NUMBER_FRAMES_SINCE_CHANGE, MIN_NUMBER_FRAMES_SINCE_CHANGE);
            var delayToCorrect = new EyeCollection<double>(new double[2]);


            while (((states[Eye.Left] != FrameRateState.OnSync || states[Eye.Right] != FrameRateState.OnSync)) && counter < MAX_NUMBER_FRAMES_TO_TRY)
            {
                // Grab an image and measure its delay.
                foreach(var eye in new Eye[] { Eye.Left, Eye.Right})
                {
                    framesSinceChange[eye]++;

                    secondLastImages[eye] = lastImages[eye];

                    // Retrieve the image from the buffer
                    lastImages[eye] = cameras[eye].GrabImageEye();
                    delays[eye] = (lastImages[eye].TimeStamp.Seconds - referenceTime) - (double)(lastImages[eye].TimeStamp.FrameNumberRaw - referenceframeNumber[eye]) / frameRate;

                    // Check if we were not on sync but now are
                    // Case 1, delay is smaller than the max allowed
                    // Case 2, we were correcting and the sign changed
                    if ((states[eye] != FrameRateState.OnSync && Math.Abs(delays[eye]) < MAX_DELAY_ALLOWED) 
                        || (states[eye] == FrameRateState.Correcting && (delays[eye] * delayToCorrect[eye] < 0)))
                    {
                        cameras[eye].SetFrameRate(frameRate);
                        states[eye] = FrameRateState.OnSync;
                    }

                    // Check if we were on sync but not anymore or if we don't know and then adjust the frame rate.
                    // Case 1, we thought we were on sync but now the delay is too big.
                    // Case 2, we are correcting but delay is not being reduced.
                    // Case 3, unknown state
                    if ((states[eye] == FrameRateState.OnSync && (Math.Abs(delays[eye]) > MAX_DELAY_ALLOWED)) 
                        || (states[eye] == FrameRateState.Correcting && Math.Abs(delays[eye]) > Math.Abs(delayToCorrect[eye])) 
                        || (states[eye] == FrameRateState.Unknown))
                    {
                        if (framesSinceChange[eye] >= MIN_NUMBER_FRAMES_SINCE_CHANGE)
                        {
                            // speed up or slow down the camera
                            var newFrameRate = Math.Min(Math.Max((frameRate / (1.0 - 10 * delays[eye])), frameRate - MAX_CHANGE_FRAMERATE), frameRate + MAX_CHANGE_FRAMERATE);

                            // Change the frame rate
                            cameras[eye].SetFrameRate(newFrameRate);

                            // Reset the counter of frames since change
                            framesSinceChange[eye] = 0;
                            delayToCorrect[eye] = delays[eye];
                            states[eye] = FrameRateState.Correcting;
                        }
                    }
                }

                if (counter % 10 == 0) Trace.WriteLine(String.Format("CAMERA SYNC: {0,4:D} |  CAM0: {1,5:F2}<-{2,5:F2}ms {3,5:F1}Hz  |  CAM1: {4,5:F2}<-{5,5:F2}ms {6,5:F1}Hz | " + states[Eye.Left] + " " + states[Eye.Right],
                                        counter, (delays[Eye.Left] * 1000), delayToCorrect[Eye.Left] * 1000, cameras[Eye.Left].lastFrameRateRequested, (delays[Eye.Right] * 1000), delayToCorrect[Eye.Right] * 1000, cameras[Eye.Right].lastFrameRateRequested));
                counter++;
            }

            if (counter == MAX_NUMBER_FRAMES_TO_TRY) throw new OpenIrisException("Cameras cound not be synced, try to start again or umplug and try.");

            // Set the frame rate back to what we want it to be. Mostly for the case where cameras didn't sync. To not leave them with the wrong frame rate.
            cameras[Eye.Left].SetFrameRate(frameRate);
            cameras[Eye.Right].SetFrameRate(frameRate);

            // Reset the first and last frame numbers so the two cameras remain syncronized 
            cameras[Eye.Left].firstTimeStamp = lastImages[Eye.Left].TimeStamp;
            cameras[Eye.Right].firstTimeStamp = lastImages[Eye.Right].TimeStamp;
            cameras[Eye.Right].LastFrameNumber = cameras[Eye.Left].LastFrameNumber = 0;
            cameraLeftEye.ShouldAdjustFrameRate = cameraRightEye.ShouldAdjustFrameRate = shouldAdjustFrameRate;

            // Grab 5 frames to make sure the frame rate is updated. Otherwise we can get an error when we check the frame rate of the
            // image sources.
            for (int i = 0; i < 10; i++)
            {
                cameras[Eye.Left].GrabImageEye();
                cameras[Eye.Right].GrabImageEye();
            }

            // Get the final frame rate reported by the camera.
            cameras[Eye.Left].FrameRate = cameras[Eye.Left].camera.GetProperty(PropertyType.FrameRate).absValue;
            cameras[Eye.Right].FrameRate = cameras[Eye.Right].camera.GetProperty(PropertyType.FrameRate).absValue;

            Trace.WriteLine(String.Format(
                "CAMERA SYNC FINISHED: delay between cameras = {0,3:F1}ms, Reported frame rates: left={1,3:F2}Hz right={2,3:F2}Hz",
                (lastImages[Eye.Left].TimeStamp.Seconds - lastImages[Eye.Right].TimeStamp.Seconds)*1000,
                cameras[Eye.Left].FrameRate,
                cameras[Eye.Right].FrameRate));
        }

        public static uint[] GetListOfFlyCaptureCameras(string modelNameFilter = "")
        {
            var cameraList = new List<uint>();

            try
            {
                using var busManager = new ManagedBusManager();

                for (uint i = 0; i < busManager.GetNumOfCameras(); i++)
                {
                    var cameraPGRGuid = busManager.GetCameraFromIndex(i);

                    using var camera = new ManagedCamera();

                    camera.Connect(cameraPGRGuid);

                    var cameraInfo = camera.GetCameraInfo();

                    if (!cameraInfo.modelName.Contains(modelNameFilter))
                        continue;

                    cameraList.Add(cameraInfo.serialNumber);
                    camera.Disconnect();
                }
            }
            catch (FC2Exception ex)
            {
                Trace.WriteLine("ERROR getting camera info: " + ex.Message);
            }

            return cameraList.ToArray();
        }

        public static uint GetEmbeddedStrobePattern(ImageEye image)
        {
            return (image?.ImageSourceData as ManagedImage)?.imageMetadata.embeddedStrobePattern & 0x0000000F ?? 0;
        }

        #endregion Static Methods


        #region private methods

        private (double delay, FrameRateState state) frameRateCorrection;

        private enum FrameRateState
        {
            Unknown,
            OnSync,
            Correcting,
        }

        /// <summary>
        /// Sets the camera to buffer frames mode.
        /// </summary>
        /// <remarks>
        /// Images accumulate in the user buffer, and the oldest image is grabbed for handling before
        /// being discarded. This member can be used to guarantee that each image is seen. However,
        /// image processing time must not exceed transmission time from the camera to the buffer.
        /// Grabbing blocks if the camera has not finished transmitting the next available image. The
        /// buffer size is by the number of Buffers parameter in the FC2Config struct. Note that this
        /// mode is the equivalent of FlycaptureLockNext in earlier versions of the FlyCapture SDK.
        /// </remarks>
        /// <param name="bufferSize">Number of frames that can be buffered.</param>
        private void SetCameraBuffer(int bufferSize)
        {
            if (bufferSize > 0)
            {
                FC2Config config = camera.GetConfiguration();
                config.grabMode = GrabMode.BufferFrames;
                config.numBuffers = (uint)bufferSize;
                config.highPerformanceRetrieveBuffer = true;
                camera.SetConfiguration(config);
            }
            else
            {
                FC2Config config = camera.GetConfiguration();
                config.grabMode = GrabMode.DropFrames;
                camera.SetConfiguration(config);
            }
        }

        /// <summary>
        /// Gets the seconds that correspond with a given timestamp adjusting for the number of cycles that have passed.
        /// </summary>
        /// <param name="timeStamp">Timestamp to convert.</param>
        /// <param name="clyces128Sec">Number of cycles of 128 seconds to be added.</param>
        /// <returns></returns>
        public static double GetTimestampSeconds(TimeStamp timeStamp, int clyces128Sec)
        {
            if (timeStamp is null) throw new ArgumentNullException(nameof(timeStamp));

            return clyces128Sec * 128 
                + (double)timeStamp?.cycleSeconds 
                + (((double)timeStamp?.cycleCount + ((double)timeStamp?.cycleOffset / 3072.0)) / 8000.0); // seconds
        }

        /// <summary>
        /// Adjusts the frame rate of the camera to match the requested frame rate.
        /// </summary>
        /// <remarks>
        /// The frame rate of the point grey cameras is never exactly the frame rate requested. For
        /// instance, if you request 100Hz the camera might run at 99.8 Hz. Thus, if recording is
        /// long the delay of the last frame relative to the expect time will be long. Moreover, if
        /// two cameras are running they will drift from each other. To control for this, this
        /// function monitors the delay of the current frame with the expected frame time and adjusts
        /// the frame rate of the camera accordingly. Speeding it up or slowing it down temporarily.
        /// The result is a variable frame rate that on average gives the expected frame rate.
        /// </remarks>
        private void AdjustFrameRate()
        {
            // Calculate the delay of the current frame
            //
            // Positive delay means that the frames are coming later than they should. Negative delay
            // means that the frames are coming earlier than they should.
            var delay = (lastTimeStamp.Seconds - firstTimeStamp.Seconds) - ((lastTimeStamp.FrameNumberRaw - firstTimeStamp.FrameNumberRaw) / RequestedFrameRate);
            
            switch (frameRateCorrection.state)
            {
                case FrameRateState.Unknown:
                case FrameRateState.OnSync:
                    // If the delay is more than a forth of the sampling period Then we need to
                    // adjust the frequency.
                    if (Math.Abs(delay) > 1 / RequestedFrameRate / 4.0)
                    {
                        // speed up or slow down the camera
                        var newFrameRate = (float)(RequestedFrameRate / (1.0 - 2 * delay));

                        // Never go more than 1Hz above or below the requested frame rate
                        newFrameRate = (float)Math.Max(newFrameRate, RequestedFrameRate - 1);
                        newFrameRate = (float)Math.Min(newFrameRate, RequestedFrameRate + 1);

                        // Change the frame rate
                        lastFrameRateRequested = newFrameRate;
                        SetFrameRate(newFrameRate);
                        FrameRate = camera.GetProperty(PropertyType.FrameRate).absValue;

                        frameRateCorrection = (delay, FrameRateState.Correcting);
                    }
                    break;

                case FrameRateState.Correcting:
                    if (delay * frameRateCorrection.delay < 0)
                    {
                        // Change the frame rate
                        SetFrameRate((float)RequestedFrameRate);
                        FrameRate = camera.GetProperty(PropertyType.FrameRate).absValue;

                        frameRateCorrection = (delay, FrameRateState.OnSync);
                    }
                    break;

                default:
                    frameRateCorrection = (delay, FrameRateState.Unknown);
                    break;
            }
        }

        private double GetTimestampSeconds(TimeStamp timeStamp)
        {
            return GetTimestampSeconds(timeStamp, cycles128sec);
        }

        /// <summary>
        /// Sets the embedded info in the frame.
        /// </summary>
        private void SetEmbeddedInfo()
        {
            EmbeddedImageInfo embeddedInfo = camera.GetEmbeddedImageInfo();
            embeddedInfo.timestamp.onOff = true;
            embeddedInfo.brightness.onOff = true;
            embeddedInfo.exposure.onOff = true;
            embeddedInfo.frameCounter.onOff = true;
            embeddedInfo.gain.onOff = true;
            embeddedInfo.GPIOPinState.onOff = true;
            embeddedInfo.ROIPosition.onOff = true;
            embeddedInfo.shutter.onOff = true;
            embeddedInfo.strobePattern.onOff = true;
            embeddedInfo.whiteBalance.onOff = true;
            camera.SetEmbeddedImageInfo(embeddedInfo);
        }

        /// <summary>
        /// Sets the exposure of the camera.
        /// </summary>
        private void SetExposure()
        {
            if (!AutoExposure)
            {
                var prop = camera.GetProperty(PropertyType.Shutter);
                prop.absControl = true;
                prop.absValue = ShutterDuration;
                prop.autoManualMode = false;
                prop.onOff = true;
                camera.SetProperty(prop);

                prop = camera.GetProperty(PropertyType.Gain);
                prop.absControl = true;
                prop.absValue = Gain;
                prop.autoManualMode = false;
                prop.onOff = true;
                camera.SetProperty(prop);
            }
        }

        /// <summary>
        /// Sets the format7 image format (mainly ROI).
        /// </summary>
        private void SetFormat7Settings()
        {
            // Set the new Format 7 settings
            Format7ImageSettings newFmt7Settings = new Format7ImageSettings();
            switch (PixelFormat)
            {
                case CameraPixelFormat.Raw8:
                    newFmt7Settings.pixelFormat = FlyCapture2Managed.PixelFormat.PixelFormatRaw8;
                    break;
                case CameraPixelFormat.Mono8:
                    newFmt7Settings.pixelFormat = FlyCapture2Managed.PixelFormat.PixelFormatMono8;
                    break;
                default:
                    break;
            }


            switch (PixelMode)
            {
                case 0:
                    FrameSize = new Size(RequestedROI.Width, RequestedROI.Height);
                    newFmt7Settings.mode = Mode.Mode0;
                    newFmt7Settings.offsetX = Convert.ToUInt32(RequestedROI.X);
                    newFmt7Settings.offsetY = Convert.ToUInt32(RequestedROI.Y);
                    newFmt7Settings.width = Convert.ToUInt32(FrameSize.Width);
                    newFmt7Settings.height = Convert.ToUInt32(FrameSize.Height);
                    break;

                case 1:
                    FrameSize = new Size((int)(RequestedROI.Width / 2.0), (int)(RequestedROI.Height / 2.0));
                    newFmt7Settings.mode = Mode.Mode1;
                    newFmt7Settings.offsetX = Convert.ToUInt32(RequestedROI.X / 2.0);
                    newFmt7Settings.offsetY = Convert.ToUInt32(RequestedROI.Y / 2.0);
                    newFmt7Settings.width = Convert.ToUInt32(FrameSize.Width);
                    newFmt7Settings.height = Convert.ToUInt32(FrameSize.Height);
                    break;

                case 2:
                    FrameSize = new Size(RequestedROI.Width, (int)(RequestedROI.Height / 2.0));
                    newFmt7Settings.mode = Mode.Mode2;
                    newFmt7Settings.offsetX = Convert.ToUInt32(RequestedROI.X);
                    newFmt7Settings.offsetY = Convert.ToUInt32(RequestedROI.Y / 2.0);
                    newFmt7Settings.width = Convert.ToUInt32(FrameSize.Width);
                    newFmt7Settings.height = Convert.ToUInt32(FrameSize.Height);
                    break;

                default:
                    throw new Exception("Format7 settings are not good.");
            }

            // Query for available Format 7 modes
            var good = true;
            var fmt7PacketInfo = camera.ValidateFormat7Settings(newFmt7Settings, ref good);
            if (good)
            {
                camera.SetFormat7Configuration(newFmt7Settings, fmt7PacketInfo.recommendedBytesPerPacket);

                ROI = new Rectangle((int)newFmt7Settings.offsetX,
                                         (int)newFmt7Settings.offsetY,
                                         (int)newFmt7Settings.width,
                                         (int)newFmt7Settings.height);
            }
            else
            {
                throw new Exception("Format7 settings are not good.");
            }
        }

        /// <summary>
        /// Changes the frame rate of the camera.
        /// </summary>
        /// <param name="newFrameRate">Frame rate (Hz).</param>
        private double SetFrameRate(double newFrameRate)
        {
            lastFrameRateRequested = (float)newFrameRate;
            var reportedFrameRate = 0.0;
            var counter = 0;
            var MAX_TRIES = 10;

            while (Math.Abs(newFrameRate - reportedFrameRate) > 0.5 && counter < MAX_TRIES)
            {
                counter++;
                var prop = new CameraProperty
                {
                    type = PropertyType.FrameRate,
                    absControl = true,
                    absValue = (float)newFrameRate,
                    onePush = false,
                    autoManualMode = false,
                    onOff = true
                };
                camera.SetProperty(prop);
                camera.SetProperty(prop);
                camera.SetProperty(prop);

                // I have been dealing with a problem wehre the frame rate was not set
                // I tried setting it 3 times in a row but didn't help much. Now I think getting
                // te frame rate after does help.
                prop = camera.GetProperty(PropertyType.FrameRate);
                reportedFrameRate = (double)prop.absValue;
            }
            Trace.WriteLine($"SetFrameRate camera:{WhichEye}, requested={newFrameRate:F2}Hz, reported={reportedFrameRate:F2}Hz after {counter} tries");
            return reportedFrameRate;
        }


        #endregion private methods

    }
}