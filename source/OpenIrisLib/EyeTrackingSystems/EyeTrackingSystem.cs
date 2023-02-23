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
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Interface for all eye tracking systems.
    /// The eye tracking system should not hold to any resources. It will not be diposed appropriately. 
    /// It must just create cameras, videos and/or head trackers and pass ownership of the objects. 
    /// </summary>
    public abstract class EyeTrackingSystem : IDisposable
    {
        /// <summary>
        /// Initializes an instance.
        /// </summary>
        public EyeTrackingSystem()
        {
            Name = "";
            Settings = new EyeTrackingSystemSettings();
        }

        /// <summary>
        /// Initializzes an insstance of the class EyeTrackinSystem.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name">Name of the system.</param>
        /// <returns>The system.</returns>
        public static EyeTrackingSystem Create(string name, EyeTrackingSystemSettings? settings)
        {
            var system = EyeTrackerPluginManager.EyeTrackingsyStemFactory?.Create(name)
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.EyeTrackingsyStemFactory?.GetDefaultSettings(name) as EyeTrackingSystemSettings
                ?? throw new OpenIrisException("Bad settings");
            system.Init(name, settings);
            return system;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the name of the eye tracker.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Settings of the system.
        /// </summary>
        public EyeTrackingSystemSettings Settings { get; private set; }

        /// <summary>
        /// Initializes the object.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name"></param>
        protected void Init(string name, EyeTrackingSystemSettings settings)
        {
            Name = name;
            Settings = settings;
        }

        /// <summary>
        /// Creates the image sources for the image grabber.
        /// </summary>
        /// <returns></returns>
        public EyeCollection<IImageEyeSource?> CreateImageEyeSources()
        {
            var newSources = (this is VideoPlayer)
                ? CreateVideos(null).Select(v => v as IImageEyeSource)
                : CreateCameras().Select(c => c as IImageEyeSource)
                ?? throw new OpenIrisException("No image sources");

            var sources = new EyeCollection<IImageEyeSource?>(newSources);

            if (sources.Count == 2)
            {
                // Dispose the sources we don't need
                if (Settings.Eye == Eye.Left)
                {
                    sources[Eye.Right]?.Stop();
                    (sources[Eye.Right] as IDisposable)?.Dispose();
                    sources[Eye.Right] = null;
                }

                if (Settings.Eye == Eye.Right)
                {
                    sources[Eye.Left]?.Stop();
                    (sources[Eye.Left] as IDisposable)?.Dispose();
                    sources[Eye.Left] = null;
                }
            }

            return sources;
        }

        /// <summary>
        /// Preprocess the images just grabbed. Depending if they are videos or cameras.
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public EyeCollection<ImageEye?> PreProcessImages(EyeCollection<ImageEye?> images)
        {
            var newimages = (this is VideoPlayer)
                ? PreProcessImagesFromVideos(images)
                : PreProcessImagesFromCameras(images)
                ?? throw new OpenIrisException("images");
            return newimages;
        }

        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        public virtual EyeCollection<CameraEye?>? CreateCameras() => null;

        /// <summary>
        /// Gets the head tracking sensor. 
        /// </summary>
        /// <returns>The head tracking sensor.</returns>
        public virtual IHeadDataSource? CreateHeadDataSource() => null;

        /// <summary>
        /// Gets the image sources.
        /// </summary>
        /// <param name="fileNames">Names of the files to load.</param>
        /// <returns>List of image eye source objects.</returns>
        public virtual EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> fileNames)
        {
            if (fileNames is null) throw new ArgumentNullException(nameof(fileNames));

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
        public virtual EyeCollection<ImageEye?> PreProcessImagesFromCameras(EyeCollection<ImageEye?> images) => images;

        /// <summary>
        /// Prepares images for processing. Split, rotate, etc. 
        /// </summary>
        /// <remarks>An specific implementation of ImageEyeGrabber can optionally override this 
        /// method to prepare the images. For instance, if a system has only one camera capturing both eyes.
        /// This method would be where the image gets split into two.</remarks>
        /// <param name="images">Images captured from the cameras.</param>
        /// <returns>Images prepared for processing.</returns>
        public virtual EyeCollection<ImageEye?> PreProcessImagesFromVideos(EyeCollection<ImageEye?> images) => images;

        /// <summary>
        /// Method to extract additional data that should be saved to the data file. 
        /// Most commonly it will grab information from generic data saved in the image by the 
        /// eyeCamera class. This will be run after all the processing has been done. 
        /// It could even be possible to reanalize the data here.
        /// </summary>
        /// <param name="procesedImages"></param>
        /// <returns></returns>
        public virtual EyeTrackerImagesAndData PostProcessImages(EyeTrackerImagesAndData procesedImages) => procesedImages;

        /// <summary>
        /// Opens a custom UI for this eye tracking system.
        /// </summary>
        /// <returns></returns>
        public virtual Form? OpenEyeTrackingSystemUI => null;

        /// <summary>
        /// Gets custom menu items for this system to show in the main window
        /// </summary>
        /// <returns></returns>
        public virtual ToolStripMenuItem[] GetToolStripMenuItems()
        {
            return null;
        }

    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Base class for the settigns of the eye tracking system. With the minimum settings that all 
    /// video or camera systems should have.
    /// http://stackoverflow.com/questions/20084/xml-serialization-and-inherited-types
    /// </summary>
    [Serializable]
    public class EyeTrackingSystemSettings : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Gets or sets the resolution of the camera in mm per pixel. This should be set up automatically
        /// after a camera system is selected.
        /// </summary>       
        [Category("Camera properties"), Description("Camera resolution ( mm per pixels)")]
        public double MmPerPix
        {
            get { return this.mmPerPix; }
            set
            {
                if (value != this.mmPerPix)
                {
                    this.mmPerPix = value;
                    this.OnPropertyChanged(this, nameof(MmPerPix));
                }
            }
        }
        private double mmPerPix = 0.15;

        [Category("Camera properties"), Description("Camera from the camera to the eyes ( mm)")]
        public double DistanceCameraToEyeMm
        {
            get { return this.distanceCameraToEyeMm; }
            set
            {
                if (value != this.distanceCameraToEyeMm)
                {
                    this.distanceCameraToEyeMm = value;
                    this.OnPropertyChanged(this, nameof(DistanceCameraToEyeMm));
                }
            }
        }
        private double distanceCameraToEyeMm = 50; // default value

        /// <summary>
        /// Gets or sets the frame rate of the cameras
        /// </summary>       
        [Category("Camera properties"), Description("Camera frame rate")]
        [NeedsRestarting]
        public float FrameRate
        {
            get { return this.frameRate; }
            set
            {
                if (value != this.frameRate)
                {
                    this.frameRate = value;
                    this.OnPropertyChanged(this, nameof(FrameRate));
                }
            }
        }
        private float frameRate = 100.0f; // default value

        [Category("Camera properties"), Description("Which eye to use.")]
        [NeedsRestarting]
        public Eye Eye
        {
            get
            {
                return this.eye;
            }
            set
            {
                if (value != this.eye)
                {
                    this.eye = value;
                    this.OnPropertyChanged(this, nameof(Eye));
                }
            }
        }
        private Eye eye = Eye.Both;

    }
}
