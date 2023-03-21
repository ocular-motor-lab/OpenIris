//-----------------------------------------------------------------------
// <copyright file="CalibrationPipelineBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
    /// Base class for calibration pipelines. When a calibration session starts
    /// The method <see cref="ProcessForEyeModel"/> will be called with new images anda data
    /// until the model is completely calibrated. 
    /// Then the method <see cref="ProcessForReference"/> will be called with new images anda data
    /// until the reference completely calibrated. It is possible that the reference is calibrated
    /// at the same time as the model. In such case this method should return inmideately.
    /// </summary>

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
        /// Process data towards setting a new physical model
        /// </summary>
        public abstract (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye imageEye, EyeTrackingPipelineSettings processingSettings);

        /// <summary>
        /// Process data for setting a new reference.
        /// </summary>
        public abstract (bool referebceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings);

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public virtual ICalibrationUIControl? GetCalibrationUI() => null;

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