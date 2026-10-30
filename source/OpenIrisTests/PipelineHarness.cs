//-----------------------------------------------------------------------
// <copyright file="PipelineHarness.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Runs eye tracking pipelines over a fixed sequence of frames, the same way EyeTrackerProcessor
    /// does for one eye, but on the calling thread and without dropping frames.
    /// </summary>
    internal static class PipelineHarness
    {
        private static readonly object initLock = new object();
        private static bool pluginsLoaded;

        /// <summary>
        /// Gets the folder of this project's source files (where the Baselines folder lives).
        /// </summary>
        public static string SourceFolder { get; } = GetSourceFolder();

        /// <summary>
        /// Gets the full path of one of the sample videos included with OpenIris (source\SampleVideos),
        /// the same ones the Simulation eye tracking system plays.
        /// </summary>
        public static string SampleVideo(Eye whichEye) =>
            Path.GetFullPath(Path.Combine(SourceFolder, "..", "SampleVideos", whichEye == Eye.Left ? "JorgeLeft.mp4" : "JorgeRight.mp4"));

        /// <summary>
        /// Processes the frames with a new instance of the pipeline, using its default settings and an
        /// empty calibration. Frames are disposed after processing.
        /// </summary>
        public static List<EyeData> Run(string pipelineName, Eye whichEye, IEnumerable<ImageEye> frames, double mmPerPix)
        {
            using var pipeline = CreatePipeline(pipelineName, whichEye, mmPerPix);
            var calibration = new EyeCalibration(whichEye);
            var results = new List<EyeData>();

            foreach (var frame in frames)
            {
                using (frame)
                {
                    var (data, imageTorsion) = pipeline.Process(frame, calibration);
                    imageTorsion?.Dispose();
                    results.Add(data);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a pipeline the same way the eye tracker does, through the plugin manager.
        /// </summary>
        public static EyeTrackingPipelineBase CreatePipeline(string pipelineName, Eye whichEye, double mmPerPix)
        {
            EnsurePluginsLoaded();

            var settings = (EyeTrackingPipelineSettings)EyeTrackerPluginManager.EyeTrackingPipelineFactory!.GetDefaultSettings(pipelineName);
            settings.MmPerPix = mmPerPix;
            return EyeTrackingPipelineBase.Create(pipelineName, whichEye, settings);
        }

        /// <summary>
        /// Reads the first frames of a video, oriented as the Simulation eye tracking system does.
        /// </summary>
        public static IEnumerable<ImageEye> ReadVideo(string fileName, Eye whichEye, int count)
        {
            using var video = new VideoEye(whichEye, fileName) { CameraOrientation = CameraOrientation.UprightMirrored };
            for (int i = 0; i < count; i++)
            {
                yield return video.GrabImageEyeFromVideo();
            }
        }

        private static void EnsurePluginsLoaded()
        {
            lock (initLock)
            {
                if (pluginsLoaded) return;

                // Safe mode: only the pipelines built into OpenIrisLib, no plugin folder.
                EyeTrackerPluginManager.Init(safeMode: true);
                pluginsLoaded = true;
            }
        }

        private static string GetSourceFolder([CallerFilePath] string path = "") => Path.GetDirectoryName(path);
    }
}
