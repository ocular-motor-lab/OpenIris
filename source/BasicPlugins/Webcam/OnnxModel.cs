//-----------------------------------------------------------------------
// <copyright file="OnnxModel.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;

    /// <summary>
    /// A network of Plugins\WebcamModels run with ONNX Runtime: on the graphics card with DirectML, or
    /// on the processor if that fails (<see cref="OnGraphicsCard"/>; the MediaPipe networks then use
    /// OpenCV instead, which is faster for them on the processor, see NeuralNetwork). One for each file, shared by all the threads, loaded the first
    /// time it is needed and kept while OpenIris runs (loading takes a while, and on the graphics card
    /// each copy holds memory there). It runs one input at a time.
    /// </summary>
    internal sealed class OnnxModel
    {
        private static readonly Dictionary<string, OnnxModel> Models = new Dictionary<string, OnnxModel>(StringComparer.OrdinalIgnoreCase);
        private static bool nativeLoaded;

        private readonly InferenceSession session;
        private readonly object runLock = new object();

        private OnnxModel(string fileName)
        {
            var path = ModelPath(fileName);
            try
            {
                // DirectML needs these two (see ONNX Runtime's DirectML documentation).
                using var options = new SessionOptions { EnableMemoryPattern = false, ExecutionMode = ExecutionMode.ORT_SEQUENTIAL };
                options.AppendExecutionProvider_DML(0);
                session = new InferenceSession(path, options);
                OnGraphicsCard = true;
            }
            catch (Exception ex) when (ex is OnnxRuntimeException || ex is EntryPointNotFoundException || ex is DllNotFoundException)
            {
                Trace.WriteLine($"Webcam models: DirectML is not available for {fileName} ({ex.Message}).");
                session = new InferenceSession(path);
            }

            InputName = session.InputMetadata.Keys.First();
            if (OnGraphicsCard) Trace.WriteLine($"Webcam models: {fileName} runs on the graphics card (DirectML).");
        }

        /// <summary>
        /// Whether the network runs on the graphics card (else on the processor).
        /// </summary>
        public bool OnGraphicsCard { get; }

        /// <summary>
        /// Name of the network's input (they have one).
        /// </summary>
        public string InputName { get; }

        /// <summary>
        /// The network in a file of Plugins\WebcamModels, loaded the first time.
        /// </summary>
        /// <exception cref="FileNotFoundException">If the file isn't there.</exception>
        public static OnnxModel Get(string fileName)
        {
            lock (Models)
            {
                if (!Models.TryGetValue(fileName, out var model))
                {
                    LoadNativeOnnxRuntime();
                    model = new OnnxModel(fileName);
                    Models.Add(fileName, model);
                }

                return model;
            }
        }

        /// <summary>
        /// Path of a network in Plugins\WebcamModels.
        /// </summary>
        /// <exception cref="FileNotFoundException">If it isn't there.</exception>
        public static string ModelPath(string fileName)
        {
            var folder = Path.Combine(Path.GetDirectoryName(typeof(OnnxModel).Assembly.Location), "WebcamModels");
            var path = Path.Combine(folder, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException($"The webcam tracking model {fileName} is missing. It should be in {folder}.", path);
            return path;
        }

        /// <summary>
        /// Runs the network.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="outputNames">Names of the outputs to read.</param>
        /// <returns>The outputs, in the order of the names.</returns>
        public float[][] Run(DenseTensor<float> input, params string[] outputNames)
        {
            lock (runLock)
            {
                using var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(InputName, input) }, outputNames);
                return outputNames.Select(name => results.First(r => r.Name == name).AsEnumerable<float>().ToArray()).ToArray();
            }
        }

        /// <summary>
        /// Loads onnxruntime.dll from lib\, next to Microsoft.ML.OnnxRuntime.dll: .NET Framework only
        /// looks for it next to OpenIris.exe. DirectML.dll is Windows' own.
        /// </summary>
        private static void LoadNativeOnnxRuntime()
        {
            if (nativeLoaded) return;

            var folder = Path.GetDirectoryName(typeof(InferenceSession).Assembly.Location);
            foreach (var candidate in new[] { Path.Combine(folder, "onnxruntime.dll"), Path.Combine(folder, "x64", "onnxruntime.dll") })
            {
                if (File.Exists(candidate) && LoadLibrary(candidate) != IntPtr.Zero) break;
            }

            nativeLoaded = true;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string fileName);
    }
}
