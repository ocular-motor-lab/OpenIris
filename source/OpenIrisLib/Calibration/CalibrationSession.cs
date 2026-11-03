//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Linq;
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
        /// Messages from clients, waiting to be delivered to the calibration on its thread.
        /// </summary>
        private readonly ConcurrentQueue<(string message, TaskCompletionSource<string> reply)> messages = new();

        /// <summary>
        /// How long a client waits for the calibration to handle its message.
        /// </summary>
        private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The calibration method. User interfaces read its state to show it.
        /// </summary>
        public CalibrationPipelineBase Pipeline => calibrationPipeline;

        /// <summary>
        /// User interface of the calibration, if the calibration method provides one itself.
        /// </summary>
        public ICalibrationUIControl? GetCalibrationUI()
        {
            return calibrationPipeline?.GetCalibrationUI();
        }

        /// <summary>
        /// Starts a calibration session.
        /// </summary>
        /// <param name="whichEyeToCalibrate">The eyes of the eye tracking system: both, left, or right.</param>
        /// <param name="calibrationPipelineName">Name of the pipeline to use for calibration.</param>
        /// <param name="settings"></param>
        /// <exception cref="InvalidOperationException">If the pipeline name does not exist.</exception>
        internal CalibrationSession(Eye whichEyeToCalibrate, string calibrationPipelineName, CalibrationSettings settings)
        {
            calibrationPipeline = CalibrationPipelineBase.Create(calibrationPipelineName, settings);
            calibrationPipeline.EyesToCalibrate = whichEyeToCalibrate;

            this.whichEyeToCalibrate = whichEyeToCalibrate;
        }

        /// <summary>
        /// Stage 1: calibrates the models (eye models, the calibration's own parameters, the head
        /// model), and maybe the references too.
        /// </summary>
        /// <returns>The new calibration. Null if the calibration was cancelled or failed.</returns>
        internal async Task<CalibrationParameters?> StartCalibratingModel(EyeTrackingPipelineSettings processingSettings)
        {
            CalibrationParameters? result = null;
            var completed = await Run("the model", frame =>
            {
                var (done, calibration) = calibrationPipeline.ProcessForCalibrationModel(frame, processingSettings);
                if (done && calibration is not null) KeepIntrinsics(calibration, frame);
                result = calibration;
                return done;
            });

            var model = result;
            if (!completed || model is null) return null;

            model.TrackingSettings = processingSettings;
            return model;
        }

        /// <summary>
        /// Whether a calibration has the references of every eye of the system, so the zero reference
        /// stage isn't needed.
        /// </summary>
        /// <param name="calibration">The calibration.</param>
        /// <returns>True if it has them.</returns>
        internal bool HasReferences(CalibrationParameters calibration) =>
            CalibrationPipelineBase.Eyes(whichEyeToCalibrate).All(eye => calibration.EyeCalibrationParameters[eye].HasReference);

        /// <summary>
        /// Stage 2: calibrates the zero references of the eyes and the head, with the models of the
        /// current calibration.
        /// </summary>
        /// <returns>The new calibration. The current one if the calibration was cancelled or failed.</returns>
        internal async Task<CalibrationParameters> StartCalibratingZeroReference(CalibrationParameters currentCalibration, EyeTrackingPipelineSettings settings)
        {
            var eyes = CalibrationPipelineBase.Eyes(whichEyeToCalibrate);

            // A copy without references, so the processing pipelines keep the current calibration.
            var tempCalibration = currentCalibration.Copy();
            tempCalibration.EyeCalibrationParameters[Eye.Left].SetReference(null);
            tempCalibration.EyeCalibrationParameters[Eye.Right].SetReference(null);
            tempCalibration.TrackingSettings = settings;

            CalibrationParameters? result = null;
            var completed = await Run("the reference", frame =>
            {
                foreach (var eye in eyes)
                {
                    // Images are processed in parallel, so frames processed with the previous eye
                    // model may still be in the queue: not for the reference (torsion needs the new one).
                    if (tempCalibration.EyeCalibrationParameters[eye].EyePhysicalModel != frame.Calibration.EyeCalibrationParameters[eye].EyePhysicalModel)
                    {
                        return false;
                    }
                }

                // Make sure there is always a model when a reference is set, if the calibration didn't
                // provide one.
                foreach (var eye in eyes)
                {
                    if (tempCalibration.EyeCalibrationParameters[eye].HasEyeModel) continue;

                    var imageEye = frame.Images[eye];
                    var model = (imageEye?.EyeData is null) ?
                        EyePhysicalModel.GetDefault(imageEye?.Size ?? new System.Drawing.Size(), settings.MmPerPix) :
                        new EyePhysicalModel(imageEye.EyeData.Pupil.Center, imageEye.EyeData.Iris.Radius * 2.0f);

                    // In the pixels of the calibration, as the models it has.
                    var shift = tempCalibration.EyeCalibrationParameters[eye].ShiftTo(imageEye?.Intrinsics ?? default);
                    model.Center = new System.Drawing.PointF(model.Center.X - shift.Width, model.Center.Y - shift.Height);
                    tempCalibration.EyeCalibrationParameters[eye].SetEyeModel(model);
                }

                var (done, calibration) = calibrationPipeline.ProcessForZeroReference(frame, tempCalibration, settings);
                if (done && calibration is not null) KeepIntrinsics(calibration, frame);
                result = calibration;
                return done;
            });

            var withReference = result;
            if (!completed || withReference is null) return currentCalibration;

            var missing = eyes.Where(eye => !withReference.EyeCalibrationParameters[eye].HasReference).ToArray();
            if (missing.Length > 0)
            {
                Trace.WriteLine($"ERROR: the calibration {calibrationPipeline.Name} completed the reference without one for the {string.Join(" and ", missing)} eye.");
                return currentCalibration;
            }

            withReference.TrackingSettings = settings;
            withReference.HeadCalibrationParameters ??= new HeadCalibration { ReferenceHeadData = new HeadData() };
            return withReference;
        }

        /// <summary>
        /// The models and references a calibration gives are in the pixels of the images it got: the
        /// calibration keeps the intrinsics of the images of the frame where the stage completed, with
        /// the eye models moved to them if they were in other pixels (the window of the camera moved
        /// since the model, before Reset reference). The references are from that frame (#61).
        /// </summary>
        private void KeepIntrinsics(CalibrationParameters calibration, EyeTrackerImagesAndData frame)
        {
            foreach (var eye in CalibrationPipelineBase.Eyes(whichEyeToCalibrate))
            {
                if (frame.Images[eye] is ImageEye image) calibration.EyeCalibrationParameters[eye].MoveEyeModelTo(image.Intrinsics);
            }
        }

        /// <summary>
        /// Runs a stage: gives the calibration each new frame, and the messages from clients between
        /// frames, all on one thread, until the stage is completed or cancelled.
        /// </summary>
        /// <param name="stage">Name of the stage, for the log.</param>
        /// <param name="processFrame">Processes a frame; returns true when the stage is completed.</param>
        /// <returns>True if the stage was completed.</returns>
        private async Task<bool> Run(string stage, Func<EyeTrackerImagesAndData, bool> processFrame)
        {
            var completed = false;
            try
            {
                using (inputBuffer = new BlockingCollection<EyeTrackerImagesAndData>(100))
                {
                    using var cancellation = new CancellationTokenSource();
                    using var calibrationTask = Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.Name = "EyeTracker:CalibrationThread";

                        // Keep processing images until the buffer is marked as complete and empty
                        foreach (var frame in inputBuffer.GetConsumingEnumerable(cancellation.Token))
                        {
                            DeliverMessages();
                            if (calibrationPipeline.Cancelled) break;

                            if (processFrame(frame))
                            {
                                completed = true;
                                break;
                            }
                        }
                    }, TaskCreationOptions.LongRunning);

                    await calibrationTask;

                    completed &= !calibrationPipeline.Cancelled && !inputBuffer.IsAddingCompleted;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ERROR calibrating {stage}: {ex}");
                CancelCalibration();
                completed = false;
            }
            finally
            {
                inputBuffer?.Dispose();
                inputBuffer = null;
            }

            return completed;
        }

        /// <summary>
        /// Delivers the queued messages to the calibration, on the calibration thread.
        /// </summary>
        private void DeliverMessages()
        {
            while (messages.TryDequeue(out var item))
            {
                try
                {
                    item.reply.TrySetResult(calibrationPipeline.HandleMessage(item.message));
                }
                catch (Exception ex)
                {
                    item.reply.TrySetResult($"ERROR: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles a message from a client: queued, and delivered to the calibration between frames
        /// on its thread. Waits for the reply.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The calibration's reply, or an error if it didn't handle the message in time (no frames).</returns>
        public string HandleMessage(string message)
        {
            var reply = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            messages.Enqueue((message, reply));
            return reply.Task.Wait(MessageTimeout)
                ? reply.Task.Result
                : $"ERROR: the calibration didn't handle '{message}' (is the eye tracker tracking?)";
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