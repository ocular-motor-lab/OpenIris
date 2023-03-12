//-----------------------------------------------------------------------
// <copyright file="CalibrationPipelineBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable enable

    using System.ComponentModel.Composition;

    public abstract class CalibrationPipelineBase : ICalibrationPipeline
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
        public static ICalibrationPipeline Create(string name, CalibrationSettings? settings = null)
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
        /// User interface of the calibration.
        /// </summary>
        public virtual ICalibrationUIControl? GetCalibrationUI() => null;

        /// <summary>
        /// Process data towards setting a new physical model
        /// </summary>
        public abstract (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(EyeTrackingPipelineSettings processingSettings, ImageEye imageEye);

        /// <summary>
        /// Process data for setting a new reference.
        /// </summary>
        public abstract (bool referebceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings, ImageEye image);
        
        public virtual void Dispose()
        {
        }
    }

}