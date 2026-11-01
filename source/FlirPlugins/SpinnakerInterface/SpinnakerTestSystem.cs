using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using SpinnakerNET;

namespace SpinnakerInterface
{
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Dale", typeof(EyeTrackingSystemSettings))]


    class SpinnakerTestSystem : EyeTrackingSystemBase
    {
        protected override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var CAMLIST = SpinnakerCameraEye.EnumerateCameras();
            if (CAMLIST == null) return null;

            SpinnakerCameraEye.BeginSynchronizedAcquisition();
            return new EyeCollection<CameraEye>(CAMLIST[0], CAMLIST[1]);
        }

        //public override GrabbedImages PreProcessImagesFromCameras(GrabbedImages images)
        //{
        //    return base.PreProcessImagesFromCameras(images);
        //}

        // These show up in the "System" menu in the GUI.
        public override EyeTrackingSystemCommand[] GetCommands()
        {
            return new[]
            {
                new EyeTrackingSystemCommand("Auto Exposure Once",
                    () => { foreach (var CAM in SpinnakerCameraEye.CAMLIST) CAM.AutoExposureOnce(); },
                    "F10"),
                new EyeTrackingSystemCommand("Toggle Camera Triggers", SpinnakerCameraEye.ToggleTriggers, "F12"),
            };
        }
    } 
}