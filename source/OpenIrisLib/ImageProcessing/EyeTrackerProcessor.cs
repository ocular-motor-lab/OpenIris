//-----------------------------------------------------------------------
// <copyright file="EyeTrackerProcessor.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class in charge of the background threads that processes images. It implements a multiplexed
    /// producer consumer. The processor is at the same time a producer and a consumer of images. It
    /// puts then in a concurrent queue and then several processing threads process the images and
    /// place them in another concurrent queue. Then, the output waiting list will go through 
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
        private readonly int numberOfThreads;
        private readonly int inputBufferSize;
        private BlockingCollection<(EyeTrackerImagesAndData imagesAndData, long orderNumber)>? inputBuffer;
        private ConcurrentDictionary<long, EyeTrackerImagesAndData>? outputWaitingList;
        private int outputNextExpectedNumber = 0;

        private bool started;

        public static EyeTrackerProcessor CreateNewForRealTime(int bufferSize, int maxNumberOfThreads)
        {
            return new EyeTrackerProcessor(true, bufferSize, maxNumberOfThreads);

        }
        public static EyeTrackerProcessor CreateNewForOffline(int maxNumberOfThreads)
        {
            return new EyeTrackerProcessor(true, 1, maxNumberOfThreads);
        }

        /// <summary>
        /// Initializes an instance of the eyeTrackerProcessor class.
        /// </summary>
        /// <param name="allowDroppedFrames">
        /// Value indicating weather frames can be dropped. This means the call to Process images
        /// will be blocking if the buffer is fulll.
        /// </param>
        /// <param name="bufferSize">Number of frames held in the buffer.</param>
        /// <param name="maxNumberOfThreads">Maximum number of threads to run.</param>
        private EyeTrackerProcessor(bool allowDroppedFrames, int bufferSize, int maxNumberOfThreads)
        {
            inputBufferSize = allowDroppedFrames ? bufferSize : 1;
            this.allowDroppedFrames = allowDroppedFrames;

            numberOfThreads = Math.Min(maxNumberOfThreads, Math.Max(1, Environment.ProcessorCount - 1));

            PipelineUI = new EyeCollection<EyeTrackingPipelineUI?>(null, null);
        }

        /// <summary>
        /// Notifies listeners that a frame has been processed and new data is available.
        /// </summary>
        internal event EventHandler<EyeTrackerImagesAndData>? ImagesProcessed;

        /// <summary>
        /// User interface for the current pipeline. For each eye.
        /// </summary>
        public EyeCollection<EyeTrackingPipelineUI?> PipelineUI { get; private set; }

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
        public int NumberFramesNotProcessed { get => NumberFramesReceived - NumberFramesProcessed; }

        /// <summary>
        /// Gets the total number of frames  currently in the buffer.
        /// </summary>
        public int NumberFramesInBuffer => inputBuffer?.Count + outputWaitingList?.Count ?? 0;

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

            var processingTasks = new List<Task>();
            var errorHandler = new TaskErrorHandler(Stop);

            try
            {
                // Initialize buffers and threads. One to process the output queue and several to process the
                // images.
                inputBuffer = new BlockingCollection<(EyeTrackerImagesAndData, long)>(inputBufferSize);
                outputWaitingList = new ConcurrentDictionary<long, EyeTrackerImagesAndData>();

                for (int i = 0; i < numberOfThreads; i++)
                {
                    var lr = new LeftRightProcessor();
                    processingTasks.Add(lr.Start(ProcessLoop, inputBuffer).ContinueWith(errorHandler.HandleError));
                }

                // Wait for the threads to finish.
                await Task.WhenAll(processingTasks);

                errorHandler.CheckForErrors();
            }
            finally
            {
                processingTasks?.ForEach(t => t?.Dispose());

                inputBuffer?.Dispose();
                inputBuffer = null;
                outputWaitingList = null;
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

            // Add the images to the input queue. The option allowDroppedFrames is used for realTime
            // processing. The thread will get not blocked here if there is no room in the
            // buffer.

            var result = true;

            if (allowDroppedFrames)
            {
                result = inputBuffer.TryAdd((imagesAndData, NumberFramesProcessed));
            }
            else
            {
                inputBuffer.Add((imagesAndData, NumberFramesProcessed));
            }

            if (result) NumberFramesProcessed++;

            return result;
        }

        /// <summary>
        /// Process loop that runs on a separate thread processing each frame whenever available in
        /// the buffer. Each image of left and right eye are processed themselves in different
        /// threads (Tasks).
        /// </summary>
        private void ProcessLoop(LeftRightProcessor leftRightProcessor)
        {
            Thread.CurrentThread.Name = "EyeTracker:ProcessLoop";

            if (inputBuffer is null) throw new InvalidOperationException("Buffer not ready.");
            if (outputWaitingList is null) throw new InvalidOperationException("Buffer not ready.");

            // Keep processing images until the buffer is marked as complete and empty
            using var cancellation = new CancellationTokenSource();
            foreach (var (imagesAndData, orderNumber) in inputBuffer.GetConsumingEnumerable(cancellation.Token))
            {
                leftRightProcessor.ProcessImages(imagesAndData);

                if (numberOfThreads == 1 | orderNumber == outputNextExpectedNumber)
                {
                    // if only one thread no need to use the output queue
                    // because the frames are not going to be out of order
                    // Same is this is the next frame we were expecting.
                    ImagesProcessed?.Invoke(this,imagesAndData);
                    outputNextExpectedNumber++;
                }
                else
                {
                    // Add the processed images and send to the output queue for reordering
                    outputWaitingList.TryAdd(orderNumber, imagesAndData);

                    // Go thru the waiting list to look for next expected order number. If the image we
                    // are waiting for is in the waiting list. Remove the item and raise an event
                    // notifying that a new image was processed
                    while (outputWaitingList.TryRemove(outputNextExpectedNumber, out EyeTrackerImagesAndData images))
                    {
                        ImagesProcessed?.Invoke(this,images);
                        outputNextExpectedNumber++;
                    }
                }

                UpdatePipelineUI(leftRightProcessor);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftRightProcessor"></param>
        private void UpdatePipelineUI(LeftRightProcessor leftRightProcessor)
        {
            if (PipelineUI[Eye.Left]?.PipelineName != leftRightProcessor.pipeline[Eye.Left].name |
                PipelineUI[Eye.Right]?.PipelineName != leftRightProcessor.pipeline[Eye.Right].name)
            {
                var pipelineUILeft = leftRightProcessor.pipeline[Eye.Left].pipeline?.GetPipelineUI(Eye.Left);
                var pipelineUIRight = leftRightProcessor.pipeline[Eye.Right].pipeline?.GetPipelineUI(Eye.Right);

                if (pipelineUILeft != null)
                    pipelineUILeft.PipelineName = leftRightProcessor.pipeline[Eye.Left].name;
                if (pipelineUIRight != null)
                    pipelineUIRight.PipelineName = leftRightProcessor.pipeline[Eye.Right].name;

                PipelineUI = new EyeCollection<EyeTrackingPipelineUI?>(pipelineUILeft, pipelineUIRight);
            }
        }
    }


    class LeftRightProcessor
    {
        private EyeTrackerImagesAndData? imagesAndData;
        private AutoResetEvent? newImagesLeftEvent;
        private AutoResetEvent? newImagesRightEvent;
        private AutoResetEvent? leftEyeDoneEvent;
        private AutoResetEvent? rightEyeDoneEvent;
        private bool stopped;
        private BlockingCollection<(EyeTrackerImagesAndData imagesAndData, long orderNumber)>? inputBuffer;
        public EyeCollection<(string name, IEyeTrackingPipeline? pipeline)> pipeline = new EyeCollection<(string, IEyeTrackingPipeline?)>(("", null), ("", null));

        public async Task Start(Action<LeftRightProcessor> ProcessLoop, BlockingCollection<(EyeTrackerImagesAndData imagesAndData, long orderNumber)>? inputBuffer)
        {
            this.inputBuffer = inputBuffer;
            var errorHandler = new TaskErrorHandler(Stop);

            try
            {
                using (newImagesLeftEvent = new AutoResetEvent(false))
                using (newImagesRightEvent = new AutoResetEvent(false))
                using (leftEyeDoneEvent = new AutoResetEvent(false))
                using (rightEyeDoneEvent = new AutoResetEvent(false))
                {
                    using var leftTask = Task.Factory.StartNew(() => ProcessOneEye(Eye.Left, newImagesLeftEvent, leftEyeDoneEvent),
                        TaskCreationOptions.LongRunning)
                        .ContinueWith(errorHandler.HandleError);

                    using var rightTask = Task.Factory.StartNew(() => ProcessOneEye(Eye.Right, newImagesRightEvent, rightEyeDoneEvent),
                        TaskCreationOptions.LongRunning)
                        .ContinueWith(errorHandler.HandleError);

                    using var inputTask = Task.Factory.StartNew(() => ProcessLoop(this),
                        TaskCreationOptions.LongRunning)
                        .ContinueWith(errorHandler.HandleError);

                    // wait for the input task first. It will finish when 
                    // the buffer is marked as complete.
                    await inputTask;

                    errorHandler.CheckForErrors();

                    // Stop the left and right tasks
                    stopped = true;
                    newImagesLeftEvent.Set();
                    newImagesRightEvent.Set();

                    await Task.WhenAll(leftTask, rightTask);

                    errorHandler.CheckForErrors();
                }
            }
            finally
            {
                stopped = true;
                newImagesLeftEvent = null;
                newImagesRightEvent = null;
                leftEyeDoneEvent = null;
                rightEyeDoneEvent = null;
                inputBuffer = null;
            }
        }

        public void Stop()
        {
            stopped = true;
            inputBuffer?.CompleteAdding();
        }

        public void ProcessImages(EyeTrackerImagesAndData images)
        {
            if (newImagesLeftEvent is null | newImagesRightEvent is null | leftEyeDoneEvent is null | rightEyeDoneEvent is null) return;

            if (stopped) return;

            imagesAndData = images;

            newImagesLeftEvent?.Set();

            newImagesRightEvent?.Set();

            leftEyeDoneEvent?.WaitOne(); // Wait for the left eye
            if (stopped) return;
            rightEyeDoneEvent?.WaitOne(); // Wait for the right eye
        }


        private void ProcessOneEye(Eye whichEye, AutoResetEvent newImageEvent, AutoResetEvent doneWithProcessingEvent)
        {
            Thread.CurrentThread.Name = "EyeTracker:RightProcessLoop";

            while (true)
            {
                // wait for new frame
                newImageEvent.WaitOne();
                if (stopped) break;

                try
                {
                    var image = imagesAndData?.Images[whichEye];

                    if (image != null)
                    {
                        EyeTrackerDebug.TrackTimeBeginPipeline(image.WhichEye, image.TimeStamp);

                        var eyeTrackingPipeline = GetCurrentEyeTrackingPipeline(imagesAndData!.TrackingSettings.EyeTrackingPipelineName, whichEye);
                        
                        (image.EyeData, image.ImageTorsion) = eyeTrackingPipeline.Process(
                            image,
                            imagesAndData.Calibration.EyeCalibrationParameters[whichEye],
                            imagesAndData.TrackingSettings);

                        EyeTrackerDebug.TrackTimeEndPipeline();
                    }
                }
                finally
                {
                    doneWithProcessingEvent.Set();
                }
            }
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
            (var currentName, var currentPipeline) = pipeline[whichEye];

            // if this thread does not have an pipeline 
            if (currentName != newPipelineName || currentPipeline is null)
            {
                currentPipeline = EyeTrackerPluginManager.EyeTrackingPipelineFactory?.Create(newPipelineName)
                    ?? throw new InvalidOperationException("bad");

                pipeline[whichEye] = (newPipelineName, currentPipeline);
            }

            return currentPipeline;
        }
    }

}