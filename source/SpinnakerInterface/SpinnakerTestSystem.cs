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
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Spinnaker Test", typeof(EyeTrackingSystemSettings))]

    class SpinnakerTestSystem : EyeTrackingSystem
    {
        public override EyeCollection<CameraEye> CreateCameras()
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

        public override ToolStripMenuItem[] GetToolStripMenuItems()
        {
            var menu1 = new ToolStripMenuItem();
            menu1.Text = "test1";
            menu1.ShortcutKeys = Keys.F11;
            menu1.Click += (o, e) =>
            {
                MessageBox.Show("hello! f11 pressed");
            };

            return new ToolStripMenuItem[]
            {
                menu1
            };
        }
    } 
}