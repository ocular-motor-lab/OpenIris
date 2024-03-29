﻿//-----------------------------------------------------------------------
// <copyright file="IImageEyeSource.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
    public interface IImageEyeSource: IDisposable
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
            return cameraOrientation switch
            {
                CameraOrientation.UprightMirrored => true,
                CameraOrientation.Rotated90Mirrored => true,
                CameraOrientation.Rotated180Mirrored => true,
                CameraOrientation.Rotated270Mirrored => true,
                _ => false,
            };
        }

        /// <summary>
        /// Gets a value indicating wether the camera is upside down.
        /// </summary>
        /// <param name="cameraOrientation"></param>
        /// <returns></returns>
        public static bool IsUpsideDown(this CameraOrientation cameraOrientation)
        {
            return cameraOrientation switch
            {
                CameraOrientation.Rotated180 => true,
                CameraOrientation.Rotated270 => true,
                CameraOrientation.Rotated180Mirrored => true,
                CameraOrientation.Rotated270Mirrored => true,
                _ => false,
            };
        }

        /// <summary>
        /// Gets a value indicating wether the camera is 90 deg rotated.
        /// </summary>
        /// <param name="cameraOrientation"></param>
        /// <returns></returns>
        public static bool IsRotated(this CameraOrientation cameraOrientation)
        {
            return cameraOrientation switch
            {
                CameraOrientation.Rotated90 => true,
                CameraOrientation.Rotated90Mirrored => true,
                CameraOrientation.Rotated270 => true,
                CameraOrientation.Rotated270Mirrored => true,
                _ => false,
            };
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
