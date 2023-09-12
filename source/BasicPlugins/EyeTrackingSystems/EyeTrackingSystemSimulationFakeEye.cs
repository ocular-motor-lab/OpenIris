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
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("SimulationFakeEye")]
    public class EyeTrackingSystemSimulationFakeEye : EyeTrackingSystemBase
    {
        private EyeTrackingSystems.FakeEyeControlUI UIform = new EyeTrackingSystems.FakeEyeControlUI();

        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        protected override CameraEye?[]? CreateAndStartCameras()
        {
            var cameraLeft = new CameraEyeVirtualEye(Eye.Left, UIform);
            cameraLeft.Start();

            var cameraRight = new CameraEyeVirtualEye(Eye.Right, UIform);
            cameraRight.Start();

            return new EyeCollection<CameraEye>(cameraLeft, cameraRight);
        }

        public override Form OpenEyeTrackingSystemUI
        {
            get
            {
                UIform.Show();

                return UIform;
            }
        }
    }
}
