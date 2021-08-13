//-----------------------------------------------------------------------
// <copyright file="IMovableImageEyeSource.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
    using System.Drawing;

    /// <summary>
    /// Interface of cameras that can move the ROI.
    /// </summary>
    public interface IMovableImageEyeSource
    {
        /// <summary>
        /// Center the ROI on a point.
        /// </summary>
        /// <param name="center"></param>
        void Center(PointF center);

        /// <summary>
        /// Move the camera in a given direction.
        /// </summary>
        /// <param name="direction"></param>
        void Move(MovementDirection direction);
    }

    /// <summary>
    /// Possible directions of motion.
    /// </summary>
    public enum MovementDirection
    {
        /// <summary>
        /// Do not move the camera.
        /// </summary>
        None, 

        /// <summary>
        /// Move the camera Up.
        /// </summary>
        Up,

        /// <summary>
        /// Move the camera Down.
        /// </summary>
        Down,

        /// <summary>
        /// Move the camera Left.
        /// </summary>
        Left,

        /// <summary>
        /// Move the camera Right.
        /// </summary>
        Right
    }
}
