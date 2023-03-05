//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Base class for eye tracking systems.
    /// </summary>
    public abstract class EyeTrackingSystemBase : IEyeTrackingSystem
    {
        /// <summary>
        /// Initializes an instance.
        /// </summary>
        public EyeTrackingSystemBase()
        {
            Settings = new EyeTrackingSystemSettings();
        }

        /// <summary>
        /// Initializzes an insstance of the class EyeTrackinSystem.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name">Name of the system.</param>
        /// <returns>The system.</returns>
        public static IEyeTrackingSystem Create(string name, EyeTrackingSystemSettings? settings)
        {
            var system = EyeTrackerPluginManager.EyeTrackingsyStemFactory?.Create(name)
                ?? throw new OpenIrisException("Bad system");
            settings ??= EyeTrackerPluginManager.EyeTrackingsyStemFactory?.GetDefaultSettings(name) as EyeTrackingSystemSettings
                ?? throw new OpenIrisException("Bad settings");

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
        /// Settings of the system.
        /// </summary>
        public EyeTrackingSystemSettings Settings { get; set; }

        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        public virtual EyeCollection<CameraEye?>? CreateCameras() => null;

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

}
