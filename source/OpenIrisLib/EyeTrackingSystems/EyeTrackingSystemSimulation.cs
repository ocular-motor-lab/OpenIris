//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSimulation.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.IO;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Simulator of eye tracker from videos.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Simulation", typeof(EyeTrackingSystemSimulationSettings))]
    public class EyeTrackingSystemSimulation : EyeTrackingSystemBase
    {
        private EyeCollection<CameraEye?>? cameras;

        /// <summary>
        /// Gets the cameras. In this case one video for each eye.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override EyeCollection<CameraEye?> CreateAndStartCameras()
        {
            var settings = Settings as EyeTrackingSystemSimulationSettings;
            var (leftVideo, leftOrientation) = GetVideo(settings?.LeftEyeVideo, "JorgeLeft", settings);
            var (rightVideo, rightOrientation) = GetVideo(settings?.RightEyeVideo, "JorgeRight", settings);

            var cameraLeft = new CameraEyeVideoSimulation(Eye.Left, leftVideo, Settings.FrameRate)
            {
                CameraOrientation = leftOrientation
            };

            var cameraRight = new CameraEyeVideoSimulation(Eye.Right, rightVideo, Settings.FrameRate)
            {
                CameraOrientation = rightOrientation
            };

            long loopAtFrame = Math.Min(cameraLeft.NumberOfFrames - 1, cameraRight.NumberOfFrames - 1);
            cameraLeft.LoopAtFrame = loopAtFrame;
            cameraRight.LoopAtFrame = loopAtFrame;

            cameras = Settings.Eye switch
            {
                Eye.Both => new EyeCollection<CameraEye?>(cameraLeft, cameraRight),
                Eye.Left => new EyeCollection<CameraEye?>(cameraLeft, null),
                Eye.Right => new EyeCollection<CameraEye?>(null, cameraRight),
                _ => throw new NotImplementedException(),
            };

            return cameras;
        }

        /// <summary>
        /// Stops and closes the videos.
        /// </summary>
        public override void Dispose()
        {
            cameras?.ForEach(camera =>
            {
                camera?.Stop();
                camera?.Dispose();
            });
            cameras = null;
        }

        /// <summary>
        /// Gets the video to play for one eye: the one in the settings if there is one. Otherwise the
        /// full sample video next to OpenIris if present (it is large and not included in releases),
        /// or else the small sample video included with OpenIris.
        /// </summary>
        private static (string fileName, CameraOrientation orientation) GetVideo(string? videoSetting, string sampleName, EyeTrackingSystemSimulationSettings? settings)
        {
            var folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (!string.IsNullOrWhiteSpace(videoSetting))
            {
                // Relative paths are relative to the OpenIris folder.
                var video = Path.Combine(folder, videoSetting);
                if (!File.Exists(video)) throw new FileNotFoundException($"Simulation video not found: {video}", video);

                return (video, settings?.VideoOrientation ?? CameraOrientation.Upright);
            }

            // The sample videos were recorded with a mirrored camera.
            var fullSample = Path.Combine(folder, sampleName + ".avi");
            if (File.Exists(fullSample)) return (fullSample, CameraOrientation.UprightMirrored);

            var smallSample = Path.Combine(folder, "SampleVideos", sampleName + ".mp4");
            if (File.Exists(smallSample)) return (smallSample, CameraOrientation.UprightMirrored);

            throw new FileNotFoundException($"Simulation video not found: {smallSample}", smallSample);
        }
    }

    /// <summary>
    /// Settings of the Simulation eye tracking system.
    /// </summary>
    public class EyeTrackingSystemSimulationSettings : EyeTrackingSystemSettings
    {
        /// <summary>
        /// Video of the left eye. Empty to use the sample video.
        /// </summary>
        [Category("Simulation"), Description("Video of the left eye (full path, or relative to the OpenIris folder). Empty to use the sample video.")]
        [NeedsRestarting]
        public string LeftEyeVideo { get => leftEyeVideo; set => SetProperty(ref leftEyeVideo, value, nameof(LeftEyeVideo)); }
        private string leftEyeVideo = string.Empty; // Default value

        /// <summary>
        /// Video of the right eye. Empty to use the sample video.
        /// </summary>
        [Category("Simulation"), Description("Video of the right eye (full path, or relative to the OpenIris folder). Empty to use the sample video.")]
        [NeedsRestarting]
        public string RightEyeVideo { get => rightEyeVideo; set => SetProperty(ref rightEyeVideo, value, nameof(RightEyeVideo)); }
        private string rightEyeVideo = string.Empty; // Default value

        /// <summary>
        /// Orientation of the videos set in LeftEyeVideo and RightEyeVideo.
        /// </summary>
        [Category("Simulation"), Description("Orientation of the videos set in LeftEyeVideo and RightEyeVideo. Upright for videos recorded with OpenIris.")]
        [NeedsRestarting]
        public CameraOrientation VideoOrientation { get => videoOrientation; set => SetProperty(ref videoOrientation, value, nameof(VideoOrientation)); }
        private CameraOrientation videoOrientation = CameraOrientation.Upright; // Default value
    }
}
