//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSimulationFakeEye.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel.Composition;
    using System.Threading;
    using OpenIris.ImageGrabbing;
    using System.Windows.Forms;

    /// <summary>
    /// Simulator of eye tracker from virtual eyeball.
    /// </summary>
    [Export(typeof(IEyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("SimulationFakeEye")]
    public class EyeTrackingSystemSimulationFakeEye : EyeTrackingSystemBase
    {
        private OpenIris.EyeTrackingSystems.FakeEyeControlUI UIform = new OpenIris.EyeTrackingSystems.FakeEyeControlUI();

        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye> CreateAndStartCameras()
        {
            var cameraLeft = new CameraEyeVirtualEye(Eye.Left, this.UIform);

            var cameraRight = new CameraEyeVirtualEye(Eye.Right, this.UIform);

            return new EyeCollection<CameraEye>(cameraLeft, cameraRight);
        }

        public override Form OpenEyeTrackingSystemUI
        {
            get
            {
                this.UIform.Show();

                return this.UIform;
            }
        }
    }
}
