//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPipelineBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for all eye tracking pipelines that process images to get data.
    /// </summary>
    public abstract class EyeTrackingPipelineBase : IDisposable
    {
        /// <summary>
        /// Name of the pipeline.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Which eye is this pipeline for.
        /// </summary>
        public Eye WhichEye { get; private set; }

        /// <summary>
        /// Settings for the pipeline processing.
        /// </summary>
        public EyeTrackingPipelineSettings Settings { get; private set; }

        /// <summary>
        /// Initializes an instance.
        /// </summary>
        protected EyeTrackingPipelineBase()
        {
            Settings = new EyeTrackingPipelineSettings();
            Name = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the pipeline.</param>
        /// <param name="eye">Which eye is the pipeline for.</param>
        /// <param name="settings">Settings of the pipeline.</param>
        /// <returns>The system.</returns>
        public static EyeTrackingPipelineBase Create(string name, Eye eye, EyeTrackingPipelineSettings? settings = null)
        {
            var pipeline = EyeTrackerPluginManager.EyeTrackingPipelineFactory?.Create(name)
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.EyeTrackingPipelineFactory?.GetDefaultSettings(name) as EyeTrackingPipelineSettings
                ?? throw new OpenIrisException("Bad settings");

            pipeline.Name = name;
            pipeline.WhichEye = eye;
            pipeline.Settings = settings;

            return pipeline;
        }

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Process images.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <returns></returns>
        public abstract (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters);

        /// <summary>
        /// Updates the image of the eye on the setup tab.
        /// </summary>
        /// <param name="whichEye">Which eye to draw.</param>
        /// <param name="dataAndImages">Data of the corresponding image.</param>
        /// <returns>The new image with all the overlay of the data.</returns>
        public virtual IInputArray? UpdatePipelineEyeImage(Eye whichEye, EyeTrackerImagesAndData dataAndImages)
        {
            if (dataAndImages is null) return null;

            return EyeImageOverlay.DrawAllData(
                                    dataAndImages.Images[whichEye],
                                    dataAndImages.Calibration.EyeCalibrationParameters[whichEye],
                                    dataAndImages.TrackingSettings);
        }

        /// <summary>
        /// Get the list of tracking settings that will be shown as sliders in the setup UI.
        /// </summary>
        /// <returns></returns>
        public virtual List<(string text, RangeDouble range, string settingName)>? GetQuickSettingsList()
        {
            return null;
        }
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
        /// Gets or sets the size of a pixel in mm, to convert the sizes of the parts of the eye, which the
        /// settings give in mm (system independent), into pixels. It belongs to the eye tracking system
        /// (the cameras' resolution and distance to the eyes): EyeTrackerSettings.TrackingPipelineSettings
        /// sets it from the system every time the settings are requested. Not saved.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public double MmPerPix { get; set; } = 0.1;

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
