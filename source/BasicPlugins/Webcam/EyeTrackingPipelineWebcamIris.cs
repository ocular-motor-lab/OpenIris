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
    [Export(typeof(EyeTrackingPipelineBase)), PluginDescription("Webcam iris", typeof(EyeTrackingPipelineSettings))]
    public sealed class EyeTrackingPipelineWebcamIris : EyeTrackingPipelineBase
    {
        private IrisLandmarks? irisLandmarks;

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
            };

            return (data, null);
        }

        public override void Dispose()
        {
            irisLandmarks?.Dispose();
            base.Dispose();
        }
    }
}
