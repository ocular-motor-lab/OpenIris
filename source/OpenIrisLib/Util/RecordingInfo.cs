//-----------------------------------------------------------------------
// <copyright file="RecordingInfo.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// What is written at the beginning of the log of each recording: the version of OpenIris, the
    /// computer, the time, the system and its cameras, the settings that matter most and the
    /// calibration. Everything needed later to know how a recording was made, next to the data.
    /// The main log gets the same, without the recording: the version and the computer when
    /// OpenIris starts, the rest each time tracking starts.
    /// </summary>
    internal static class RecordingInfo
    {
        /// <summary>
        /// Version of OpenIris, as GitVersion stamps it on OpenIris.exe (0.2.0-alpha.5+...Sha...).
        /// Only the exe has it: OpenIrisLib's assembly version otherwise (in the tests).
        /// </summary>
        public static readonly string Version =
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == "OpenIris")
                .Select(a => (a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute)?.InformationalVersion)
                .FirstOrDefault(v => v is not null)
            ?? $"OpenIrisLib {typeof(EyeTracker).Assembly.GetName().Version}";

        /// <summary>
        /// The information about a recording, as lines of text.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker that records.</param>
        /// <param name="dataFileName">The data file of the recording.</param>
        /// <param name="options">The options of the recording.</param>
        /// <param name="videos">The videos processed, when processing videos (the eye tracking
        /// system isn't there yet when the recording starts).</param>
        public static string Describe(EyeTracker eyeTracker, string dataFileName, RecordingOptions options, ProcessVideoOptions? videos = null) =>
            Describe(eyeTracker, "OpenIris recording", dataFileName, options, videos);

        /// <summary>
        /// The information written in the main log when OpenIris starts: what doesn't change, the
        /// version and the computer.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker, with its settings loaded.</param>
        public static string DescribeStartup(EyeTracker eyeTracker) =>
            Describe(eyeTracker, "OpenIris started", null, null, null, setup: false);

        /// <summary>
        /// The information written in the main log when tracking starts: the same as for a
        /// recording, without the recording and without what was written when OpenIris started.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker, with its eye tracking system created.</param>
        public static string DescribeTracking(EyeTracker eyeTracker) =>
            Describe(eyeTracker, "OpenIris tracking", null, null, null, computer: false);

        private static string Describe(EyeTracker eyeTracker, string title, string? dataFileName, RecordingOptions? options, ProcessVideoOptions? videos, bool setup = true, bool computer = true)
        {
            var now = DateTimeOffset.Now;
            var settings = eyeTracker.Settings;
            var system = eyeTracker.EyeTrackingSystem;
            var grabber = eyeTracker.ImageGrabber;
            var text = new StringBuilder();

            void Line(string name, object? value, int indent = 0) =>
                text.AppendLine((new string(' ', indent) + name + ":").PadRight(24) + " " + Convert.ToString(value, CultureInfo.InvariantCulture));
            void Section(string title) => text.AppendLine().AppendLine(title);
            string Size(System.Drawing.Size size) => $"{size.Width} x {size.Height} px";

            text.AppendLine($" {title} ".PadLeft(30 + (title.Length + 2) / 2, '=').PadRight(60, '='));
            Line("Started", $"{now:yyyy-MM-dd HH:mm:ss.fff zzz}");
            Line("Started (UTC)", $"{now.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff}");
            Line("OpenIris version", Version);
            if (setup)
            {
                Line("Settings profile", settings.Profile);
                if (videos is null) Line("Session", settings.SessionName);
                if (dataFileName is not null && options is not null)
                {
                    Line("Data file", $"{dataFileName} ({Columns(EyeTrackerData.GetStringHeader())} columns)");
                    Line("Calibrated data file", $"{dataFileName.Replace(".txt", "-calibrated.txt")} ({Columns(EyeTrackerData.GetCalibratedStringHeader())} columns)");
                    Line("Raw videos", !options.SaveRawVideo ? "no" : options.DecimateRatioRawVideo > 1 ? $"1 of every {options.DecimateRatioRawVideo} frames" : "every frame");
                }
                else
                {
                    Line("Data folder", settings.DataFolder);
                    if (!computer) Line("Free disk space", FreeSpace(settings.DataFolder));
                }

                Line("LSL stream", settings.StreamToLsl ? settings.LslStreamName : "off");

                if (videos is not null)
                {
                    Section("Processing videos");
                    Line("Left video", videos.VideoFileNames[Eye.Left] ?? "none", 2);
                    Line("Right video", videos.VideoFileNames[Eye.Right] ?? "none", 2);
                    Line("Recorded with", videos.EyeTrackingSystem, 2);
                    Line("Calibration file", string.IsNullOrEmpty(videos.CalibrationFileName) ? "none" : videos.CalibrationFileName, 2);
                    Line("Frames", videos.CustomRange.IsEmpty ? "all" : $"{videos.CustomRange.Begin} to {videos.CustomRange.End}", 2);
                    Line("Processed video", videos.SaveProcessedVideo ? "yes" : "no", 2);
                }

                // When processing videos, the system is created after the recording starts.
                if (system is not null || videos is null)
                {
                    Section("Eye tracking system");
                    Line("Name", system?.Name ?? "none", 2);
                }

                if (system is not null)
                {
                    Line("Pixel scale", $"{system.Settings.PixPerMm} pixels per mm", 2);
                    Line("Frame rate set", $"{system.Settings.FrameRate} Hz", 2);
                    if (grabber is not null && grabber.FrameRateMeasured > 0) Line("Frame rate measured", $"{grabber.FrameRateMeasured:0.0} Hz", 2);
                    if (grabber is not null && !grabber.FrameSize.IsEmpty) Line("Frame size", Size(grabber.FrameSize), 2);
                    Line("Lights", system.Settings.Lights.Length == 0 ? "none" : string.Join(", ", system.Settings.Lights), 2);
                    Line("Between the eyes", $"{system.Settings.DistanceBetweenEyesMm} mm", 2);

                    foreach (var source in system.imageSources?.ToArray() ?? Array.Empty<IImageEyeSource?>())
                    {
                        if (source is not CameraEye camera)
                        {
                            if (source is not null) Line("Image source", source.GetType().Name, 2);
                            continue;
                        }

                        Section($"Camera {camera.WhichEye}");
                        Line("Device", string.IsNullOrEmpty(camera.Description) ? camera.GetType().Name : camera.Description, 2);
                        Line("Frame", camera.SensorSize == camera.FrameSize ? Size(camera.FrameSize) : $"{Size(camera.FrameSize)} of a {Size(camera.SensorSize)} sensor", 2);
                        Line("Frame rate", $"{camera.FrameRate} Hz", 2);
                        Line("Orientation", camera.CameraOrientation, 2);
                        Line("Intrinsics", camera.Intrinsics, 2);
                        var extrinsics = system.Settings.CameraExtrinsics?.Count switch
                        {
                            1 => system.Settings.CameraExtrinsics[Eye.Both],
                            2 when camera.WhichEye != Eye.Both => system.Settings.CameraExtrinsics[camera.WhichEye],
                            _ => default,
                        };
                        Line("Extrinsics", extrinsics.IsEmpty ? "Unknown (facing the eye)" : extrinsics.ToString(), 2);
                        var cameraTime = camera.GetCameraTime();
                        if (!double.IsNaN(cameraTime)) Line("Camera time", $"{cameraTime:0.000000} s (the camera's clock now, as Seconds in the data file)", 2);
                    }
                }

                Section("Tracking and calibration");
                Line("Tracking pipeline", settings.EyeTrackingPipeline, 2);
                Line("Calibration method", settings.CalibrationMethod, 2);
                foreach (var eye in eyeTracker.Calibration.EyeCalibrationParameters)
                {
                    if (eye is null) continue;
                    var model = eye.EyePhysicalModel;
                    var reference = eye.ReferenceData.Pupil.Center;
                    Line($"{eye.WhichEye} eye model", eye.HasEyeModel ? $"center ({model.Center.X:0.0}, {model.Center.Y:0.0}) px, radius {model.Radius:0.0} px" : "none", 2);
                    Line($"{eye.WhichEye} reference", eye.HasReference ? $"pupil at ({reference.X:0.0}, {reference.Y:0.0}) px" : "none", 2);
                    Line($"{eye.WhichEye} intrinsics", eye.Intrinsics, 2);
                    Line($"{eye.WhichEye} extrinsics", eye.Extrinsics, 2);
                }
            }

            if (computer)
            {
                Section("Computer");
                Line("Name", Environment.MachineName, 2);
                Line("Windows", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})", 2);
                Line("Processor", $"{Registry(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString")?.Trim()}, {Environment.ProcessorCount} logical processors", 2);
                Line("Memory", $"{TotalMemoryGB():0.0} GB", 2);
                if (setup) Line("Free disk space", FreeSpace(dataFileName ?? settings.DataFolder), 2);
                Line(".NET", $"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture} process", 2);
                Line("Timer", Stopwatch.IsHighResolution ? $"high resolution, {Stopwatch.Frequency} Hz" : "low resolution", 2);
            }

            text.AppendLine();
            if (dataFileName is not null) text.AppendLine("All the settings and the calibration: the -settings.xml and .cal files of the recording.");
            text.AppendLine("=============================================================");
            return text.ToString();
        }

        /// <summary>
        /// What is written at the end of the log of each recording.
        /// </summary>
        /// <param name="duration">How long the recording was.</param>
        /// <param name="dataFiles">What was written to the data files.</param>
        /// <param name="rawVideos">What was written to the raw videos.</param>
        /// <param name="events">What was written to the events file.</param>
        public static string Summary(TimeSpan duration, string dataFiles, string rawVideos, string events)
        {
            static string Line(string name, string value) => (name + ":").PadRight(24) + " " + value + Environment.NewLine;

            return Environment.NewLine +
                "================ OpenIris recording finished ================" + Environment.NewLine +
                Line("Duration", duration.ToString(@"hh\:mm\:ss\.fff")) +
                Line("Data files", dataFiles) +
                Line("Raw videos", rawVideos) +
                Line("Events", events) +
                "=============================================================" + Environment.NewLine;
        }

        private static int Columns(string header) => header.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

        private static string? Registry(string key, string name)
        {
            try
            {
                using var subKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key);
                return subKey?.GetValue(name)?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FreeSpace(string fileName)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(fileName)));
                return $"{drive.AvailableFreeSpace / 1e9:0.0} GB of {drive.TotalSize / 1e9:0.0} GB on {drive.Name}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static double TotalMemoryGB()
        {
            var status = new MemoryStatus { Length = (uint)Marshal.SizeOf(typeof(MemoryStatus)) };
            return GlobalMemoryStatusEx(ref status) ? status.TotalPhysical / 1e9 : double.NaN;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    }
}
