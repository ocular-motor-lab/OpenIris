//-----------------------------------------------------------------------
// <copyright file="Program.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Process
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Processes a recording from the command line, as Process Video does in OpenIris: for scripts that
    /// process many recordings. See docs/user-manual/processing-videos.md.
    /// </summary>
    internal static class Program
    {
        private const string Usage = @"Processes the videos of a recording with OpenIris, as Process Video does.

OpenIrisProcess [options] VIDEO [VIDEO]

  VIDEO               One video with both eyes, or the left and right videos (NAME-Left.avi and
                      NAME-Right.avi, or left first).
  --profile NAME      Settings profile with the pipeline and its settings. Default: the one used last.
  --system NAME       Eye tracking system the videos were recorded with. Default: the profile's.
  --calibration FILE  Calibration (.cal) for the calibrated data. Default: none.
  --output FOLDER     Folder for the data. Default: the DataFolder setting.
  --range FIRST-LAST  Only these video frames, counting from 0. Default: all.
  --save-video        Also save the processed video, with the tracking drawn on it.
  --verbose           Show the log.

The data goes to a new folder, NAME-PostProc-DATE, whose data file is printed at the end. Exit code 0
if the recording was processed, 1 for wrong arguments, 2 if processing failed.";

        private static int Main(string[] args)
        {
            try
            {
                return Run(args).GetAwaiter().GetResult();
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                Console.Error.WriteLine("Run OpenIrisProcess --help for the options.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR processing: " + ex.Message);
                Trace.WriteLine("ERROR processing: " + ex);
                return 2;
            }
        }

        private static async Task<int> Run(string[] args)
        {
            if (args.Length == 0 || args.Any(a => a == "--help" || a == "-h" || a == "/?"))
            {
                Console.WriteLine(Usage);
                return args.Length == 0 ? 1 : 0;
            }

            // The options.
            string? profile = null, system = null, calibration = null, output = null;
            Range range = new Range();
            bool saveVideo = false, verbose = false;
            var videos = new System.Collections.Generic.List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");
                switch (args[i])
                {
                    case "--profile": profile = Value(); break;
                    case "--system": system = Value(); break;
                    case "--calibration": calibration = Path.GetFullPath(Value()); break;
                    case "--output": output = Path.GetFullPath(Value()); break;
                    case "--range": range = ParseRange(Value()); break;
                    case "--save-video": saveVideo = true; break;
                    case "--verbose": verbose = true; break;
                    default:
                        if (args[i].StartsWith("--")) throw new ArgumentException($"Unknown option {args[i]}.");
                        videos.Add(Path.GetFullPath(args[i]));
                        break;
                }
            }

            if (videos.Count < 1 || videos.Count > 2) throw new ArgumentException("Give one video, or the left and right videos.");
            foreach (var file in videos.Append(calibration).Where(f => f != null))
            {
                if (!File.Exists(file)) throw new ArgumentException($"File not found: {file}");
            }

            if (verbose) Trace.Listeners.Add(new ConsoleTraceListener());

            // The settings are in the OpenIris folder (the paths above are already full).
            Directory.SetCurrentDirectory(OpenIrisFolders.Program);

            // No remote control servers: OpenIris may be open, with the ports.
            var eyeTracker = EyeTracker.Create(profile, startRemoteServices: false);
            var settings = eyeTracker.Settings;
            if (profile != null && !string.Equals(settings.Profile, profile, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"There is no settings profile '{profile}'. The profiles: {string.Join(", ", settings.Profiles)}.");
            }

            // Only for this run: the settings aren't saved.
            if (output != null) settings.DataFolder = output;

            var options = new ProcessVideoOptions
            {
                VideoFileNames = videos.Count == 1 ? new EyeCollection<string?>(videos[0]) : LeftAndRight(videos[0], videos[1]),
                EyeTrackingSystem = system ?? settings.EyeTrackerSystem,
                CalibrationFileName = calibration ?? string.Empty,
                CustomRange = range,
                SaveProcessedVideo = saveVideo,
            };
            Console.WriteLine($"Processing {string.Join(" and ", videos.Select(Path.GetFileName))} with the profile '{settings.Profile}' " +
                $"({options.EyeTrackingSystem}, {settings.EyeTrackingPipeline}){(calibration is null ? "" : ", calibration " + Path.GetFileName(calibration))}.");

            // Progress every few seconds while it processes.
            var processing = eyeTracker.ProcessVideo(options);
            while (await Task.WhenAny(processing, Task.Delay(3000)) != processing)
            {
                if (eyeTracker.VideoPlayer is VideoPlayer player)
                {
                    var range0 = player.FrameRange;
                    var done = (long)player.CurrentFrameNumber - range0.Begin;
                    var total = range0.End - range0.Begin + 1;
                    Console.WriteLine($"  {Math.Max(0, done)} of {total} frames ({100.0 * Math.Max(0, done) / Math.Max(1, total):F0}%)");
                }
            }

            await processing;
            Console.WriteLine("Data file: " + settings.LastRecordedFile);
            return 0;
        }

        private static EyeCollection<string?> LeftAndRight(string first, string second)
        {
            bool IsRight(string file) => Path.GetFileNameWithoutExtension(file).EndsWith("-Right", StringComparison.OrdinalIgnoreCase);
            return IsRight(first) && !IsRight(second) ? new EyeCollection<string?>(second, first) : new EyeCollection<string?>(first, second);
        }

        private static Range ParseRange(string text)
        {
            var parts = text.Split('-');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var first) || !long.TryParse(parts[1], out var last) || first < 0 || last < first)
            {
                throw new ArgumentException($"--range needs FIRST-LAST, for example 100-5000, not '{text}'.");
            }

            return new Range(first, last);
        }
    }
}
