using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using static System.Net.Mime.MediaTypeNames;
using static OpenIris.EyeTrackerExtentionMethods;
using System.Collections.Concurrent;

namespace SpinnakerInterface
{
#nullable enable

    // OPEN IRIS VERSION NOT DALES

    class CameraEyeSpinnaker : CameraEye, IMovableImageEyeSource, IVariableExposureImageEyeSource
    {
        public enum TriggerMode
        {
            Default,
            Master,
            Slave,
        }

        private IManagedCamera cam;
        private string camModelName = "";
        private TriggerMode isMaster;
        // Note that "CurrentFrameID" has FirstFrameID subtracted out of it, and +1 added, so it is "one based"
        // (i.e., there is no "frame 0"), so it can be directly compared to NumFramesGrabbed.
        private bool IsFirstFrame = true;
        private ulong FirstFrameID, CurrentFrameID;  // CurrentFrameID is based on the internal camera hardware frame counter.
        private ulong NumFramesGrabbed = 0;
        private long lastExposureEndLineStatusAll;
        private long lastLineStatusAll;
        private ImageEyeTimestamp lastTimestamp;


        private string line0Status = "FALSE";


        private const string TRIGGER_LINE = "Line2";
        private const string STROBE_OUT_LINE = "Line1";
        private const string INPUT_LINE = "Line0";

        private string errortest;

        Vector2 maxROI_Offset, roiSize;
        public Vector2 MaxROI_Offset { get { return maxROI_Offset; } set { maxROI_Offset = value; } }

        double maxGain;

        public Point Offset { get {
                lock (_roiLock)
                {
                    var offset = new Point(_lastOffset.X, _lastOffset.Y);
                }
                return offset;
         } }

        private Point offset;

        // --- Thread-Safe ROI Update Fields ---
        private readonly object _roiLock = new object();
        private Point _roiChangeDelta = Point.Empty;
        private bool _centerRequested = false;
        private PointF _pupilCenterForCentering;
        private Point _lastOffset = Point.Empty;
        public double Gain { get { return gain; } set { gain = value; } }
        private double gain;

        #region Static Methods

        public static List<IManagedCamera> FindCameras(int numberOfRequiredCameras, Eye whichEye, string? leftEyeCamSerialNum, string? rightEyeCamSerialNum)
        {
            // Retrieve singleton reference to Spinnaker system object
            ManagedSystem system = new ManagedSystem();

            // Retrieve list of cameras from the system
            var camList_ = system.GetCameras();
            if (numberOfRequiredCameras > camList_.Count)
                throw new Exception($"Need at least {numberOfRequiredCameras} camera(s). {camList_.Count} FLIR Spinnaker compatible camera(s) found.");

            // output found cameras
            List<IManagedCamera> foundCameras = new List<IManagedCamera>();

            // if the selected serial number didn't found use the default cameras' indices (0 for left, 1 for right)
            if (leftEyeCamSerialNum != null && camList_.GetBySerial(leftEyeCamSerialNum) == null)
            {
                Trace.WriteLine($"Warning: Didn't find (left) camera with selected serial number of: {leftEyeCamSerialNum}");
                leftEyeCamSerialNum = null;
            }
            if (rightEyeCamSerialNum != null && camList_.GetBySerial(rightEyeCamSerialNum) == null)
            {
                Trace.WriteLine($"Warning: Didn't find right camera with selected serial number of: {rightEyeCamSerialNum}");
                rightEyeCamSerialNum = null;
            }

            // Assign the correct camera to whichEye
            switch (whichEye, numberOfRequiredCameras)
            {
                case (Eye.Both, 2):
                    foundCameras.Add(leftEyeCamSerialNum == null || rightEyeCamSerialNum == null ? camList_[0] : camList_.GetBySerial(leftEyeCamSerialNum));
                    foundCameras.Add(rightEyeCamSerialNum == null || leftEyeCamSerialNum == null ? camList_[1] : camList_.GetBySerial(rightEyeCamSerialNum));
                    return foundCameras;
                case (_, 1):
                    foundCameras.Add(leftEyeCamSerialNum == null ? camList_[0] : camList_.GetBySerial(leftEyeCamSerialNum));
                    return foundCameras;
                default:
                    throw new Exception($"Error: Dual camera selected for tracking single eye. For tracking single eye, please use Spinnaker Single Camera.");
            }
        }

        public static void BeginSynchronizedAcquisition(CameraEyeSpinnaker masterCam, CameraEyeSpinnaker slaveCam)
        {
            masterCam.isMaster = TriggerMode.Master;
            slaveCam.isMaster = TriggerMode.Slave;

            masterCam.Start();
            slaveCam.Start();

            slaveCam.cam.BeginAcquisition();
            masterCam.cam.BeginAcquisition();
        }

