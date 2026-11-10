//-----------------------------------------------------------------------
// <copyright file="AppearanceNetwork.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;

    /// <summary>
    /// The eye-embedding network of saccade.js (eye_embedding.onnx in Plugins\WebcamModels, see its
    /// README.md): from the strip of both eyes (<see cref="AppearanceStrip"/>) to an embedding that the
    /// N-point calibration maps to the gaze. It runs with ONNX Runtime on the graphics card (DirectML,
    /// about 5 ms per frame), or on the processor if that fails (about 50 ms). One for all the
    /// processing threads (<see cref="Shared"/>), loaded the first time it is needed and kept while
    /// OpenIris runs: loading takes a while, and with DirectML each copy holds memory on the graphics
    /// card. Runs one frame at a time.
    /// </summary>
    internal sealed class AppearanceNetwork : IDisposable
    {
        private static readonly object LoadLock = new object();
        private static readonly Lazy<AppearanceNetwork> SharedNetwork = new Lazy<AppearanceNetwork>(() => new AppearanceNetwork());
        private static bool nativeLoaded;

        private readonly object runLock = new object();

        private readonly InferenceSession session;
        private readonly DenseTensor<float> input = new DenseTensor<float>(new[] { 1, AppearanceStrip.Height, AppearanceStrip.Width, 1 });
        private readonly NamedOnnxValue[] inputs;

        public AppearanceNetwork()
        {
            LoadNativeOnnxRuntime();
            var model = NeuralNetwork.ModelPath("eye_embedding.onnx");
            inputs = new[] { NamedOnnxValue.CreateFromTensor("eye_image", input) };

            try
            {
                // DirectML needs these two (see ONNX Runtime's DirectML documentation).
                using var options = new SessionOptions { EnableMemoryPattern = false, ExecutionMode = ExecutionMode.ORT_SEQUENTIAL };
                options.AppendExecutionProvider_DML(0);
                session = new InferenceSession(model, options);
                Device = "graphics card (DirectML)";
            }
            catch (Exception ex) when (ex is OnnxRuntimeException || ex is EntryPointNotFoundException || ex is DllNotFoundException)
            {
                Trace.WriteLine($"Appearance gaze: DirectML is not available ({ex.Message}). The network runs on the processor.");
                session = new InferenceSession(model);
                Device = "processor";
            }

            Trace.WriteLine($"Appearance gaze: saccade.js's network on the {Device}.");
        }

        /// <summary>
        /// The network for all the processing threads.
        /// </summary>
        public static AppearanceNetwork Shared => SharedNetwork.Value;

        /// <summary>
        /// Where the network runs.
        /// </summary>
        public string Device { get; }

        /// <summary>
        /// Runs the network on a strip of both eyes.
        /// </summary>
        public AppearanceEmbedding Run(Image<Gray, byte> strip)
        {
            if (strip.Width != AppearanceStrip.Width || strip.Height != AppearanceStrip.Height) throw new ArgumentException($"The strip must be {AppearanceStrip.Width}x{AppearanceStrip.Height}.", nameof(strip));

            lock (runLock)
            {
                // The pixels in order, 0 to 255, as 1 x 36 x 144 x 1.
                var data = strip.Data;
                var buffer = input.Buffer.Span;
                for (int y = 0, i = 0; y < AppearanceStrip.Height; y++)
                {
                    for (int x = 0; x < AppearanceStrip.Width; x++, i++) buffer[i] = data[y, x, 0];
                }

                using var results = session.Run(inputs);
                var embedding = results.First(r => r.Name == "embedding").AsEnumerable<float>().ToArray();
                var weight = results.First(r => r.Name == "cal_weight").AsEnumerable<float>().First();
                return new AppearanceEmbedding(embedding, weight);
            }
        }

        public void Dispose()
        {
            session.Dispose();
        }

        /// <summary>
        /// Loads onnxruntime.dll from lib\, next to Microsoft.ML.OnnxRuntime.dll: .NET Framework only
        /// looks for it next to OpenIris.exe. DirectML.dll is Windows' own.
        /// </summary>
        private static void LoadNativeOnnxRuntime()
        {
            lock (LoadLock)
            {
                if (nativeLoaded) return;

                var folder = Path.GetDirectoryName(typeof(InferenceSession).Assembly.Location);
                foreach (var candidate in new[] { Path.Combine(folder, "onnxruntime.dll"), Path.Combine(folder, "x64", "onnxruntime.dll") })
                {
                    if (File.Exists(candidate) && LoadLibrary(candidate) != IntPtr.Zero) break;
                }

                nativeLoaded = true;
            }
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string fileName);
    }

    /// <summary>
    /// The strip of both eyes of a frame, which goes with the image of each eye (in its EyeCrop): the
    /// pipeline of whichever eye gets there first runs the network, and both get its embedding.
    /// </summary>
    internal sealed class AppearanceInput
    {
        private readonly object runLock = new object();
        private Image<Gray, byte>? strip;
        private AppearanceEmbedding? embedding;

        public AppearanceInput(Image<Gray, byte> strip)
        {
            this.strip = strip;
        }

        /// <summary>
        /// The embedding of the strip, running the network the first time.
        /// </summary>
        /// <param name="network">The network of the calling thread, only asked for if it has to run.</param>
        public AppearanceEmbedding? GetEmbedding(Func<AppearanceNetwork> network)
        {
            lock (runLock)
            {
                if (strip is Image<Gray, byte> image)
                {
                    // Only once, even if it fails.
                    strip = null;
                    using (image)
                    {
                        embedding = network().Run(image);
                    }
                }

                return embedding;
            }
        }
    }
}
