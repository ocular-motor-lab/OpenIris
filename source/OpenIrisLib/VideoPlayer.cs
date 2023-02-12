//-----------------------------------------------------------------------
// <copyright file="VideoPlayer.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.IO;
    using System.Timers;
    using System.Linq;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Class that handles reading frames from a video file.
    /// </summary>
    public sealed class VideoPlayer : EyeTrackingSystem
    {
        private enum VideoPlayerState { Idle, Playing, Paused, Finished, Stopping }
        private VideoPlayerState state;

        private readonly EyeTrackingSystem eyeTrackingSystem;
        private readonly EyeTrackingSystemSettings eyeTrackingSystemSettings;
        private Timer? frameRateTimer;
        private ulong lastFrameNumber;
        private (bool isOn, ulong frameNumber) scrolling;

        /// <summary>
        /// Initializes a new instance of the VideoPlayer class.
        /// </summary>
        /// <param name="fileName">Names of the video files.</param>
        public VideoPlayer(string fileName)
            : this(new EyeTrackingSystemWebCam(),
                  new EyeCollection<string?>(fileName),
                  new Range(),
                  true)
        { }

        /// <summary>
        /// Initializes a new instance of the VideoPlayer class for playback, not for processing.
        /// </summary>
        /// <param name="fileNameLeft">Names of the video file for left eye.</param>
        /// <param name="fileNameRight">Names of the video file for right eye.</param>
        public VideoPlayer(string fileNameLeft, string fileNameRight)
            : this(new EyeTrackingSystemDualWebCam(),
                  new string[] { fileNameLeft, fileNameRight },
                  new Range(),
                  true)
        { }

        /// <summary> Initializes a new instance of the VideoPlayer class for postprocessing.
        /// </summary> 
        /// <param name="options">Options for video playing.</param> 
        /// <returns>New ImageGrabberVideoFile object.</returns>
        public static VideoPlayer PlayVideo(PlayVideoOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            var videoPlayer = new VideoPlayer(
                EyeTrackingSystem.Create(options.EyeTrackingSystem, null),
                options.VideoFileNames,
                (options as ProcessVideoOptions)?.CustomRange ?? new Range(),
                false);

            videoPlayer.Play();

            return videoPlayer;
        }

        /// <summary>
        /// Initializes a new instance of the ImageEyeVideoPlayer class.
        /// </summary>
        /// <param name="system">Image eye sources.</param>
        /// <param name="fileNames">Names of the video files.</param>
        /// <param name="frameRange">Range of frames to be played</param>
        /// <param name="useTimer">
        /// Value indicating whether the video is being processed. In that case the video will be
        /// controlled by the imagegrabber.
        /// </param>
        /// <returns>New ImageGrabberVideoFile object.</returns>
        private VideoPlayer(EyeTrackingSystem system, EyeCollection<string?> fileNames, Range frameRange, bool useTimer)
        {
            eyeTrackingSystemSettings = (EyeTrackingSystemSettings?)EyeTrackerPluginManager.EyeTrackingsyStemFactory?.GetDefaultSettings(system.Name)
                ?? throw new InvalidOperationException("No EyeTrackingsyStemFactory");

            eyeTrackingSystem = system;
            Init(eyeTrackingSystem.Name + " Videos", eyeTrackingSystemSettings);

            // Check that all the video files exist
            foreach (var file in fileNames.Where(f => f != null && !File.Exists(f)))
            {
                throw new ArgumentException("File " + file + " does not exist.");
            }

            // Initialize the video files
            (Videos, FrameCount, FrameRate, FrameSize) = CheckVideos(eyeTrackingSystem.CreateVideos(fileNames));

            Videos.ForEach(v => { if (v != null) v.VideoPlayer = this; });

            // Get frame range
            FrameRange = frameRange;
            if (FrameRange.IsEmpty)
            {
                FrameRange = new Range(0, FrameCount - 1);
            }

            // Scroll to the begining if necessary
            if (FrameRange.Begin > 0)
            {
                var begin = (ulong)FrameRange.Begin;
                Videos.ForEach(v => v?.Scroll(begin));
            }

            if (useTimer)
            {
                // Set the timer to grab frames
                frameRateTimer = new Timer(1000.0 / FrameRate);
                frameRateTimer.Elapsed += (o, e) => GrabImages();
            }

            // Set the state as paused waiting for play to start
            state = VideoPlayerState.Paused;
        }

        /// <summary>
        /// Dispose objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                frameRateTimer?.Dispose();
                frameRateTimer = null;
            }
        }

        /// <summary>
        /// Event to notify that new images have just been grabbed.
        /// </summary>
        public event EventHandler<EyeCollection<ImageEye?>>? ImagesGrabbed;

        /// <summary>
        /// Event raised when the video playing has finished.
        /// </summary>
        public event EventHandler? VideoFinished;

        /// <summary>
        /// Gets or sets the current frame number.
        /// </summary>
        public ulong CurrentFrameNumber
        {
            get
            {
                // This is confusing. Not sure if correct. But it is necessary to keep
                // consistency during scrolling.
                return scrolling.isOn ? scrolling.frameNumber : lastFrameNumber;
            }
        }

        /// <summary>
        /// List of sources of images of the eyes.
        /// </summary>
        public EyeCollection<VideoEye?> Videos { get; }

        /// <summary>
        /// Gets or sets the total number of frames in the video file.
        /// </summary>
        public long FrameCount { get; }

        /// <summary>
        /// Gets or sets Ranges of frames to be played.
        /// </summary>
        public Range FrameRange { get; }

        /// <summary>
        /// Frame rate of the video.
        /// </summary>
        public double FrameRate { get; }

        /// <summary>
        /// Frame size of the videos.
        /// </summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Value indicating whether the video is currently playing.
        /// </summary>
        public bool Playing { get => state == VideoPlayerState.Playing; }

        /// <summary>
        /// Starts grabbing images using a timer.
        /// </summary>
        public void Play()
        {
            switch (state)
            {
                case VideoPlayerState.Idle:
                    frameRateTimer?.Start();
                    state = VideoPlayerState.Playing;
                    break;

                case VideoPlayerState.Paused:
                case VideoPlayerState.Playing:
                    state = VideoPlayerState.Playing;
                    break;
                case VideoPlayerState.Finished:
                    break;

                default:
                    throw new InvalidOperationException("Invalid state of the video player.");
            }
        }

        /// <summary>
        /// Pauses playing.
        /// </summary>
        public void Pause()
        {
            switch (state)
            {
                case VideoPlayerState.Paused:
                case VideoPlayerState.Playing:
                    state = VideoPlayerState.Paused;
                    break;
                case VideoPlayerState.Finished:
                    break;

                default:
                    throw new Exception("Invalid state of the video player.");
            }
        }

        /// <summary>
        /// Scrolls the playback to the specified frame number.
        /// </summary>
        /// <param name="frameNumber">Frame number to play next.</param>
        public void Scroll(ulong frameNumber)
        {
            switch (state)
            {
                case VideoPlayerState.Playing:
                case VideoPlayerState.Paused:
                case VideoPlayerState.Finished:
                    // Important to note that to ensure as smooth scrolling as possible it must be
                    // possible to reenter here. That is, if we were scrolling but get a new command
                    // to scrool we must not ignore the new command, instead we need to change 
                    // change the frame we want to scroll to
                    scrolling = (true, frameNumber);

                    if (state == VideoPlayerState.Finished)
                    {
                        state = VideoPlayerState.Paused;
                    }

                    break;

                default:
                    throw new Exception("Invalid state of the video player.");
            }
        }

        /// <summary>
        /// Stops the video player.
        /// </summary>
        public void Stop()
        {
            switch (state)
            {
                case VideoPlayerState.Idle:
                case VideoPlayerState.Playing:
                case VideoPlayerState.Paused:
                case VideoPlayerState.Finished:
                case VideoPlayerState.Stopping:
                    state = VideoPlayerState.Stopping;

                    Videos?.ForEach(video => video?.Stop());

                    state = VideoPlayerState.Idle;
                    break;

                default:
                    throw new Exception("Invalid state of the video player.");
            }
        }

        /// <summary>
        /// Grabs images from the video(s). This methods may be called from an image grabber to act
        /// as if it was a camera. Or it can be called within a timer to play in stand alone mode.
        /// </summary>
        /// <returns>The grabbed images from the videos.</returns>
        internal EyeCollection<ImageEye?>? GrabImages()
        {
            switch (state)
            {
                case VideoPlayerState.Idle:
                case VideoPlayerState.Stopping:
                case VideoPlayerState.Finished:
                    return null;
                case VideoPlayerState.Playing:
                case VideoPlayerState.Paused:

                    // If scrolling move all the videos to the next frame.
                    if (scrolling.isOn)
                    {
                        Videos.ForEach(video => video?.Scroll(scrolling.frameNumber));
                    }
                    else
                    {
                        // Check if the videos are finished.
                        if (lastFrameNumber >= (ulong)FrameRange.End)
                        {
                            if (state != VideoPlayerState.Finished)
                            {
                                state = VideoPlayerState.Finished;
                                scrolling = (false, 0);

                                VideoFinished?.Invoke(this, new EventArgs());
                            }
                            return null;
                        }
                    }


                    // Grab the images from the videos.
                    var images = new EyeCollection<ImageEye?>(new ImageEye?[Videos.Count]);
                    
                    foreach (var video in Videos)
                    {
                        if (video is null) continue;

                        var image = video.GrabImageEye();

                        if (image is null) continue;

                        lastFrameNumber = image.TimeStamp.FrameNumber;

                        images[video.WhichEye] = image;
                    }

                    // If paused keep grabbing the same frame
                    if (state == VideoPlayerState.Paused)
                    {
                        Videos.ForEach(video => video?.Scroll(lastFrameNumber));
                    }

                    // If wanted to scroll and got the right frame number we are not scrolling anymore
                    if (scrolling.isOn & scrolling.frameNumber == lastFrameNumber)
                    {
                        scrolling = (false, 0);
                    }

                    var grabbedImages = eyeTrackingSystem.PreProcessImagesFromVideos(images);
                    ImagesGrabbed?.Invoke(this, grabbedImages);


                    return grabbedImages;

                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Gets the videos
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        public override EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> fileNames)
        {
            return Videos;
        }

        /// <summary>
        /// Postprocess the data and images.
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public override EyeTrackerImagesAndData PostProcessImages(EyeTrackerImagesAndData procesedImages)
        {
            return eyeTrackingSystem?.PostProcessImages(procesedImages) ?? procesedImages;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newVideos"></param>
        /// <returns></returns>
        private static (EyeCollection<VideoEye?>, long, double, Size) CheckVideos(EyeCollection<VideoEye?> newVideos)
        {
            // Get the number of frames (get the shortest in the case the videos have different durations)
            var frameCount = newVideos.Min(video => video?.NumberOfFrames) ??
                throw new ArgumentException("Videos contain no frames. Or they might be corrupt");

            // Get the frame rate and frame size from the videos and check that it is the same for all of them

            var frameRate = newVideos.Where(v => v != null).Select(v => v?.FrameRate).Distinct().Single() ??
                throw new ArgumentException("Videos report no frame rate. Or they might be corrupt.");

            var frameSize = newVideos.Where(v => v != null).Select(v => v?.FrameSize).Distinct().Single() ??
                throw new ArgumentException("Videos report no frame size. Or they might be corrupt.");

            return (newVideos, frameCount, frameRate, frameSize);
        }
    }

    /// <summary>
    /// Options for the play video command.
    /// </summary>
    public class PlayVideoOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public PlayVideoOptions()
        {
            VideoFileNames = new EyeCollection<string?>();
            CalibrationFileName = "";
            EyeTrackingSystem = "";
        }

        /// <summary>
        /// Gets a value indicating which eye videos have been selected.
        /// </summary>
        public Eye WhichEye
        {
            get => VideoFileNames?.Count switch
            {
                1 => Eye.Both,
                2 => (VideoFileNames?[Eye.Left], VideoFileNames?[Eye.Right]) switch
                {
                    (null, _) => Eye.Right,
                    (_, null) => Eye.Left,
                    (_, _) => Eye.Both,
                },
            };
        }

        /// <summary>
        /// Gets or sets the names and full paths of the video files.
        /// </summary>
        public EyeCollection<string?> VideoFileNames { get; set; }

        /// <summary>
        /// Gets or sets the  names and full paths of the calibration file.
        /// </summary>
        public string CalibrationFileName { get; set; }

        /// <summary>
        /// Gets or sets the type of camera system used to record the videos.
        /// </summary>
        public string EyeTrackingSystem { get; set; }
    }

    /// <summary>
    /// Options for the process video command.
    /// </summary>
    public class ProcessVideoOptions : PlayVideoOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the processed video should be saved or only the data.
        /// </summary>
        public bool SaveProcessedVideo { get; set; }

        /// <summary>
        /// Gets or sets the range of frames to be played. Beginning and end.
        /// </summary>
        public Range CustomRange { get; set; }

        /// <summary>
        /// Gets the file name that should be given to the new file.
        /// </summary>
        /// <returns></returns>
        public string GetProcessedFileName()
        {
            return VideoFileNames?.Count switch
            {
                1 => VideoFileNames[Eye.Both],
                2 => (VideoFileNames?[Eye.Left], VideoFileNames?[Eye.Right]) switch
                {
                    (null, _) => VideoFileNames?[Eye.Right]?.Replace("-Right", ""),
                    (_, null) => VideoFileNames?[Eye.Left]?.Replace("-Left", ""),
                    (_, _) => VideoFileNames?[Eye.Left]?.Replace("-Left", ""),
                },
                _ => throw new Exception("Wrong number of videos.")
            } ?? throw new Exception("No file name.");
        }
    }
}