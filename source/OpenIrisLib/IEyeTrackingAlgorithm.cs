
//-----------------------------------------------------------------------
// <copyright file="IEyeTrackingAlgorithm.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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

    /// <summary>
    /// Interface for all eye tracking algorithms that process images to get data.
    /// </summary>
    public interface IEyeTrackingAlgorithm
    {
        /// <summary>
        /// Process the images to get data.
        /// </summary>
        /// <param name="imageEye"></param>
        /// <param name="eyeCalibrationParameters"></param>
        /// <param name="trackingSettings"></param>
        /// <returns></returns>
        (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters, EyeTrackingAlgorithmSettings trackingSettings);

        /// <summary>
        /// Get the UI to change parameters of the algorithm.
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        IAlgorithmUI? GetAlgorithmUI(Eye whichEye);
    }

    /// <summary>
    /// Base clase for settings of eye tracking algorithms.
    /// </summary>
    /// // There may be a better solution https://stackoverflow.com/questions/16220242/generally-accepted-way-to-avoid-knowntype-attribute-for-every-derived-class
    [Serializable]
    [KnownType("GetDerivedTypes")] // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.knowntypeattribute?view=netframework-4.8
    public class EyeTrackingAlgorithmSettings : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Gets the derived types for the serialization over wcf.
        /// </summary>
        /// <returns></returns>
        public static Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(EyeTrackingAlgorithmSettings))).ToArray();

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        public EyeTrackingAlgorithmSettings()
        {
            AlgorithmName = "JOM algorithm";
        }

        /// <summary>
        /// Name of the algorithm will be automatically set.
        /// </summary>
        [Browsable(false)]
        public string AlgorithmName { get; set; }

        /// <summary>
        /// Gets or sets the minimum radius of the pupil. This is a bit complicated. 
        /// The mm per pixel is a setting that depends on the eye tracking system, resolution of the cameras and distance to 
        /// the eyes. Whenever the eye tracking system is changed it will also change this setting. That way all the settings
        /// about eye properties can be set in milimiters (system independent) and then convert to pixels using this values.
        /// That is, the algorithms need values in pixels but it is much easier and consistent to think in miliminters for sizes
        /// of eye parts.
        /// </summary>
        [Browsable(false)]
        public double MmPerPix { get; set; }

        /// <summary>
        /// Gets or sets the left part to the frame that is not processed. Right, top, left, bottom.
        /// </summary>
        [Category("General tracking settings"), Description("Part to the frame that is not processed. Right, top, left, bottom.")]
        public Rectangle CroppingLeftEye
        {
            get
            {
                return this.croppingLeftEye;
            }

            set
            {
                if (value != this.croppingLeftEye)
                {
                    this.croppingLeftEye = value;
                    this.OnPropertyChanged(this, nameof(CroppingLeftEye));
                }
            }
        }
        private Rectangle croppingLeftEye = new Rectangle(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the left part to the frame that is not processed. Right, top, left, bottom.
        /// </summary>
        [Category("General tracking settings"), Description("Part to the frame that is not processed. Right, top, left, bottom.")]
        public Rectangle CroppingRightEye
        {
            get { return this.croppingRightEye; }
            set
            {
                if (value != this.croppingRightEye)
                {
                    this.croppingRightEye = value;
                    this.OnPropertyChanged(this, nameof(CroppingRightEye));
                }
            }
        }
        private Rectangle croppingRightEye = new Rectangle(0, 0, 0, 0);
    }

}
