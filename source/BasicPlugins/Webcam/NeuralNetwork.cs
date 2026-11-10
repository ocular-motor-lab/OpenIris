//-----------------------------------------------------------------------
// <copyright file="NeuralNetwork.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Dnn;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using Microsoft.ML.OnnxRuntime.Tensors;

    /// <summary>
    /// One of MediaPipe's networks in Plugins\WebcamModels, which take a square RGB image
    /// (1x3xSizexSize). On the graphics card with ONNX Runtime and DirectML if it can (<see cref="OnnxModel"/>,
    /// one for all the objects of the same file), about 2 ms; otherwise with OpenCV's DNN module, which
    /// on the processor is faster than ONNX Runtime for these networks (about 10 ms). Not thread safe:
    /// use one object per thread.
    /// </summary>
    internal sealed class NeuralNetwork : IDisposable
    {
        private readonly string[] outputNames;
        private readonly float[] input;
        private readonly GCHandle inputHandle;

        // On the graphics card.
        private readonly OnnxModel? model;
        private readonly DenseTensor<float>? tensor;

        // On the processor.
        private readonly Net? net;
        private readonly Mat? inputBlob;
        private readonly VectorOfMat? outputBlobs;

        /// <summary>
        /// Loads a network that takes a square RGB image (1x3xSizexSize).
        /// </summary>
        /// <param name="fileName">Name of the file in Plugins\WebcamModels.</param>
        /// <param name="size">Width and height of the input image.</param>
        /// <param name="outputNames">Names of the outputs to read.</param>
        public NeuralNetwork(string fileName, int size, params string[] outputNames)
        {
            Size = size;
            this.outputNames = outputNames;

            // The input tensor or blob wraps this array, so filling the array sets the input.
            input = new float[3 * size * size];
            inputHandle = GCHandle.Alloc(input, GCHandleType.Pinned);

            var onnx = OnnxModel.Get(fileName);
            if (onnx.OnGraphicsCard)
            {
                model = onnx;
                tensor = new DenseTensor<float>(input, new[] { 1, 3, size, size });
            }
            else
            {
                Trace.WriteLine($"Webcam models: {fileName} runs on the processor (OpenCV).");
                net = DnnInvoke.ReadNetFromONNX(OnnxModel.ModelPath(fileName));
                inputBlob = new Mat(new[] { 1, 3, size, size }, DepthType.Cv32F, inputHandle.AddrOfPinnedObject());
                outputBlobs = new VectorOfMat();
            }
        }

        /// <summary>
        /// Width and height of the input image.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Runs the network on a grayscale image of Size x Size, repeated in the three color channels.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="scale">Scale from pixel values to network input.</param>
        /// <param name="offset">Offset added after scaling.</param>
        /// <param name="mirror">Whether to flip the image horizontally.</param>
        /// <returns>The outputs, in the order of the names given to the constructor.</returns>
        public float[][] Run(Image<Gray, byte> image, float scale, float offset, bool mirror = false)
        {
            if (image.Width != Size || image.Height != Size) throw new ArgumentException($"The image must be {Size}x{Size}.", nameof(image));

            var data = image.Data;
            var plane = Size * Size;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var value = data[y, mirror ? Size - 1 - x : x, 0] * scale + offset;
                    var i = y * Size + x;
                    input[i] = value;
                    input[plane + i] = value;
                    input[2 * plane + i] = value;
                }
            }

            if (model != null) return model.Run(tensor!, outputNames);

            net!.SetInput(inputBlob);
            net.Forward(outputBlobs, outputNames);

            var outputs = new float[outputNames.Length][];
            for (int i = 0; i < outputNames.Length; i++)
            {
                var blob = outputBlobs![i];
                outputs[i] = new float[blob.Total.ToInt32()];
                Marshal.Copy(blob.DataPointer, outputs[i], 0, outputs[i].Length);
            }

            return outputs;
        }

        public void Dispose()
        {
            // The ONNX Runtime model is shared and stays loaded (OnnxModel).
            outputBlobs?.Dispose();
            inputBlob?.Dispose();
            net?.Dispose();
            inputHandle.Free();
        }
    }
}
