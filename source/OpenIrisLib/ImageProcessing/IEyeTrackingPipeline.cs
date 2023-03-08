
//-----------------------------------------------------------------------
// <copyright file="IEyeTrackingPipeline.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Linq;
    using System.Drawing;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// Interface for all eye tracking pipelines that process images to get data.
    /// </summary>
    public interface IEyeTrackingPipeline
    {
        /// <summary>
        /// Process the images to get data.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <param name="trackingSettings"></param>
        /// <returns></returns>
        (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters, EyeTrackingPipelineSettings trackingSettings);

        /// <summary>
        /// Get the UI to change parameters of the eye tracking pipeline.
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        EyeTrackingPipelineUIControl? GetPipelineUI();
    }

    /// <summary>
    /// Base clase for settings of eye tracking pipelines.
    /// </summary>
    /// // There may be a better solution https://stackoverflow.com/questions/16220242/generally-accepted-way-to-avoid-knowntype-attribute-for-every-derived-class
    [Serializable]
    [KnownType("GetDerivedTypes")] // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.knowntypeattribute?view=netframework-4.8
    public class EyeTrackingPipelineSettings : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Gets the derived types for the serialization over wcf. This is necessary for the settings to be loaded. It's complicated. Because we are loading plugins in runtime we 
        /// don't know a prioiry the types. 
        /// </summary>
        /// <returns></returns>
        public static Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(EyeTrackingPipelineSettings))).ToArray();

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        public EyeTrackingPipelineSettings()
        {
            EyeTrackingPipelineName = "JOM";
        }

        /// <summary>
        /// Name of the pipeline will be automatically set.
        /// </summary>
        [Browsable(false)]
        public string EyeTrackingPipelineName { get; set; }

        /// <summary>
        /// Gets or sets the minimum radius of the pupil. This is a bit complicated. 
        /// The mm per pixel is a setting that depends on the eye tracking system, resolution of the cameras and distance to 
        /// the eyes. Whenever the eye tracking system is changed it will also change this setting. That way all the settings
        /// about eye properties can be set in milimiters (system independent) and then convert to pixels using this values.
        /// That is, the pipelines need values in pixels but it is much easier and consistent to think in miliminters for sizes
        /// of eye parts.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public Func<double> GetMmPerPix { get; set; } = () => 0.1;

        /// <summary>
        /// Gets or sets the left part to the frame that is not processed. Right, top, left, bottom.
        /// </summary>
        [Category("General tracking settings"), Description("Part to the frame that is not processed. Right, top, left, bottom.")]
        public Rectangle CroppingLeftEye { get => croppingLeftEye; set => SetProperty(ref croppingLeftEye, value, nameof(CroppingLeftEye)); }
        private Rectangle croppingLeftEye = new Rectangle(0, 0, 0, 0); // Default value

        /// <summary>
        /// Gets or sets the left part to the frame that is not processed. Right, top, left, bottom.
        /// </summary>
        [Category("General tracking settings"), Description("Part to the frame that is not processed. Right, top, left, bottom.")]
        public Rectangle CroppingRightEye { get => croppingRightEye; set => SetProperty(ref croppingRightEye, value, nameof(CroppingRightEye)); }
        private Rectangle croppingRightEye = new Rectangle(0, 0, 0, 0); // Default value
    }

}
