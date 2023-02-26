//-----------------------------------------------------------------------
// <copyright file="EyeTrackerCalibrationPipeline.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class that contains controls the eye calibration process. The calibration parameters are
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
    [Export(typeof(CalibrationPipeline)), PluginDescription("Auto", typeof(CalibrationSettings))]
    public class CalibrationPipeline
    {
        private readonly BlockingCollection<EyeTrackerImagesAndData> inputBuffer;
        private bool cancelled;

        /// <summary>
        /// Temporary calibration parameters during calibraiton.
        /// </summary>
        protected CalibrationParameters TempCalibrationParameters { get; set; }

        /// <summary>
        /// Settings for the calibration.
        /// </summary>
        protected CalibrationSettings? CalibrationSettings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerCalibrationManager class.
        /// </summary>
        protected CalibrationPipeline()
        {
            inputBuffer = new BlockingCollection<EyeTrackerImagesAndData>(100);
            TempCalibrationParameters = CalibrationParameters.Default;
        }

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public ICalibrationUI? CalibrationUI { get; protected set; }

        /// <summary>
        /// Calibrates the physical model of the eyeball and its relationship to the camera.
        /// </summary>
        /// <returns>The new calibration parametrers. Null if calibration was cancelled.</returns>
        internal async Task<CalibrationParameters?> CalibrateEyeModel(CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings settings)
        {
            using var cancellation = new CancellationTokenSource();

            try
            {
                CalibrationSettings = calibrationSettings;

                TempCalibrationParameters = CalibrationParameters.Default;
                TempCalibrationParameters.TrackingSettings = settings;

                var calibrationTask = Task.Factory.StartNew(() =>
                 {
                     Thread.CurrentThread.Name = "EyeTracker:CalibrationThread";

                     // Keep processing images until the buffer is marked as complete and empty
                     foreach (var data in inputBuffer.GetConsumingEnumerable(cancellation.Token))
                     {
                         ProcessForEyeModel(data);
                         if (HasEyeModels(data)) break;
                     }
                 }, TaskCreationOptions.LongRunning);

                await calibrationTask;

                if (!cancelled)
                {
                    return TempCalibrationParameters;
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
                CalibrationUI = null;
            }

            return null;
        }

        /// <summary>
        /// Calibrates the zero eye position and gets a reference image for torsion.
        /// </summary>
        /// <returns>The new calibration parametrers. Null if calibration was cancelled.</returns>
        internal async Task<CalibrationParameters> CalibrateZeroReference(CalibrationParameters currentCalibration, CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings settings)
        {
            if (currentCalibration is null) currentCalibration = CalibrationParameters.Default;

            using var cancellation = new CancellationTokenSource();

            try
            {
                CalibrationSettings = calibrationSettings;

                // Start with the current calibration and clear the References
                TempCalibrationParameters = currentCalibration.Copy();
                TempCalibrationParameters.EyeCalibrationParameters[Eye.Left].SetReference(null);
                TempCalibrationParameters.EyeCalibrationParameters[Eye.Right].SetReference(null);
                TempCalibrationParameters.TrackingSettings = settings;

                var calibrationTask = Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.Name = "EyeTracker:CalibrationThread";

                    // Keep processing images until the buffer is marked as complete and empty
                    foreach (var data in inputBuffer.GetConsumingEnumerable(cancellation.Token))
                    {
                        ProcessForReference(data);
                        if (HasReferences(data)) break;
                    }
                }, TaskCreationOptions.LongRunning);

                await calibrationTask;

                if (!cancelled)
                {
                    return TempCalibrationParameters;
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
                CalibrationUI = null;
            }

            return currentCalibration;
        }

        /// <summary>
        /// Processes new data.
        /// </summary>
        /// <param name="newData">New data from the last frame.</param>
        internal void ProcessNewDataAndImages(EyeTrackerImagesAndData newData)
        {
            if (inputBuffer.IsAddingCompleted) return;

            // Add the frame images to the input queue for processing
            inputBuffer.TryAdd(newData);
        }

        /// <summary>
        /// Starts a new calibration
        /// </summary>
        internal void CancelCalibration()
        {
            cancelled = true;

            if (inputBuffer.IsAddingCompleted) return;

            inputBuffer.CompleteAdding();
        }

        /// <summary>
        /// Sets both eye globes 
        /// </summary>
        /// <param name="leftEye"></param>
        /// <param name="rightEye"></param>
        protected void SetPhysicalModels(EyePhysicalModel leftEye, EyePhysicalModel rightEye)
        {
            TempCalibrationParameters.EyeCalibrationParameters[Eye.Left].SetEyeModel(leftEye);
            TempCalibrationParameters.EyeCalibrationParameters[Eye.Right].SetEyeModel(rightEye);
        }

        /// <summary>
        /// Process data towards setting a new physical model
        /// </summary>
        /// <param name="data"></param>
        protected virtual void ProcessForEyeModel(EyeTrackerImagesAndData data)
        {
            if (data is null) return;

            foreach (var imageEye in data.Images)
            {
                if (imageEye is null) continue;

                if (imageEye.EyeData?.ProcessFrameResult == ProcessFrameResult.Good)
                {
                    var eyeGlobe = new EyePhysicalModel(imageEye.EyeData.Pupil.Center, (float)(imageEye.EyeData.Iris.Radius * 2.0));
                    TempCalibrationParameters.EyeCalibrationParameters[imageEye.WhichEye].SetEyeModel(eyeGlobe);
                }
            }
        }

        /// <summary>
        /// Checks if the calibration already has a reference.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HasEyeModels(EyeTrackerImagesAndData data)
        {
            // If all the eye models are ready we are done.
            if ((data.Images[Eye.Left] is null || TempCalibrationParameters.EyeCalibrationParameters[Eye.Left].HasEyeModel) &&
                 (data.Images[Eye.Right] is null || TempCalibrationParameters.EyeCalibrationParameters[Eye.Right].HasEyeModel))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Process data for setting a new reference.
        /// </summary>
        /// <param name="data"></param>
        protected virtual void ProcessForReference(EyeTrackerImagesAndData data)
        {
            if (data is null) return;

            foreach (var image in data.Images)
            {
                if (image is null) continue;

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

        /// <summary>
        /// Checks if the calibration already has a reference.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HasReferences(EyeTrackerImagesAndData data)
        {
            // If all the references are ready we are done.
            if ((data.Images[Eye.Left] is null || TempCalibrationParameters.EyeCalibrationParameters[Eye.Left].HasReference) &&
                (data.Images[Eye.Right] is null || TempCalibrationParameters.EyeCalibrationParameters[Eye.Right].HasReference))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Settings.
    /// </summary>
    /// 
    [KnownType("GetDerivedTypes")] // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.knowntypeattribute?view=netframework-4.8

    public class CalibrationSettings : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Gets the derived types for the serialization over wcf. This is necessary for the settings to be loaded. It's complicated. Because we are loading plugins in runtime we 
        /// don't know a prioiry the types. 
        /// </summary>
        /// <returns></returns>
        public static Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(CalibrationSettings))).ToArray();
    }
}