//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationNPoint.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Calibration
{
    using Emgu.CV;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using OpenIris.ImageProcessing;


    [Export(typeof(CalibrationPipelineBase)), PluginDescription("N-Point", typeof(CalibrationSettings))]
    public class EyeCalibrationNPoint : CalibrationPipelineBase
    {
        private ICalibrationUIControl CalibrationUI;

        public EyeCollection<List<PointF>> CalibrationPoints { get; set; }
        public EyeCollection<List<PointF>> PupilPositions { get; set; }
        public EyeCollection<Image<Gray, byte>> ScatterImages { get; set; }
        public ImageEye LastImageLeftEye { get; set; }
        public ImageEye LastImageRightEye { get; set; }

        private EyeCollection<EyePhysicalModel> eyeModels;

        public override ICalibrationUIControl GetCalibrationUI() => CalibrationUI;

        public EyeCalibrationNPoint()
        {
            PupilPositions = new EyeCollection<List<PointF>>( new List<PointF>(), new List<PointF>());

            CalibrationPoints = new EyeCollection<List<PointF>>(new List<PointF>(), new List<PointF>());
            CalibrationUI = new EyeCalibrationNPointUI(this);
        }

        /// <summary>
        /// Sets the models from the UI.
        /// </summary>
        /// <param name="leftEye"></param>
        /// <param name="rightEye"></param>
        public void SetPhysicalModelsFromUI(EyePhysicalModel leftEye, EyePhysicalModel rightEye)
        {
            eyeModels = new EyeCollection<EyePhysicalModel> { leftEye, rightEye };
        }

        public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye imageEye, EyeTrackingPipelineSettings processingSettings)
        {
            PupilPositions[imageEye.WhichEye].Add(imageEye.EyeData.Pupil.Center);

            if (ScatterImages is null)
            {
                ScatterImages = new EyeCollection<Image<Gray, byte>>(
                    new Image<Gray, byte>(imageEye.Size),
                    new Image<Gray, byte>(imageEye.Size));
            }

            var x = (int)imageEye.EyeData.Pupil.Center.X;
            var y = (int)imageEye.EyeData.Pupil.Center.Y;
            this.ScatterImages[imageEye.WhichEye].Data[y, x, 0] = 1;

            if (imageEye != null)
            {
                if (imageEye.WhichEye == Eye.Left) LastImageLeftEye = imageEye;
                if (imageEye.WhichEye == Eye.Right) LastImageRightEye = imageEye;
            }

            return (true, EyePhysicalModel.EmptyModel);
        }

        public override (bool referebceCalibrationCompleted, ImageEye referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            CalibrationUI = null;

            if (image == null) return (false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }
        public void Dispose()
        {
        }
    }
}