        #endregion Static Methods

        #region constructor
        public CameraEyeSpinnaker(Eye whichEye, IManagedCamera camera, double frameRate, int gain, Rectangle roi)
        {
            cam = camera;

            WhichEye = whichEye;
            FrameRate = frameRate;
            this.gain = gain;
            FrameSize = roi.Size;

            isMaster = TriggerMode.Default;
            cam.Init();
        }

        #endregion constructor


        #region public methods

        public override void Start()
        {
            InitParameters();
            InitParameters_TriggerSettings();
        }

        public override void Stop()
        {
            // TODO: potentially reset trigger settings here.
            cam.EndAcquisition();
        }

        protected override ImageEye GrabImageFromCamera()
        {
            // Apply any pending ROI changes from the UI thread. This must be done
            // here, in the "safe zone" between grabbing frames.
            SyncROI();

            ImageEye? newImageEye = null;
            IManagedImage rawImage = null;
            if (!cam.IsStreaming()) { 
                return null; 
            }
            try
            {
                using (rawImage = cam.GetNextImage())
                {
                    if (rawImage.IsIncomplete)
                    {
                        return null;
                    }

                    ++NumFramesGrabbed;

                    ulong RawFrameID = rawImage.FrameID;
                    // Subtract out the initial hardware frame number, so we always start at Hardware Frame 0.
                    if (IsFirstFrame)
                    {
                        FirstFrameID = RawFrameID;
                        IsFirstFrame = false;
                    }

                    CurrentFrameID = RawFrameID - FirstFrameID + 1;  // First frame ID will be 1, not 0.

                    // Build the new timestamp
                    var timestamp = new ImageEyeTimestamp(
                        seconds: rawImage.TimeStamp / 1e9,
                        frameNumber: CurrentFrameID,
                        frameNumberRaw: RawFrameID);

                    lastExposureEndLineStatusAll = rawImage.ChunkData.ExposureEndLineStatusAll;
                    //lastLineStatusAll = rawImage.ChunkData.LineStatusAll; //it is not available

                    lastTimestamp = timestamp;
                    unsafe
                    {
                        Buffer.MemoryCopy((byte*)rawImage.DataPtr + rawImage.Width * rawImage.Height, (byte*)rawImage.DataPtr, rawImage.ManagedData.Length, rawImage.ManagedData.Length - rawImage.Width * rawImage.Height);
                    }

                    newImageEye = new ImageEye(
                                     (int)rawImage.Width,
                                     (int)rawImage.Height,
                                     (int)rawImage.Stride,
                                     rawImage.DataPtr,
                                     timestamp)
                    {
                        WhichEye = WhichEye,
                        ImageSourceData = (lastExposureEndLineStatusAll, rawImage),

                    };

                    return newImageEye;
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine("Camera ERROR " + ex);
                throw;
            }
            finally
            {
              // rawImage?.Release();
              // rawImage?.Dispose();
            }
        }

        // If this Info property is implemented, then it is evaluated periodically by the GUI, and
        // the string it returns is displayed on the Timing tab, if Debug is enabled.
        //
        // Handy for showing a continuously updated Debug string in the Timing tab. Can show things
        // like camera frame numbers, dropped frames, whatever. MUST enable Debug in the Settings in
        // order for the Debug and Timing tabs to show up in the user interface. Every camera gets
        // its own string, so we must distinguish which camera this pertains to.
        public override object Info =>
            $"This string shows up in Timing tab!! [{WhichEye}{(isMaster == TriggerMode.Master ? "[Master]" : "")}: {camModelName}]\n"
          + $"FrameID {CurrentFrameID}  #Grabbed {NumFramesGrabbed}  #Dropped {CurrentFrameID - NumFramesGrabbed}\n\n"
          + $"GPIO LineStatusAll {lastLineStatusAll}" + $"  GPIO ExposureEndLineStatusAll {lastExposureEndLineStatusAll} \n\n"
          + $"Seconds {lastTimestamp.Seconds}\n\n";

        // Center the pupil in the ROI. The centerPupil parameter gives the current pixel
        // location of the tracked pupil within the ROI, so we use it to offset the
        // current ROI to bring the pupil to the center.
        public void Center(PointF centerPupil)
        {
            // The UI thread now only writes its intent to a shared variable.
            // It does NOT read GetROI() from the camera.
            lock (_roiLock)
            {
                _centerRequested = true;
                _pupilCenterForCentering = centerPupil;
            }
        }

        public void Move(MovementDirection direction)
        {
            // The UI thread simply accumulates the requested changes.
            // It does NOT read GetROI() from the camera.
            lock (_roiLock)
            {
                switch (direction)
                {
                    case MovementDirection.Down: _roiChangeDelta.Y += 4; break;
                    case MovementDirection.Up: _roiChangeDelta.Y -= 4; break;
                    case MovementDirection.Left: _roiChangeDelta.X -= 4; break;
                    case MovementDirection.Right: _roiChangeDelta.X += 4; break;
                }
            }
        }
        #endregion public methods

        #region private methods
        // Initialize camera parameters. This sets up the camera to be a "slave" of the
        // frame triggers. Later, we pick one camera to be the "master".
        private void InitParameters()
        {
            camModelName = cam.DeviceModelName.Value;

            IsFirstFrame = true;  // Remember to grab first frame number and save it.

            cam.GammaEnable.Value = false;

            // Image frame/format settings.
            cam.AcquisitionMode.FromString("Continuous");
            cam.AcquisitionFrameRateEnable.Value = true;
            cam.BinningHorizontal.Value = 1;
            cam.BinningVertical.Value = 1;
            cam.OffsetX.Value = 0;
            cam.OffsetY.Value = 0;
            cam.Width.Value = FrameSize.Width;
            cam.Height.Value = FrameSize.Height;

            Vector2 maxFrameSize = new Vector2(cam.WidthMax.Value, cam.HeightMax.Value);
            roiSize = new Vector2(FrameSize.Width, FrameSize.Height);
            maxROI_Offset = maxFrameSize - roiSize;

            maxGain = cam.Gain.Max;

            Debug.WriteLine($"Centering ROI. FrameMax {maxFrameSize}, ROI_SIZE {roiSize}");
            //Center the ROI in the middle of the physical camera frame.
            SetROI(maxROI_Offset / 2);

            // Image Chunk data, saved with each video frame.
            cam.ChunkSelector.FromString("FrameID");
            cam.ChunkEnable.Value = true;
            cam.ChunkModeActive.Value = true;

            // This saves the status of all 4 GPIO digital lines.
            try
            {
                cam.ChunkSelector.FromString("ExposureEndLineStatusAll");
                cam.ChunkEnable.Value = true;
                cam.ChunkModeActive.Value = true;

                cam.ChunkSelector.FromString("LineStatusAll");
                cam.ChunkEnable.Value = true;
                cam.ChunkModeActive.Value = true;

            }
            //catch(Exception e) { errortest = e.ToString(); }
            catch { }
            //# Gain settings.
            cam.GainAuto.FromString("Off");
            cam.Gain.Value = Gain;
            //# Exposure settings.
            cam.ExposureAuto.FromString("Off");
            //# Set to 500uSec less than frame interval.
            cam.ExposureTime.Value = 1e6 / FrameRate - 500;

            cam.AcquisitionFrameRateEnable.Value = true;
            cam.AcquisitionFrameRate.Value = FrameRate;
        }
        private void InitParameters_TriggerSettings()
        {
            //camera firmware version
            Trace.WriteLine($"{isMaster} ({WhichEye}) Camera Firmware Version: " + cam.DeviceFirmwareVersion.ToString());
            Trace.WriteLine($"{isMaster} ({WhichEye}) Camera Model Name: " + cam.DeviceModelName.ToString());

            switch (isMaster)
            {
                case TriggerMode.Master:
                    cam.LineSelector.FromString(TRIGGER_LINE);
                    cam.LineMode.FromString("Output");
                    cam.LineSource.FromString("ExposureActive");

                    // Allow internal camera triggering, so this camera generates frame triggers.
                    cam.TriggerMode.FromString("Off");

                    break;
                case TriggerMode.Slave:
                    //# Trigger Settings
                    // Slave must have higher aquisition rate that master or else risk falling behind due to clock skew
                    //cam.AcquisitionFrameRateEnable.Value = false;
                    cam.AcquisitionFrameRate.Value = FrameRate + 50;

                    cam.LineSelector.FromString(TRIGGER_LINE);
                    try { cam.V3_3Enable.Value = false; } catch { }
                    cam.LineMode.FromString("Input");
                    cam.LineInverter.Value = false;
                    try { cam.LineInputFilterSelector.FromString("Deglitch"); } catch { }

                    cam.TriggerSource.FromString(TRIGGER_LINE);
                    cam.TriggerSelector.FromString("FrameStart");
                    cam.TriggerActivation.FromString("RisingEdge");
                    try { cam.TriggerOverlap.FromString("ReadOut"); } catch { }
                    cam.TriggerMode.FromString("On");

                    //# MUST make sure all non-master cameras set strobeOutLine to high.
                    cam.LineSelector.FromString(STROBE_OUT_LINE);
                    cam.LineInverter.Value = false;

                    //# For Firefly, set to Input. For Blackfly, this will be an error,
                    //# since the line is hard-wired as an output.        
                    try { cam.LineMode.FromString("Input"); } catch { }


                    //# Set line 0 as input for receiving ttl pulses for synchornization
                    try
                    {
                        cam.LineSelector.FromString(INPUT_LINE);
                        cam.LineMode.FromString("Input");
                        cam.LineInverter.Value = false;

                    }
                    catch { }
                    //# For Firefly, set to Input. For Blackfly, this will be an error,
                    //# since the line is hard-wired as an output.        
                    try { cam.LineMode.FromString("Input"); } catch { }

                    try
                    {
                        //# For Blackfly, make sure the output is HIGH, so that only
                        //# the master is pulling the line low.
                        cam.LineSource.FromString("UserOutput3");
                        cam.UserOutputSelector.Value = 3;
                        cam.UserOutputValue.Value = true;
                    }
                    catch { }
                    break;
                case TriggerMode.Default:
                    // Allow internal camera triggering, so this camera generates frame triggers.
                    cam.TriggerMode.FromString("Off");
                    cam.BeginAcquisition();
                    break;
                default:
                    break;
            }


            // TODO:
            // DO WE NEED THIS? cam.TriggerSource.FromString("Software");
        }

        // Make sure ROI is a multiple of 4 pixels, and is properly bounded between 0 and maxROI_Offset.
        private void SetROI(Vector2 Offset)
        {
            Offset = Max(Vector2.Zero, Min(maxROI_Offset, Round(Offset / 4) * 4)); // force to be a multiple of 4

            offset = new Point((int)Offset.X, (int)Offset.Y);

            cam.OffsetX.Value = offset.X;
            cam.OffsetY.Value = offset.Y;

            _lastOffset = offset;

            Trace.WriteLine($"Camera {WhichEye} ROI moved to ({offset.X},{offset.Y}).");
        }

        /// <summary>
        /// Synchronizes the camera's ROI with any pending changes from the UI thread.
        /// </summary>
        /// <remarks>
        /// MOTIVATION FOR THIS FUNCTION:
        /// A hardware-level race condition can occur if we try to change camera parameters (like the ROI)
        /// at the same time the camera's firmware is acting on a high-priority hardware trigger. This is
        /// especially problematic for the slave camera, which relies on unpredictable external triggers.
        ///
        /// The crash happens when a call to read a camera parameter (e.g., GetROI()) is immediately
        /// followed by a hardware trigger, which is then followed by a call to write a parameter (e.g., SetROI()).
        /// The firmware enters an unstable state and fails.
        ///
        /// This function solves the problem by ensuring that ALL communication with the camera hardware
        /// (both reading and writing of parameters) happens exclusively within this single, atomic block.
        /// This block is only ever called from the camera's own grabbing thread, during the "safe zone"
        /// after one frame has been acquired and before the next one is requested. This completely
        /// eliminates any possibility of a conflict with an incoming hardware trigger.
        /// </remarks>
        private void SyncROI()
        {
            Point delta;
            bool shouldCenter;
            PointF pupilCenter = PointF.Empty;

            // Step 1: Atomically get the pending changes requested by the UI thread.
            lock (_roiLock)
            {
                delta = _roiChangeDelta;
                shouldCenter = _centerRequested;
                if (shouldCenter)
                {
                    pupilCenter = _pupilCenterForCentering;
                }
                // Reset the shared variables for the next command.
                _roiChangeDelta = Point.Empty;
                _centerRequested = false;
            }

            // Step 2: If there's a change pending, perform the entire Read-Calculate-Write
            // operation as a single, uninterrupted unit.
            if (delta != Point.Empty || shouldCenter)
            {
                // READ from the hardware.
                var currentOffset = new Vector2(cam.OffsetX.Value, cam.OffsetY.Value);

                // CALCULATE the new state based on the read value and the UI's intent.
                if (shouldCenter)
                {
                    // A center command overrides any pending move commands.
                    currentOffset = currentOffset + ToVector2(pupilCenter) - roiSize / 2;
                }
                else
                {
                    currentOffset.X += delta.X;
                    currentOffset.Y += delta.Y;
                }

                // WRITE the final state back to the hardware.
                SetROI(currentOffset);
            }
        }

        public bool IncreaseExposure()
        {
            // check the upper limit
            if (gain + 1 <= maxGain)
            {
                gain += 1;
                cam.Gain.Value = gain;
                Trace.WriteLine($"Camera {WhichEye} gain set to {gain}.");
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ReduceExposure()
        {
            // check the lower limit
            if (gain - 1 > 0)
            {
                gain -= 1;
                cam.Gain.Value = gain;
                Trace.WriteLine($"Camera {WhichEye} gain set to {gain}.");
                return true;
            }
            else
            {
                return false;
            }
        }

        public double SetGain(double gain_input)
        {
            gain = gain_input;
            cam.Gain.Value = gain;
            return gain;
        }

        #endregion private methods
    }
}
