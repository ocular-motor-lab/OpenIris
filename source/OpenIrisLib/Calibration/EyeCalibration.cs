//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationParameters.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using System;
    using System.Drawing;
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
        /// Gets or sets the intrinsics of the camera in the pixels of the images the eye model and the
        /// reference are in (see ImageEye.Intrinsics), copied by the calibration session from the
        /// frames. Empty if unknown, as in calibration files from before 0.2.
        /// </summary>
        public CameraIntrinsics Intrinsics { get; set; }

        /// <summary>
        /// Gets or sets the extrinsics of the camera that sees this eye (where it is and how it's
        /// turned from the eye), copied by the calibration session from the system settings. Empty if
        /// unknown, as in calibration files from before 0.2: then the camera faces the eye
        /// (CameraExtrinsics.Facing).
        /// </summary>
        public CameraExtrinsics Extrinsics { get; set; }

        /// <summary>
        /// Gets or sets the lights of the system, from the center of this eye, copied by the
        /// calibration session from the system settings (EyeTrackingSystemSettings.LightsFrom).
        /// </summary>
        public LightPosition[] Lights { get; set; } = new LightPosition[0];

        /// <summary>
        /// How much the eye model and the reference move to be in the pixels of an image with the given
        /// intrinsics: by how much its principal point moved, for example when the window of the camera
        /// moved. Nothing if either intrinsics are unknown.
        /// </summary>
        /// <param name="intrinsics">Intrinsics of the image.</param>
        public SizeF ShiftTo(CameraIntrinsics intrinsics) =>
            Intrinsics.IsEmpty || intrinsics.IsEmpty ? SizeF.Empty : new SizeF(intrinsics.Cx - Intrinsics.Cx, intrinsics.Cy - Intrinsics.Cy);

        /// <summary>
        /// The eye model in the pixels of an image with the given intrinsics (see ShiftTo).
        /// </summary>
        /// <param name="intrinsics">Intrinsics of the image.</param>
        public EyePhysicalModel EyeModelFor(CameraIntrinsics intrinsics)
        {
            var model = EyePhysicalModel;
            if (model.IsEmpty) return model;

            var shift = ShiftTo(intrinsics);
            model.Center = new PointF(model.Center.X + shift.Width, model.Center.Y + shift.Height);
            return model;
        }

        /// <summary>
        /// The pupil of the reference in the pixels of an image with the given intrinsics (see ShiftTo).
        /// </summary>
        /// <param name="intrinsics">Intrinsics of the image.</param>
        public PupilData ReferencePupilFor(CameraIntrinsics intrinsics)
        {
            var pupil = ReferenceData.Pupil;
            if (pupil.IsEmpty) return pupil;

            var shift = ShiftTo(intrinsics);
            return new PupilData(new PointF(pupil.Center.X + shift.Width, pupil.Center.Y + shift.Height), pupil.Size, pupil.Angle);
        }

        /// <summary>
        /// Moves the eye model to the pixels of an image with the given intrinsics, and keeps those
        /// intrinsics. The reference is not moved. Nothing if the intrinsics are unknown.
        /// </summary>
        /// <param name="intrinsics">Intrinsics of the image.</param>
        public void MoveEyeModelTo(CameraIntrinsics intrinsics)
        {
            if (intrinsics.IsEmpty) return;

            EyePhysicalModel = EyeModelFor(intrinsics);
            Intrinsics = intrinsics;
        }

        /// <summary>
        /// What the calibration found beyond the eye model and the reference, which changes the
        /// calibrated data as that calibration needs (for example the map of N-point). Null for
        /// calibrations that only use the eye model and the reference.
        /// </summary>
        public EyeCalibrationData? CalibrationData { get; set; }

        /// <summary> 
        /// Gets the reference image for the torsion.
        /// </summary>
        [XmlIgnore]
        public Image<Gray, byte>? ImageTorsionReference { get; set; }

        /// <summary>
        /// Torsion image in byte format to work with serialization: a BMP file, as calibration files
        /// have always had.
        /// </summary>
        // http://stackoverflow.com/questions/1907077/serialize-a-bitmap-in-c-net-to-xml
        [XmlElement("ImageTorsionReference")]
        public byte[]? ImageTorsionReferenceSerialized
        {
            get
            {
                // serialize
                if (ImageTorsionReference is null) return null;

                using var buffer = new VectorOfByte();
                CvInvoke.Imencode(".bmp", ImageTorsionReference, buffer);
                return buffer.ToArray();
            }
            set
            {
                ImageTorsionReference = null;

                // deserialize
                if (value != null)
                {
                    using var mat = new Mat();
                    CvInvoke.Imdecode(value, ImreadModes.Grayscale, mat);
                    ImageTorsionReference = mat.ToImage<Gray, byte>();
                }
            }
        }

        /// <summary>
        /// Gets the image of the eye at the reference, as the tracking pipeline received it, saved with
        /// the calibration so the reference can be seen or processed again later. Null in calibration
        /// files from before 0.2.
        /// </summary>
        [XmlIgnore]
        public Image<Gray, byte>? ReferenceImage { get; set; }

        /// <summary>
        /// The reference image for serialization: a PNG file (lossless).
        /// </summary>
        [XmlElement("ReferenceImage")]
        public byte[]? ReferenceImageSerialized
        {
            get
            {
                if (ReferenceImage is null) return null;

                using var buffer = new VectorOfByte();
                CvInvoke.Imencode(".png", ReferenceImage, buffer);
                return buffer.ToArray();
            }
            set
            {
                ReferenceImage = null;
                if (value != null)
                {
                    using var mat = new Mat();
                    CvInvoke.Imdecode(value, ImreadModes.Grayscale, mat);
                    ReferenceImage = mat.ToImage<Gray, byte>();
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

            // A copy: the image may be the camera's buffer, reused for later frames. The whole image,
            // whatever its ROI.
            ReferenceImage = null;
            if (processedImageEye?.Image is Image<Gray, byte> image)
            {
                var copy = image.Clone();
                copy.ROI = System.Drawing.Rectangle.Empty;
                ReferenceImage = copy;
            }
        }

        /// <summary>
        /// Makes a copy of the EyeCalibration data.
        /// </summary>
        /// <returns></returns>
        public EyeCalibration Copy()
        {
            return new EyeCalibration()
            {
                WhichEye = WhichEye,
                ImageSize = ImageSize,
                Intrinsics = Intrinsics,
                Extrinsics = Extrinsics,
                Lights = (LightPosition[])Lights.Clone(),
                EyePhysicalModel = EyePhysicalModel,
                ImageTorsionReference = ImageTorsionReference,
                ReferenceImage = ReferenceImage,
                ReferenceData = ReferenceData.Copy(),
                CalibrationData = CalibrationData,
            };
        }
    }
}
