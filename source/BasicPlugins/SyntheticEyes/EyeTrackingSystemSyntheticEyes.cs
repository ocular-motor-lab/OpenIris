//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSyntheticEyes.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System.ComponentModel.Composition;
    using System.Windows.Forms;
    using OpenIris.EyeTrackingSystems;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Synthetic eyes: images of a drawn eyeball that can be rotated to any orientation from a window,
    /// in several coordinate systems. To test the tracking and the calibration with a known answer.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Synthetic Eyes")]
    public class EyeTrackingSystemSyntheticEyes : EyeTrackingSystemBase
    {
        private readonly SyntheticEyesUI window = new SyntheticEyesUI();
        private readonly SyntheticEyeRenderer renderer = new SyntheticEyeRenderer();

        /// <summary>
        /// Gets the cameras: one for each eye, both looking the same way.
        /// </summary>
        protected override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var cameraLeft = new CameraEyeSynthetic(Eye.Left, renderer, () => window.Rotation);
            cameraLeft.Start();

            var cameraRight = new CameraEyeSynthetic(Eye.Right, renderer, () => window.Rotation);
            cameraRight.Start();

            return new EyeCollection<CameraEye>(cameraLeft, cameraRight);
        }

        /// <summary>
        /// The window to set the orientation of the eyes.
        /// </summary>
        public override Form OpenEyeTrackingSystemUI
        {
            get
            {
                window.Show();
                return window;
            }
        }

        /// <summary>
        /// Closes the window and frees the images.
        /// </summary>
        public override void Dispose()
        {
            if (!window.IsDisposed)
            {
                if (window.InvokeRequired) window.BeginInvoke(new System.Action(window.Close));
                else window.Close();
            }
            renderer.Dispose();
            base.Dispose();
        }
    }
}
