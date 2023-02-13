//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationParameters.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>
    /// Class containing all the info regarding the calibration of one eye. Three main things
    ///     1) Eye physical model. Globel model of the eye in image coordinates.
    ///     2) Reference data. Eye data corresponding with the zero eye position. 
    ///     3) Reference torsion image. For torsoin algorithms it has the option to also 
    ///         save an image as reference.
    /// </summary>
    [Serializable]
    public class EyeCalibration
    {
        /// <summary>
        /// Initializes a new instance of the EyeTrackerCalibrationInfo class.
        /// </summary>
        public EyeCalibration()
        {
            ReferenceData = new EyeData();
        }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerCalibrationInfo class.
        /// </summary>
        /// <param name="whichEye">Left or right eye.</param>
        public EyeCalibration(Eye whichEye)
            : this()
        {
            WhichEye = whichEye;
        }

        /// <summary>
        /// True if it is empty.
        /// </summary>
        public bool IsEmpty { get { return !HasEyeModel && !HasReference; } }

        /// <summary>
        /// True if it has an eye model already.
        /// </summary>
        public bool HasEyeModel { get { return !EyePhysicalModel.IsEmpty; } }

        /// <summary>
        /// True if ther eis already a reference.
        /// </summary>
        public bool HasReference { get { return !ReferenceData.Pupil.IsEmpty; } }

        /// <summary>
        /// Gets the size in pixels of the raw images.
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// Left or right eye.
        /// </summary>
        public Eye WhichEye { get; set; }

        /// <summary>
        /// Gets the radius and the center of the eye globe.
        /// </summary>
        public EyePhysicalModel EyePhysicalModel { get; set; }

        /// <summary>
        /// Gets the reference data.
        /// </summary>
        public EyeData ReferenceData { get; set; }

        /// <summary> 
        /// Gets the reference image for the torsion.
        /// </summary>
        [XmlIgnore]
        public Image<Gray, byte>? ImageTorsionReference { get; set; }

        /// <summary>
        /// Torsion image in byte format to work with serialization.
        /// </summary>
        // http://stackoverflow.com/questions/1907077/serialize-a-bitmap-in-c-net-to-xml
        [XmlElement("ImageTorsionReference")]
        public byte[]? ImageTorsionReferenceSerialized
        {
            get
            {
                // serialize
                if (ImageTorsionReference is null) return null;

                using var ms = new MemoryStream();

                ImageTorsionReference.Bitmap.Save(ms, ImageFormat.Bmp);
                return ms.ToArray();
            }
            set
            {
                ImageTorsionReference = null;

                // deserialize
                if (value != null)
                {
                    using var ms = new MemoryStream(value);

                    ImageTorsionReference = new Image<Gray, byte>(new Bitmap(ms));
                }
            }
        }

        /// <summary>
        /// Sets the eye model in the calibration.
        /// </summary>
        /// <param name="eyePhysicalModel"></param>
        public void SetEyeModel(EyePhysicalModel eyePhysicalModel)
        {
            EyePhysicalModel = eyePhysicalModel;
        }

        /// <summary>
        /// Sets the reference data for the calibration.
        /// </summary>
        /// <param name="processedImageEye"></param>
        public void SetReference(ImageEye? processedImageEye)
        {
            ReferenceData = processedImageEye?.EyeData ?? new EyeData();
            ImageTorsionReference = processedImageEye?.ImageTorsion;
        }

        /// <summary>
        /// Makes a copy of the EyeCalibration data.
        /// </summary>
        /// <returns></returns>
        public EyeCalibration Copy()
        {
            return new EyeCalibration()
            {
                EyePhysicalModel = EyePhysicalModel,
                ImageTorsionReference = ImageTorsionReference,
                ReferenceData = ReferenceData.Copy(),
            };
        }
    }
}
