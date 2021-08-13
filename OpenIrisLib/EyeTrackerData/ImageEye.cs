//-----------------------------------------------------------------------
// <copyright file="ImageEye.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Image wrapper to add some eye tracking related functionality.
    /// </summary>
    [Serializable]
    public class ImageEye : IDisposable
    {
        /// <summary>
        /// Initializes an empty instance of the class ImageEye. 
        /// </summary>
        public ImageEye(Image<Gray,byte> image, Eye whichEye, ImageEyeTimestamp timeStamp, object? extraData)
        {
            Image = image;
            WhichEye = whichEye;
            TimeStamp = timeStamp;
            ImageSourceData = extraData;
        }

        /// <summary>
        /// Initializes a new instance of the ImageEye class.
        /// </summary>
        /// <param name="bitmap">Bitmap of the image.</param>
        /// <param name="whichEye">The eye.</param>
        /// <param name="timeStamp">The timestamp.</param>
        public ImageEye(Bitmap bitmap, Eye whichEye, ImageEyeTimestamp timeStamp)
        {
            Image = new Image<Gray, byte>(bitmap);
            WhichEye = whichEye;
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// Initializes an image directly with a pointer to the data.
        /// </summary>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="stride">Size of aligned image row in bytes.</param>
        /// <param name="data">Pointer to the data.</param>
        /// <param name="timeStamp">Timestamp of the image.</param>
        public ImageEye(int width, int height, int stride, IntPtr data, ImageEyeTimestamp timeStamp)
        {
            Image = new Image<Gray, byte>(width, height, stride, data);
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// Initializes a new instance of the ImageEye class from a serialized object (Implementation of ISerializable).
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected ImageEye(SerializationInfo info, StreamingContext context)
        {
            Image = new Image<Gray, byte>(info, context);

            if (info is null) throw new ArgumentNullException(nameof(info));

            WhichEye = (Eye)info.GetValue("WhichEye", WhichEye.GetType());
            TimeStamp = (ImageEyeTimestamp)info.GetValue("TimeStamp", TimeStamp.GetType());
        }

        /// <summary>
        /// Gets the image. Be very careful using this! it may cause lots of problems with threads.
        /// </summary>
        public Image<Gray, byte> Image { get; set; }

        /// <summary>
        /// Aditional generic object that can store informaiton from the image source that was used to grab the image.
        /// </summary>
        public object? ImageSourceData {get; set;} 

        /// <summary>
        /// Gets Left or Right Eye.
        /// </summary>
        public Eye WhichEye { get; set; }
        
        /// <summary>
        /// Gets the size of the image of the eye in pixels.
        /// </summary>
        public Size Size { get { return Image?.Size ?? new Size(); } }
        
        /// <summary>
        /// Gets the timestamp from the time the image was captured.
        /// </summary>
        public ImageEyeTimestamp TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the EyeData.
        /// </summary>
        public EyeData? EyeData { get; set; }

        /// <summary>
        /// Gets the image of the iris.
        /// </summary>
        public Image<Gray, byte>? ImageTorsion { get; set; }

        /// <summary>
        /// Creates a copy of the image.
        /// </summary>
        /// <returns>The copy.</returns>
        public ImageEye Copy(Rectangle roi)
        {
            return new ImageEye(Image.Copy(roi), WhichEye, TimeStamp, ImageSourceData);
        }

        /// <summary>
        /// Corrects the orientation of the image. Useful in cases where the camera is rotated.
        /// Or when there are mirrors.
        /// </summary>
        /// <param name="cameraOrientation">Orientation of the camera reltive to the eye.</param>
        /// <returns>The new images with the correct orientation.</returns>
        public void CorrectOrientation(CameraOrientation cameraOrientation)
        {
            if (!cameraOrientation.IsRotated())
            {
                // Fix the image
                if (!cameraOrientation.IsMirrored() && !cameraOrientation.IsUpsideDown())
                {
                    // Do nothing
                }

                if (!cameraOrientation.IsMirrored() && cameraOrientation.IsUpsideDown())
                {
                    FlipHorizontal();
                    FlipVertical();
                }

                if (cameraOrientation.IsMirrored() && !cameraOrientation.IsUpsideDown())
                {
                    FlipHorizontal();
                }

                if (cameraOrientation.IsMirrored() && cameraOrientation.IsUpsideDown())
                {
                    FlipVertical();
                }
            }
            else
            {
                Transpose();

                // Fix the image
                if (!cameraOrientation.IsMirrored() && !cameraOrientation.IsUpsideDown())
                {
                    // Do nothing
                }

                if (!cameraOrientation.IsMirrored() && cameraOrientation.IsUpsideDown())
                {
                    FlipHorizontal();
                    FlipVertical();
                }

                if (cameraOrientation.IsMirrored() && !cameraOrientation.IsUpsideDown())
                {
                    FlipHorizontal();
                }

                if (cameraOrientation.IsMirrored() && cameraOrientation.IsUpsideDown())
                {
                    FlipVertical();
                }
            }
        }

        /// <summary>
        /// Rotates the image 180 degrees.
        /// </summary>
        public void Rotate180()
        {
            Image = Image.Flip(Emgu.CV.CvEnum.FlipType.Horizontal).Flip(Emgu.CV.CvEnum.FlipType.Vertical);
        }

        /// <summary>
        /// Flips the image horizontally.
        /// </summary>
        public void FlipHorizontal()
        {
            Image = Image.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
        }

        /// <summary>
        /// Flips the image vertically.
        /// </summary>
        public void FlipVertical()
        {
            Image = Image.Flip(Emgu.CV.CvEnum.FlipType.Vertical);
        }

        /// <summary>
        /// Transposes the image.
        /// </summary>
        public void Transpose()
        {
            var imageTemp = new Image<Gray, byte>(Size.Height, Size.Width);
            CvInvoke.Transpose(Image, imageTemp);
            Image = imageTemp;
        }
                
        /// <summary>
        /// Copies a fragment of the image.
        /// </summary>
        /// <param name="roi">Region of interest limiting the fragment.</param>
        /// <returns>The fragment of the image.</returns>
        public void Crop(Rectangle roi)
        {
            Image = Image.Copy(roi);
        }

        /// <summary>
        /// Copies a fragment of the image and transposes it.
        /// </summary>
        /// <param name="roi">Region of interest limiting the fragment.</param>
        /// <returns>The fragment of the image.</returns>
        public ImageEye GetCroppedAndTransposedImage(Rectangle roi)
        {
            var imageTemp = new Image<Gray, byte>(roi.Height, roi.Width);
            Image.ROI = roi;
            CvInvoke.Transpose(Image, imageTemp);
            Image.ROI = new Rectangle();
            Image = imageTemp;

            return new ImageEye(imageTemp, WhichEye, TimeStamp, ImageSourceData);
        }
                
        /// <summary>
        /// Updates a pixel in the image.
        /// </summary>
        /// <param name="i">Row of the pixel.</param>
        /// <param name="j">Column of the pixel</param>
        /// <param name="k">Color of the pixel.</param>
        /// <param name="newValue">New value for the pixel</param>
        public void UpdateData(int i, int j, int k, byte newValue)
        {
            Image.Data[i, j, k] = newValue;
        }

        /// <summary>
        /// Gets the value of a pixel in the image.
        /// </summary>
        /// <param name="i">Row of the pixel.</param>
        /// <param name="j">Column of the pixel</param>
        /// <param name="k">Color of the pixel.</param>
        /// <returns>Value for the pixel.</returns>
        public byte GetData(int i, int j, int k)
        {
            return Image.Data[i, j, k];
        }

        /// <summary>
        /// Gets a binary image with white in the pixels with a value below a threshold.
        /// </summary>
        /// <param name="thresholdDark">The threshold.</param>
        /// <returns>The binary image.</returns>
        public Image<Gray, byte> ThresholdDark(double thresholdDark)
        {
            return Image.ThresholdBinaryInv(new Gray(thresholdDark), new Gray(1));
        }

        /// <summary>
        /// Gets a binary image with white in the pixels with a value above a threshold.
        /// </summary>
        /// <param name="thresholdBright">The threshold.</param>
        /// <returns>The binary image.</returns>
        public Image<Gray, byte> ThresholdBright(double thresholdBright)
        {
            return Image.ThresholdBinaryInv(new Gray(thresholdBright), new Gray(1));
        }

        /// <summary>
        /// Gets a segment of the image resized and thresholded with the dark pixels.
        /// </summary>
        /// <param name="thresholdDark">The threshold.</param>
        /// <param name="newSize">New size of the image.</param>
        /// <param name="roi">Region of the segment.</param>
        /// <returns>The binary image</returns>
        public Image<Gray,byte> ThresholdDarkResized(double thresholdDark, Size newSize, Rectangle roi)
        {
            Image.ROI = roi;
            var imageThreshold = Image.Resize(newSize.Width, newSize.Height, Emgu.CV.CvEnum.Inter.Linear).ThresholdBinaryInv(new Gray(thresholdDark), new Gray(255));
            Image.ROI = new Rectangle();

            return imageThreshold;
        }

        /// <summary>
        /// Fills in the object information from the stream (Implementation of ISerializable).
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));

            info.AddValue("WhichEye", WhichEye);
            info.AddValue("TimeStamp", TimeStamp);

            Image.GetObjectData(info, context);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">True if disponsing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) Image?.Dispose();
        }
    }
}
