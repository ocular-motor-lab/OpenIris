//-----------------------------------------------------------------------
// <copyright file="EyeTracker.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using OpenIris.ImageGrabbing;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Main class that offers the public interface to the library for eye tracking.
    /// </summary>
    public sealed class EyeTracker : IDisposable
    {
        private static EyeTracker? eyeTracker;

        /// <summary>
        /// Initializes an instance of the EyeTracker class.
        /// </summary>
        /// <param name="safeMode">True activates safe mode by not using external plugins.</param>
        private EyeTracker(bool safeMode = false)
        {
            try
            {
                var logpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"EyeTrackerLog-{DateTime.Now.ToString("yyyyMMMdd-HHmmss")}.Log");
                EyeTrackerLog.Create(logpath);

                DataBuffer = new EyeTrackerDataBuffer();
                Calibration = CalibrationParameters.Default;

                // Initialize the object that loads the different eye tracking system objects. It can
                // load objects from new classes present in new dlls in the application folder.
                EyeTrackerPluginManager.Init(safeMode);

                // Load settings. Needs to happen after the plugins have been initialized to properly
                // load the settings of each plugin
                Settings = EyeTrackerSettings.Load();

                // Start the server to accept remote requests For some reason I don't understand this
                // cannot be done in a separate thread.
                EyeTrackerRemoteService.Start(this);

                Trace.WriteLine("Eye tracker initializing complete.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Initializes an instance of the EyeTracker class.
        /// </summary>
        /// <param name="safeMode">True activates safe mode by not using external plugins.</param>
        public static EyeTracker Start(bool safeMode = false)
        {
            // Singleton
            return eyeTracker ??= new EyeTracker(safeMode);
        }

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public void Dispose()
        {
            // Stop everything just in case
            StopTracking();
            EyeTrackerRemoteService.StopService();
            Settings?.Save();
            eyeTracker = null;
        }

        /// <summary>
        /// Gets the Eye tracker system corresponding with the current hardware set up.
        /// </summary>
        public EyeTrackingSystem? EyeTrackingSystem { get; private set; }

        /// <summary>
        /// Gets the image grabber. In charge of getting images from cameras or videos.
        /// </summary>
        public EyeTrackerImageGrabber? ImageGrabber { get; private set; }

        /// <summary>
        /// Gets the current video player. If playing from video.
        /// </summary>
        public VideoPlayer? VideoPlayer { get; private set; }

        /// <summary>
        /// Gets the image processor. Processes the images to get data.
        /// </summary>
        public EyeTrackerProcessor? ImageProcessor { get; private set; }

        /// <summary>
        /// Head tracker
        /// </summary>
        public HeadTracker? HeadTracker { get; private set; }

        /// <summary>
        /// Current recording session if recording.
        /// </summary>
        public RecordingSession? RecordingSession { get; private set; }

        /// <summary>
        /// Current calibration manager.
        /// </summary>
        public CalibrationPipeline? CalibrationSession { get; private set; }

        /// <summary>
        /// Gets the current calibration parameters.
        /// </summary>
        public CalibrationParameters Calibration { get; private set; }

        /// <summary>
        /// Gets the current processed images and data. Alternative to listen to the event (new data available).
        /// </summary>
        public EyeTrackerImagesAndData? LastImagesAndData { get; private set; }

        /// <summary>
        /// Buffer with data used for plots.
        /// </summary>
        public EyeTrackerDataBuffer DataBuffer { get; private set; }

        /// <summary>
        /// Gets the settings object.
        /// </summary>
        public EyeTrackerSettings Settings { get; private set; }

        /// <summary>
        /// Event raised when there is new processed data available.
        /// </summary>
        public event EventHandler<EyeTrackerImagesAndData>? NewDataAndImagesAvailable;

        /// <summary>
        /// Returns a value indicating wether the eyeTracker is in debugging mode.
        /// </summary>
        public static bool DEBUG => eyeTracker?.Settings.Debug ?? false;

        /// <summary>
        /// True if eye tracker is not started yet.
        /// </summary>
        public bool NotStarted => EyeTrackingSystem is null;

        /// <summary>
        /// True if eye tracker is tracking.
        /// </summary>
        public bool Tracking { get; private set; }

        /// <summary>
        /// True if eye tracker is calibrating.
        /// </summary>
        public bool Calibrating => CalibrationSession != null;

        /// <summary>
        /// True if eye tracker is recording.
        /// </summary>
        public bool Recording => RecordingSession != null;

        /// <summary>
        /// True if eye tracker is playing a video.
        /// </summary>
        public bool PlayingVideo => VideoPlayer != null;

        /// <summary>
        /// True if eye tracker is postprocessing a video.
        /// </summary>
        public bool PostProcessing { get; private set; }

        /// <summary>
        /// Starts tracking from cameras, using the eye tracking system selected in the configuration settings.
        /// </summary>
        public async Task StartTracking()
        {
            Trace.WriteLine("StartTracking");
            if (!NotStarted) throw new InvalidOperationException("ERROR: Cannot Start Tracking in the current state.");

            var errorHandler = new TaskErrorHandler(StopTracking);
            try
            {
                DataBuffer.Reset();

                // Get EyeTracker System. If playing from video this is already set by PlayVideo.
                // Then set up image grabber, processor and head tracker.
                if (PlayingVideo)
                {
                    EyeTrackingSystem = VideoPlayer!;
                    ImageProcessor = EyeTrackerProcessor.CreateNewForOffline(handleImagesProcessed, Settings.MaxNumberOfProcessingThreads);
                    ImageGrabber = EyeTrackerImageGrabber.CreateNewForVideos(handleImagesGrabbed, VideoPlayer!, Settings.BufferSize, Settings.TrackingMode);
                    HeadTracker = await HeadTracker.CreateNewforOffLine(EyeTrackingSystem);
                }
                else
                {
                    EyeTrackingSystem = EyeTrackingSystem.Create(Settings.EyeTrackerSystem, Settings.EyeTrackingSystemSettings);
                    ImageProcessor = EyeTrackerProcessor.CreateNewForRealTime(handleImagesProcessed, Settings.BufferSize, Settings.MaxNumberOfProcessingThreads);
                    ImageGrabber = await EyeTrackerImageGrabber.CreateNewForCameras(handleImagesGrabbed, EyeTrackingSystem, Settings.BufferSize, Settings.TrackingMode);
                    HeadTracker = await HeadTracker.CreateNewForRealTime(EyeTrackingSystem);
                }

                // Action for every time new images are grabbed
                void handleImagesGrabbed(EyeCollection<ImageEye?> grabbedImages)
                {
                    // The best way to signal we are already tracking is after we get the first image.
                    Tracking = true;

                    // Prepare the images for processing depending on the system. For instance flipping,
                    // cropping, splitting, increasing contrast, whatever ...
                    grabbedImages = EyeTrackingSystem!.PreProcessImages(grabbedImages);
                    RecordingSession?.TryRecordImages(grabbedImages);

                    ImageProcessor?.TryProcessImages(grabbedImages, Calibration, Settings.TrackingpipelineSettings);
                }

                // Action for every time new images are processed
                void handleImagesProcessed(EyeTrackerImagesAndData processedImages)
                {
                    // After an image is processed we collected additional data into a common 
                    // structure and we save it in the data buffer. Each eye tracking system can 
                    // also post process the entire data frame
                    processedImages.Data = new EyeTrackerData();
                    processedImages.Data.EyeDataRaw = new EyeCollection<EyeData?>(processedImages.Images[Eye.Left]?.EyeData, processedImages.Images[Eye.Right]?.EyeData);
                    processedImages.Data.HeadDataRaw = HeadTracker?.GetHeadDataCorrespondingToImages(processedImages.Images) ?? new HeadData();
                    processedImages.Data.EyeDataCalibrated = Calibration?.GetCalibratedEyeData(processedImages.Data.EyeDataRaw);
                    processedImages.Data.HeadDataCalibrated = Calibration?.GetCalibratedHeadData(processedImages.Data.HeadDataRaw);
                    processedImages.Data.FrameRate = ImageGrabber!.FrameRate;

                    LastImagesAndData = EyeTrackingSystem!.PostProcessImages(processedImages);

                    DataBuffer.Add(LastImagesAndData.Data);

                    // Then we try to record the data and images (usually only in postprocessing)
                    // and we also pass it to the calibration manager.
                    RecordingSession?.TryRecordImagesAndData(LastImagesAndData);
                    CalibrationSession?.ProcessNewDataAndImages(LastImagesAndData);

                    // Finally we propagate the event in case there are clients.
                    NewDataAndImagesAvailable?.Invoke(this, LastImagesAndData);
                }

                // Start the threads (Tasks) to grabb and process images. If there is an error in either
                // of them, the error handler will stop everything. 
                await Task.WhenAll(
                    ImageProcessor.Start().ContinueWith(errorHandler.HandleError),
                    ImageGrabber.Start().ContinueWith(errorHandler.HandleError),
                    HeadTracker.Start()?.ContinueWith(errorHandler.HandleError));

                errorHandler.CheckForErrors();

                // Wait for a recording to end, just in case is still going.
                await (RecordingSession?.RecordingTask ?? Task.CompletedTask);
            }
            catch
            {
                // Just in case stop everything. It is important to have here because recording and
                // calibration also need to be stopped if there is an error and they have no way of
                // knowing that an error happened during grabbing or processing.
                StopTracking();
                throw;
            }
            finally
            {
                EyeTrackingSystem?.Dispose();

                Tracking = false;

                HeadTracker = null;
                ImageGrabber = null;
                ImageProcessor = null;
                EyeTrackingSystem = null;
            }
        }

        /// <summary>
        /// Stops tracking.
        /// </summary>
        public void StopTracking()
        {
            Trace.WriteLine("Stopping Eye Tracker.");

            try
            {
                //
                // The order of stopping is very important
                //
                // 1. Stop the calibration as soon as possible. It is the least relevant.
                // 2. Stop the video player.
                // 3. Stop the head tracker.
                // 4. Stop the image grabber next to make sure that all the played frames are grabbed.
                // 5. Stop the processor to make sure all the grabbed frames are processed
                // 6. Stop the recorder to make sure all the grabbed and processed frames are recorded
                //

                CalibrationSession?.CancelCalibration();

                VideoPlayer?.Stop();
                HeadTracker?.Stop();
                ImageGrabber?.Stop();
                ImageProcessor?.Stop();

                if (ImageGrabber != null)
                {
                    // Tell the recorder to record up to the last frame grabbed by the image grabber
                    RecordingSession?.Stop(ImageGrabber.CurrentFrameNumber);
                }
                else
                {
                    // Rare case where recorder is not null but image grabber is. Just stop as
                    // quickly as possible. It should really never happen
                    RecordingSession?.Stop();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR stopping: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Starts playing a video.
        /// </summary>
        /// <param name="options">Parameters for the video player.</param>
        /// <returns>Video player object that can control the video playback.</returns>
        public async Task PlayVideo(PlayVideoOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            Trace.WriteLine("PlayVideo :" + options.VideoFileNames);

            try
            {
                // Change to the eye tracking system of the video being played
                // Change this memory of the last system used for video so it is shown as default the next time
                Settings.LastVideoEyeTrackerSystem = Settings.EyeTrackerSystem = options.EyeTrackingSystem;

                // Load calibration if necessary
                LoadCalibration(options.CalibrationFileName);

                // Play video and start tracking
                using (VideoPlayer = VideoPlayer.PlayVideo(options))
                {
                    await StartTracking();
                }
            }
            finally
            {
                VideoPlayer = null;
            }
        }

        /// <summary>
        /// Starts processing a video.
        /// </summary>
        /// <param name="options">Select video dialog options.</param>
        public async Task ProcessVideo(ProcessVideoOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (Tracking) throw new InvalidOperationException("ERROR: Cannot Start Tracking in the current state.");

            Trace.WriteLine("Processing videos: " + Path.GetFileNameWithoutExtension(options.GetProcessedFileName()));

            try
            {
                PostProcessing = true;

                var optionsProcessedVideo = new ProcessedRecordingOptions
                {
                    FrameRate = ImageGrabber?.FrameRate ?? 30.0,
                    SessionName = Path.GetFileNameWithoutExtension(options.GetProcessedFileName()) + "-PostProc",
                    DataFolder = Settings.DataFolder,
                    SaveRawVideo = false,
                    SaveProcessedVideo = options.SaveProcessedVideo,
                    WhichEye = options.WhichEye,

                    AddCross = true,
                    IncreaseContrast = false,
                    AddEyelids = false,
                };

                // Start recording (important to initialize before the playing, so all frames are recorded).
                // Play video and wait until it finishes. This will also start the tracking
                using var recordingTask = StartRecording(optionsProcessedVideo);
                using var playingVideoTask = PlayVideo(options);

                if (VideoPlayer is null) throw new InvalidOperationException("Video Player failed");

                // Stop recoridng When the video finishes up to the last frame that needed to be recorded.
                VideoPlayer.VideoFinished += (_, e) => RecordingSession?.Stop(VideoPlayer.FrameRange.End);

                // Wait for recording to finish. Then stop the tracking and wait for the videoplayer 
                // task that should finish at the same time as the stop tracking task
                await recordingTask;
                StopTracking();
                await playingVideoTask;
            }
            finally
            {
                PostProcessing = false;
            }
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        /// <param name="recordingOptions">Options for the recording.</param>
        public async Task StartRecording(RecordingOptions recordingOptions)
        {
            if (recordingOptions is null) throw new ArgumentNullException(nameof(recordingOptions));
            Trace.WriteLine("StartRecordingAsync");

            if (Recording) return;

            try
            {
                RecordingSession = new RecordingSession(recordingOptions);
                Settings.LastRecordedFile = RecordingSession.DataFileName;

                await RecordingSession.Start(Calibration, Settings);
            }
            catch
            {
                RecordingSession?.Stop();
                throw;
            }
            finally
            {
                RecordingSession = null;
            }
        }

        /// <summary>
        /// Stop the recording when an specific frame number is reached.
        /// </summary>
        public void StopRecording()
        {
            Trace.WriteLine("StopRecording");
            RecordingSession?.Stop(RecordingSession.NumberFrameLastInVideo);
        }

        /// <summary>
        /// Starts the calibration.
        /// </summary>
        public async Task StartCalibration()
        {
            Trace.WriteLine("StartCalibration");
            if (!Tracking) throw new InvalidOperationException("ERROR: Cannot Start Calibrating in the current state.");

            if (Calibrating) return;

            try
            {
                CalibrationSession = EyeTrackerPluginManager.CalibrationFactory?.Create(Settings.CalibrationMethod)
                    ?? throw new InvalidOperationException("No factory");

                var tempCalibrationParameters = await CalibrationSession.CalibrateEyeModel(Settings.CalibrationSettings, Settings.TrackingpipelineSettings);
                if (tempCalibrationParameters is null) return;

                // IMPORTANT!! Need to update calibration so the zero reference processing is done with a
                // proper eye model. 
                // TODO: think a better way of doing this.
                Calibration = tempCalibrationParameters;

                Calibration = await CalibrationSession.CalibrateZeroReference(Calibration, Settings.CalibrationSettings, Settings.TrackingpipelineSettings);
            }
            catch
            {
                CancelCalibration();
                throw;
            }
            finally
            {
                CalibrationSession = null;
            }
        }

        /// <summary>
        /// Cancels the calibration.
        /// </summary>
        public void CancelCalibration()
        {
            Trace.WriteLine("CancelCalibration");

            CalibrationSession?.CancelCalibration();
        }

        /// <summary>
        /// Resets the calibration to the default values.
        /// </summary>
        public void ResetCalibration()
        {
            Trace.WriteLine("ResetCalibration");

            Calibration = CalibrationParameters.Default;
        }

        /// <summary>
        /// Resets the reference.
        /// </summary>
        public async Task ResetReference()
        {
            Trace.Assert(Tracking, "ERROR: Cannot reset reference in the current state.");
            Trace.WriteLine("ResetZeroReference");

            if (Calibrating) return;

            try
            { 
                CalibrationSession = EyeTrackerPluginManager.CalibrationFactory?.Create(Settings.CalibrationMethod) 
                    ?? throw new InvalidOperationException("No factory");

                Calibration = await CalibrationSession.CalibrateZeroReference(Calibration, Settings.CalibrationSettings, Settings.TrackingpipelineSettings);
            }
            catch
            {
                CancelCalibration();
                throw;
            }
            finally
            {
                CalibrationSession = null;
            }
        }

        /// <summary>
        /// Loads the calibration from a file.
        /// </summary>
        /// <param name="file">Full path to the calibration file.</param>
        public void LoadCalibration(string file)
        {
            Trace.WriteLine("LoadCalibration");

            if (Calibrating) return;

            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                Calibration = CalibrationParameters.Load(file);
            }
        }

        /// <summary>
        /// Records a new event.
        /// </summary>
        /// <param name="eventMessage">Message associated with the event.</param>
        /// <param name="data">Data associated with the event.</param>
        /// <returns>The current frame number that will be associated with the event.</returns>
        public long RecordEvent(string eventMessage, object? data)
        {
            Trace.WriteLine("RecordEvent = " + eventMessage);

            if (ImageGrabber is null) return 0;

            var currentFrameNumber = ImageGrabber.CurrentFrameNumber;

            RecordingSession?.TryRecordEvent(new EyeTrackerEvent(eventMessage, currentFrameNumber, data));

            return currentFrameNumber;
        }

        /// <summary>
        /// Centers the camera ROI around the current pupil center.
        /// </summary>
        public void CenterEyes()
        {
            if (LastImagesAndData?.Data?.EyeDataRaw != null)
            {
                var dataLeftEye = LastImagesAndData.Data.EyeDataRaw[Eye.Left];
                var dataRightEye = LastImagesAndData.Data.EyeDataRaw[Eye.Right];

                ImageGrabber?.CenterEyes(
                    (dataLeftEye?.ProcessFrameResult == ProcessFrameResult.Good)
                        ? dataLeftEye.Pupil.Center
                        : PointF.Empty,
                    (dataRightEye?.ProcessFrameResult == ProcessFrameResult.Good)
                        ? dataRightEye.Pupil.Center
                        : PointF.Empty);
            }
        }

        /// <summary>
        /// Moves the camera (or ROI).
        /// </summary>
        /// <param name="whichEyeToMove">Left,right or both.</param>
        /// <param name="direction">Which direction to move.</param>
        public void MoveCamera(Eye whichEyeToMove, MovementDirection direction)
        {
            ImageGrabber?.MoveCamera(whichEyeToMove, direction);
        }
    }
}