//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Base class for eye tracking systems.
    /// </summary>
    public abstract class EyeTrackingSystemBase : IDisposable
    {
        /// <summary>
        /// Initializes an instance.
        /// </summary>
        protected EyeTrackingSystemBase()
        {
            Settings = new EyeTrackingSystemSettings();
            Name = string.Empty;
        }

        /// <summary>
        /// Name of the eye tracking System.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Settings of the system.
        /// </summary>
        public EyeTrackingSystemSettings Settings { get; private set; }

        /// <summary>
        /// Initializzes an insstance of the class EyeTrackinSystem.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name">Name of the system.</param>
        /// <returns>The system.</returns>
        public static EyeTrackingSystemBase Create(string name, EyeTrackingSystemSettings? settings = null)
        {
            var system = EyeTrackerPluginManager.EyeTrackingsyStemFactory?.Create(name) as EyeTrackingSystemBase
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.EyeTrackingsyStemFactory?.GetDefaultSettings(name) as EyeTrackingSystemSettings
                ?? throw new OpenIrisException("Bad settings");

            system.Name = name;
            system.Settings = settings;

            return system;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
        }


        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        public virtual EyeCollection<CameraEye?>? CreateAndStartCameras() => null;

        /// <summary>
        /// Gets the head tracking sensor. 
        /// </summary>
        /// <returns>The head tracking sensor.</returns>
        public virtual IHeadDataSource? CreateHeadDataSourceWithCameras() => null;

        /// <summary>
        /// Gets the head tracking sensor. 
        /// </summary>
        /// <returns>The head tracking sensor.</returns>
        public virtual IHeadDataSource? CreateHeadDataSourceWithVideos() => null;

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <param name="fileNames">Names of the files to load.</param>
        /// <returns>List of image eye source objects.</returns>
        public virtual EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> fileNames)
        {
            switch (fileNames.Count)
            {
                case 1:
                    var fileName = fileNames[Eye.Both] ?? throw new NullReferenceException("Filename missing.");
                    return new EyeCollection<VideoEye?>(new VideoEye(Eye.Both, fileName));
                case 2:
                    var filenameLeft = fileNames[Eye.Left];
                    var filenameRight = fileNames[Eye.Right];

                    if (filenameLeft is null && filenameRight is null)
                        throw new NullReferenceException("Filenames missing.");

                    var videoLeft = (filenameLeft != null && filenameLeft.Length > 1)
                        ? new VideoEye(Eye.Left, filenameLeft)
                        : null;
                    var videoRight = (filenameRight != null && filenameRight.Length > 1)
                        ? new VideoEye(Eye.Right, filenameRight)
                        : null;

                    return new EyeCollection<VideoEye?>(videoLeft, videoRight);
                default:
                    throw new InvalidOperationException("The number of video files must be one or two.");
            }
        }

        /// <summary>
        /// Prepares images for processing. Split, rotate, etc. 
        /// </summary>
        /// <remarks>An specific implementation of ImageEyeGrabber can optionally override this 
        /// method to prepare the images. For instance, if a system has only one camera capturing both eyes.
        /// This method would be where the image gets split into two.</remarks>
        /// <param name="images">Images captured from the cameras.</param>
        /// <returns>Images prepared for processing.</returns>
        public virtual EyeCollection<ImageEye?> PreProcessImages(EyeCollection<ImageEye?> images) => images;

        /// <summary>
        /// Method to extract additional data that should be saved to the data file. 
        /// Most commonly it will grab information from generic data saved in the image by the 
        /// eyeCamera class. This will be run after all the processing has been done. 
        /// It could even be possible to reanalize the data here.
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public virtual EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData procesedImages) => procesedImages;

        /// <summary>
        /// Opens a custom UI for this eye tracking system.
        /// </summary>
        /// <returns></returns>
        public virtual Form? OpenEyeTrackingSystemUI => null;

        /// <summary>
        /// Gets custom menu items for this system to show in the main window
        /// </summary>
        /// <returns></returns>
        public virtual ToolStripMenuItem[]? GetToolStripMenuItems() => null;
    }



    /// <summary>
    /// Base class for the settigns of the eye tracking system. With the minimum settings that all 
    /// video or camera systems should have.
    /// http://stackoverflow.com/questions/20084/xml-serialization-and-inherited-types
    /// </summary>
    [Serializable]
    [KnownType("GetDerivedTypes")] // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.knowntypeattribute?view=netframework-4.8
    public class EyeTrackingSystemSettings : EyeTrackerSettingsBase
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Gets the derived types for the serialization over wcf. This is necessary for the settings to be loaded. It's complicated. Because we are loading plugins in runtime we 
        /// don't know a prioiry the types. 
        /// </summary>
        /// <returns></returns>
        public static Type[] GetDerivedTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(_ => _.IsSubclassOf(typeof(EyeTrackingSystemSettings))).ToArray();

        [Browsable(false)]
        public double MmPerPix { get => 1.0 / pixPerMm; }

        /// <summary>
        /// Gets or sets the resolution of the camera in mm per pixel. This should be set up automatically
        /// after a camera system is selected.
        /// </summary>       
        [Category("Camera properties"), Description("Camera resolution ( mm per pixels)")]
        [NeedsRestarting]
        public virtual double PixPerMm
        {
            get => pixPerMm;
            set
            {
                SetProperty(ref pixPerMm, value, nameof(PixPerMm));
                MmPerPixChanged?.Invoke(this, new EventArgs());
            }
        }
        private double pixPerMm = 6;

        [Category("Camera properties"), Description("Distance from the camera to the eye ( mm)")]
        public virtual double DistanceCameraToEyeMm { get => distanceCameraToEyeMm; set => SetProperty(ref distanceCameraToEyeMm, value, nameof(DistanceCameraToEyeMm)); }
        private double distanceCameraToEyeMm = 50; // default value

        [Category("Camera properties"), Description("Camera frame rate")]
        [NeedsRestarting]
        public virtual float FrameRate { get => frameRate; set => SetProperty(ref frameRate, value, nameof(FrameRate)); }
        private float frameRate = 100.0f; // default value

        [Category("Camera properties"), Description("Which eye to use.")]
        [NeedsRestarting]
        public Eye Eye { get => eye; set => SetProperty(ref eye, value, nameof(Eye)); }
        private Eye eye = Eye.Both; // default value

        [field: NonSerialized]
        public event EventHandler? MmPerPixChanged;
    }
}
