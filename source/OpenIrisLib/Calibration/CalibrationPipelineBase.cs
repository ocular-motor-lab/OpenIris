//-----------------------------------------------------------------------
// <copyright file="CalibrationPipelineBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
    using System;
#nullable enable

    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class for calibration pipelines. A calibration has two stages, each called with every
    /// processed frame (both eye images with their data, and the head data) until the pipeline says
    /// it's completed:
    /// 1. <see cref="ProcessForCalibrationModel"/>: the models (eye models, the calibration's own
    ///    parameters, the head model) and, optionally, the references too.
    /// 2. <see cref="ProcessForZeroReference"/>: the references of the eyes and the head, from the same
    ///    moment or period. Skipped by "Calibrate" if stage 1 already set them; "Reset reference" runs
    ///    only this stage, with the current models.
    /// </summary>
    /// <remarks>
    /// The calibration session calls the stages and <see cref="HandleMessage"/> from one thread, never
    /// at the same time; messages from clients are queued and delivered between frames. For the
    /// references, it only passes frames processed with the new eye models (torsion needs them). It
    /// checks there is an eye model and a reference for each eye in <see cref="EyesToCalibrate"/>.
    /// Everything returned is saved in the calibration file, so a calibration's own types must be
    /// serializable as XML. Use the raw data of the frames: their calibrated data comes from the
    /// calibration being replaced. See docs/plugin-development/calibration-pipelines.md.
    /// </remarks>
    public abstract class CalibrationPipelineBase : IDisposable
    {
        /// <summary>
        /// Name of the plugin, gets set automatically.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CalibrationSettings Settings { get; set; }

        /// <summary>
        /// Indicates weather the calibration was cancelled.
        /// </summary>
        public bool Cancelled { get; }

        protected CalibrationPipelineBase()
        {
            Name = string.Empty;
            Settings = new CalibrationSettings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the pipeline.</param>
        /// <param name="settings">Settings of the pipeline.</param>
        /// <returns>The system.</returns>
        public static CalibrationPipelineBase Create(string name, CalibrationSettings? settings = null)
        {
            var pipeline = EyeTrackerPluginManager.CalibrationPipelineFactory?.Create(name)
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.CalibrationPipelineFactory?.GetDefaultSettings(name) as CalibrationSettings
                ?? throw new OpenIrisException("Bad settings");

            pipeline.Name = name;
            pipeline.Settings = settings;

            return pipeline;
        }

        /// <summary>
        /// Gets the eyes of the eye tracking system (Left, Right or Both), set by the session: the
        /// calibration must give a model and a reference for each.
        /// </summary>
        public Eye EyesToCalibrate { get; internal set; } = Eye.Both;

        /// <summary>
        /// Stage 1: processes a frame towards the models.
        /// </summary>
        /// <param name="frame">Processed frame: both eye images with their data (the image of an eye
        /// the system doesn't have is null), and the head data (frame.Data.HeadDataRaw).</param>
        /// <param name="processingSettings">Settings of the tracking pipeline.</param>
        /// <returns>Whether it's completed, and then a calibration with an eye model for each eye in
        /// <see cref="EyesToCalibrate"/>, and optionally the calibration's own parameters, the head
        /// model, and the references.</returns>
        public abstract (bool completed, CalibrationParameters? calibration) ProcessForCalibrationModel(EyeTrackerImagesAndData frame, EyeTrackingPipelineSettings processingSettings);

        /// <summary>
        /// Stage 2: processes a frame towards the references.
        /// </summary>
        /// <param name="frame">Processed frame, processed with the models of <paramref name="currentCalibration"/>.</param>
        /// <param name="currentCalibration">The calibration with the models, without references.</param>
        /// <param name="processingSettings">Settings of the tracking pipeline.</param>
        /// <returns>Whether it's completed, and then the calibration with the references of each eye
        /// in <see cref="EyesToCalibrate"/> and of the head, from the same moment or period (from one
        /// frame or many, as the calibration decides).</returns>
        public abstract (bool completed, CalibrationParameters? calibration) ProcessForZeroReference(EyeTrackerImagesAndData frame, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings);

        /// <summary>
        /// Handles a message from a client (web API, TCP/UDP CALIBRATION|..., MATLAB): plain text, with
        /// the meaning this calibration gives it.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>"ack", or an error text. Sent back to the client.</returns>
        public virtual string HandleMessage(string message) => "ack";

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public virtual ICalibrationUIControl? GetCalibrationUI() => null;

        /// <summary>
        /// The calibration with the references taken from one frame: each eye's image and data (as
        /// <see cref="EyeCalibration.SetReference"/> takes them) and the head data. For calibrations
        /// that take the reference from a single good frame.
        /// </summary>
        /// <param name="calibration">The calibration with the models.</param>
        /// <param name="frame">The frame.</param>
        /// <returns>A copy of the calibration with the references set.</returns>
        protected CalibrationParameters WithReferences(CalibrationParameters calibration, EyeTrackerImagesAndData frame)
        {
            var result = calibration.Copy();
            foreach (var eye in Eyes(EyesToCalibrate))
            {
                result.EyeCalibrationParameters[eye].SetReference(frame.Images[eye]);
            }

            result.HeadCalibrationParameters ??= new HeadCalibration();
            result.HeadCalibrationParameters.ReferenceHeadData = frame.Data?.HeadDataRaw ?? new HeadData();
            return result;
        }

        /// <summary>
        /// Whether every eye of the system has an image tracked well in the frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns>True if all were tracked.</returns>
        protected bool AllEyesTracked(EyeTrackerImagesAndData frame) =>
            Eyes(EyesToCalibrate).All(eye => frame.Images[eye]?.EyeData?.ProcessFrameResult == ProcessFrameResult.Good);

        /// <summary>
        /// The eyes in Left, Right or Both.
        /// </summary>
        /// <param name="eyes">Left, Right or Both.</param>
        /// <returns>The eyes, one by one.</returns>
        internal static Eye[] Eyes(Eye eyes) => eyes switch
        {
            Eye.Left => new[] { Eye.Left },
            Eye.Right => new[] { Eye.Right },
            _ => new[] { Eye.Left, Eye.Right },
        };

        public virtual void Dispose()
        {
        }
    }


    /// <summary>
    /// Settings.
    /// </summary>
    [KnownType("GetDerivedTypes")] // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.knowntypeattribute?view=netframework-4.8
    public class CalibrationSettings : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Gets the derived types for the serialization over wcf. This is necessary for the settings to be loaded. It's complicated. Because we are loading plugins in runtime we 
        /// don't know a prioiry the types. 
        /// </summary>
        /// <returns></returns>
        public static System.Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(CalibrationSettings))).ToArray();
    }
}