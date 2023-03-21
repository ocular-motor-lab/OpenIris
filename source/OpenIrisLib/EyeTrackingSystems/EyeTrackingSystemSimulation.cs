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
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Simulation", typeof(EyeTrackingSystemSettings))]
    public class EyeTrackingSystemSimulation : EyeTrackingSystemBase
    {
        /// <summary>
        /// Gets the cameras. In this case just one single camera.
        /// </summary>
        /// <returns>The list of cameras.</returns>
        public override EyeCollection<CameraEye?> CreateAndStartCameras()
        {
            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileNames = new string[]
                {
                    currentPath + @"\JorgeLeft.avi",
                    currentPath + @"\JorgeRight.avi"
                };

            var cameraLeft = new CameraEyeVideoSimulation(Eye.Left, fileNames[(int)Eye.Left], Settings.FrameRate)
            {
                CameraOrientation = CameraOrientation.UprightMirrored
            };

            var cameraRight = new CameraEyeVideoSimulation(Eye.Right, fileNames[(int)Eye.Right], Settings.FrameRate)
            {
                CameraOrientation = CameraOrientation.UprightMirrored
            };

            long loopAtFrame = Math.Min(cameraLeft.NumberOfFrames - 1, cameraRight.NumberOfFrames - 1);
            cameraLeft.LoopAtFrame = loopAtFrame;
            cameraRight.LoopAtFrame = loopAtFrame;

            return Settings.Eye switch
            {
                Eye.Both => new EyeCollection<CameraEye?>(cameraLeft, cameraRight),
                Eye.Left => new EyeCollection<CameraEye?>(cameraLeft, null),
                Eye.Right => new EyeCollection<CameraEye?>(null, cameraRight),
                _ => throw new NotImplementedException(),
            };
        }
    }

    // TODO: idea to disable inherited settings in case a new system does not need them.

    //public class EyeTrackingSystemSimulationSettings: EyeTrackingSystemSettings
    //{
    //    [Category("Camera properties"), Description("Camera frame rate")]
    //    [NeedsRestarting]
    //    [ReadOnly(true)]
    //    public override float FrameRate { get => 100; set => SetProperty(ref frameRate, 100, nameof(FrameRate)); }
    //    private float frameRate = 100.0f; // default value


    //    public float FrameRatet { get => 100; set => SetProperty(ref tframeRate, 100, nameof(FrameRatet)); }
    //    private float tframeRate = 100.0f; // default value
    //}
}
