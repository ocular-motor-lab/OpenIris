//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class that controls the eye calibration process. The calibration parameters are
    /// actually saved in the main EyeTracker object. And the implementation of the calibration
    /// method and its corresponding UI is a custom separate class.
    /// </summary>
    /// <remarks>
    /// Importantly there are two distinct phases for the eye calibration.
    /// 1) Calibration of the physical model of the eye globe. For this phase the calibration
    /// implementation will determine the position and size of the eyeglobe within the image. This is
    /// absolutely necessary for the geometric correction.
    /// 2) Calibration of the zero reference position. For this phases it is necessary to determine
    /// what is the orientation of the eye in 3D that corresponds with the zero position. For the
    /// torsional dimension this includes the recording of the reference image of the iris. For that
    /// reason it is necessary to do other calibration first to properly geocmetrically correct the
    /// image of the iris.
    /// </remarks>
    public sealed class CalibrationSession : IDisposable
    {
        private readonly Eye whichEyeToCalibrate;
        private readonly CalibrationPipelineBase calibrationPipeline;
        private BlockingCollection<EyeTrackerImagesAndData>? inputBuffer;

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public ICalibrationUIControl? GetCalibrationUI()
        {
            return calibrationPipeline?.GetCalibrationUI();
        }

        /// <summary>
        /// Starts a calibration session.
        /// </summary>
        /// <param name="whichEyeToCalibrate">Which eye we need to calibrate: both, left, or right.</param>
        /// <param name="calibrationPipelineName">Name of the pipeline to use for calibration.</param>
        /// <param name="settings"></param>
        /// <returns>The calibration parameters.</returns>
        /// <exception cref="InvalidOperationException">If the pipeline name does not exist.</exception>
        internal CalibrationSession(Eye whichEyeToCalibrate, string calibrationPipelineName, CalibrationSettings settings)
        {
            calibrationPipeline = CalibrationPipelineBase.Create(calibrationPipelineName, settings);

            this.whichEyeToCalibrate = whichEyeToCalibrate;
        }

        /// <summary>
        /// Calibrates the physical model of the eyeball and its relationship to the camera.
        /// </summary>
        /// <returns>The new calibration parametrers. Null if calibration was cancelled.</returns>
        internal async Task<CalibrationParameters?> StartCalibratingEyeModel(CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings processingSettings)
        {
            var eyeModels = new EyeCollection<EyePhysicalModel>(EyePhysicalModel.EmptyModel, EyePhysicalModel.EmptyModel);
            var hasModel = new EyeCollection<bool>(whichEyeToCalibrate == Eye.Right, whichEyeToCalibrate == Eye.Left);

            try
            {
                using (inputBuffer = new BlockingCollection<EyeTrackerImagesAndData>(100))
                {
                    using var cancellation = new CancellationTokenSource();
                    using var calibrationTask = Task.Factory.StartNew(() =>
                     {
                         Thread.CurrentThread.Name = "EyeTracker:CalibrationThread";

                         // Keep processing images until the buffer is marked as complete and empty
                         foreach (var data in inputBuffer.GetConsumingEnumerable(cancellation.Token))
                         {
                             foreach (var image in data.Images)
                             {
                                 if (image is null) continue;
                                 if (hasModel[image.WhichEye]) continue;

                                 (hasModel[image.WhichEye], eyeModels[image.WhichEye]) = calibrationPipeline.ProcessForEyeModel(processingSettings, image);
                             }

                             if (hasModel[Eye.Left] && hasModel[Eye.Right]) break;

                             if (calibrationPipeline.Cancelled) break;
                         }
                     }, TaskCreationOptions.LongRunning);

                    await calibrationTask;

                    if (calibrationPipeline.Cancelled is false && inputBuffer.IsAddingCompleted is false)
                    {
                        var calibrationParameters = CalibrationParameters.Default;
                        calibrationParameters.TrackingSettings = processingSettings;

                        calibrationParameters.EyeCalibrationParameters[Eye.Left].SetEyeModel(eyeModels[Eye.Left]);
                        calibrationParameters.EyeCalibrationParameters[Eye.Right].SetEyeModel(eyeModels[Eye.Right]);

                        return calibrationParameters;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR calibrating eye model: " + ex);

                // Cancel the ongoing calibration
                CancelCalibration();
            }
            finally
            {
                inputBuffer?.Dispose();
                inputBuffer = null;
            }

            return null;
        }

        /// <summary>
        /// Calibrates the zero eye position and gets a reference image for torsion.
        /// </summary>
        /// <returns>The new calibration parametrers. Null if calibration was cancelled.</returns>
        internal async Task<CalibrationParameters> StartCalibratingZeroReference(CalibrationParameters currentCalibration, CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings settings)
        {
            var eyeReferences = new EyeCollection<ImageEye?>(null, null);
            var hasReference = new EyeCollection<bool>(whichEyeToCalibrate == Eye.Right, whichEyeToCalibrate == Eye.Left);

            // Start with the current calibration and clear the References
            // Make a copy so we don't mess with the processing pipelines
            var tempCalibration = currentCalibration.Copy();
            tempCalibration.EyeCalibrationParameters[Eye.Left].SetReference(null);
            tempCalibration.EyeCalibrationParameters[Eye.Right].SetReference(null);
            tempCalibration.TrackingSettings = settings;

            try
            {
                using (inputBuffer = new BlockingCollection<EyeTrackerImagesAndData>(100))
                {
                    using var cancellation = new CancellationTokenSource();
                    using var calibrationTask = Task.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Name = "EyeTracker:CalibrationThread";

                            // Keep processing images until the buffer is marked as complete and empty
                            foreach (var data in inputBuffer.GetConsumingEnumerable(cancellation.Token))
                            {
                                foreach (var imageEye in data.Images)
                                {
                                    if (imageEye is null || hasReference[imageEye.WhichEye]) continue;

                                    // Wait until we get an image that has the correct eye model.
                                    // Because images are processed in paralel in the processing pipele,
                                    // here in the calibration, there are images in the queue for may have been
                                    // processed with the wrong eye model. So we don't want to use them for reference.
                                    if (tempCalibration.EyeCalibrationParameters[imageEye.WhichEye].EyePhysicalModel
                                        != data.Calibration.EyeCalibrationParameters[imageEye.WhichEye].EyePhysicalModel)
                                    {
                                        continue;
                                    }

                                    // Make sure there is always a model when a reference is set.
                                    // Ideally this should never happen
                                    if (!tempCalibration.EyeCalibrationParameters[imageEye.WhichEye].HasEyeModel)
                                    {
                                        var model = (imageEye.EyeData is null) ?
                                            EyePhysicalModel.GetDefault(imageEye.Size, settings.MmPerPix) :
                                            new EyePhysicalModel(imageEye.EyeData.Pupil.Center, imageEye.EyeData.Iris.Radius * 2.0f);

                                        tempCalibration.EyeCalibrationParameters[imageEye.WhichEye].SetEyeModel(model);
                                    }

                                    (hasReference[imageEye.WhichEye], eyeReferences[imageEye.WhichEye]) = calibrationPipeline.ProcessForReference(tempCalibration, settings, imageEye);
                                }

                                if (hasReference[Eye.Left] && hasReference[Eye.Right]) break;

                                if (calibrationPipeline.Cancelled) break;
                            }
                        }, TaskCreationOptions.LongRunning);

                    await calibrationTask;

                    if (!calibrationPipeline.Cancelled && inputBuffer.IsAddingCompleted is false)
                    {
                        tempCalibration.EyeCalibrationParameters[Eye.Left].SetReference(eyeReferences[Eye.Left]);
                        tempCalibration.EyeCalibrationParameters[Eye.Right].SetReference(eyeReferences[Eye.Right]);
                        return tempCalibration;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR calibrating the reference: " + ex.Message);

                // Cancel the ongoing calibration
                CancelCalibration();
            }
            finally
            {
                inputBuffer?.Dispose();
                inputBuffer = null;
            }

            return currentCalibration;
        }

        /// <summary>
        /// Processes new data.
        /// </summary>
        /// <param name="newData">New data from the last frame.</param>
        internal void ProcessNewDataAndImages(EyeTrackerImagesAndData newData)
        {
            if (inputBuffer?.IsAddingCompleted ?? true) return;

            // Add the frame images to the input queue for processing
            inputBuffer?.TryAdd(newData);
        }

        /// <summary>
        /// Starts a new calibration
        /// </summary>
        internal void CancelCalibration()
        {
            inputBuffer?.CompleteAdding();
        }

        public void Dispose()
        {
            calibrationPipeline?.Dispose();
            inputBuffer?.Dispose();
        }
    }

}