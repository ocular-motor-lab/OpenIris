//-----------------------------------------------------------------------
// <copyright file="HeadTracker.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    /// <summary>
    /// Class to grab head tracking data.
    /// </summary>
    public class HeadTracker
    {
        private IHeadDataSource? headDataSource;
        private ConcurrentQueue<HeadData>? headDataBuffer;
        private bool grabbing = false;

        /// <summary>
        /// Gets the number of frames dropped at the sensor.
        /// </summary>
        public long DroppedFramesAtCamera { get; private set; }

        /// <summary>
        /// Gets the numer of frames dropped because they arrived out of sequence or too late.
        /// </summary>
        public long DroppedFramesSequence { get; private set; }

        /// <summary>
        /// Gets the status of the HeadTracker.
        /// </summary>
        public string Status => (headDataSource is null) ? 
            "" 
            : 
            "[HEAD: Drops Camera:" + DroppedFramesAtCamera +
                " Drops Seq:" + DroppedFramesSequence +
                " Buffer:" + headDataBuffer?.Count ?? 0 + "]";


        public async static Task<HeadTracker?> CreateNewforOffLine(IEyeTrackingSystem eyeTrackingSystem)
        {
            var headDataSource = await Task.Run(eyeTrackingSystem.CreateHeadDataSourceWithVideos);

            return (headDataSource is null) ? null : new HeadTracker(headDataSource);
        }
        public async static Task<HeadTracker?> CreateNewForRealTime(IEyeTrackingSystem eyeTrackingSystem)
        {
            var headDataSource = await Task.Run(eyeTrackingSystem.CreateHeadDataSourceWithCameras);

            return (headDataSource is null) ? null : new HeadTracker(headDataSource);
        }

        /// <summary>
        /// Initializes an instance of HeadTracker.
        /// </summary>
        /// <param name="headDataSource"></param>
        private HeadTracker(IHeadDataSource? headDataSource)
        {
            this.headDataSource = headDataSource;
        }

        /// <summary>
        /// Starts the background thread grabbing head data.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            try
            {
                headDataBuffer = new ConcurrentQueue<HeadData>();

                grabbing = true;

                await Task.Factory.StartNew(GrabbingLoop, TaskCreationOptions.LongRunning);
            }
            finally
            {
                headDataBuffer = null;

                (headDataSource as IDisposable)?.Dispose();
                headDataSource = null;
            }
        }

        /// <summary>
        /// Stops the head tracking
        /// </summary>
        public void Stop()
        {
            grabbing = false;
        }

        /// <summary>
        /// Responds to a request from the EyeTracker for the head data that corresponds
        /// with a set of images
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public HeadData GetHeadDataCorrespondingToImages(EyeCollection<ImageEye?> procesedImages)
        {
            if (procesedImages is null) return new HeadData();
            if (headDataSource is null) return new HeadData();

            // Get one current timestamp
            var imageFrameNumber = procesedImages[Eye.Right]?.EyeData?.Timestamp.FrameNumberRaw;

            while (headDataBuffer.Count > 0)
            {
                var result = headDataBuffer.TryPeek(out HeadData data);
                if (!result)
                {
                    break;
                }

                var headFrameNumber = data.TimeStamp.FrameNumber;

                // If head data is older than the image drop it because we asume the
                // processed images come in order so we will never get a request for this frame
                if (headFrameNumber < imageFrameNumber)
                {
                    //Trace.WriteLine("Dropped head data because too old.");
                    headDataBuffer.TryDequeue(out _);
                    DroppedFramesSequence++;
                    continue;
                }

                // If the head data is much newer that is probably because there was a breakpoint
                // or something else weird that slowed down the camera a lot.
                // This should not happen often
                if (headFrameNumber > imageFrameNumber + 100)
                {
                    if (EyeTracker.DEBUG) Trace.WriteLine("Dropped head data because too new.");
                    headDataBuffer.TryDequeue(out _);
                    DroppedFramesSequence++;
                    continue;
                }

                // If the request for the data came to early. Should not happen often
                // But could if for some reason the head tracker thread is slowed down
                if (headFrameNumber > imageFrameNumber)
                {
                    if (EyeTracker.DEBUG) Trace.WriteLine("Data was not here yet.");
                    break;
                }

                // Got the right data, take it out from the queue
                if (headFrameNumber == imageFrameNumber)
                {
                    headDataBuffer.TryDequeue(out data);
                    return data;
                }
            }

            return null;
        }

        private void GrabbingLoop()
        {
            if (headDataSource is null) return;

            Thread.CurrentThread.Name = "Camera:HeadGrabbing";

            long lastFrame = -1;
            while (grabbing)
            {
                try
                {
                    var data = headDataSource.GrabHeadData();

                    // If there was no packet sleep a bit and come back later
                    if (data is null)
                    {
                        Thread.Sleep(2);
                        continue;
                    }

                    // If the buffer is ful do not add any more.
                    if (headDataBuffer.Count >= 100) continue;

                    headDataBuffer.Enqueue(data);

                    if (lastFrame >= 0)
                    {
                        DroppedFramesAtCamera += (long)data.TimeStamp.FrameNumberRaw - (lastFrame + 1);
                    }

                    lastFrame = (long)data.TimeStamp.FrameNumberRaw;
                }
                catch (OpenIrisException) when (!grabbing)
                {
                    // If there was an error but we are not grabbing it doesn't matter.
                    return;
                }
                catch (OpenIrisException ex)
                {
                    Trace.WriteLine("ERROR getting head data packet. But continue anyway. " + ex.Message);
                    // If the error is really bad the camera will crash too. 
                }
            }
        }
    }

    /// <summary>
    /// Initerface for all sources of head data
    /// </summary>
    public interface IHeadDataSource
    {
        /// <summary>
        /// Grabs head data from the specific sensor implementation.
        /// </summary>
        /// <returns></returns>
        HeadData GrabHeadData();

        /// <summary>
        /// Stops the sensor
        /// </summary>
        void Stop();
    }
}
