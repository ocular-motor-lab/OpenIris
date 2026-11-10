//-----------------------------------------------------------------------
// <copyright file="AppearanceTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.Calibration;

    /// <summary>
    /// The webcam's appearance-based gaze, against saccade.js's own test data (TestData\saccadejs): the
    /// strip of both eyes byte for byte, the network as OpenCV runs it, and the ridge regression; then
    /// the map through the N-point calibration and the calibration file.
    /// </summary>
    [TestClass]
    public class AppearanceTests
    {
        private static readonly string Data = Path.Combine(PipelineHarness.SourceFolder, "TestData", "saccadejs");

        [TestInitialize]
        public void LoadPlugins() => PipelineHarness.EnsurePluginsLoaded();

        [TestMethod]
        public void StripIsTheSameAsSaccadeJs()
        {
            using var frame = new Image<Bgr, byte>(Path.Combine(Data, "pipe_frame.png"));
            using var gray = frame.Convert<Gray, byte>();
            AssertSame("pipe_frame_gray.png", gray, "Gray as BT.601, as the webcam camera converts it.");

            // The landmarks are MediaPipe's, fractions of the image with 0 at the edge of the first pixel.
            using var cases = JsonDocument.Parse(File.ReadAllText(Path.Combine(Data, "cases.json")));
            var pipeline = cases.RootElement.GetProperty("pipeline");
            var mesh = pipeline.GetProperty("landmarks").EnumerateArray()
                .Select(p => new PointF(p[0].GetSingle() * gray.Width - 0.5f, p[1].GetSingle() * gray.Height - 0.5f))
                .ToArray();

            var box = pipeline.GetProperty("bbox");
            var expected = Rectangle.FromLTRB(box.GetProperty("left").GetInt32(), box.GetProperty("top").GetInt32(), box.GetProperty("right").GetInt32(), box.GetProperty("bottom").GetInt32());
            var region = AppearanceStrip.Region(mesh, gray.Size);
            Assert.AreEqual(expected, region);

            using (var crop = gray.Copy(region!.Value))
            {
                AssertSame("pipe_crop_gray.png", crop, "Crop.");
            }

            using var strip = AppearanceStrip.Cut(gray, mesh) ?? throw new AssertFailedException("No strip.");
            AssertSame("pipe_clahe.png", strip, "Stretched and equalized.");
        }

        [TestMethod]
        public void StripNeedsTheEyesInTheImage()
        {
            var mesh = Enumerable.Repeat(new PointF(-50, -50), 468).ToArray();
            Assert.IsNull(AppearanceStrip.Region(mesh, new Size(320, 240)), "Out of the image.");
        }

        /// <summary>
        /// ONNX Runtime runs the network (BasicPlugins\Webcam\Models) on the graphics card with DirectML as
        /// on the processor: the outputs of the original network with ONNX Runtime in Python. On the processor
        /// if there is no DirectML (GitHub's computers). The input as AppearanceNetwork makes it.
        /// </summary>
        [TestMethod]
        public void NetworkGivesTheSameOutputsOnTheGraphicsCard()
        {
            using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(Data, "embedding_reference.json")));
            var expected = reference.RootElement.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray();
            var expectedWeight = reference.RootElement.GetProperty("cal_weight").GetSingle();

            var model = Path.Combine(PipelineHarness.SourceFolder, "..", "BasicPlugins", "Webcam", "Models", "eye_embedding.onnx");
            using var strip = new Image<Gray, byte>(Path.Combine(Data, "pipe_clahe.png"));
            var input = new DenseTensor<float>(strip.Data.Cast<byte>().Select(b => (float)b).ToArray(), new[] { 1, AppearanceStrip.Height, AppearanceStrip.Width, 1 });

            InferenceSession session;
            try
            {
                using var options = new SessionOptions { EnableMemoryPattern = false, ExecutionMode = ExecutionMode.ORT_SEQUENTIAL };
                options.AppendExecutionProvider_DML(0);
                session = new InferenceSession(model, options);
            }
            catch (OnnxRuntimeException ex)
            {
                Console.WriteLine("No DirectML, on the processor: " + ex.Message);
                session = new InferenceSession(model);
            }

            using (session)
            using (var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor("eye_image", input) }))
            {
                var embedding = results.First(r => r.Name == "embedding").AsEnumerable<float>().ToArray();
                var weight = results.First(r => r.Name == "cal_weight").AsEnumerable<float>().First();

                Assert.AreEqual(expected.Length, embedding.Length);
                var worst = expected.Zip(embedding, (a, b) => Math.Abs(a - b)).Max();
                Assert.IsTrue(worst < 1e-4, $"Largest difference {worst}.");
                Assert.AreEqual(expectedWeight, weight, 1e-5);
            }
        }

        [TestMethod]
        public void RidgeIsTheSameAsSaccadeJs()
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Data, "ridge_case.json")));
            var c = json.RootElement;
            float[] Floats(JsonElement e) => e.EnumerateArray().Select(v => v.GetSingle()).ToArray();
            var embeddings = c.GetProperty("embeddings").EnumerateArray().Select(Floats).ToArray();
            var coords = c.GetProperty("coords").EnumerateArray().Select(Floats).ToArray();
            var weights = Floats(c.GetProperty("weights"));
            var kernel = c.GetProperty("kernel").EnumerateArray().Select(Floats).ToArray();
            var center = c.GetProperty("center").GetDouble();

            var rows = embeddings.Select((e, i) => (e, (double)coords[i][0], (double)coords[i][1], (double)weights[i])).ToList();
            var (x, y) = AppearanceGazeMap.Solve(rows, c.GetProperty("lambda").GetDouble(), center, center);
            for (int i = 0; i < kernel.Length; i++)
            {
                Assert.AreEqual(kernel[i][0], x[i], 1e-5);
                Assert.AreEqual(kernel[i][1], y[i], 1e-5);
            }

            var map = new AppearanceGazeMap { Horizontal = x, Vertical = y, CenterHorizontal = center, CenterVertical = center };
            var probes = c.GetProperty("probes").EnumerateArray().Select(Floats).ToArray();
            var predictions = c.GetProperty("predictions").EnumerateArray().Select(Floats).ToArray();
            for (int i = 0; i < probes.Length; i++)
            {
                var gaze = map.Apply(new AppearanceEmbedding(probes[i], 1)) ?? throw new AssertFailedException("No gaze.");
                Assert.AreEqual(predictions[i][0], gaze.horizontal, 1e-5);
                Assert.AreEqual(predictions[i][1], gaze.vertical, 1e-5);
            }
        }

        /// <summary>
        /// Embeddings that are a linear function of the gaze, plus noise: the N-point calibration with
        /// the source Appearance finds the gaze again, for both eyes, and frames without an embedding
        /// get no position.
        /// </summary>
        [TestMethod]
        public void NPointCalibrationFitsTheEmbedding()
        {
            var random = new Random(1);
            var directions = Enumerable.Range(0, 32).Select(_ => (h: (float)(random.NextDouble() - 0.5), v: (float)(random.NextDouble() - 0.5))).ToArray();
            AppearanceEmbedding Embed(PointF gaze, double noise) => new AppearanceEmbedding(
                directions.Select(d => (float)(0.05 * (d.h * gaze.X + d.v * gaze.Y) + noise * (random.NextDouble() - 0.5))).ToArray(),
                0.5f + (float)random.NextDouble() / 2);

            var calibrator = new LinearCalibrator();
            foreach (var target in NPointSequence.Layout(9, 10, 8, new Random(2)))
            {
                calibrator.StartFixation(target);
                for (int frame = 0; frame < 30; frame++)
                {
                    foreach (var eye in new[] { Eye.Left, Eye.Right })
                    {
                        calibrator.PushData(new EyeData { WhichEye = eye, Appearance = Embed(target, 0.01) });
                    }
                }

                calibrator.StopFixation();
            }

            calibrator.CompleteCalibration(EyeSignalSource.Appearance, EyeSignalSource.CR1);

            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                var map = calibrator.Maps[eye] as AppearanceGazeMap ?? throw new AssertFailedException($"No map for the {eye} eye.");
                foreach (var gaze in new[] { new PointF(0, 0), new PointF(5, -3), new PointF(-8, 6) })
                {
                    var data = new EyeData { WhichEye = eye, Appearance = Embed(gaze, 0) };
                    var calibrated = map.Adjust(data, new CalibratedEyeData());
                    Assert.AreEqual(gaze.X, calibrated.HorizontalPosition, 0.5, $"Horizontal at {gaze}.");
                    Assert.AreEqual(gaze.Y, calibrated.VerticalPosition, 0.5, $"Vertical at {gaze}.");
                }

                Assert.IsTrue(double.IsNaN(map.Adjust(new EyeData { WhichEye = eye }, new CalibratedEyeData()).HorizontalPosition), "No embedding, no position.");
            }

            Assert.IsNull(calibrator.GetUIData(EyeSignalSource.Appearance, EyeSignalSource.None)[0].LeftX, "No 2D signal to show.");
        }

        [TestMethod]
        public void MapIsSavedInTheCalibrationFile()
        {
            var file = Path.Combine(Path.GetTempPath(), "OpenIrisAppearanceMapTest-" + Guid.NewGuid().ToString("N") + ".cal");
            try
            {
                var map = new AppearanceGazeMap { Horizontal = new[] { 1.5, -2.0 }, Vertical = new[] { 0.25, 3.0 }, CenterHorizontal = 1, CenterVertical = -2 };
                var calibration = new CalibrationParameters();
                calibration.EyeCalibrationParameters[Eye.Left].CalibrationData = map;
                calibration.Save(file);

                var loaded = CalibrationParameters.Load(file).EyeCalibrationParameters[Eye.Left].CalibrationData as AppearanceGazeMap
                    ?? throw new AssertFailedException("No map after loading.");
                CollectionAssert.AreEqual(map.Horizontal, loaded.Horizontal);
                CollectionAssert.AreEqual(map.Vertical, loaded.Vertical);
                Assert.AreEqual(-2.0, loaded.CenterVertical);
                Assert.AreEqual(map.Apply(new AppearanceEmbedding(new[] { 1f, 2f }, 1)), loaded.Apply(new AppearanceEmbedding(new[] { 1f, 2f }, 1)));
                Assert.IsNull(loaded.Apply(new AppearanceEmbedding(new[] { 1f }, 1)), "An embedding of another length (another network).");
            }
            finally
            {
                File.Delete(file);
            }
        }

        private static void AssertSame(string expectedFile, Image<Gray, byte> actual, string what)
        {
            using var expected = new Image<Gray, byte>(Path.Combine(Data, expectedFile));
            Assert.AreEqual(expected.Size, actual.Size, what);
            using var difference = expected.AbsDiff(actual);
            var different = CvInvoke.CountNonZero(difference);
            Assert.AreEqual(0, different, $"{what} {different} pixels differ from {expectedFile}.");
        }
    }
}
