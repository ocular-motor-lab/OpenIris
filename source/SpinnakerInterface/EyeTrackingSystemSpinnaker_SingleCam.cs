﻿using System.Threading.Tasks;
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

    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Spinnaker Single Camera - RS Test", typeof(EyeTrackingSystemSettingsSpinnaker_SingleCam))]

    class EyeTrackingSystemSpinnaker_SingleCam : EyeTrackingSystemBase
    {
        protected CameraEyeSpinnaker camera = null;
        
        public override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSettingsSpinnaker_SingleCam ?? throw new ArgumentNullException(nameof(Settings));

            try
            {
                var cameraList = CameraEyeSpinnaker.FindCameras(Settings.Eye, 1);

                camera = new CameraEyeSpinnaker(
                whichEye: Settings.Eye,
                camera: cameraList[0],
                frameRate: (double)Settings.FrameRate,
                roi: new Rectangle { Width = 720, Height = 450 });

                camera.Start();
            }
            catch (Exception ex)
            {
                if (camera != null)
                {
                   camera.Stop();
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs. " + ex.Message, ex);
            }

            switch (settings.Eye)
            {
                case Eye.Left:
                    return new EyeCollection<CameraEye?>(camera, null);
                case Eye.Right:
                    return new EyeCollection<CameraEye?>(null, camera);
                case Eye.Both:
                    return new EyeCollection<CameraEye?>(camera);
                default: return new EyeCollection<CameraEye?>(camera);
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
    public class EyeTrackingSystemSettingsSpinnaker_SingleCam : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemSettingsSpinnaker_SingleCam()
        {
            PixPerMm = 7;
            DistanceCameraToEyeMm = 70;
        }
    }
}
