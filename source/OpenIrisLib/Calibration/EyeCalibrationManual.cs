//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationManualUI.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris.Calibration
{
#nullable enable

    using System;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Manual calibration. Select the eyeglobe parameters by hand
    /// </summary>
    [Export(typeof(CalibrationPipeline)), PluginDescription("Manual Calibration", typeof(CalibrationSettings))]
    public class EyeCalibrationManual : CalibrationPipeline
    {
        /// <summary>
        /// Last images of the eyes
        /// </summary>
        public EyeCollection<ImageEye?> LastImages { get; set; }

        /// <summary>
        /// Initializes.
        /// </summary>
        public EyeCalibrationManual()
        {
            LastImages = new EyeCollection<ImageEye?>(null, null);
        }

        /// <summary>
        /// Sets the models from the UI.
        /// </summary>
        /// <param name="leftEye"></param>
        /// <param name="rightEye"></param>
        public void SetPhysicalModelsFromUI (EyePhysicalModel leftEye, EyePhysicalModel rightEye)
        {
            SetPhysicalModels(leftEye, rightEye);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        protected override void ProcessForEyeModel(EyeTrackerImagesAndData data)
        {
            if (CalibrationUI == null) CalibrationUI = new EyeCalibrationManualUI(this);

            var ui = CalibrationUI as EyeCalibrationManualUI;

            foreach (var image in data.Images)
            {
                if (image == null) continue;

                LastImages[image.WhichEye] = image;

                // If we already have eye model don't do anything
                if (TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].HasEyeModel) continue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        protected override void ProcessForReference(EyeTrackerImagesAndData data)
        {
            if (data == null) return;

            CalibrationUI = null;

            foreach (var image in data.Images)
            {
                if (image == null) continue;

                LastImages[image.WhichEye] = image;

                // Wait until we get an image that has the correct eye model.
                // Because images are queued, there are images int he queue for processing
                // that will not get the updated eye model.
                if (TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].EyePhysicalModel
                    != data.Calibration.EyeCalibrationParameters[image.WhichEye].EyePhysicalModel)
                {
                    continue;
                }

                // If we already have reference don't do anything
                if (TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].HasReference) continue;

                if (image.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) continue;

                // Make sure there is always a model when a reference is set.
                if (!TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].HasEyeModel)
                {
                    var model = new EyePhysicalModel(
                                            image.EyeData.Pupil.Center,
                                            image.EyeData.Iris.Radius * 2.0f);

                    TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].SetEyeModel(model);
                }
                TempCalibrationParameters.EyeCalibrationParameters[image.WhichEye].SetReference(image);
            }
        }
    }

}
