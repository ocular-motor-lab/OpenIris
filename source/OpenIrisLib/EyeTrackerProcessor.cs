//-----------------------------------------------------------------------
// <copyright file="EyeTrackerProcessor.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class in charge of the background threads that processes images. It implements a multiplexed
    /// producer consumer. The processor is at the same time a producer and a consumer of images. It
    /// puts then in a concurrent queue and then several processing threads process the images and
    /// place them in another concurrent queue. Then, the output thread will go through that queue
    /// reorder the frames properly (they may have been finished processed out of order) and
    /// propagates the new data so other entities can use it.
    /// </summary>
    /// <remarks>
    /// The processor objects receives the frames from the <see cref="TryProcessImages"/> method and
    /// outputs the processed frames with the <see cref="ImagesProcessed"/> event.
    /// </remarks>
    public sealed class EyeTrackerProcessor
    {
        private readonly bool allowDroppedFrames;
        private readonly int bufferSize;
        private readonly ConcurrentDictionary<int, (string? name, EyeCollection<IEyeTrackingPipeline>?)> pipeline;
        private BlockingCollection<(EyeTrackerImagesAndData images, long orderNumber)>? inputBuffer;
        private BlockingCollection<(EyeTrackerImagesAndData images, long orderNumber)>? outputBuffer;

        private bool started;

        /// <summary>
        /// Initializes an instance of the eyeTrackerProcessor class.
        /// </summary>
        /// <param name="allowDroppedFrames">
        /// Value indicating weather frames can be dropped. This means the call to Process images
        /// will be blocking if the buffer is fulll.
        /// </param>
        /// <param name="bufferSize">Number of frames held in the buffer.</param>
        internal EyeTrackerProcessor(bool allowDroppedFrames, int bufferSize = 100)
        {
            // TODO: it may make sense to make this dependent on the frame rate. But not sure.
            // Code is cleaner like this. Maybe it makes more sense to make it a setting.
            this.bufferSize = allowDroppedFrames ? bufferSize : 1;
            this.allowDroppedFrames = allowDroppedFrames;

            pipeline = new ConcurrentDictionary<int, (string? name, EyeCollection<IEyeTrackingPipeline>?)>();
            PipelineUI = new EyeCollection<IPipelineUI?>(null, null);
        }

        /// <summary>
        /// Notifies listeners that a frame has been processed and new data is available.
        /// </summary>
        internal event EventHandler<EyeTrackerImagesAndData>? ImagesProcessed;

        /// <summary>
        /// User interface for the current pipeline. For each eye.
        /// </summary>
        public EyeCollection<IPipelineUI?> PipelineUI { get; private set; }

        /// <summary>
        /// Gets the total number of frames received.
        /// </summary>
        public int NumberFramesReceived { get; private set; }

        /// <summary>
        /// Gets the total number of frames processed.
        /// </summary>
        public int NumberFramesProcessed { get; private set; }

        /// <summary>
        /// Gets the total number of frames dropped.
        /// </summary>
        public int NumberFramesNotProcessed { get; private set; }

        /// <summary>
        /// Gets the total number of frames  currently in the buffer.
        /// </summary>
        public int NumberFramesInBuffer => inputBuffer?.Count ?? 0;

        /// <summary>
        /// Gets a string with a status message regarding the processing.
        /// </summary>
        public string ProcessingStatus => $"Tracking " +
            $"[Frames in buffer:{NumberFramesInBuffer}, " +
            $"Dropped:{NumberFramesNotProcessed} " +
            $"({Math.Round(100.0 * NumberFramesNotProcessed / (double)(NumberFramesProcessed + NumberFramesNotProcessed))}%)]";

        /// <summary>
        /// Starts the processing threads.
        /// </summary>
        internal async Task Start()
        {
            if (started) throw new InvalidOperationException("Cannot start the processor again. Already running.");
            started = true;

            var numberOfThreads = Math.Max(1, Environment.ProcessorCount - 1);
            var processingTasks = new List<Task>();
            var errorHandler = new TaskErrorHandler(Stop);

            try
            {
                using (inputBuffer = new BlockingCollection<(EyeTrackerImagesAndData, long)>(bufferSize))
                using (outputBuffer = new BlockingCollection<(EyeTrackerImagesAndData, long)>())
                {
                    // Initialize buffers and threads. One to process the output queue and several to process the
                    // images. The number of processing threads will the number of processors minus 1.
                    using var outputTask = Task.Factory.StartNew(OutputLoop, TaskCreationOptions.LongRunning).ContinueWith(errorHandler.HandleError);

                    for (int i = 0; i < numberOfThreads; i++)
                    {
                        processingTasks.Add(Task.Factory.StartNew(ProcessLoop, TaskCreationOptions.LongRunning).ContinueWith(errorHandler.HandleError));
                    }

                    // Wait for the threads to finish.
                    await Task.WhenAll(processingTasks);

                    // Now that the processing threads are done. Do not add more items to the output queue
                    // and wait for the output thread to finish.
                    outputBuffer.CompleteAdding();

                    // Wait for the output task to be done
                    await outputTask;

                    errorHandler.CheckForErrors();
                }
            }
            finally
            {
                processingTasks?.ForEach(t => t?.Dispose());

                inputBuffer = null;
                outputBuffer = null;
            }
        }

        /// <summary>
        /// Stops the processing.
        /// </summary>
        public void Stop()
        {
            // Do not add more items to the input queue. Marking the queue with complete adding will
            // cause that the processingThreads threads to finish when the input queue is empty.
            inputBuffer?.CompleteAdding();
        }

        /// <summary>
        /// Adds a frame to the processing queue, if it is full waits or not dependening if the
        /// processing is configured to allow dropped frames.
        /// </summary>
        /// <param name="imagesAndData">Images to be processed.</param>
        /// <returns>
        /// True if the images were queued for processing. False if the frames were dropped because
        /// the buffere was full
        /// </returns>
        internal bool TryProcessImages(EyeTrackerImagesAndData imagesAndData)
        {
            if (inputBuffer is null) throw new InvalidOperationException("Buffer not ready.");

            NumberFramesReceived++;

            if (inputBuffer.IsAddingCompleted) return false;

            // Add the images to the input queue. The option AvoidDropFrames is used for offline
            // (not-realtime) processing the thread will get blocked here until there is room in the
            // buffer. Otherwise the images will be dropped if the input queue is full.

            var result = true;
            var dataForBuffer = (imagesAndData, NumberFramesProcessed);

            if (allowDroppedFrames)
            {
                result = inputBuffer.TryAdd(dataForBuffer);
            }
            else
            {
                inputBuffer.Add(dataForBuffer);
            }

            if (result) NumberFramesProcessed++;
            else NumberFramesNotProcessed++;

            return result;
        }

        /// <summary>
        /// Process loop that runs on a separate thread processing each frame whenever available in
        /// the buffer. Each image of left and right eye are processed themselves in different
        /// threads (Tasks).
        /// </summary>
        private void ProcessLoop()
        {
            Thread.CurrentThread.Name = "EyeTracker:ProcessLoop";

            if (inputBuffer is null || outputBuffer is null) throw new InvalidOperationException("Buffer not ready.");

            // Keep processing images until the buffer is marked as complete and empty
            using var cancellation = new CancellationTokenSource();

            foreach (var item in inputBuffer.GetConsumingEnumerable(cancellation.Token))
            {
                foreach (var image in item.images.Images)
                {
                    if (image is null) continue;

                    EyeTrackerDebug.TrackTimeBeginingFrame();

                    var eyeTrackingPipeline = GetCurrentEyeTrackingPipeline(item.images.TrackingSettings.EyeTrackingPipelineName, image.WhichEye);
                    var calibration = item.images.Calibration.EyeCalibrationParameters[image.WhichEye];
                    var settings = item.images.TrackingSettings;

                    (image.EyeData, image.ImageTorsion) = eyeTrackingPipeline.Process(image, calibration, settings);

                    EyeTrackerDebug.TrackTimeEndFrame();
                }

                // Add the processed images and send to the output queue
                outputBuffer.Add(item);
            }
        }

        /// <summary>
        /// Consumer loop. Gets images from the output queue and reorders them before raising the
        /// events to notify listeners that new data is available. That way we ensure the
        /// ImageProcessed event is always raised with images in the right order even thought it was
        /// possible that during parallel processing they got mixed up.
        /// </summary>
        private void OutputLoop()
        {
            Thread.CurrentThread.Name = "EyeTracker:ProcessOutputLoop";

            if (outputBuffer is null) throw new InvalidOperationException("Buffer not ready.");

            // List of frames that have been processed out of order.
            var outputWaitingList = new Dictionary<long, EyeTrackerImagesAndData>();
            var outputNextExpectedNumber = 0;

            // Keep processing images until the buffer is marked as complete and empty
            using var cancellation = new CancellationTokenSource();

            foreach ((var processedImages, var orderNumber) in outputBuffer.GetConsumingEnumerable(cancellation.Token))
            {
                // If the image is the one I am waiting for raise an event notifying that a new image
                // was processed and increament the current expected frame number. If it is not the
                // one waiting for add it to the waiting list.
                if (orderNumber == outputNextExpectedNumber)
                {
                    outputNextExpectedNumber++;
                    ImagesProcessed?.Invoke(this, processedImages);
                }
                else
                {
                    outputWaitingList.Add(orderNumber, processedImages);
                }

                // Go thru the waiting list to look for next expected order number. If the image we
                // are waiting for is in the waiting list. Remove the item and raise an event
                // notifying that a new image was processed
                while (outputWaitingList.TryGetValue(outputNextExpectedNumber, out EyeTrackerImagesAndData images))
                {
                    outputWaitingList.Remove(outputNextExpectedNumber);
                    outputNextExpectedNumber++;
                    ImagesProcessed?.Invoke(this, images);
                }
            }

            Trace.WriteLine("Processing output loop finished.");
        }

        /// <summary>
        /// Method to create the pipeline object to process the images. For each frame it has to check if the settings
        /// have changed and it needs to update to a new pipeline. It creates an pipeline object for each thread and 
        /// each eye to avoid conflicts in multithreading. Otherwise the pipeline classes would need to be thread safe
        /// which is quite hard.
        /// </summary>
        /// <param name="newPipelineName">New pipeline name.</param>
        /// <param name="whichEye">Which eye we are working with.</param>
        /// <returns>The new pipeline object.</returns>
        private IEyeTrackingPipeline GetCurrentEyeTrackingPipeline(string newPipelineName, Eye whichEye)
        {
            var ID = Thread.CurrentThread.ManagedThreadId;

            (var currentName, var currentPipeline) = pipeline.ContainsKey(ID) ? pipeline[ID] : ("", null);

            // if this thread does not have an pipeline 
            if (currentName != newPipelineName || currentPipeline is null)
            {
                currentPipeline = new EyeCollection<IEyeTrackingPipeline>(
                   EyeTrackerPluginManager.EyeTrackingPipelineFactory?.Create(newPipelineName) ?? throw new InvalidOperationException("bad"),
                   EyeTrackerPluginManager.EyeTrackingPipelineFactory?.Create(newPipelineName) ?? throw new InvalidOperationException("bad")); ;

                PipelineUI[Eye.Left] = currentPipeline[Eye.Left].GetPipelineUI(Eye.Left);
                PipelineUI[Eye.Right] = currentPipeline[Eye.Right].GetPipelineUI(Eye.Right);

                if (pipeline.ContainsKey(ID))
                {
                    pipeline[ID] = (newPipelineName, currentPipeline);
                }
                else
                {
                    var result = pipeline.TryAdd(ID, (newPipelineName, currentPipeline));
                    if (!result) throw new InvalidOperationException("bad dictionary.");
                }
            }

            return currentPipeline[whichEye];
        }
    }
}