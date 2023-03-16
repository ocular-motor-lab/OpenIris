using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using SpinnakerNET;
using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using Spinnaker;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace SpinnakerInterface
{
#nullable enable

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Dual Camera - RS Test", typeof(EyeTrackingSystemSettings))]

    class EyeTrackingSystemSpinnaker_DualCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker cameraLeft = null;
        protected CameraEyeSpinnaker cameraRight = null;

        public override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(Eye.Both, 2);

                var camera1 = new CameraEyeSpinnaker(
                whichEye: Eye.Left,
                camera: cameraList[0],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });

                var camera2 = new CameraEyeSpinnaker(
                whichEye: Eye.Right,
                camera: cameraList[1],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });

                if ( cameraList[0].DeviceSerialNumber == 22428697.ToString())
                {
                    cameraLeft = camera1;
                    cameraRight = camera2;
                }
                else if(cameraList[1].DeviceSerialNumber == 22428697.ToString())
                {
                    cameraLeft = camera2;
                    cameraRight = camera1;
                }
                else
                {
                    cameraLeft = camera1;
                    cameraRight = camera2;
                }


                CameraEyeSpinnaker.BeginSynchronizedAcquisition(cameraLeft, cameraRight);

            }
            catch (Exception ex)
            {
                if (cameraLeft != null || cameraRight != null)
                {
                    CameraEyeSpinnaker.EndSynchronizedAcquisition(cameraLeft,cameraRight);
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }
            return new EyeCollection<CameraEye?>(cameraLeft,cameraRight);
        }
       
    }
}
