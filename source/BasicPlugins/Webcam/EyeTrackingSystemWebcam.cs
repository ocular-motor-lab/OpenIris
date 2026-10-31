//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemWebcam.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Eye tracking with a webcam that films the face. Finds the face and the eyes, and gives the
    /// pipeline one image per eye, centered on the eye, level and at a fixed scale. Use it with the
    /// "Webcam iris" pipeline.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Webcam", typeof(EyeTrackingSystemWebcamSettings), VideoEyeConfiguration.OneVideo)]
    public sealed class EyeTrackingSystemWebcam : EyeTrackingSystemBase
    {
        private readonly object trackerLock = new object();
        private CameraEyeWebcam? camera;
        private WebcamEyeLocator? tracker;
        private volatile WebcamFaceView? faceView;

        protected override EyeCollection<CameraEye?>? CreateAndStartCameras()
        {
            var settings = (EyeTrackingSystemWebcamSettings)Settings;

            camera = new CameraEyeWebcam(Eye.Both, settings.CameraIndex, new Size(settings.ResolutionWidth, settings.ResolutionHeight), settings.FrameRate);
            camera.Start();

            // The camera sees both eyes. Settings.Eye only chooses which eye images to process.
            return Settings.Eye switch
            {
                Eye.Left => new EyeCollection<CameraEye?>(camera, null),
                Eye.Right => new EyeCollection<CameraEye?>(null, camera),
                _ => new EyeCollection<CameraEye?>(camera),
            };
        }

        public override EyeCollection<ImageEye?> PreProcessImages(EyeCollection<ImageEye?> images)
        {
            // Videos go through here twice, first when read and then before processing: only the
            // image of the whole face needs cutting.
            var face = images.FirstOrDefault(image => image?.WhichEye == Eye.Both);
            if (face is null) return images;

            var settings = (EyeTrackingSystemWebcamSettings)Settings;
            lock (trackerLock)
            {
                tracker ??= new WebcamEyeLocator(settings.EyeImageSize);
            }

            var eyes = tracker.Process(face);
            faceView?.Offer(face.Image, tracker, eyes);
            if (Settings.Eye == Eye.Left) eyes[Eye.Right] = null;
            if (Settings.Eye == Eye.Right) eyes[Eye.Left] = null;
            return eyes;
        }

        /// <summary>
        /// Opens the window with the whole face when tracking from the webcam starts. OpenIris
        /// closes it when tracking stops.
        /// </summary>
        public override Form? OpenEyeTrackingSystemUI
        {
            get
            {
                if (!((EyeTrackingSystemWebcamSettings)Settings).ShowFaceView) return null;

                var view = new WebcamFaceView();
                view.FormClosed += (_, __) => faceView = null;
                view.Show();
                faceView = view;
                return view;
            }
        }

        public override void Dispose()
        {
            faceView = null;
            camera?.Stop();
            camera?.Dispose();
            camera = null;

            lock (trackerLock)
            {
                tracker?.Dispose();
                tracker = null;
            }
        }
    }

    /// <summary>
    /// Settings of the webcam system.
    /// </summary>
    public class EyeTrackingSystemWebcamSettings : EyeTrackingSystemSettings
    {
        public EyeTrackingSystemWebcamSettings()
        {
            Eye = Eye.Both;
            FrameRate = 30;

            // In the eye images: the eye (~28 mm corner to corner) is EyeImageSize / 2.3 pixels wide.
            PixPerMm = 2;
        }

        [Category("Webcam"), Description("Index of the webcam, 0 for the first one.")]
        [NeedsRestarting]
        public int CameraIndex { get => cameraIndex; set => SetProperty(ref cameraIndex, value, nameof(CameraIndex)); }
        private int cameraIndex = 0;

        [Category("Webcam"), Description("Image width to ask the webcam for. More pixels on the eyes track better. The webcam may choose another resolution.")]
        [NeedsRestarting]
        public int ResolutionWidth { get => resolutionWidth; set => SetProperty(ref resolutionWidth, value, nameof(ResolutionWidth)); }
        private int resolutionWidth = 1280;

        [Category("Webcam"), Description("Image height to ask the webcam for.")]
        [NeedsRestarting]
        public int ResolutionHeight { get => resolutionHeight; set => SetProperty(ref resolutionHeight, value, nameof(ResolutionHeight)); }
        private int resolutionHeight = 720;

        [Category("Eye images"), Description("Width and height in pixels of the image of each eye cut out of the webcam image.")]
        [NeedsRestarting]
        public int EyeImageSize { get => eyeImageSize; set => SetProperty(ref eyeImageSize, value, nameof(EyeImageSize)); }
        private int eyeImageSize = 128;

        [Category("Webcam"), Description("Open a window with the whole webcam image, the face mesh and the regions cut out as eye images when tracking starts.")]
        public bool ShowFaceView { get => showFaceView; set => SetProperty(ref showFaceView, value, nameof(ShowFaceView)); }
        private bool showFaceView = true;
    }
}
