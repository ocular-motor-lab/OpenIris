//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationNPoint.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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


    [Export(typeof(CalibrationSession)), PluginDescription("N-Point", typeof(CalibrationSettings))]
    public class EyeCalibrationNPoint : CalibrationSession
    {
        public EyeCollection<List<PointF>> CalibrationPoints { get; set; }
        public EyeCollection<List<PointF>> PupilPositions { get; set; }
        public EyeCollection<Image<Gray, byte>> ScatterImages { get; set; }
        public ImageEye LastImageLeftEye { get; set; }
        public ImageEye LastImageRightEye { get; set; }

        public EyeCalibrationNPoint()
        {
            PupilPositions = new EyeCollection<List<PointF>>( new List<PointF>(), new List<PointF>());

            CalibrationPoints = new EyeCollection<List<PointF>>( new List<PointF>(), new List<PointF>());
        }


        /// <summary>
        /// Sets the models from the UI.
        /// </summary>
        /// <param name="leftEye"></param>
        /// <param name="rightEye"></param>
        public void SetPhysicalModelsFromUI(EyePhysicalModel leftEye, EyePhysicalModel rightEye)
        {
            SetPhysicalModels(leftEye, rightEye);
        }

        protected override void ProcessForEyeModel(EyeTrackerImagesAndData data)
        {
            if (data is null) return;

            if (CalibrationUI == null) CalibrationUI = new EyeCalibrationNPointUI(this);

            foreach (var imageEye in data.Images)
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
            }
        }

        protected override void ProcessForReference(EyeTrackerImagesAndData data)
        {
            foreach (var image in data.Images)
            {
                TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].SetReference(image);
            }
        }

    }
}
