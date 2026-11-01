//-----------------------------------------------------------------------
// <copyright file="RecordAndProcessTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Runs OpenIris end to end without the user interface, as a user would: tracks with the
    /// Simulation system, records a short raw video of both eyes, and then processes that recording
    /// saving the processed video (GitHub issue #41: the processed video used to come out empty).
    /// </summary>
    [TestClass]
    public class RecordAndProcessTests
    {
        private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(60);

        [TestMethod]
        [Timeout(300000)]
        public async Task RecordedVideoCanBeProcessedAndSaved()
        {
            // EyeTracker loads and saves its settings (OpenIrisSettings.xml, Settings\) in the current folder. Use a new
            // temporary folder so the test starts from the default settings and never touches the
            // user's settings. The recordings go there too. It is kept if the test fails.
            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var previousFolder = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folder);

            var eyeTracker = EyeTracker.Create();
            try
            {
                eyeTracker.Settings.EyeTrackerSystem = "Simulation";
                var simulation = (EyeTrackingSystemSimulationSettings)eyeTracker.Settings.EyeTrackingSystemSettings;
                simulation.LeftEyeVideo = PipelineHarness.SampleVideo(Eye.Left);
                simulation.RightEyeVideo = PipelineHarness.SampleVideo(Eye.Right);
                simulation.VideoOrientation = CameraOrientation.UprightMirrored;
                eyeTracker.Settings.DataFolder = folder;
                eyeTracker.Settings.SessionName = "Test";
                eyeTracker.Settings.RecordVideo = true;

                // Track and record a short raw video.
                var tracking = eyeTracker.StartTracking();
                await WaitUntil(() => eyeTracker.Tracking, tracking, "tracking to start");

                // Also record events: one right after starting, before the first frame is recorded
                // (as a stimulus program marking the start of a trial would), and one later.
                var recording = eyeTracker.StartRecording();
                eyeTracker.RecordEvent("RightAfterStart", null);
                await WaitUntil(() => eyeTracker.Recording, recording, "recording to start");
                await Task.Delay(1500);
                eyeTracker.RecordEvent("DuringRecording", null);
                eyeTracker.StopRecording();
                await WithinLimit(recording, "recording to finish");

                eyeTracker.StopTracking();
                await WithinLimit(tracking, "tracking to stop");

                var events = File.ReadAllText(Directory.GetFiles(folder, "Test-*-events.txt", SearchOption.AllDirectories).Single());
                StringAssert.Contains(events, "RightAfterStart", "An event sent right after starting the recording was lost.");
                StringAssert.Contains(events, "DuringRecording");

                var rawLeft = Directory.GetFiles(folder, "Test-*-Left.avi", SearchOption.AllDirectories).Single();
                var rawRight = rawLeft.Replace("-Left.avi", "-Right.avi");
                var (recordedFrames, eyeWidth) = ReadVideo(rawLeft);
                Assert.IsTrue(recordedFrames >= 10, $"Only {recordedFrames} frames were recorded in {rawLeft}.");
                Assert.AreEqual(recordedFrames, ReadVideo(rawRight).frames, "The left and right recordings have a different number of frames.");

                // Every frame in the videos is in -videoframes.txt, in order. (The data file may not
                // have them all: processing can drop frames the raw videos have.)
                var videoFrames = File.ReadAllLines(rawLeft.Replace("-Left.avi", "-videoframes.txt")).Skip(1).Select(l => l.Split(' ')).ToArray();
                Assert.AreEqual(recordedFrames, videoFrames.Length, "Lines in -videoframes.txt.");
                CollectionAssert.AreEqual(Enumerable.Range(0, recordedFrames).Select(i => i.ToString()).ToArray(), videoFrames.Select(f => f[1]).ToArray());
                CollectionAssert.AreEqual(Enumerable.Range(0, recordedFrames).Select(i => i.ToString()).ToArray(), videoFrames.Select(f => f[2]).ToArray());
                var frameNumbers = videoFrames.Select(f => long.Parse(f[0])).ToArray();
                Assert.IsTrue(frameNumbers.Zip(frameNumbers.Skip(1), (a, b) => b > a).All(x => x), "Frame numbers of -videoframes.txt should increase.");

                // Process the recording saving the processed video, as with Process Video in the menu.
                await WithinLimit(
                    eyeTracker.ProcessVideo(new ProcessVideoOptions
                    {
                        VideoFileNames = new EyeCollection<string?>(rawLeft, rawRight),
                        EyeTrackingSystem = "Simulation",
                        SaveProcessedVideo = true,
                    }),
                    "processing to finish");

                var processedVideo = Directory.GetFiles(folder, "*-PostProc-*.avi", SearchOption.AllDirectories).Single();
                var (processedFrames, processedWidth) = ReadVideo(processedVideo);

                Assert.IsTrue(processedFrames > 0, $"The processed video is empty: {processedVideo}");
                Assert.AreEqual(2 * eyeWidth, processedWidth, "The processed video should show both eyes side by side.");
                Assert.IsTrue(
                    processedFrames >= recordedFrames - 2,
                    $"The processed video has {processedFrames} frames but {recordedFrames} were recorded.");
            }
            finally
            {
                eyeTracker.Dispose();
                Directory.SetCurrentDirectory(previousFolder);
            }

            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                // A file may still be open. It is in the temp folder anyway.
            }
        }

        /// <summary>
        /// Reads a video and returns its number of frames and its width in pixels.
        /// </summary>
        private static (int frames, int width) ReadVideo(string fileName)
        {
            using var video = new VideoCapture(fileName);
            var frames = 0;
            var width = 0;
            while (true)
            {
                using var frame = video.QueryFrame();
                if (frame is null || frame.IsEmpty) break;

                width = frame.Width;
                frames++;
            }

            return (frames, width);
        }

        /// <summary>
        /// Waits for a condition, failing if the task behind it ends first (with its error, if any)
        /// or if it takes too long.
        /// </summary>
        private static async Task WaitUntil(Func<bool> condition, Task running, string what)
        {
            var deadline = DateTime.Now + WaitLimit;
            while (!condition())
            {
                if (running.IsCompleted)
                {
                    await running;
                    Assert.Fail($"Stopped before {what}.");
                }

                if (DateTime.Now > deadline) Assert.Fail($"Timed out waiting for {what}.");

                await Task.Delay(50);
            }
        }

        /// <summary>
        /// Waits for a task, failing if it takes too long.
        /// </summary>
        private static async Task WithinLimit(Task task, string what)
        {
            if (await Task.WhenAny(task, Task.Delay(WaitLimit)) != task) Assert.Fail($"Timed out waiting for {what}.");

            await task;
        }
    }
}
