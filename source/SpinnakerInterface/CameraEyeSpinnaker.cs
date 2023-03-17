using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using OpenIris;
using OpenIris.ImageGrabbing;
using SpinnakerNET;
using static OpenIris.EyeTrackerExtentionMethods;

namespace SpinnakerInterface
{
#nullable enable

    class CameraEyeSpinnaker : CameraEye, IMovableImageEyeSource
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

        private const string TRIGGER_LINE = "Line2";
        private const string STROBE_OUT_LINE = "Line1";


        Vector2 maxROI_Offset, roiSize;

        #region Static Methods

        public static List<IManagedCamera> FindCameras(Eye whichEye, int numberOfCameras)
        {
            // TODO: add optional search by serial number string

            // Retrieve singleton reference to Spinnaker system object
            ManagedSystem system = new ManagedSystem();

            // Retrieve list of cameras from the system
            var camList_ = system.GetCameras();

            switch (whichEye, numberOfCameras, camList_.Count)
            {
                case (Eye.Both, 1, 1):
                    return camList_;
                case (Eye.Both, 2, 2):
                    return camList_;
                case (_, 1, 1) when whichEye == Eye.Left | whichEye == Eye.Right:
                    return camList_;
                default:
                    throw new Exception($"Need exactly {numberOfCameras} camera(s). {camList_.Count} FLIR Spinnaker compatible camera(s) found.");
            }
        }

        public static void BeginSynchronizedAcquisition(CameraEyeSpinnaker masterCam, CameraEyeSpinnaker slaveCam)
        {
            masterCam.isMaster = TriggerMode.Master;
            slaveCam.isMaster = TriggerMode.Slave;

            masterCam.Start();
            slaveCam.Start();
        }

        #endregion Static Methods

        #region constructor
        public CameraEyeSpinnaker(Eye whichEye, IManagedCamera camera, double frameRate, Rectangle roi)
        {
            cam = camera;

            WhichEye = whichEye;
            FrameRate = frameRate;
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

            cam.BeginAcquisition();
        }

        public override void Stop()
        {
            // TODO: potentially reset trigger settings here.
            cam.EndAcquisition();
        }

        protected override ImageEye GrabImageFromCamera()
        {
            IManagedImage rawImage = null;
            if (!cam.IsStreaming()) { return null; }
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

                    return new ImageEye(
                                     (int)rawImage.Width,
                                     (int)rawImage.Height,
                                     (int)rawImage.Stride,
                                     rawImage.DataPtr,
                                     timestamp)
                    {
                        WhichEye = WhichEye,
                        ImageSourceData = rawImage
                    };
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                rawImage?.Release();
                rawImage?.Dispose();
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
          + $"FrameID {CurrentFrameID}  #Grabbed {NumFramesGrabbed}  #Dropped {CurrentFrameID - NumFramesGrabbed}\n\n";

        // Center the pupil in the ROI. The centerPupil parameter gives the current pixel
        // location of the tracked pupil within the ROI, so we use it to offset the
        // current ROI to bring the pupil to the center. One liner!!
        public void Center(PointF centerPupil) => SetROI(GetROI() - ToVector2(centerPupil) + roiSize / 2);

        public void Move(MovementDirection direction)
        {
            var Offset = GetROI();  // Read values from Spinnaker API.

            switch (direction)
            {
                case MovementDirection.Down: Offset.Y += 4; break;
                case MovementDirection.Up: Offset.Y -= 4; break;
                case MovementDirection.Left: Offset.X -= 4; break;
                case MovementDirection.Right: Offset.X += 4; break;
            }
            SetROI(Offset);
        }
        #endregion public methods

        #region private methods
        // Initialize camera parameters. This sets up the camera to be a "slave" of the
        // frame triggers. Later, we pick one camera to be the "master".
        private void InitParameters()
        {
            // Make sure camera is really stopped.
            cam.BeginAcquisition();
            cam.EndAcquisition();

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
            Debug.WriteLine($"Centering ROI. FrameMax {maxFrameSize}, ROI_SIZE {roiSize}");
            //Center the ROI in the middle of the physical camera frame.
            SetROI(maxROI_Offset / 2);

            // Image Chunk data, saved with each video frame.
            cam.ChunkModeActive.Value = true;
            cam.ChunkSelector.FromString("FrameID");
            cam.ChunkEnable.Value = true;

            // This saves the status of all 4 GPIO digital lines.
            try
            {
                cam.ChunkSelector.FromString("ExposureEndLineStatusAll");
                cam.ChunkEnable.Value = true;
            }
            catch { }

            //# Gain settings.
            cam.GainAuto.FromString("Off");
            cam.Gain.Value = 9;
            //# Exposure settings.
            cam.ExposureAuto.FromString("Off");

            //# Set to 500uSec less than frame interval.
            cam.AutoExposureExposureTimeUpperLimit.Value = 1e6 / FrameRate - 500;
            cam.ExposureTime.Value = cam.AutoExposureExposureTimeUpperLimit.Value - 100;

            cam.AcquisitionFrameRateEnable.Value = true;
            cam.AcquisitionFrameRate.Value = FrameRate;
        }
        private void InitParameters_TriggerSettings()
        {
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
            Offset = Max(Vector2.Zero, Min(maxROI_Offset, Round(Offset / 4) * 4));
            (cam.OffsetX.Value, cam.OffsetY.Value) = ((long)Offset.X, (long)Offset.Y);
        }
        private Vector2 GetROI() => new Vector2(cam.OffsetX.Value, cam.OffsetY.Value);

        #endregion private methods
    }
}
