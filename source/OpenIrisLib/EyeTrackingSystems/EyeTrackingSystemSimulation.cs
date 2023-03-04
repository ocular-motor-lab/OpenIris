//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSimulation.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel.Composition;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Simulator of eye tracker from videos.
    /// </summary>
    [Export(typeof(IEyeTrackingSystem)), PluginDescriptionEyeTrackingSystem("Simulation", typeof(EyeTrackingSystemSettings))]
    public class EyeTrackingSystemSimulation : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye?> CreateCameras()
        {
            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileNames = new string[]
                {
                    currentPath + @"\JorgeLeft.avi",
                    currentPath + @"\JorgeRight.avi"
                };

            var cameraLeft = new CameraEyeVideoSimulation(Eye.Left, fileNames[(int)Eye.Left], Settings.FrameRate);
            cameraLeft.CameraOrientation = CameraOrientation.UprightMirrored;

            var cameraRight = new CameraEyeVideoSimulation(Eye.Right, fileNames[(int)Eye.Right], Settings.FrameRate);
            cameraRight.CameraOrientation = CameraOrientation.UprightMirrored;

            long loopAtFrame = Math.Min(cameraLeft.NumberOfFrames - 1, cameraRight.NumberOfFrames - 1);
            cameraLeft.LoopAtFrame = loopAtFrame;
            cameraRight.LoopAtFrame = loopAtFrame;

            return new EyeCollection<CameraEye?>(cameraLeft, cameraRight);
        }
    }
}
