
using System;
using System.Diagnostics;
using OpenIris;
using OpenIris.ImageGrabbing;

using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace SpinnakerInterface
{
    class SpinnakerCameraEye : CameraEye
    {
        int numCam;
        IManagedCamera cam;
        long frameCounter;

        public SpinnakerCameraEye(int numCam)
        {
            this.numCam = numCam;
        }

        public override void Start()
        {
            if (cam != null) return;

            // Retrieve singleton reference to system object
            ManagedSystem system = new ManagedSystem();

            // Get current library version
            // version = self.system.GetLibraryVersion()
            // print('FLIR Spinnaker Library version: %d.%d.%d.%d' % (version.major, version.minor, version.type, version.build))

            // Retrieve list of cameras from the system
            var cam_list = system.GetCameras();

            if (cam_list.Count == 0)
            {
                throw new Exception("No CAMERAS!!");
            }

            Trace.WriteLine($"Found {cam_list.Count} cameras. Calling cam.Init()...");

            cam = cam_list[numCam];
            cam.Init();
            cam_list.Clear();

            //self.InitParameters()
            //self.InitROI()

            FrameRate = cam.AcquisitionFrameRate;
            cam.BeginAcquisition();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        protected override ImageEye GrabImageFromCamera()
        {
            //
            // Retrieve next received image
            //
            // *** NOTES ***
            // Capturing an image houses images on the camera buffer. 
            // Trying to capture an image that does not exist will 
            // hang the camera.
            //
            // Using-statements help ensure that images are released.
            // If too many images remain unreleased, the buffer will
            // fill, causing the camera to hang. Images can also be
            // released manually by calling Release().
            // 
            using (IManagedImage rawImage = cam.GetNextImage())
            {
                //
                // Ensure image completion
                //
                // *** NOTES ***
                // Images can easily be checked for completion. This 
                // should be done whenever a complete image is 
                // expected or required. Alternatively, check image
                // status for a little more insight into what 
                // happened.
                //
                if (rawImage.IsIncomplete)
                {
                    Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                }
                else
                {
                    //
                    // Convert image to mono 8
                    //
                    // *** NOTES ***
                    // Images can be converted between pixel formats
                    // by using the appropriate enumeration value.
                    // Unlike the original image, the converted one 
                    // does not need to be released as it does not 
                    // affect the camera buffer.
                    //
                    using (IManagedImage convertedImage = rawImage.Convert(PixelFormatEnums.Mono8))
                    {
                        frameCounter++;

                        // Build the new timestamp
                        var timestamp = new ImageEyeTimestamp
                        {
                            FrameNumber = (ulong)frameCounter,
                            FrameNumberRaw = rawImage.ID,
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

            }

            return null;
        }

    }
}
