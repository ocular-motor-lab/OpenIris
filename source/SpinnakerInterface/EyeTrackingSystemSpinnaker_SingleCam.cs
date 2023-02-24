using System.Threading.Tasks;
using OpenIris;
using OpenIris.ImageGrabbing;
using System.ComponentModel.Composition;
using SpinnakerNET;
using System.Windows.Forms;
using System.Drawing;
using System;
using Spinnaker;
using System.Diagnostics;

namespace SpinnakerInterface
{
    [Export(typeof(EyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Spinnaker Single Camera - RS Test", typeof(EyeTrackingSystemSettings))]

    class EyeTrackingSystemSpinnaker_SingleCam : EyeTrackingSystem
    {
        protected Spinnaker_SingleCam camera = null;

        public override EyeCollection<CameraEye> CreateCameras()
        {
            var settings = Settings as EyeTrackingSystemSettings;

            // Retrieve singleton reference to Spinnaker system object
            ManagedSystem system = new ManagedSystem();

            // Retrieve list of cameras from the system
            var cam_list = system.GetCameras();

            if (cam_list.Count < 1)
            {
                throw new Exception($"NEED AT LEAST ONE CAMERAS!! Found {cam_list.Count} FLIR Spinnaker compatible camera(s).");
            }

            Trace.WriteLine($"Found {cam_list.Count} cameras. Calling cam.Init()...");
            camera = new Spinnaker_SingleCam(cam_list[0]);

            camera.CameraOrientation = CameraOrientation.Rotated180;

            // Initialize left camera if necessary
            try
            {
                this.camera.Start();
            }
            catch (Exception ex)
            {
                if (this.camera != null)
                {
                    this.camera.Stop();
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs.", ex);
            }

            return new EyeCollection<CameraEye>(this.camera);
        }

        public override EyeCollection<ImageEye> PreProcessImagesFromCameras(EyeCollection<ImageEye> images)
        {
            var settings = Settings as EyeTrackingSystemSettings;

            var roiLeft = new Rectangle(1160, 0, 760, images[Eye.Both].Size.Height);
            var roiRight = new Rectangle(0, 0, 760, images[Eye.Both].Size.Height);

            switch (settings.Eye)
            {
                case Eye.Left:
                    var imageLeft = images[Eye.Both].Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    return new EyeCollection<ImageEye?>(imageLeft, null);
                case Eye.Right:
                    var imageRight = images[Eye.Both].Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye?>(null, imageRight);
                case Eye.Both:
                    imageLeft = images[Eye.Both].Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    imageRight = images[Eye.Both].Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye?>(imageLeft, imageRight);
                default:
                    return null;
            }
        }

        public override EyeCollection<ImageEye> PreProcessImagesFromVideos(EyeCollection<ImageEye> images)
        {
            // Because only the right image has the timestamp in the bytes we copy the raw frame number to the left image
            images[Eye.Left].TimeStamp.FrameNumberRaw = images[Eye.Right].TimeStamp.FrameNumberRaw;
            return base.PreProcessImagesFromVideos(images);
        }
    }
}
