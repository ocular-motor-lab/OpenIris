using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;

// A hack to let us quickly attach to a GUI menu item.
//using OpenIris.UI;

using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace SpinnakerInterface
{
    class SpinnakerCameraEye : CameraEye, IMovableImageEyeSource
    {
        // Static members, to keep track of all cameras.
        public static List<SpinnakerCameraEye> CAMLIST = new List<SpinnakerCameraEye>();
        public static int NCAMS => CAMLIST.Count();

        // One "MASTER" camera is chosen "arbitrarily" to generate hardware triggers for
        // all cameras.
        public static SpinnakerCameraEye MASTERCAM = null;

        static bool __TriggersEnabled = false;
        public static bool TriggersEnabled => __TriggersEnabled;
        public static void EnableTriggers(bool Enable) => MASTERCAM.EnableMasterTriggers(Enable);
        //public static void ToggleTriggers(object sender, System.EventArgs e) => EnableTriggers(!TriggersEnabled);
        public static void ToggleTriggers() => EnableTriggers(!TriggersEnabled);

        public static List<SpinnakerCameraEye> EnumerateCameras()
        {
            // Retrieve singleton reference to Spinnaker system object
            ManagedSystem system = new ManagedSystem();

            // Get current library version
            // version = self.system.GetLibraryVersion()
            // print('FLIR Spinnaker Library version: %d.%d.%d.%d' % (version.major, version.minor, version.type, version.build))

            // Retrieve list of cameras from the system
            var cam_list = system.GetCameras();

            if (cam_list.Count < 2)
            {
                throw new Exception($"NEED TWO CAMERAS!! Found {cam_list.Count} FLIR Spinnaker compatible camera(s).");
            }

            Trace.WriteLine($"Found {cam_list.Count} cameras. Calling cam.Init()...");
            CAMLIST.Add(new SpinnakerCameraEye(cam_list[0], Eye.Left));
            CAMLIST.Add(new SpinnakerCameraEye(cam_list[1], Eye.Right));

            //JOM camList[0].CameraOrientation = CameraOrientation.Rotated180;
            //JOM camList[1].CameraOrientation = CameraOrientation.Rotated180;

            // Pick one of the cameras (MUST be a Blackfly) to be the MASTER.
            MASTERCAM = null;
            foreach (var CAM in CAMLIST)
                if (CAM.cam.DeviceModelName.Value.Contains("Blackfly"))
                {
                    MASTERCAM = CAM;
                    break;
                }

            // Attach menu item to our Trigger Toggle function.
            // TODO: UPDATE :: OpenIris.UI.EyeTrackerGuiViewModel.PauseMenuItem.Click += new System.EventHandler( SpinnakerCameraEye.ToggleTriggers);

            return CAMLIST;
        }

        public static void EndSynchronizedAcquisition()
        {
            if (MASTERCAM == null) return;
            MASTERCAM.EnableMasterTriggers(false);   // Stop the triggers.
            Thread.Sleep(50);
            foreach (var CAM in CAMLIST)
                try { CAM.cam.EndAcquisition(); } catch { }
        }

        public static void BeginSynchronizedAcquisition()
        {
            EndSynchronizedAcquisition();  // Make sure everyone is stopped.

            CAMLIST[0].Start();
            CAMLIST[1].Start();
            MASTERCAM.SetMaster();
            MASTERCAM.cam.BeginAcquisition();
            MASTERCAM.EnableMasterTriggers(true);

            //foreach (var CAM in camList) CAM.AutoExposureOnce();
        }

        // =========================================================================================
        // Below here are all instance members.

        int Count = 0;

        IManagedCamera cam;
        string CamModelName = "";

        // Note that "CurrentFrameID" has FirstFrameID subtracted out of it, and +1 added, so it is "one based"
        // (i.e., there is no "frame 0"), so it can be directly compared to NumFramesGrabbed.
        bool IsFirstFrame = true;
        ulong FirstFrameID, CurrentFrameID;  // CurrentFrameID is based on the internal camera hardware frame counter.
        ulong NumFramesGrabbed = 0;

        // If this Info property is implemented, then it is evaluated periodically by the GUI, and
        // the string it returns is displayed on the Timing tab, if Debug is enabled.
        //
        // Handy for showing a continuously updated Debug string in the Timing tab. Can show things
        // like camera frame numbers, dropped frames, whatever. MUST enable Debug in the Settings in
        // order for the Debug and Timing tabs to show up in the user interface. Every camera gets
        // its own string, so we must distinguish which camera this pertains to.
        public override object Info =>
            $"This string shows up in Timing tab!! [{WhichEye}{(ISMASTER ? "[Master]" : "")}: {CamModelName}] (updated periodically {Count++})\n"
          + $"FrameID {CurrentFrameID}  #Grabbed {NumFramesGrabbed}  #Dropped {CurrentFrameID - NumFramesGrabbed}\n\n";

        bool ISMASTER => this == MASTERCAM;

        public SpinnakerCameraEye(IManagedCamera CAM, Eye which)
        {
            this.cam = CAM;
            this.WhichEye = which;
            Init();
        }

        public void Init()
        {
            cam.Init();
            CamModelName = cam.DeviceModelName.Value;
        }

        public override void Start()
        {
            if (cam == null) return;

            //self.InitParameters()
            //self.InitROI()

            //FrameRate = cam.AcquisitionFrameRate;
            FrameRate = FRAME_RATE;

            //cam.AcquisitionFrameRate.Value = FrameRate;
            InitParameters();
            cam.BeginAcquisition();
        }

        public override void Stop()
        {
            cam.EndAcquisition();
        }

        const string TRIGGER_LINE = "Line2";
        const string STROBE_OUT_LINE = "Line1";
        public void EnableMasterTriggers(bool Enable)
        {
            if (this != MASTERCAM) return;

            if (Enable)
                // Allow internal camera triggering, so this camera generates frame triggers.
                cam.TriggerMode.FromString("Off");
            else
            {
                // Disable camera triggering.
                cam.TriggerSource.FromString("Software");
                cam.TriggerMode.FromString("On");
            }
            __TriggersEnabled = Enable;
        }

        //const double FRAME_RATE = 499;
        //const double FRAME_RATE = 100;
        const double FRAME_RATE = 250;
        const int ROI_XSIZE = 400, ROI_YSIZE = 300;

        private Vector2 FRAME_SIZE_MAX;  // These are read in from the camera, and stored for convenience.
        readonly Vector2 ROI_SIZE = new Vector2(ROI_XSIZE, ROI_YSIZE);
        Vector2 ROI_OFFSET_MAX;

        Vector2 Round(Vector2 V) => new Vector2((float)Math.Round(V.X), (float)Math.Round(V.Y));
        Vector2 Max(Vector2 V1, Vector2 V2) => Vector2.Max(V1, V2);
        Vector2 Min(Vector2 V1, Vector2 V2) => Vector2.Min(V1, V2);

        // Make sure ROI is a multiple of 4 pixels, and is properly bounded between 0 and maxROI_Offset.
        void SetROI(Vector2 Offset)
        {
            Offset = Max(Vector2.Zero, Min(ROI_OFFSET_MAX, Round(Offset / 4) * 4));
            (cam.OffsetX.Value, cam.OffsetY.Value) = ((long)Offset.X, (long)Offset.Y);
        }
        Vector2 GetROI() => new Vector2(cam.OffsetX.Value, cam.OffsetY.Value);
        Vector2 ToVector2(PointF P) => new Vector2(P.X, P.Y);

        // Center the ROI in the middle of the physical camera frame.
        void CenterROI() => SetROI((FRAME_SIZE_MAX - ROI_SIZE) / 2);

        // Center the pupil in the ROI. The centerPupil parameter gives the current pixel
        // location of the tracked pupil within the ROI, so we use it to offset the
        // current ROI to bring the pupil to the center. One liner!!
        public void Center(PointF centerPupil) => SetROI(GetROI() - ToVector2(centerPupil) + ROI_SIZE/2);

        public void Move(MovementDirection direction)
        {
            var Offset = GetROI();  // Read values from Spinnaker API.

            switch (direction)
            {
                case MovementDirection.Down:  Offset.Y += 4; break;
                case MovementDirection.Up:    Offset.Y -= 4; break;
                case MovementDirection.Left:  Offset.X -= 4; break;
                case MovementDirection.Right: Offset.X += 4; break;
            }
            SetROI(Offset);
        }

        // Initialize camera parameters. This sets up the camera to be a "slave" of the
        // frame triggers. Later, we pick one camera to be the "master".
        public void InitParameters()
        {
            // Make sure camera is really stopped.
            cam.BeginAcquisition();
            cam.EndAcquisition();

            IsFirstFrame = true;  // Remember to grab first frame number and save it.

            cam.GammaEnable.Value = false;

            // Image frame/format settings.
            cam.AcquisitionFrameRateEnable.Value = false;
            cam.BinningHorizontal.Value = 1;
            cam.BinningVertical.Value = 1;
            cam.OffsetX.Value = 0;
            cam.OffsetY.Value = 0;
            cam.Width.Value = ROI_XSIZE;
            cam.Height.Value = ROI_YSIZE;
            FRAME_SIZE_MAX = new Vector2(cam.WidthMax.Value, cam.HeightMax.Value);
            ROI_OFFSET_MAX = FRAME_SIZE_MAX - ROI_SIZE;
            Debug.WriteLine($"Centering ROI. FrameMax {FRAME_SIZE_MAX}, ROI_SIZE {ROI_SIZE}");
            CenterROI();

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

            // USB 2.0 is limited to ~43M, so try that first.
            //cam.DeviceLinkThroughputLimit.SetValue(int(43e6));
            //try: cam.DeviceLinkThroughputLimit.SetValue(int(500e6));
            //except: pass


            //# Trigger Settings
            cam.LineSelector.FromString(TRIGGER_LINE);
            try { cam.V3_3Enable.Value = false; } catch { }
            cam.LineMode.FromString("Input");
            cam.LineInverter.Value = false;
            try { cam.LineInputFilterSelector.FromString("Deglitch"); } catch { }
            cam.TriggerSelector.FromString("FrameStart");
            cam.TriggerSource.FromString(TRIGGER_LINE);
            cam.TriggerActivation.FromString("RisingEdge");
            try { cam.TriggerOverlap.FromString("ReadOut"); } catch { }

            cam.TriggerMode.FromString("On");
            //cam.TriggerMode.FromString("Off");

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

            //# Exposure settings.
            cam.AcquisitionMode.FromString("Continuous");
            cam.GainAuto.FromString("Off");
            cam.ExposureAuto.FromString("Off");

            //# Set to 500uSec less than frame interval.
            cam.AutoExposureExposureTimeUpperLimit.Value = 1e6 / FRAME_RATE - 500;
            //#cam.AutoExposureExposureTimeUpperLimit.SetValue(1e6/FRAME_RATE - 2000)

            cam.ExposureTime.Value = cam.AutoExposureExposureTimeUpperLimit.Value - 100;
        }

        public void AutoExposureOnce()
        {
            // Do one-time Auto Exposure setting (lasts a few seconds).
            cam.GainAuto.FromString("Once");
            cam.ExposureAuto.FromString("Once");
        }

        public void SetMaster(bool Enable = false)
        {
            cam.EndAcquisition();         // Make sure camera is not acquiring images.
            EnableMasterTriggers(false);  // And disable trigger until we are ready!

            cam.LineSelector.FromString(TRIGGER_LINE);
            cam.LineMode.FromString("Output");
            cam.LineSource.FromString("ExposureActive");

            cam.AcquisitionMode.FromString("Continuous");
            cam.AcquisitionFrameRateEnable.Value = true;
            cam.AcquisitionFrameRate.Value = FRAME_RATE;

            if (Enable)
                EnableMasterTriggers(true);
        }

        protected override ImageEye GrabImageFromCamera()
        {
            if (!cam.IsStreaming())
                return null;

            //
            // Retrieve next received image
            //
            // *** NOTES ***
            // Capturing an image houses images on the camera buffer. 
            // Trying to capture an image that does not exist will 
            // hang the camera.
            //
            // Using-statements help ensure that images are released.
            // If too many images remain unreleased, the buffer will
            // fill, causing the camera to hang. Images can also be
            // released manually by calling Release().
            // 

            // 2020/08/14 Dale - We need to use try/except here, because GetNextImage() will
            // throw an exception if it is called after (or during?) EndAcquisition().
            //
            //   https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement
            //
            // We expand the "using" statement into the equivalent try/finally syntax, and add "except"
            // to catch the exception and return null.

            //using (IManagedImage rawImage = cam.GetNextImage())

            IManagedImage rawImage = null;
            try
            {
                rawImage = cam.GetNextImage();

                // Ensure image completion
                //
                // *** NOTES ***
                //
                // Images can easily be checked for completion. This should be done whenever a
                // complete image is expected or required. Alternatively, check image status for a
                // little more insight into what happened.
                //
                if (rawImage.IsIncomplete)
                {
                    Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                }
                else
                {
                    //
                    // Convert image to mono 8
                    //
                    // *** NOTES ***
                    // Images can be converted between pixel formats by using the appropriate
                    // enumeration value. Unlike the original image, the converted one does not need
                    // to be released as it does not affect the camera buffer.
                    using (IManagedImage convertedImage = rawImage)
                    {
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
                        var timestamp = new ImageEyeTimestamp
                        {
                            FrameNumber = CurrentFrameID,
                            FrameNumberRaw = RawFrameID,
                            Seconds = rawImage.TimeStamp / 1e9
                        };

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

            }
            catch
            {
                return null;
            }
            finally
            {
                rawImage?.Dispose();
            }

            return null;
        }
    }
}
