//-----------------------------------------------------------------------
// <copyright file="IImageEyeSource.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
#nullable enable

    using System;
    using System.Drawing;
    using OpenIris;
    
    /// <summary>
    /// Generic interface for a camera in an eye tracker or a video file.
    /// </summary>
    public interface IImageEyeSource
    {
        /// <summary>
        /// Gets or sets which eye the images are from: left eye, right eye, or both.
        /// </summary>
        Eye WhichEye { get; }

        /// <summary>
        /// Gets the frame rate reported by the image source. Specific implementations of cameras should override this property.
        /// </summary>
        double FrameRate { get; }

        /// <summary>
        /// Gets the frame rate of the video file. Specific implementations of cameras should override this property.
        /// </summary>
        Size FrameSize { get; }
        
        /// <summary>
        /// Gets a value indicating whether the camera is upside down, rotated or mirrored.
        /// </summary>
        CameraOrientation CameraOrientation { get; }

        /// <summary>
        /// Gets arbitrary aditional information about image source.
        /// </summary>
        object Info { get; }

        /// <summary>
        /// Grab new image of the eye. Specific implementations of cameras should override this property.
        /// </summary>
        /// <returns>Image of the eye.</returns>
        ImageEye? GrabImageEye();
        
        /// <summary>
        /// Stops capturing images. Important. IF the call to grabImageEye is blocking it must unblock it.
        /// </summary>
        void Stop();
    }


    /// <summary>
    /// Extension methods for CameraOrientation
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets a value indicating wether the camera is mirrored.
        /// </summary>
        /// <param name="cameraOrientation"></param>
        /// <returns></returns>
        public static bool IsMirrored(this CameraOrientation cameraOrientation)
        {
            return (
                cameraOrientation == CameraOrientation.UprightMirrored ||
                cameraOrientation == CameraOrientation.Rotated90Mirrored ||
                cameraOrientation == CameraOrientation.Rotated180Mirrored ||
                cameraOrientation == CameraOrientation.Rotated270Mirrored);
        }

        /// <summary>
        /// Gets a value indicating wether the camera is upside down.
        /// </summary>
        /// <param name="cameraOrientation"></param>
        /// <returns></returns>
        public static bool IsUpsideDown(this CameraOrientation cameraOrientation)
        {
            return (
                cameraOrientation == CameraOrientation.Rotated180 ||
                cameraOrientation == CameraOrientation.Rotated270 ||
                cameraOrientation == CameraOrientation.Rotated180Mirrored ||
                cameraOrientation == CameraOrientation.Rotated270Mirrored);
        }

        /// <summary>
        /// Gets a value indicating wether the camera is 90 deg rotated.
        /// </summary>
        /// <param name="cameraOrientation"></param>
        /// <returns></returns>
        public static bool IsRotated(this CameraOrientation cameraOrientation)
        {
            return (
                cameraOrientation == CameraOrientation.Rotated90 ||
                cameraOrientation == CameraOrientation.Rotated90Mirrored ||
                cameraOrientation == CameraOrientation.Rotated270 ||
                cameraOrientation == CameraOrientation.Rotated270Mirrored);
        }
    }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public enum CameraOrientation
    {
        Upright,
        Rotated90,
        Rotated180,
        Rotated270,
        UprightMirrored,
        Rotated90Mirrored,
        Rotated180Mirrored,
        Rotated270Mirrored,
    }

}
