﻿//-----------------------------------------------------------------------
// <copyright file="EyeTrackerImageGrabber.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Image grabber that grabs images in the background and fires an event to notify that a new
    /// image is available. Different eye tracking systems will provide different image sources. They
    /// can also provide a delegate to pre-process images. The image grabber handles both cameras and
    /// videos with slightly different logic.
    /// To try to eliminate the posibility of code running externally during the event from slowing down 
    /// the grabbing thread, there is a second thread that acts as a consumer of the grabbed images to 
    /// fire the events.
    /// </summary>
    public sealed class EyeTrackerImageGrabber
    {
        private readonly int numberOfImageSources;
        private readonly VideoPlayer? videoPlayer;
        private readonly int bufferSize;

        private CancellationTokenSource? cancellation;

        private IImageEyeSource?[]? imageSources;
        private BlockingCollection<ImageEye>? cameraBuffer;
        private EyeCollection<Queue<ImageEye>?>? cameraQueues;
        private (double TimeStamp, long FrameCounter) lastCheckGrabbing;
        private bool started;
        private bool stopping;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoPlayer"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        /// <exception cref="OpenIrisException"></exception>
        internal static EyeTrackerImageGrabber CreateNewForVideos(VideoPlayer videoPlayer, int bufferSize = 100)
        {
            var newSources = videoPlayer.Videos.Select(v => v as IImageEyeSource)
                ?? throw new OpenIrisException("No videos.");

            var sources = new EyeCollection<IImageEyeSource?>(newSources);

            return new EyeTrackerImageGrabber(sources, bufferSize, ((EyeTrackingSystemBase)videoPlayer).Settings.Eye);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eyeTrackingSystem"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        /// <exception cref="OpenIrisException"></exception>
        internal static async Task<EyeTrackerImageGrabber> CreateNewForCameras(EyeTrackingSystemBase eyeTrackingSystem, int bufferSize = 100)
        {
               var newSources = await Task.Run(() => eyeTrackingSystem.CreateAndStartCameras1().Select(c => c as IImageEyeSource))
                ?? throw new OpenIrisException("No cameras started.");

            var sources = new EyeCollection<IImageEyeSource?>(newSources);

            return new EyeTrackerImageGrabber(sources, bufferSize, eyeTrackingSystem.Settings.Eye);
        }

        /// <summary>
        /// Initializes an instance of the class <see cref="EyeTrackerImageGrabber"/> for grabbing from cameras.
        /// </summary>
        /// <param name="sources">Image sources, cameras or videos.</param>
        /// <param name="bufferSize">Number of frames held in the buffer.</param>
        /// <param name="whichEye">Which eye to grab from, left, right, or both.</param>
        private EyeTrackerImageGrabber(EyeCollection<IImageEyeSource?> sources, int bufferSize = 100, Eye whichEye = Eye.Both)
        {
            // Check if the sources are videos and get the video player
            videoPlayer = (sources.FirstOrDefault(s => s is VideoEye) as VideoEye)?.VideoPlayer;

            imageSources = sources;

            this.bufferSize = bufferSize;

            // Check same frame rate and frame size
            numberOfImageSources = CheckNumberOfSources(imageSources, whichEye);
            FrameSize = CheckFrameSize(imageSources);
            FrameRate = CheckFrameRate(imageSources);
        }

        /// <summary>
        /// Notifies listeners about a new frame available. This event runs in the grabber thread. Any
        /// event handler should be quick.
        /// </summary>
        public event EventHandler<ImageEye?[]>? ImagesGrabbed;

        /// <summary>
        /// Gets the frame number of the last image grabbed.
        /// </summary>
        public long CurrentFrameNumber { get; private set; }

        /// <summary>
        /// Gets the number of frames grabbed . It does not necessarily match the current frame
        /// number. Because frame numbers may start at any arbitrary number and also because there
        /// may be drops.
        /// </summary>
        public int NumberFramesGrabbed { get; private set; }

        /// <summary>
        /// Gets the number of frames dropped.
        /// </summary>
        public int NumberFramesDropped { get; private set; }

        /// <summary>
        /// Gets the estimatated frame rate. It is updated every second based on frame numbers.
        /// </summary>
        public double FrameRateMeasured { get; private set; }

        /// <summary>
        /// Gets the frame rate in frames per second coming from the image sources.
        /// </summary>
        public double FrameRate { get; private set; }

        /// <summary>
        /// Gets the frame size in pixels coming from the image sources.
        /// </summary>
        public Size FrameSize { get; private set; }

        /// <summary>
        /// Gets the diagnostics information.
        /// </summary>
        public string CameraDebugInfo
        {
            get
            {
                if (imageSources is null) return string.Empty;

                var diagnostics = new System.Text.StringBuilder();
                foreach (var source in imageSources)
                {
                    if (source is null) continue;

                    diagnostics.Append(source.Info.ToString());
                }
                return diagnostics.ToString();
            }
        }

        /// <summary>
        /// Gets a string with a status message regarding the grabbing of images and head data.
        /// </summary>
        public string GrabbingStatus =>$"Grabbing " +
            $"at {FrameRateMeasured:0.0}Hz  {FrameSize.Width}x{FrameSize.Height}px " +
            $"Frames:{NumberFramesGrabbed} Dropped:{NumberFramesDropped} Buffer:{ cameraBuffer?.Count ?? 0}";

        /// <summary>
        /// Starts grabbing.
        /// </summary>
        internal async Task Start()
        {
            if (started) throw new InvalidOperationException("Cannot start the processor again. Already running.");
            started = true;

            Task? cameraTasks = null;

            try
            {
                var errorHandler = new TaskErrorHandler(Stop);
                var usingCameras = videoPlayer is null;

                if (usingCameras && numberOfImageSources > 1)
                {
                    // Setup the queue and start the threads for each camera
                    cameraBuffer = new BlockingCollection<ImageEye>(bufferSize);
                    cameraQueues = new EyeCollection<Queue<ImageEye>?>(imageSources.Select(c => (c != null) ? new Queue<ImageEye>() : null));
                    cameraTasks = Task.WhenAll(imageSources.Select(s => Task.Factory.StartNew(() => 
                        CameraLoop(s as CameraEye), 
                        TaskCreationOptions.LongRunning).ContinueWith(errorHandler.HandleError)).ToArray());
                }

                using var timer = new Timer(_ => CheckGrabbing(), state: null, dueTime: 1000, period: 1000);
                using (cancellation = new CancellationTokenSource())
                {
                    // Start the grabbing loop
                    await Task.Factory.StartNew(
                        GrabLoop, 
                        TaskCreationOptions.LongRunning).ContinueWith(errorHandler.HandleError);

                    // wait for the camera threads if necessary
                    if (cameraTasks is not null) await cameraTasks;

                    errorHandler.CheckForErrors();
                }
            }
            finally
            {
                cameraTasks?.Dispose();

                cameraBuffer?.Dispose();
                cameraBuffer = null;

                cancellation = null;

                imageSources = null;
            }
        }

        /// <summary>
        /// Stops grabbing.
        /// </summary>
        internal void Stop()
        {
            if (stopping) return;
            stopping = true;

            // Mark the camera queue as complete. This will stop the grabbing loop.
            cameraBuffer?.CompleteAdding();
        }

        /// <summary>
        /// Loop that grabs images from a camera and puts them in a thread safe queue.
        /// </summary>
        /// <param name="camera">Camera to grab from.</param>
        private void CameraLoop(CameraEye? camera)
        {
            if (camera is null) return;

            Thread.CurrentThread.Name = "CameraGrabbingLoop:" + camera.WhichEye;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            while (true)
            {
                var image = camera.GrabImageEye();

                // Check here if the buffer iscompleted because the GrabImageEye is blocking usually
                // so it may take a lot of time and the buffer may be completed then. 
                if (cameraBuffer?.IsAddingCompleted ?? true) break;

                if (image is null) continue;
                
                cameraBuffer?.TryAdd(image);
            }

            Trace.WriteLine(Thread.CurrentThread.Name + " finished.");
        }

        /// <summary>
        /// Loop that grabs images from the imageEyeSources. These can be cameras or videos.
        /// </summary>
        private void GrabLoop()
        {
            Thread.CurrentThread.Name = "EyeTracker:GrabLoop";
            Trace.WriteLine("Grabbing loop started.");

            var usingCameras = (videoPlayer is null);

            // Select the image grabbing function
            // If grabbing from video(s) Need to grab from video player instead of image eye
            // sources because the video player controls the pause/playback/scrolling
            Func<EyeCollection<ImageEye?>?>? GrabImages = null;

            switch (usingCameras, numberOfImageSources)
            {
                case (true, 1):
                    var singleCamera = imageSources.Single(c => (c != null));
                    GrabImages = singleCamera!.WhichEye switch
                    {
                        Eye.Both => () => new EyeCollection<ImageEye?>(singleCamera?.GrabImageEye()),
                        Eye.Left => () => new EyeCollection<ImageEye?>(singleCamera?.GrabImageEye(), null),
                        Eye.Right => () => new EyeCollection<ImageEye?>(null, singleCamera?.GrabImageEye()),
                        _ => throw new NotImplementedException(),
                    };
                    break;
                case (true, 2):
                    GrabImages = GrabImagesFromTwoCameras;
                    break;
                case (true, _):
                    throw new InvalidOperationException("Only for 1 or 2 cameras");
                case (false, _):
                    GrabImages = videoPlayer!.GrabImages;
                    break;
            };

            while (!stopping)
            {
                var grabbedImages = GrabImages();

                // If some images were grabbed add them to the output queue, for
                // pre-processing and for propagation to other consumers of images.
                if (grabbedImages != null)
                {
                    if (CurrentFrameNumber > 0 && videoPlayer is null)
                    {
                        NumberFramesDropped += (int)(grabbedImages.GetFrameNumber() - (CurrentFrameNumber + 1));
                    }
                    CurrentFrameNumber = grabbedImages.GetFrameNumber();
                    NumberFramesGrabbed++;

                    ImagesGrabbed?.Invoke(this, grabbedImages);
                }
            }

            Trace.WriteLine("Grabber loop finished.");
        }

        private EyeCollection<ImageEye?>? GrabImagesFromTwoCameras()
        {
            if (cameraBuffer is null || cameraQueues is null || cancellation is null)
                throw new InvalidOperationException("Camera buffers are not ready.");

            // Wait for an image from the queue
            var result = cameraBuffer.TryTake(out ImageEye? image, millisecondsTimeout: -1, cancellation.Token);
            if (!result || image is null) return null;

            try
            {
                var cameraQueue = cameraQueues[image.WhichEye] ?? throw new InvalidOperationException("This queue should not be null.");

                // When grabbing from two cameras, it is possible that a frame is dropped in one camera
                // but not in the other. For that reason it is necessary to have a buffer to hold frames
                // from one camera while the corresponding frame from the other camera may or may not arrive.

                // Adds the image to the queue
                cameraQueue.Enqueue(image);
                image = null; // Necessary to dispose properly after passing ownership of image

                // First check if there is an image in all the (not null) queues. If not return and we wait
                // for more images before we check again.
                if (cameraQueues.Min(q => q?.Count) == 0) return null;

                // The determine what is the smallest frame number present in the queues.
                var minFrameNumberInQueues = cameraQueues.Min(q => q?.Peek().TimeStamp.FrameNumber);

                // Now, in a second pass, make sure all the queues have the same frame number. Collect the
                // images with that frame number. It is very important to collect all the images even if
                // that frame number is missing in some other queue to deal with dropped frames. If there
                // was dropped frame this will clean the queues until a non-dropped frame arrives to all
                // the queues.
                var images = new ImageEye[cameraQueues.Count];
                bool anyImageMissing = false;

                for (int i = 0; i < cameraQueues.Count; i++)
                {
                    var queue = cameraQueues[i];

                    if (queue is null) continue;

                    // If the queue has the frame number we are looking for dequeue de image.
                    if (queue.Peek().TimeStamp.FrameNumber != minFrameNumberInQueues)
                    {
                        // Do not return in this condition because it needs to pass all the queues
                        anyImageMissing = true;
                        continue;
                    }

                    images[i] = queue.Dequeue();
                }

                if (anyImageMissing) return null;

                return new EyeCollection<ImageEye?>(images);

            }
            finally
            {
                image?.Dispose();
            }
        }

        /// <summary>
        /// Checks if the frame rate of all the sources is the same.
        /// </summary>
        /// <param name="newImageEyeSources">Image sources.</param>
        /// <returns>The frame rate.</returns>
        private static double CheckFrameRate(EyeCollection<IImageEyeSource?> newImageEyeSources)
        {
            var maxFrameRateDifference = 0.5;

            var frameRates = newImageEyeSources.Where(s => s != null).Select(s => s!.FrameRate) ??
                throw new InvalidOperationException("No image source to get frame rate from.");

            if (Math.Abs(frameRates.Min() - frameRates.Max()) > maxFrameRateDifference)
                throw new InvalidOperationException("The frame rate of the two cameras is not the same");

            if (frameRates.Max() < 1)
                throw new InvalidOperationException("Frame rate is zero");

            return frameRates.Average();
        }

        private static int CheckNumberOfSources(EyeCollection<IImageEyeSource?> sources, Eye whichEyes)
        {
            var numberOfSources = 0;
            switch (whichEyes)
            {
                case Eye.Left:
                    if (sources.Count != 2 || sources[Eye.Left] == null || sources[Eye.Right] != null )
                        throw new InvalidOperationException("The number of image imageEyeSource is not correct for " + whichEyes + " eye(s)");
                    numberOfSources = 1;
                    break;
                case Eye.Right:
                    if (sources.Count != 2 || sources[Eye.Right] == null || sources[Eye.Left] != null)
                        throw new InvalidOperationException("The number of image imageEyeSource is not correct for " + whichEyes + " eye(s)");
                    numberOfSources = 1;
                    break;
                case Eye.Both:
                    if (sources.Count == 1 && sources[Eye.Both] == null)
                        throw new InvalidOperationException("The number of image imageEyeSource is not correct for " + whichEyes + " eye(s)");
                    if (sources.Count == 2 && (sources[Eye.Left] == null || sources[Eye.Right] == null))
                        throw new InvalidOperationException("The number of image imageEyeSource is not correct for " + whichEyes + " eye(s)");
                    numberOfSources = sources.Count;
                    break;
                default:
                    throw new InvalidOperationException("The number of image imageEyeSource is not correct for " + whichEyes + " eye(s)");
            }

            return numberOfSources;
        }


        /// <summary>
        /// Checks if the frame size of all the sources is the same.
        /// </summary>
        /// <param name="newImageEyeSources">Image sources.</param>
        /// <returns>The frame size.</returns>
        private static Size CheckFrameSize(EyeCollection<IImageEyeSource?> newImageEyeSources)
        {
            try
            {
                return newImageEyeSources
                    .Where(s => s != null)
                    .Select(s => s!.CameraOrientation.IsRotated() ? s.FrameSize.Transpose() : s.FrameSize)
                    .Distinct().Single();
            }
            catch
            {
                throw new InvalidOperationException("The frame size of the two cameras is not the same");
            }
        }

        private void CheckGrabbing()
        {
            if (lastCheckGrabbing.TimeStamp == 0.0)
            {
                lastCheckGrabbing = (TimeStamp: EyeTrackerDebug.TimeElapsed.TotalMilliseconds, FrameCounter: NumberFramesGrabbed);
            }
            else
            {
                // Estimate the frame rate every second more or less
                var timeNow = EyeTrackerDebug.TimeElapsed.TotalMilliseconds;
                FrameRateMeasured = (NumberFramesGrabbed - lastCheckGrabbing.FrameCounter) / (timeNow - lastCheckGrabbing.TimeStamp) * 1000;
                lastCheckGrabbing = (timeNow, NumberFramesGrabbed);
            }
        }
    }
}