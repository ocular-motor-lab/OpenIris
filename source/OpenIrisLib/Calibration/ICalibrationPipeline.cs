//-----------------------------------------------------------------------
// <copyright file="CalibrationSession.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
    using System;
#nullable enable

    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Interface for calibration pipelines. When a calibration session starts
    /// The method <see cref="ProcessForEyeModel"/> will be called with new images anda data
    /// until the model is completely calibrated. 
    /// Then the method <see cref="ProcessForReference"/> will be called with new images anda data
    /// until the reference completely calibrated. It is possible that the reference is calibrated
    /// at the same time as the model. In such case this method should return inmideately.
    /// </summary>

    public interface ICalibrationPipeline : IPlugin, IDisposable
    {
        (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings processingSettings, ImageEye image);
        (bool referebceCalibrationCompleted, ImageEye referenceData) ProcessForReference(CalibrationParameters currentCalibration, CalibrationSettings calibrationSettings, EyeTrackingPipelineSettings processingSettings, ImageEye image);


        /// <summary>
        /// Indicates weather the calibration was cancelled.
        /// </summary>
        public bool Cancelled { get; }

        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public CalibrationUIControl? GetCalibrationUI();
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
        public static System.Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(CalibrationSettings))).ToArray();
    }
}