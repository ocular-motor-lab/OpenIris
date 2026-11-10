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
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.ML.OnnxRuntime.Tensors;

    /// <summary>
    /// The eye-embedding network of saccade.js (eye_embedding.onnx in Plugins\WebcamModels, see its
    /// README.md): from the strip of both eyes (<see cref="AppearanceStrip"/>) to an embedding that the
    /// N-point calibration maps to the gaze. It runs with ONNX Runtime (<see cref="OnnxModel"/>): on the
    /// graphics card with DirectML, about 5 ms per frame, or on the processor if that fails, about 50 ms.
    /// One for all the processing threads (<see cref="Shared"/>).
    /// </summary>
    internal sealed class AppearanceNetwork
    {
        private static readonly Lazy<AppearanceNetwork> SharedNetwork = new Lazy<AppearanceNetwork>(() => new AppearanceNetwork());

        private readonly object runLock = new object();
        private readonly OnnxModel model = OnnxModel.Get("eye_embedding.onnx");
        private readonly DenseTensor<float> input = new DenseTensor<float>(new[] { 1, AppearanceStrip.Height, AppearanceStrip.Width, 1 });

        private AppearanceNetwork()
        {
        }

        /// <summary>
        /// The network for all the processing threads.
        /// </summary>
        public static AppearanceNetwork Shared => SharedNetwork.Value;

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

                var outputs = model.Run(input, "embedding", "cal_weight");
                return new AppearanceEmbedding(outputs[0], outputs[1][0]);
            }
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
