//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace OpenIris
{
    using OpenIris.ImageGrabbing;
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Forms;

    /// <summary>
    /// 
    /// </summary>
    public interface IEyeTrackingSystem: IDisposable
    {
        string Name { get; set; }
        EyeTrackingSystemSettings Settings { get; set; }

        EyeCollection<CameraEye> CreateCameras();
        EyeCollection<VideoEye> CreateVideos(EyeCollection<string> fileNames);

        IHeadDataSource CreateHeadDataSourceWithCameras();
        IHeadDataSource CreateHeadDataSourceWithVideos();

        EyeCollection<ImageEye> PreProcessImagesFromCameras(EyeCollection<ImageEye> images);
        EyeCollection<ImageEye> PreProcessImagesFromVideos(EyeCollection<ImageEye> images);

        EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData procesedImages);


        Form OpenEyeTrackingSystemUI { get; }
        ToolStripMenuItem[] GetToolStripMenuItems();
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

        /// <summary>
        /// Gets or sets the resolution of the camera in mm per pixel. This should be set up automatically
        /// after a camera system is selected.
        /// </summary>       
        [Category("Camera properties"), Description("Camera resolution ( mm per pixels)")]
        public double MmPerPix { get => mmPerPix; set => SetProperty(ref mmPerPix, value, nameof(MmPerPix)); }
        private double mmPerPix = 0.15; // default value

        [Category("Camera properties"), Description("Distance from the camera to the eye ( mm)")]
        public double DistanceCameraToEyeMm { get => distanceCameraToEyeMm; set => SetProperty(ref distanceCameraToEyeMm, value, nameof(DistanceCameraToEyeMm)); }
        private double distanceCameraToEyeMm = 50; // default value

        [Category("Camera properties"), Description("Camera frame rate")]
        [NeedsRestarting]
        public float FrameRate { get => frameRate; set => SetProperty(ref frameRate, value, nameof(FrameRate)); }
        private float frameRate = 100.0f; // default value

        [Category("Camera properties"), Description("Which eye to use.")]
        [NeedsRestarting]
        public Eye Eye { get => eye; set => SetProperty(ref eye, value, nameof(Eye)); }
        private Eye eye = Eye.Both; // default value
    }
}