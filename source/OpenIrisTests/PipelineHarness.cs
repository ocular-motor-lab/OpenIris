//-----------------------------------------------------------------------
// <copyright file="PipelineHarness.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
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
        /// Gets the full path of one of the sample videos included with OpenIris (sample-videos, at the root of the repository),
        /// the same ones the Simulation eye tracking system plays.
        /// </summary>
        public static string SampleVideo(Eye whichEye) =>
            Path.GetFullPath(Path.Combine(SourceFolder, "..", "..", "sample-videos", whichEye == Eye.Left ? "JorgeLeft.mp4" : "JorgeRight.mp4"));

        /// <summary>
        /// Pupil threshold for the synthetic eye: its pupil is 20 and the darkest iris spokes about
        /// 70, so 60 (the default) lets spokes into the pupil. Chosen with PipelineTuningTests.
        /// </summary>
        public const int SyntheticEyeDarkThreshold = 40;

        /// <summary>
        /// Pupil threshold for the sample videos, chosen with PipelineTuningTests: 60 to 80 work, 70
        /// has the least pupil jitter.
        /// </summary>
        public const int SampleVideoDarkThreshold = 70;

        /// <summary>
        /// Frame of the sample videos used as the calibration's reference, the same for both eyes
        /// because they are calibrated together: both pupils are round (the eyes look at the
        /// cameras), and torsion is less noisy than with frame 0.
        /// </summary>
        public const int SampleVideoReference = 346;

        /// <summary>
        /// Processes the first frames of the synthetic eye, with the first frame as the reference.
        /// </summary>
        public static List<EyeData> RunSyntheticEye(string pipelineName, Eye whichEye, int count, Action<EyeTrackingPipelineSettings>? configure = null) =>
            Run(pipelineName, whichEye, SyntheticEye.Frames(whichEye, count), SyntheticEye.MmPerPix,
                configure: configure ?? Thresholds(SyntheticEyeDarkThreshold));

        /// <summary>
        /// Processes the first frames of a sample video, calibrated with its reference frame. Same
        /// pixel scale as the Simulation system's default settings.
        /// </summary>
        public static List<EyeData> RunSampleVideo(string pipelineName, Eye whichEye, int count, int? referenceFrame = null, Action<EyeTrackingPipelineSettings>? configure = null) =>
            Run(pipelineName, whichEye, ReadVideo(SampleVideo(whichEye), whichEye, count), SyntheticEye.MmPerPix, eyeModelFromReference: true,
                reference: ReadFrame(SampleVideo(whichEye), whichEye, referenceFrame ?? SampleVideoReference),
                configure: configure ?? Thresholds(SampleVideoDarkThreshold));

        /// <summary>
        /// Sets the pupil (dark) and reflection (bright) thresholds of both eyes.
        /// </summary>
        public static Action<EyeTrackingPipelineSettings> Thresholds(int dark, int bright = 250) => s =>
        {
            if (s is not EyeTrackingPipelinePupilCRSettings settings) return;
            settings.DarkThresholdLeftEye = settings.DarkThresholdRightEye = dark;
            settings.BrightThresholdLeftEye = settings.BrightThresholdRightEye = bright;
        };

        /// <summary>
        /// Processes the frames with a new instance of the pipeline. The calibration's reference is
        /// the reference frame, or the first frame, as when setting the reference in a calibration,
        /// so torsion is measured against its iris. Frames are disposed after processing.
        /// </summary>
        /// <param name="eyeModelFromReference">Also take the eye model from the reference, as after
        /// a calibration. Leave it out for the synthetic eye, which moves without rotating: without
        /// an eye model, the pipeline centers the iris on the pupil of each frame.</param>
        /// <param name="reference">Frame to use as the reference (disposed), instead of the first.</param>
        /// <param name="configure">Changes to the pipeline's default settings, such as thresholds.</param>
        public static List<EyeData> Run(string pipelineName, Eye whichEye, IEnumerable<ImageEye> frames, double mmPerPix, bool eyeModelFromReference = false, ImageEye? reference = null, Action<EyeTrackingPipelineSettings>? configure = null)
        {
            using var pipeline = CreatePipeline(pipelineName, whichEye, mmPerPix, configure);
            EyeCalibration? calibration = null;
            if (reference is not null)
            {
                using (reference) calibration = Calibrate(pipelineName, reference, mmPerPix, eyeModelFromReference, configure);
            }

            var results = new List<EyeData>();
            foreach (var frame in frames)
            {
                if (frame is null) break;

                using (frame)
                {
                    calibration ??= Calibrate(pipelineName, frame, mmPerPix, eyeModelFromReference, configure);
                    var (data, imageTorsion) = pipeline.Process(frame, calibration);
                    imageTorsion?.Dispose();
                    results.Add(data);
                }
            }

            return results;
        }

        /// <summary>
        /// Makes a calibration with a frame as the reference, processed by a separate instance of
        /// the pipeline so the one being tested starts fresh.
        /// </summary>
        private static EyeCalibration Calibrate(string pipelineName, ImageEye reference, double mmPerPix, bool eyeModelFromReference, Action<EyeTrackingPipelineSettings>? configure)
        {
            var calibration = new EyeCalibration(reference.WhichEye);
            using var pipeline = CreatePipeline(pipelineName, reference.WhichEye, mmPerPix, configure);
            var (data, imageTorsion) = pipeline.Process(reference, calibration);
            if (eyeModelFromReference && data.ProcessFrameResult == ProcessFrameResult.Good)
            {
                calibration.SetEyeModel(new EyePhysicalModel(data.Iris.Center, (float)data.Iris.Radius * 2));
            }

            calibration.SetReference(new ImageEye(reference.Image, reference.WhichEye, reference.TimeStamp, null) { EyeData = data, ImageTorsion = imageTorsion });
            return calibration;
        }

        /// <summary>
        /// Creates a pipeline the same way the eye tracker does, through the plugin manager.
        /// </summary>
        public static EyeTrackingPipelineBase CreatePipeline(string pipelineName, Eye whichEye, double mmPerPix, Action<EyeTrackingPipelineSettings>? configure = null)
        {
            EnsurePluginsLoaded();

            var settings = (EyeTrackingPipelineSettings)EyeTrackerPluginManager.EyeTrackingPipelineFactory!.GetDefaultSettings(pipelineName);
            settings.MmPerPix = mmPerPix;

            // So torsion is exactly the same every run.
            if (settings is EyeTrackingPipelineJOMSettings jom) jom.TorsionNoiseSeed = 1;
            configure?.Invoke(settings);
            return EyeTrackingPipelineBase.Create(pipelineName, whichEye, settings);
        }

        /// <summary>
        /// Reads one frame of a video, oriented as the Simulation eye tracking system does.
        /// </summary>
        public static ImageEye ReadFrame(string fileName, Eye whichEye, int index)
        {
            using var video = new VideoEye(whichEye, fileName) { CameraOrientation = CameraOrientation.UprightMirrored };
            for (int i = 0; i < index; i++) video.GrabImageEyeFromVideo()?.Dispose();
            return video.GrabImageEyeFromVideo();
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

        internal static void EnsurePluginsLoaded()
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
