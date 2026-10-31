//-----------------------------------------------------------------------
// <copyright file="ManualLabelingTests.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// The Manual Labeling system (BasicPlugins) puts the labels a person made into the data: processing
    /// videos that have a labels file must write each marker where its settings say.
    /// </summary>
    [TestClass]
    public class ManualLabelingTests
    {
        [TestMethod]
        [Timeout(300000)]
        public async Task LabelsGoIntoTheDataFile()
        {
            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var previousFolder = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folder);

            // Copies of the sample videos, so the labels file goes in the temporary folder.
            var left = Path.Combine(folder, "Left.mp4");
            var right = Path.Combine(folder, "Right.mp4");
            File.Copy(PipelineHarness.SampleVideo(Eye.Left), left);
            File.Copy(PipelineHarness.SampleVideo(Eye.Right), right);
            File.WriteAllText(Path.Combine(folder, "Left-labels.csv"),
                "Frame,Eye,Marker,X,Y\r\n" +
                "5,Left,Pupil,100.5,80.25\r\n" +
                "5,Right,Pupil,110,90\r\n" +
                "5,Left,Tip,12.5,34\r\n" +
                "5,Right,Tip,56.4,78.6\r\n" +
                "7,Left,Glint,40,41\r\n" +
                "9,Left,NotAMarker,1,2\r\n");

            var eyeTracker = EyeTracker.Create();
            try
            {
                eyeTracker.Settings.EyeTrackerSystem = "Manual Labeling";
                eyeTracker.Settings.EyeTrackingPipeline = "Nothing";
                eyeTracker.Settings.DataFolder = folder;
                SetMarkers(eyeTracker.Settings.EyeTrackingSystemSettings,
                    ("Pupil", "Pupil", "Pupil"),
                    ("Glint", "CR2", "None"),
                    ("Tip", "Double0and1", "Int2and3"));

                var processing = eyeTracker.ProcessVideo(new ProcessVideoOptions
                {
                    VideoFileNames = new EyeCollection<string?>(left, right),
                    EyeTrackingSystem = "Manual Labeling",
                    CustomRange = new Range(0, 20),
                });
                Assert.AreSame(processing, await Task.WhenAny(processing, Task.Delay(TimeSpan.FromSeconds(120))), "Processing took too long.");
                await processing;

                var dataFile = Directory.GetFiles(folder, "*-PostProc-*.txt", SearchOption.AllDirectories).Single(f => !f.EndsWith("-events.txt"));
                var lines = File.ReadAllLines(dataFile);
                var columns = lines[0].Split(' ');
                var rows = lines.Skip(1).Select(l => l.Split(' ')).ToArray();
                double Value(string[] row, string column) => double.Parse(row[Array.IndexOf(columns, column)], CultureInfo.InvariantCulture);
                string[] Frame(int frame) => rows.First(r => Value(r, "LeftFrameNumber") == frame);

                var frame5 = Frame(5);
                Assert.AreEqual(100.5, Value(frame5, "LeftPupilX"), 1e-3);
                Assert.AreEqual(80.25, Value(frame5, "LeftPupilY"), 1e-3);
                Assert.AreEqual(110, Value(frame5, "RightPupilX"), 1e-3);
                Assert.AreEqual(12.5, Value(frame5, "Double0"), 1e-3, "Left Tip X goes in Double0.");
                Assert.AreEqual(34, Value(frame5, "Double1"), 1e-3);
                Assert.AreEqual(56, Value(frame5, "Int2"), "Right Tip X goes in Int2, rounded.");
                Assert.AreEqual(79, Value(frame5, "Int3"));

                var frame7 = Frame(7);
                Assert.AreEqual(40, Value(frame7, "LeftCR2X"), 1e-3, "Glint goes in CR2.");
                Assert.AreEqual(0, Value(frame7, "LeftPupilX"), "Frames without the marker keep the pipeline's values.");
                Assert.AreEqual(0, Value(frame7, "Double0"));

                Assert.AreEqual(0, Value(Frame(9), "LeftPupilX"), "A label of a marker not in the settings goes nowhere.");
            }
            finally
            {
                eyeTracker.Settings.EyeTrackerSystem = "Simulation";
                eyeTracker.Settings.EyeTrackingPipeline = "JOM";
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
        /// Sets the markers of the Manual Labeling settings: (name, left eye target, right eye target).
        /// Through reflection: OpenIris loads BasicPlugins from the Plugins folder, so the test can't
        /// use its types directly (they would be a second copy that doesn't match).
        /// </summary>
        private static void SetMarkers(object settings, params (string name, string left, string right)[] markers)
        {
            var plugin = settings.GetType().Assembly;
            var markerType = plugin.GetType("OpenIris.ManualLabelingMarker", true);
            var targetType = plugin.GetType("OpenIris.ManualLabelingMarkerTarget", true);

            var array = Array.CreateInstance(markerType, markers.Length);
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = Activator.CreateInstance(markerType);
                markerType.GetProperty("Name").SetValue(marker, markers[i].name);
                markerType.GetProperty("LeftEye").SetValue(marker, Enum.Parse(targetType, markers[i].left));
                markerType.GetProperty("RightEye").SetValue(marker, Enum.Parse(targetType, markers[i].right));
                array.SetValue(marker, i);
            }
            settings.GetType().GetProperty("Markers").SetValue(settings, array);
        }
    }
}
