//-----------------------------------------------------------------------
// <copyright file="CameraTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests with real cameras: the two FLIR cameras of Spinnaker Dual Camera. They start the built
    /// OpenIris.exe (bin\x64\CONFIGURATION) in a temporary folder, so the user's settings aren't
    /// touched, and drive it through the web API.
    /// </summary>
    /// <remarks>
    /// They skip themselves (inconclusive) when OpenIris isn't built, port 9001 is in use (OpenIris
    /// already open), the Spinnaker plugin isn't installed, or the cameras don't start. Category
    /// Hardware: the continuous integration leaves them out.
    /// </remarks>
    [TestClass]
    public class CameraTests
    {
        private const string Api = "http://localhost:9001/api";
        private const string SystemName = "Spinnaker Dual Camera";

        public TestContext TestContext { get; set; } = null!;

        [DataTestMethod]
        [TestCategory("Hardware")]
        [DataRow(100, true, DisplayName = "100 Hz with raw videos")]
        [DataRow(450, false, DisplayName = "450 Hz without raw videos")]
        [Timeout(180000)]
        public void SpinnakerDualCameraRecordsEveryFrame(int frameRate, bool recordVideo)
        {
            var config = typeof(CameraTests).Assembly.Location.Contains(@"\Release\") ? "Release" : "Debug";
            var exe = Path.GetFullPath(Path.Combine(PipelineHarness.SourceFolder, "..", "..", "bin", "x64", config, "OpenIris.exe"));
            if (!File.Exists(exe)) Assert.Inconclusive($"OpenIris isn't built: {exe}");
            if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port == 9001))
            {
                Assert.Inconclusive("Port 9001 is in use: close OpenIris to run the camera tests.");
            }

            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisCameraTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var openIris = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = folder, UseShellExecute = false })!;
            var keepFolder = false;
            try
            {
                WaitFor(() => TryGet(http, "/status") != null, TimeSpan.FromSeconds(60), "OpenIris didn't start.");

                Put(http, "EyeTrackerSystem", SystemName);
                if (Status(http).GetProperty("EyeTrackerSystem").GetString() != SystemName) Assert.Inconclusive($"The {SystemName} plugin isn't installed.");
                Put(http, "System.FrameRate", frameRate);
                Put(http, "MaxNumberOfProcessingThreads", 4);
                Put(http, "DataFolder", folder);
                Put(http, "RecordVideo", recordVideo);
                Put(http, "SessionName", "CameraTest");

                Post(http, "/tracking/start");
                if (!WaitUntil(() => Status(http).GetProperty("Tracking").GetBoolean(), TimeSpan.FromSeconds(20)))
                {
                    Assert.Inconclusive("The cameras didn't start (not connected?). " + LastError(Path.Combine(Path.GetDirectoryName(exe), "Logs")));
                }
                Thread.Sleep(3000);

                Post(http, "/recording/start");
                Thread.Sleep(20000);
                Post(http, "/recording/stop");
                WaitFor(() => !Status(http).GetProperty("Recording").GetBoolean(), TimeSpan.FromSeconds(60), "The recording didn't stop.");

                var status = Status(http);
                Post(http, "/tracking/stop");
                TestContext.WriteLine($"{status.GetProperty("GrabberStatus").GetString()} | {status.GetProperty("ProcessorStatus").GetString()}");

                keepFolder = true;
                Assert.AreEqual(0, Number(status.GetProperty("GrabberStatus").GetString()!, @"Dropped:(\d+)"), "Frames dropped by the cameras.");
                Assert.AreEqual(0, Number(status.GetProperty("ProcessorStatus").GetString()!, @"Dropped:(\d+)"), "Frames dropped by the processing.");

                // The recording's log: frames to record >> not dropped >> recorded, for each recorder.
                var dataFile = Directory.GetFiles(folder, "CameraTest-*.txt", SearchOption.AllDirectories).Single(f => !f.Contains("-events") && !f.Contains("-videoframes"));
                var log = File.ReadAllText(dataFile.Replace(".txt", "-log.log"));
                var processed = Counts(log, "ProcessedFramesRecorder");
                TestContext.WriteLine($"Processed frames recorder: {string.Join(" >> ", processed)}");
                Assert.IsTrue(processed[0] > frameRate * 15, $"Only {processed[0]} frames in 20 s at {frameRate} Hz.");
                Assert.IsTrue(processed.All(n => n == processed[0]), "Processed frames lost.");
                Assert.AreEqual(processed[0] + 1, File.ReadLines(dataFile).Count(), "Lines of the data file (with the header).");

                // Each frame has the window of the sensor it was taken with: 720x450, at an offset
                // that is a multiple of 4.
                var dataLines = File.ReadAllLines(dataFile);
                var columns = dataLines[0].Split(' ');
                foreach (var line in dataLines.Skip(1).Select(l => l.Split(' ')))
                {
                    foreach (var eye in new[] { "Left", "Right" })
                    {
                        double Column(string name) => double.Parse(line[Array.IndexOf(columns, eye + name)], System.Globalization.CultureInfo.InvariantCulture);
                        Assert.AreEqual(720, Column("RoiWidth"), eye + "RoiWidth");
                        Assert.AreEqual(450, Column("RoiHeight"), eye + "RoiHeight");
                        Assert.IsTrue(Column("RoiX") >= 0 && Column("RoiX") % 4 == 0, $"{eye}RoiX {Column("RoiX")}");
                        Assert.IsTrue(Column("RoiY") >= 0 && Column("RoiY") % 4 == 0, $"{eye}RoiY {Column("RoiY")}");
                    }
                }

                var first = dataLines[1].Split(' ');
                TestContext.WriteLine($"ROI left ({first[Array.IndexOf(columns, "LeftRoiX")]}, {first[Array.IndexOf(columns, "LeftRoiY")]}), right ({first[Array.IndexOf(columns, "RightRoiX")]}, {first[Array.IndexOf(columns, "RightRoiY")]})");

                if (recordVideo)
                {
                    var raw = Counts(log, "RawFramesRecorder");
                    TestContext.WriteLine($"Raw frames recorder: {string.Join(" >> ", raw)}");
                    Assert.IsTrue(raw.All(n => n == processed[0]), "Raw video frames lost.");
                    foreach (var eye in new[] { "Left", "Right" })
                    {
                        var video = new FileInfo(dataFile.Replace(".txt", $"-{eye}.avi"));
                        Assert.IsTrue(video.Exists && video.Length > 0, $"No {eye} raw video.");
                    }

                    // Every frame has its place in both videos in -videoframes.txt.
                    var videoFrames = File.ReadAllLines(dataFile.Replace(".txt", "-videoframes.txt")).Skip(1).ToArray();
                    Assert.AreEqual(raw[2], videoFrames.Length, "Lines in -videoframes.txt.");
                    Assert.IsFalse(videoFrames.Any(l => l.Contains("NaN")), "Frames missing from a raw video.");
                }

                keepFolder = false;
            }
            finally
            {
                if (!openIris.HasExited)
                {
                    openIris.CloseMainWindow();
                    if (!openIris.WaitForExit(20000)) openIris.Kill();
                }

                if (keepFolder) TestContext.WriteLine($"Recording kept in {folder}");
                else Directory.Delete(folder, true);
            }
        }

        private static string? TryGet(HttpClient http, string path)
        {
            try
            {
                return http.GetStringAsync(Api + path).Result;
            }
            catch (AggregateException)
            {
                return null;
            }
        }

        private static JsonElement Status(HttpClient http) => JsonDocument.Parse(http.GetStringAsync(Api + "/status").Result).RootElement;

        private static void Put(HttpClient http, string setting, object value)
        {
            using var content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
            var response = http.PutAsync($"{Api}/settings/{setting}", content).Result;
            Assert.IsTrue(response.IsSuccessStatusCode, $"Setting {setting}: {response.Content.ReadAsStringAsync().Result}");
        }

        private static void Post(HttpClient http, string path)
        {
            var response = http.PostAsync(Api + path, null).Result;
            Assert.IsTrue(response.IsSuccessStatusCode, $"{path}: {response.Content.ReadAsStringAsync().Result}");
        }

        private static bool WaitUntil(Func<bool> condition, TimeSpan limit)
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < limit)
            {
                if (condition()) return true;
                Thread.Sleep(500);
            }
            return false;
        }

        private static void WaitFor(Func<bool> condition, TimeSpan limit, string message)
        {
            if (!WaitUntil(condition, limit)) Assert.Fail(message);
        }

        private static int Number(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static int[] Counts(string log, string recorder)
        {
            var match = Regex.Match(log, recorder + @" -> \((\d+) >> (\d+) >> (\d+)\)");
            Assert.IsTrue(match.Success, $"No {recorder} counts in the recording log.");
            return new[] { 1, 2, 3 }.Select(i => int.Parse(match.Groups[i].Value)).ToArray();
        }

        private static string LastError(string logs)
        {
            var log = Directory.Exists(logs) ? new DirectoryInfo(logs).GetFiles("*.Log").OrderBy(f => f.LastWriteTime).LastOrDefault()?.FullName : null;
            if (log is null) return string.Empty;
            using var reader = new StreamReader(new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return reader.ReadToEnd().Split('\n').LastOrDefault(l => l.Contains("ERROR"))?.Trim() ?? string.Empty;
        }
    }
}
