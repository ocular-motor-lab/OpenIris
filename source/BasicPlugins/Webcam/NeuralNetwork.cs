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
    using System.IO;
    using System.Runtime.InteropServices;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Dnn;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    /// <summary>
    /// One of the ONNX networks in Plugins\WebcamModels, run with OpenCV's DNN module. Not thread
    /// safe: use one object per thread.
    /// </summary>
    internal sealed class NeuralNetwork : IDisposable
    {
        private readonly Net net;
        private readonly string[] outputNames;
        private readonly float[] input;
        private readonly GCHandle inputHandle;
        private readonly Mat inputBlob;
        private readonly VectorOfMat outputBlobs = new VectorOfMat();

        /// <summary>
        /// Loads a network that takes a square RGB image (1x3xSizexSize).
        /// </summary>
        /// <param name="fileName">Name of the file in Plugins\WebcamModels.</param>
        /// <param name="size">Width and height of the input image.</param>
        /// <param name="outputNames">Names of the outputs to read.</param>
        public NeuralNetwork(string fileName, int size, params string[] outputNames)
        {
            var folder = Path.Combine(Path.GetDirectoryName(typeof(NeuralNetwork).Assembly.Location), "WebcamModels");
            var path = Path.Combine(folder, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException($"The webcam tracking model {fileName} is missing. It should be in {folder}.", path);

            net = DnnInvoke.ReadNetFromONNX(path);
            Size = size;
            this.outputNames = outputNames;

            // The input blob wraps this array, so filling the array sets the input.
            input = new float[3 * size * size];
            inputHandle = GCHandle.Alloc(input, GCHandleType.Pinned);
            inputBlob = new Mat(new[] { 1, 3, size, size }, DepthType.Cv32F, inputHandle.AddrOfPinnedObject());
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

            net.SetInput(inputBlob);
            net.Forward(outputBlobs, outputNames);

            var outputs = new float[outputNames.Length][];
            for (int i = 0; i < outputNames.Length; i++)
            {
                var blob = outputBlobs[i];
                outputs[i] = new float[blob.Total.ToInt32()];
                Marshal.Copy(blob.DataPointer, outputs[i], 0, outputs[i].Length);
            }

            return outputs;
        }

        public void Dispose()
        {
            outputBlobs.Dispose();
            inputBlob.Dispose();
            inputHandle.Free();
            net.Dispose();
        }
    }
}
