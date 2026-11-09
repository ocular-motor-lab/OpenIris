//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelineWebcamIris.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Finds the iris and the eyelids with MediaPipe's iris landmark network. Made for the eye
    /// images of the Webcam system, which are centered on the eye corners, level and 2.3 times
    /// wider than the eye.
    /// </summary>
    /// <remarks>
    /// In visible light the pupil is hard to see, so the iris center stands in for the pupil center
    /// and the iris diameter for the pupil size.
    /// </remarks>
    [Export(typeof(EyeTrackingPipelineBase)), PluginDescription("Webcam iris", typeof(EyeTrackingPipelineWebcamIrisSettings))]
    public sealed class EyeTrackingPipelineWebcamIris : EyeTrackingPipelineBase
    {
        private IrisLandmarks? irisLandmarks;
        private AppearanceNetwork? appearanceNetwork;

        public override (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters)
        {
            if (imageEye is null) throw new ArgumentNullException(nameof(imageEye));

            if (imageEye.ImageSourceData is EyeCrop crop && !crop.Found)
            {
                return (new EyeData
                {
                    WhichEye = imageEye.WhichEye,
                    Timestamp = imageEye.TimeStamp,
                    ImageSize = imageEye.Size,
                    ProcessFrameResult = ProcessFrameResult.MissingPupil,
                }, null);
            }

            irisLandmarks ??= new IrisLandmarks();
            var eye = irisLandmarks.Find(imageEye.Image, imageEye.WhichEye);

            var data = new EyeData
            {
                WhichEye = imageEye.WhichEye,
                Timestamp = imageEye.TimeStamp,
                ImageSize = imageEye.Size,
                ProcessFrameResult = ProcessFrameResult.Good,
                Pupil = new PupilData(eye.IrisCenter, new SizeF(eye.IrisRadius * 2, eye.IrisRadius * 2), 0),
                Iris = new IrisData(eye.IrisCenter, eye.IrisRadius),
                Eyelids = new EyelidData
                {
                    Upper = eye.UpperEyelid,
                    Lower = eye.LowerEyelid,
                },

                // The gaze network, run by the first eye to get here.
                Appearance = (Settings as EyeTrackingPipelineWebcamIrisSettings)?.AppearanceGaze == true
                    ? (imageEye.ImageSourceData as EyeCrop)?.Appearance?.GetEmbedding(() => appearanceNetwork ??= new AppearanceNetwork())
                    : null,
            };

            return (data, null);
        }

        public override void Dispose()
        {
            irisLandmarks?.Dispose();
            appearanceNetwork?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Settings of the Webcam iris pipeline.
    /// </summary>
    [Serializable]
    public class EyeTrackingPipelineWebcamIrisSettings : EyeTrackingPipelineSettings
    {
        [System.ComponentModel.Category("Appearance gaze"), System.ComponentModel.Description(
            "Also run the eye-embedding network of saccade.js on an image of both eyes, for the N-point calibration with the signal " +
            "source Appearance, which maps the network's output to the gaze instead of the iris. Slow, about 100 ms per frame: raise " +
            "MaxNumberOfProcessingThreads (7 threads process about 12 frames per second on 16 cores), or process recorded videos.")]
        public bool AppearanceGaze { get => appearanceGaze; set => SetProperty(ref appearanceGaze, value, nameof(AppearanceGaze)); }
        private bool appearanceGaze;
    }
}
