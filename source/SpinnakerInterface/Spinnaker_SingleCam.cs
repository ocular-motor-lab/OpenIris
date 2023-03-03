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

using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.ComponentModel.Composition;
using System.Runtime;
using System.ComponentModel;

namespace SpinnakerInterface
{
    class Spinnaker_SingleCam : CameraEye
    {
        IManagedCamera cam;
        string CamModelName = "";

        // Note that "CurrentFrameID" has FirstFrameID subtracted out of it, and +1 added, so it is "one based"
        // (i.e., there is no "frame 0"), so it can be directly compared to NumFramesGrabbed.
        bool IsFirstFrame = true;
        ulong FirstFrameID, CurrentFrameID;  // CurrentFrameID is based on the internal camera hardware frame counter.
        ulong NumFramesGrabbed = 0;

        public static Spinnaker_SingleCam camera = null;

        public Spinnaker_SingleCam(Eye whichEye, IManagedCamera CAM)
        {
            this.cam = CAM;
            cam.Init();
            CamModelName = cam.DeviceModelName.Value;
            WhichEye = whichEye;
            
            //to check if the cam feature changed:
            //var nodemap = cam.GetNodeMap();
            //IEnum iGainAuto = nodemap.GetNode<IEnum>("GainAuto");
            
            cam.AcquisitionMode.FromString("Continuous");
            cam.AcquisitionFrameRateEnable.FromString("True");
            
            cam.OffsetX.FromString("0");
            cam.OffsetY.FromString("0");
            cam.Width.FromString("720");//max is 720
            cam.Height.FromString("450");//max is 450

            //To allow high frame rates
            cam.GainAuto.FromString("Off");
            cam.ExposureAuto.FromString("Off");

            cam.ExposureTime.FromString("1676");
            cam.Gain.FromString("9");
            cam.AcquisitionFrameRate.FromString("500");
        }

        public override void Start()
        {
            if(cam==null) { return; }

            FrameRate = 100;

            cam.BeginAcquisition();
        }

        public override void Stop()
        {
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
    }
}
