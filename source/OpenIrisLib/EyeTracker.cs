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
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Main class that offers the public interface to the library for eye tracking.
    /// </summary>
    public sealed partial class EyeTracker : IDisposable
    {
        private static EyeTracker? eyeTracker;
        
        /// <summary>
        /// Constructor for EyeTracker. 
        /// This is just to make sure the static property is never null.
        /// This is not an initialization of the eye tracker. For that, Init must be called.
        /// That should not be a problem. The only way to access an EyeTracker object
        /// is by the static Start method and that one will call init appropriately.
        /// </summary>
        private EyeTracker()
        {
            DataBuffer = new EyeTrackerDataBuffer();
            Calibration = CalibrationParameters.Default;

            try
            {
                EyeTrackerDebug.Init();

                EyeTrackerLog.Create(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    $"OpenIrisLog-{DateTime.Now:yyyyMMMdd-HHmmss}.Log"));
                try
                {
                    // Initialize the object that loads the different eye tracking system objects. It can
                    // load objects from new classes present in new dlls in the application folder.
                    EyeTrackerPluginManager.Init(false);
                }
                catch (PluginManagerException ex)
                {
                    Trace.WriteLine("ERROR loading plugins :" + ex.Message);
                    Trace.WriteLine("ERROR loading plugins : going into safe mode.");
                    EyeTrackerPluginManager.Init(safeMode: true);
                }

                // Load settings. Needs to happen after the plugins have been initialized to properly
                // load the settings of each plugin
                Settings = EyeTrackerSettings.Load();

                // Start the server to accept remote requests For some reason I don't understand this
                // cannot be done in a separate thread.
                EyeTrackerRemoteServices.Start(this);
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
        public static EyeTracker Create()
        {
            eyeTracker ??= new EyeTracker();

            return eyeTracker;
        }

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public void Dispose()
        {
            // Stop everything just in case
            StopTracking();
            EyeTrackerRemoteServices.StopService();
            Settings?.Save();
        }

        /// <summary>
        /// Gets the Eye tracker system corresponding with the current hardware set up.
        /// </summary>
        public EyeTrackingSystemBase? EyeTrackingSystem { get; private set; }

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
        /// Current calibration session.
        /// </summary>
        public CalibrationSession? CalibrationSession { get; private set; }

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
        /// Event raised when there is new processed data available including calibrated and head.
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
        /// Average time processing;
        /// </summary>
        public double AverageFrameProcessingTime { get; private set; }

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
                // Initialize a few things
                ResetStats();
                DataBuffer.Reset();

                // Get EyeTracker System. If playing from video this is already set by PlayVideo.
                // Then set up image grabber, processor and head tracker.
                if (PlayingVideo)
                {
                    EyeTrackingSystem = VideoPlayer!;
                    ImageProcessor = new EyeTrackerProcessor( EyeTrackerProcessor.Mode.Offline, EyeTrackingSystem, Settings.MaxNumberOfProcessingThreads);
                    ImageGrabber = EyeTrackerImageGrabber.CreateNewForVideos(VideoPlayer!, Settings.BufferSize);
                    HeadTracker = await HeadTracker.CreateNewforOffLine(EyeTrackingSystem);
                }
                else
                {
                    EyeTrackingSystem = EyeTrackingSystemBase.Create(Settings.EyeTrackerSystem, Settings.EyeTrackingSystemSettings);
                    ImageProcessor = new EyeTrackerProcessor(EyeTrackerProcessor.Mode.RealTime, EyeTrackingSystem, Settings.MaxNumberOfProcessingThreads, Settings.BufferSize);
                    ImageGrabber = await EyeTrackerImageGrabber.CreateNewForCameras(EyeTrackingSystem, Settings.BufferSize);
                    HeadTracker = await HeadTracker.CreateNewForRealTime(EyeTrackingSystem);
                }

                // Action for every time new images are grabbed
                ImageGrabber.ImagesGrabbed += (_, grabbedImages) =>
                {
                    // The best way to signal we are already tracking is after we get the first image.
                    Tracking = true;

                    RecordingSession?.TryRecordImages(grabbedImages, ImageGrabber.FrameRate);
                    ImageProcessor?.TryProcessImages(grabbedImages, Calibration, Settings.EyeTrackingPipeline, Settings.TrackingPipelineSettings);
                };

                // Action for every time new images are processed
                ImageProcessor.ImagesProcessed += (_, processedImages) =>
                {
                    // After an image is processed we collected additional data into a common 
                    // structure and we save it in the data buffer. Each eye tracking system can 
                    // also post process the entire data frame
                    var eyeDataRaw = new EyeCollection<EyeData?>(processedImages.Images[Eye.Left]?.EyeData, processedImages.Images[Eye.Right]?.EyeData);
                    var headDataRaw = HeadTracker?.GetHeadDataCorrespondingToImages(processedImages.Images);
                    processedImages.Data = new()
                    {
                        EyeDataRaw = eyeDataRaw,
                        HeadDataRaw = headDataRaw,
                        EyeDataCalibrated = Calibration?.GetCalibratedEyeData(eyeDataRaw),
                        HeadDataCalibrated = Calibration?.GetCalibratedHeadData(headDataRaw),
                        FrameRate = ImageGrabber!.FrameRate
                    };

                    LastImagesAndData = EyeTrackingSystem!.PostProcessImagesAndData(processedImages);

                    DataBuffer.Add(LastImagesAndData.Data);

                    // Then we try to record the data and images (usually only in postprocessing)
                    // and we also pass it to the calibration manager.
                    RecordingSession?.TryRecordImagesAndData(LastImagesAndData, ImageGrabber.FrameRate);
                    CalibrationSession?.ProcessNewDataAndImages(LastImagesAndData);

                    UpdateStats(processedImages);

                    // Finally we propagate the event in case there are clients.
                    NewDataAndImagesAvailable?.Invoke(this, LastImagesAndData);
                };

                // Start the threads (Tasks) to grabb and process images. If there is an error in either
                // of them, the error handler will stop everything. 
                await Task.WhenAll(
                    ImageProcessor.Start().ContinueWith(errorHandler.HandleError),
                    ImageGrabber.Start().ContinueWith(errorHandler.HandleError),
                    HeadTracker?.Start()?.ContinueWith(errorHandler.HandleError) ?? Task.CompletedTask);

                errorHandler.CheckForErrors();

                // Wait for a recording to end, just in case is still going.
                await (RecordingSession?.Wait() ?? Task.CompletedTask);
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
                RecordingSession = null;
            }
        }

        private void ResetStats()
        {
            AverageFrameProcessingTime = double.NaN;
        }

        private void UpdateStats(EyeTrackerImagesAndData processedImages)
        {
            var deltaLeftTime = processedImages.Data?.TimeProcessed - processedImages.Images[Eye.Left]?.TimeStamp.TimeGrabbed ?? double.NaN;
            var deltaRightTime = processedImages.Data?.TimeProcessed - processedImages.Images[Eye.Right]?.TimeStamp.TimeGrabbed ?? double.NaN;

            var newTime = (deltaLeftTime, deltaRightTime) switch
            {
                (double.NaN, double.NaN) => double.NaN,
                (double.NaN, _) => deltaRightTime,
                (_, double.NaN) => deltaLeftTime,
                (_, _) => (deltaLeftTime + deltaRightTime) / 2.0,

            };
            AverageFrameProcessingTime = AverageFrameProcessingTime switch
            {
                double.NaN => newTime,
                _ => AverageFrameProcessingTime * 0.95 + 0.05 * newTime
            };
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

                // Start recording (important to initialize before the playing, so all frames are recorded).
                RecordingSession = new RecordingSession(new ProcessedRecordingOptions
                {
                    SessionName = Path.GetFileNameWithoutExtension(options.GetProcessedFileName()) + "-PostProc",
                    DataFolder = Settings.DataFolder,
                    SaveRawVideo = false,
                    SaveProcessedVideo = options.SaveProcessedVideo,
                    WhichEye = options.WhichEye,

                    AddPupilCross = true,
                    IncreaseContrast = false,
                    AddEyelids = false,
                });
                using var recordingTask = RecordingSession.Start(Calibration, Settings);

                // Play video and wait until it finishes. This will also start the tracking
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
                RecordingSession = null;
            }
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        public async Task StartRecording()
        {
            Trace.WriteLine("StartRecordingAsync");

            if (Recording) return;

            try
            {
                RecordingSession = new RecordingSession(new()
                {
                    SessionName = Settings.SessionName,
                    SaveRawVideo = Settings.RecordVideo,
                    DecimateRatioRawVideo = Settings.DecimateVideoRatio,
                    DataFolder = Settings.DataFolder
                });
                await RecordingSession.Start(Calibration, Settings);
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
                using (CalibrationSession = new CalibrationSession(Settings.EyeTrackingSystemSettings.Eye, Settings.CalibrationMethod, Settings.CalibrationSettings))
                {

                    var tempCalibration = await CalibrationSession.StartCalibratingEyeModel(Settings.CalibrationSettings, Settings.TrackingPipelineSettings);
                    if (tempCalibration is null) return;

                    // IMPORTANT!! Need to update calibration so the zero reference processing is done with a
                    // proper eye model.
                    Calibration = tempCalibration;

                    Calibration = await CalibrationSession.StartCalibratingZeroReference(Calibration, Settings.CalibrationSettings, Settings.TrackingPipelineSettings);
                }
            }
            catch
            {
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
                using (CalibrationSession = new CalibrationSession(Settings.EyeTrackingSystemSettings.Eye, Settings.CalibrationMethod, Settings.CalibrationSettings))
                {
                    Calibration = await CalibrationSession.StartCalibratingZeroReference(Calibration, Settings.CalibrationSettings, Settings.TrackingPipelineSettings);
                }
            }
            catch
            {
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

            RecordingSession?.TryRecordEvent(eventMessage, currentFrameNumber, data);

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

                var centerLeft = (dataLeftEye?.ProcessFrameResult == ProcessFrameResult.Good) ?
                    dataLeftEye.Pupil.Center : PointF.Empty;

                var centerRight = (dataRightEye?.ProcessFrameResult == ProcessFrameResult.Good) ?
                    dataRightEye.Pupil.Center : PointF.Empty;

                EyeTrackingSystem?.CenterEyes(centerLeft, centerRight);
            }
        }

        /// <summary>
        /// Moves the camera (or ROI).
        /// </summary>
        /// <param name="whichEyeToMove">Left,right or both.</param>
        /// <param name="direction">Which direction to move.</param>
        public void MoveCamera(Eye whichEyeToMove, MovementDirection direction)
        {
            EyeTrackingSystem?.MoveCamera(whichEyeToMove, direction);
        }

        /// <summary>
        /// </summary>
        public void IncreaseExposure()
        {
            EyeTrackingSystem?.IncreaseExposure();
        }

        /// <summary>
        /// </summary>
        public void ReduceExposure()
        {
            EyeTrackingSystem?.ReduceExposure();
        }
    }
}