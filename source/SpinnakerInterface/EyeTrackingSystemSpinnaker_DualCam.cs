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

namespace SpinnakerInterface
{
#nullable enable

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Dual Camera - RS Test", typeof(EyeTrackingSystemSettingsSpinnakerTest))]

    class EyeTrackingSystemSpinnaker_DualCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker cameraLeft = null;
        protected CameraEyeSpinnaker cameraRight = null;

        public override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnakerTest ?? throw new ArgumentNullException(nameof(Settings));

            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(Eye.Both, 1);

                cameraLeft = new CameraEyeSpinnaker(
                whichEye: Eye.Left,
                camera: cameraList[0],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });

                cameraRight = new CameraEyeSpinnaker(
                whichEye: Eye.Right,
                camera: cameraList[1],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });


                //MAke one master
                camera.SetAsMaster();
                    camera.SetAsSlave();

                cameraLeft.Start();
                cameraRight.Start();
            }
            catch (Exception ex)
            {
                if (this.camera != null)
                {
                    this.camera.Stop();
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }

            switch (settings.Eye)
            {
                case Eye.Left:                    
                    return new EyeCollection<CameraEye?>(this.camera,null);
                case Eye.Right:
                    return new EyeCollection<CameraEye?>(null, camera);
                case Eye.Both:
                    return new EyeCollection<CameraEye?>(camera);
                default:
                    return new EyeCollection<CameraEye?>(this.camera);
            }

           
        }

        public override EyeCollection<ImageEye> PreProcessImagesFromCameras(EyeCollection<ImageEye> images)
        {
            var settings = Settings as EyeTrackingSystemSettings;

            switch (settings.Eye)
            {
                case Eye.Both:
                    var roiLeft = new Rectangle(images[Eye.Both].Size.Width/2, 0, images[Eye.Both].Size.Width/2, images[Eye.Both].Size.Height);
                    var roiRight = new Rectangle(0, 0, images[Eye.Both].Size.Width/2, images[Eye.Both].Size.Height);

                    var imageLeft = images[Eye.Both].Copy(roiLeft);
                    imageLeft.WhichEye = Eye.Left;
                    var imageRight = images[Eye.Both].Copy(roiRight);
                    imageRight.WhichEye = Eye.Right;
                    return new EyeCollection<ImageEye>(imageLeft, imageRight);
                default:
                    return images;
            }
        }

        public override EyeCollection<ImageEye> PreProcessImagesFromVideos(EyeCollection<ImageEye> images)
        {
            // Because only the right image has the timestamp in the bytes we copy the raw frame number to the left image
            images[Eye.Left].TimeStamp.FrameNumberRaw = images[Eye.Right].TimeStamp.FrameNumberRaw;
            return base.PreProcessImagesFromVideos(images);
        }
    }
    public class EyeTrackingSystemSettingsSpinnakerTest : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnakerTest()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
        }
    }
}
