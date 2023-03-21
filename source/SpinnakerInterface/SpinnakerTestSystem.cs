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
using System.Windows.Forms;

namespace SpinnakerInterface
{
    //[Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Test", typeof(EyeTrackingSystemSettings))]

    class SpinnakerTestSystem : EyeTrackingSystemBase
    {
        public override EyeCollection<CameraEye> CreateAndStartCameras()
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
        public override ToolStripMenuItem[] GetToolStripMenuItems()
        {
            var menu_autoexposure = new ToolStripMenuItem();
            menu_autoexposure.Text = "Auto Exposure Once";
            menu_autoexposure.ShortcutKeys = Keys.F10;
            menu_autoexposure.Click += (o, e) =>
            { foreach (var CAM in SpinnakerCameraEye.CAMLIST) CAM.AutoExposureOnce(); };

            var menu_togtrig = new ToolStripMenuItem();
            menu_togtrig.Text = "Toggle Camera Triggers";
            menu_togtrig.ShortcutKeys = Keys.F12;
            menu_togtrig.Click += (o, e) =>
            {
                SpinnakerCameraEye.ToggleTriggers();
            };

            return new ToolStripMenuItem[]
            {
                menu_autoexposure, menu_togtrig
            };
        }
    } 
}