//-----------------------------------------------------------------------
// <copyright file="CameraEyeUEye.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System;
    using System.Diagnostics;
    using System.Drawing;

    /// <summary>
    /// Class to control a uEye camera in the context of eye tracking.
    /// </summary>
    /// <remarks>These cameras are manufactured by IDS Imaging Development Systems GmbH, 
    /// Dimbacher Straße 6-8, 74182 Obersulm, Germany 
    /// http://en.ids-imaging.com/store/produkte/kameras/where/ids-family/le/ids-interface/usb-2.0.html.
    /// They have their our .NET api that is included in the uEyeDotNET.dll library.
    /// </remarks>
    internal class CameraEyeUEye : CameraEye
    {
        private long numberFramesGrabbed;

        /// <summary>
        /// Camera object.
        /// </summary>
        private uEye.Camera camera;

        /// <summary>
        /// Stopwatch used to create timestamps for the frames.
        /// </summary>
        private Stopwatch timecounter = new Stopwatch();

        /// <summary>
        /// Temporary buffer for frames.
        /// </summary>
        private Bitmap lastBitmap = null;

        /// <summary>
        /// Starts capturing images.
        /// </summary>
        public override void Start()
        {
            // Connect Event
            camera.EventFrame += OnFrameEvent;
        }

        /// <summary>
        /// Stops capturing images.
        /// </summary>
        public void StopCapture()
        {
            if (camera != null)
            {
                camera.Acquisition.Stop();
                camera = null;
            }

            if (lastBitmap != null)
            {
                lastBitmap.Dispose();
                lastBitmap = null;
            }
        }

        /// <summary>
        /// Retrieves an image from the camera buffer.
        /// </summary>
        /// <returns>Image grabbed.</returns>
        protected override ImageEye GrabImageFromCamera()
        {
            while (lastBitmap is null)
            {
                System.Threading.Thread.Sleep(1);
            }

            // Build the timestamp
            // Set up the timestamp for the image
            ImageEyeTimestamp timestamp = new ImageEyeTimestamp();
            timestamp.FrameNumber = (ulong)numberFramesGrabbed++;
            timestamp.Seconds = timecounter.ElapsedMilliseconds / 1000;

            var bitmap = lastBitmap;
            lastBitmap = null;

            // Convert the image to OpenCV format
            var imageEye = new ImageEye(bitmap, WhichEye, timestamp);

            return imageEye;
        }

        /// <summary>
        /// Initializes the camera.
        /// </summary>
        public void Init()
        {
            camera = new uEye.Camera();

            uEye.Defines.Status statusRet;

            // Open Camera
            statusRet = camera.Init();
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                Trace.WriteLine("Camera initializing failed");
                return;
            }

            // Allocate Memory
            Int32 s32MemID;
            statusRet = camera.Memory.Allocate(out s32MemID, true);
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                Trace.WriteLine("Allocate Memory failed");
                return;
            }

            // Start Live Video
            statusRet = camera.Acquisition.Capture();
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                Trace.WriteLine("Start Live Video failed");
                return;
            }

            camera.Timing.Framerate.Set(60.0);
            camera.Size.AOI.Set(new Rectangle(220, 350, 800, 300));
            camera.Gain.Hardware.Scaled.SetMaster(10);
            camera.Focus.Zone.SetAOI(new Rectangle(100, 75, 200, 150));

            camera.Timing.Framerate.GetFrameRateRange(out var range);
            camera.Timing.Exposure.Get(out var f);
            camera.Timing.Framerate.Get(out var value);
            camera.Timing.Framerate.Get(out var framerate);
            FrameRate = framerate;

            camera.Size.AOI.Get(out var aoi);
            FrameSize = aoi.Size;
        }
        
        /// <summary>
        /// Handles the FrameEvent.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFrameEvent(object sender, EventArgs e)
        {
            uEye.Camera Camera = sender as uEye.Camera;

            Int32 s32MemID;
            Camera.Memory.GetActive(out s32MemID);

            Bitmap bitmap = null;
            try
            {
                bitmap = new Bitmap(1280, 1024);
                camera.Memory.ToBitmap(s32MemID, out bitmap);

                lastBitmap = bitmap;
                bitmap = null;
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        public override void Stop()
        {
        }

    }
}
