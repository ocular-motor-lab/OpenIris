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
        private Form myUI;

        public override EyeCollection<CameraEye> CreateCameras()
        {
                if (myUI == null)
                {
                    myUI = new SpinnakerUI();
                    myUI.Show();
                }
            var CAMLIST = SpinnakerCameraEye.EnumerateCameras();
            if (CAMLIST == null) return null;

            SpinnakerCameraEye.BeginSynchronizedAcquisition();
            return new EyeCollection<CameraEye>(CAMLIST[0], CAMLIST[1]);
        }

        //public override GrabbedImages PreProcessImagesFromCameras(GrabbedImages images)
        //{
        //    return base.PreProcessImagesFromCameras(images);
        //}

        public override Form OpenEyeTrackingSystemUI
        {
            get
            {
                return myUI;
            }
        }

    }
}