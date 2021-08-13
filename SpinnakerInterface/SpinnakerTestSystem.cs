using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;

namespace SpinnakerInterface
{
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Spinnaker Test", typeof(EyeTrackingSystemSettings))]
    class SpinnakerTestSystem : EyeTrackingSystem
    {
        public override EyeCollection<CameraEye> CreateCameras()
        {
            // TODO: add parameter to constructor to be able to select 2 different cameras

            var cameraLeft = new SpinnakerCameraEye(0)
            {
                WhichEye = Eye.Left
            };

            cameraLeft.Start();


            // TODO: Add some cound to make sure the cameras are syncrhonized properly
            // Look at micromedical 
            // CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);
            //

            return new EyeCollection<CameraEye>(cameraLeft, null);
        }
    }

    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Spinnaker Test Binoc", typeof(EyeTrackingSystemSettings))]
    class SpinnakerTestSystemBinoc : EyeTrackingSystem
    {
        public override EyeCollection<CameraEye> CreateCameras()
        {
            // TODO: Check if you have to cameras

            // TODO: add parameter to constructor to be able to select 2 different cameras

            var cameraLeft = new SpinnakerCameraEye(0)
            {
                WhichEye = Eye.Left
            };

            cameraLeft.Start();

            var cameraRight = new SpinnakerCameraEye(1)
            {
                WhichEye = Eye.Right
            };

            cameraRight.Start();

            // TODO: Add some cound to make sure the cameras are syncrhonized properly
            // Look at micromedical 
            // CameraEyeFlyCapture.SyncCameras(cameraLeftEye, cameraRightEye, settings.FrameRate);
            //

            return new EyeCollection<CameraEye>(cameraLeft, cameraRight);
        }
    }
}