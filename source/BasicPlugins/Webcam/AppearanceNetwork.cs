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
    using System.Runtime.InteropServices;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Dnn;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    /// <summary>
    /// The eye-embedding network of saccade.js (eye_embedding.onnx in Plugins\WebcamModels, see its
    /// README.md): from the strip of both eyes (<see cref="AppearanceStrip"/>) to an embedding that the
    /// N-point calibration maps to the gaze. About 100 ms per frame. Not thread safe: use one object per
    /// thread.
    /// </summary>
    internal sealed class AppearanceNetwork : IDisposable
    {
        private readonly Net net;
        private readonly float[] input = new float[AppearanceStrip.Width * AppearanceStrip.Height];
        private readonly GCHandle inputHandle;
        private readonly Mat inputBlob;
        private readonly Mat inputImage;
        private readonly VectorOfMat outputBlobs = new VectorOfMat();
        private readonly string[] outputNames = { "embedding", "cal_weight" };

        public AppearanceNetwork()
        {
            net = DnnInvoke.ReadNetFromONNX(NeuralNetwork.ModelPath("eye_embedding.onnx"));

            // The input, 1 x 36 x 144 x 1 (the pixels in order, 0 to 255), and an image over the same
            // memory to write it.
            inputHandle = GCHandle.Alloc(input, GCHandleType.Pinned);
            inputBlob = new Mat(new[] { 1, AppearanceStrip.Height, AppearanceStrip.Width, 1 }, DepthType.Cv32F, inputHandle.AddrOfPinnedObject());
            inputImage = new Mat(AppearanceStrip.Height, AppearanceStrip.Width, DepthType.Cv32F, 1, inputHandle.AddrOfPinnedObject(), AppearanceStrip.Width * sizeof(float));
        }

        /// <summary>
        /// Runs the network on a strip of both eyes.
        /// </summary>
        public AppearanceEmbedding Run(Image<Gray, byte> strip)
        {
            if (strip.Width != AppearanceStrip.Width || strip.Height != AppearanceStrip.Height) throw new ArgumentException($"The strip must be {AppearanceStrip.Width}x{AppearanceStrip.Height}.", nameof(strip));

            strip.Mat.ConvertTo(inputImage, DepthType.Cv32F);
            net.SetInput(inputBlob);
            net.Forward(outputBlobs, outputNames);

            var embedding = new float[outputBlobs[0].Total.ToInt32()];
            Marshal.Copy(outputBlobs[0].DataPointer, embedding, 0, embedding.Length);
            var weight = new float[1];
            Marshal.Copy(outputBlobs[1].DataPointer, weight, 0, 1);
            return new AppearanceEmbedding(embedding, weight[0]);
        }

        public void Dispose()
        {
            outputBlobs.Dispose();
            inputImage.Dispose();
            inputBlob.Dispose();
            inputHandle.Free();
            net.Dispose();
        }
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
