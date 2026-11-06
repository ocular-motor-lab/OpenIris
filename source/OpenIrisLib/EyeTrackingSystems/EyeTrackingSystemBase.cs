//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemBase.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.Serialization;
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
        /// Gets the most frames from the cameras that OpenIris holds at once in its buffers (<see
        /// cref="EyeTracker.MaxFramesInBuffers"/>), set before the cameras are created. Cameras that
        /// give images without copying them need at least this many buffers of their own.
        /// </summary>
        public int MaxFramesInBuffers { get; internal set; }

        /// <summary>
        /// Image sources (cameras or videos) of this eye tracking system.
        /// </summary>
        public EyeCollection<IImageEyeSource?>? imageSources;

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
        /// Called when tracking stops. By default, stops and frees the cameras the system created.
        /// Without it they stay connected until the garbage collector frees them, from its own thread,
        /// maybe while a new camera object uses the same camera: that crashed OpenIris inside the
        /// FlyCapture SDK. Systems that free their cameras themselves override this.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var camera in (imageSources ?? Enumerable.Empty<IImageEyeSource?>()).OfType<CameraEye>().Distinct())
            {
                try
                {
                    camera.Stop();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("ERROR stopping a camera: " + ex.Message);
                }

                camera.Dispose();
            }

            imageSources = null;
        }

        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        internal EyeCollection<CameraEye?>? CreateAndStartCameras1()
        {
            var cameras = CreateAndStartCameras();
            imageSources = new EyeCollection<IImageEyeSource?>(cameras.Select(c => c as IImageEyeSource));

            foreach (var camera in cameras ?? Enumerable.Empty<CameraEye?>())
            {
                if (camera is not null) camera.Intrinsics = Settings.IntrinsicsOf(camera);
            }

            return cameras;
        }

        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        internal EyeCollection<VideoEye?>? CreateVideos_imageSource(EyeCollection<string?> fileNames)
        {
            var videos = CreateVideos(fileNames);
            imageSources = new EyeCollection<IImageEyeSource?>(videos.Select(c => c as IImageEyeSource));
            return videos;
        }

        /// <summary>
        /// Gets the cameras.
        /// </summary>
        /// <returns>List of image eye source objects.</returns>
        protected virtual EyeCollection<CameraEye?>? CreateAndStartCameras() => null;

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
        protected virtual EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> fileNames)
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
        /// Gets the actions of this system that the user can run. The Windows user interface shows
        /// them in the System menu.
        /// </summary>
        /// <remarks>
        /// To give the system a window, don't open it here: export a view for the user interface
        /// instead (in the Windows user interface, an EyeTrackingSystemWindow), so the system itself
        /// doesn't depend on any user interface library.
        /// </remarks>
        public virtual EyeTrackingSystemCommand[]? GetCommands() => null;

        /// <summary>
        /// Gets a value indicating weather the cameras can move the ROI.
        /// </summary>
        public bool CamerasMovable { get => imageSources?.Any(s => s is IMovableImageEyeSource) ?? false; }

        /// <summary>
        /// Gets a value indicating weather the cameras can change exposure.
        /// </summary>
        public bool CamerasHaveVariableEspsure { get => imageSources?.Any(s => s is IVariableExposureImageEyeSource) ?? false; }


        /// <summary>
        /// Centers the camera ROI around the current pupil center.
        /// </summary>
        internal void CenterEyes(PointF centerLeftEye, PointF centerRightEye)
        {
            if (imageSources is null) return;

            foreach (var camera in imageSources)
            {
                if (camera is IMovableImageEyeSource movableCamera)
                {
                    var center = camera.WhichEye switch
                    {
                        Eye.Left => centerLeftEye,
                        Eye.Right => centerRightEye,
                        Eye.Both => new PointF(
                                (centerLeftEye.X + centerRightEye.X) / 2,
                                (centerLeftEye.Y + centerRightEye.Y) / 2),
                        _ => throw new InvalidOperationException("Not valid eye"),
                    };

                    movableCamera.Center(center);
                }
            }
        }

        /// <summary>
        /// Moves the camera (or ROI).
        /// </summary>
        /// <param name="whichEyeToMove">Left,right or both.</param>
        /// <param name="direction">Which direction to move.</param>
        internal void MoveCamera(Eye whichEyeToMove, MovementDirection direction)
        {
            if (imageSources is null) throw new InvalidOperationException("Sources cannot be null");

            foreach (var camera in imageSources)
            {
                if (camera is IMovableImageEyeSource movableCamera)
                {
                    if ((camera.WhichEye == whichEyeToMove) || (whichEyeToMove == Eye.Both) || (camera.WhichEye == Eye.Both))
                    {
                        movableCamera?.Move(direction);
                    }
                }
            }

            SaveCameraMove();
        }

        protected virtual void SaveCameraMove() { }
        protected virtual void SaveCameraExposure() { }

        /// <summary>
        /// </summary>
        internal void IncreaseExposure()
        {
            if (imageSources is null) throw new InvalidOperationException("Sources cannot be null");

            foreach (var camera in imageSources)
            {
                if (camera is IVariableExposureImageEyeSource cameraExpo)
                {
                    cameraExpo?.IncreaseExposure();
                }
            }

            SaveCameraExposure();
        }

        /// <summary>
        /// </summary>
        internal void ReduceExposure()
        {
            if (imageSources is null) throw new InvalidOperationException("Sources cannot be null");

            foreach (var camera in imageSources)
            {
                if (camera is IVariableExposureImageEyeSource cameraExpo)
                {
                    cameraExpo?.ReduceExposure();
                }
            }

            SaveCameraExposure();
        }

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

        [Category("Camera properties"), Description("Intrinsics of the cameras from a camera calibration, as OpenCV gives them: one for a camera that sees both eyes, " +
            "or left and right, as the cameras of the system. For the whole sensor and the images as the camera sends them (before the camera orientation). " +
            "Unknown (Fx 0): from the sensor size and PixPerMm. Calibrate again after changing them.")]
        [NeedsRestarting]
        [TypeConverter(typeof(EyeCollectionConverter<CameraIntrinsics>))]
        public EyeCollection<CameraIntrinsics>? CameraIntrinsics { get => cameraIntrinsics; set => SetProperty(ref cameraIntrinsics, value, nameof(CameraIntrinsics)); }
        private EyeCollection<CameraIntrinsics>? cameraIntrinsics;

        /// <summary>
        /// Unknown intrinsics for each camera of a system: one, or left and right.
        /// </summary>
        /// <param name="cameras">The cameras of the system, as its attribute says.</param>
        public static EyeCollection<CameraIntrinsics> UnknownIntrinsics(VideoEyeConfiguration cameras) => Unknown<CameraIntrinsics>(cameras);

        /// <summary>
        /// Unknown values (default) for each camera of a system: one, or left and right.
        /// </summary>
        /// <param name="cameras">The cameras of the system, as its attribute says.</param>
        public static EyeCollection<T> Unknown<T>(VideoEyeConfiguration cameras) where T : struct =>
            cameras == VideoEyeConfiguration.OneVideo
                ? new EyeCollection<T>(default(T))
                : new EyeCollection<T>(default, default);

        /// <summary>
        /// The intrinsics of a camera: measured, from the settings, or else from the pixel scale.
        /// </summary>
        public CameraIntrinsics IntrinsicsOf(CameraEye camera)
        {
            var measured = (CameraIntrinsics?.Count, camera.WhichEye) switch
            {
                (1, Eye.Both) or (2, Eye.Left) or (2, Eye.Right) => CameraIntrinsics![camera.WhichEye],
                _ => default,
            };

            return measured.IsEmpty ? OpenIris.CameraIntrinsics.FromPixelScale(camera.SensorSize, PixPerMm) : measured;
        }

        [Category("Camera properties"), Description("Where each camera is and how it's turned, as OpenCV's rvec (radians) and tvec (mm): from the head frame " +
            "(x forward, y to the subject's left, z up, origin at the center of the eye the camera looks at, or between the eyes for a camera that sees both) " +
            "to the camera's (x right, y down, z forward, in the image after the camera orientation). One for a camera that sees both eyes, or left and right. " +
            "Unknown (tvec 0): facing the eye, with the image's up as the head's up. Calibrate again after changing them.")]
        [TypeConverter(typeof(EyeCollectionConverter<CameraExtrinsics>))]
        public EyeCollection<CameraExtrinsics>? CameraExtrinsics { get => cameraExtrinsics; set => SetProperty(ref cameraExtrinsics, value, nameof(CameraExtrinsics)); }
        private EyeCollection<CameraExtrinsics>? cameraExtrinsics;

        /// <summary>
        /// The extrinsics of the camera that sees an eye: measured, from the settings, or else facing
        /// the eye at the assumed distance.
        /// </summary>
        /// <param name="eye">The eye.</param>
        public CameraExtrinsics ExtrinsicsOf(Eye eye)
        {
            var measured = (CameraExtrinsics?.Count, eye) switch
            {
                (1, _) => CameraExtrinsics![Eye.Both],
                (2, Eye.Left) or (2, Eye.Right) => CameraExtrinsics![eye],
                _ => default,
            };

            return measured.IsEmpty ? OpenIris.CameraExtrinsics.Facing(OpenIris.CameraIntrinsics.AssumedDistanceToEyeMm) : measured;
        }

        [Category("Camera properties"), Description("The lights (LEDs) of the system, that make the corneal reflections: their positions in mm in the head frame " +
            "with the origin between the eyes (x forward, y to the subject's left, z up). None, one or more, as the system has. (0, 0, 0) if unknown.")]
        public LightPosition[] Lights { get => lights; set => SetProperty(ref lights, value, nameof(Lights)); }
        private LightPosition[] lights = new LightPosition[0];

        [Category("Stimulus display"), Description("Width of the visible part of the screen where the calibration targets are shown, in mm.")]
        public double ScreenWidthMm { get => screenWidthMm; set => SetProperty(ref screenWidthMm, value, nameof(ScreenWidthMm)); }
        private double screenWidthMm = 530;

        [Category("Stimulus display"), Description("Distance from the eyes to the screen, in mm.")]
        public double ViewingDistanceMm { get => viewingDistanceMm; set => SetProperty(ref viewingDistanceMm, value, nameof(ViewingDistanceMm)); }
        private double viewingDistanceMm = 570;

        [Category("Stimulus display"), Description("The display where OpenIris shows the calibration targets itself (N-point calibration, Local mode): " +
            "its number in Windows' display settings (1, 2...). 0 for the main display.")]
        public int StimulusDisplay { get => stimulusDisplay; set => SetProperty(ref stimulusDisplay, value, nameof(StimulusDisplay)); }
        private int stimulusDisplay = 0;

        [Category("Camera properties"), Description("Distance between the centers of the eyes, in mm, to know where the lights are from each eye.")]
        public double DistanceBetweenEyesMm { get => distanceBetweenEyesMm; set => SetProperty(ref distanceBetweenEyesMm, value, nameof(DistanceBetweenEyesMm)); }
        private double distanceBetweenEyesMm = 63;

        /// <summary>
        /// The lights, from the center of an eye (between the eyes for Both). Unknown lights stay unknown.
        /// </summary>
        /// <param name="eye">The eye.</param>
        public LightPosition[] LightsFrom(Eye eye)
        {
            // The left eye is to the left (+y) of the point between the eyes.
            var eyeY = eye switch { Eye.Left => DistanceBetweenEyesMm / 2, Eye.Right => -DistanceBetweenEyesMm / 2, _ => 0 };
            return Lights.Select(light => light.IsEmpty ? light : new LightPosition(light.X, (float)(light.Y - eyeY), light.Z)).ToArray();
        }

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
