//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSyntheticEyes.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System.ComponentModel.Composition;
    using OpenIris.EyeTrackingSystems;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Synthetic eyes: images of a drawn eyeball that can be rotated to any orientation from a window,
    /// in several coordinate systems. To test the tracking and the calibration with a known answer.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Synthetic Eyes")]
    public class EyeTrackingSystemSyntheticEyes : EyeTrackingSystemBase
    {
        private readonly SyntheticEyeRenderer renderer = new SyntheticEyeRenderer();
        private volatile EyeRotation rotation = EyeRotation.Primary;

        /// <summary>
        /// Orientation of both eyes. The window (SyntheticEyesWindow) sets it, the cameras read it
        /// from their threads: EyeRotation is immutable, so reading the reference is enough.
        /// </summary>
        public EyeRotation Rotation { get => rotation; set => rotation = value; }

        /// <summary>
        /// Gets the cameras: one for each eye, both looking the same way.
        /// </summary>
        protected override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var cameraLeft = new CameraEyeSynthetic(Eye.Left, renderer, () => Rotation);
            cameraLeft.Start();

            var cameraRight = new CameraEyeSynthetic(Eye.Right, renderer, () => Rotation);
            cameraRight.Start();

            return new EyeCollection<CameraEye>(cameraLeft, cameraRight);
        }

        /// <summary>
        /// Frees the images.
        /// </summary>
        public override void Dispose()
        {
            renderer.Dispose();
            base.Dispose();
        }
    }
}
