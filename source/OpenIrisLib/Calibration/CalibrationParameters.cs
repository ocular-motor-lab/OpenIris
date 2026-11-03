//-----------------------------------------------------------------------
// <copyright file="CalibrationParameters.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Class containing all the info regarding the calibration of one eye.
    /// </summary>
    [Serializable]
    public class CalibrationParameters
    {
        /// <summary>
        /// Default calibration parameters.
        /// </summary>
        public static CalibrationParameters Default => new();

        /// <summary>
        /// Initializes a new instance of the CalibrationParameters class;
        /// </summary>
        public CalibrationParameters()
        {
            EyeCalibrationParameters = new(new EyeCalibration(Eye.Left),new EyeCalibration(Eye.Right));
            HeadCalibrationParameters = new();
        }

        /// <summary>
        /// Calibration parameters for Eye data.
        /// </summary>
        [XmlIgnore]
        public EyeCollection<EyeCalibration> EyeCalibrationParameters { get; set; }

        /// <summary>
        /// The eye calibrations as saved in the file (same XML as EyeCalibrationParameters). An array,
        /// because when reading the file .NET adds the eyes to an existing collection (the two the
        /// constructor creates) but replaces an array.
        /// </summary>
        [XmlArray(nameof(EyeCalibrationParameters))]
        [XmlArrayItem(nameof(EyeCalibration))]
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public EyeCalibration[] EyeCalibrationParametersForXml
        {
            get => System.Linq.Enumerable.ToArray(EyeCalibrationParameters);
            set => EyeCalibrationParameters = new EyeCollection<EyeCalibration>(value);
        }

        /// <summary>
        /// Gets calibration info for the head.
        /// </summary>
        public HeadCalibration? HeadCalibrationParameters { get; set; }

        /// <summary>
        /// Settings that affect the calibration. They will be saved and load with the calibration.
        /// For instant the pupil thresholds.
        /// </summary>
        public EyeTrackingPipelineSettings? TrackingSettings { get; set; }

        /// <summary>
        /// The calibration's own parameters for the whole calibration (not one eye), of the kind the
        /// calibration method defines; null if it has none. The parameters of each eye are in
        /// <see cref="EyeCalibration.CalibrationData"/>.
        /// </summary>
        public CalibrationModelParameters? CalibrationModelParameters { get; set; }

        /// <summary>
        /// Loads the parameters from a file.
        /// </summary>
        /// <param name="fileName">Path to the file.</param>
        /// <returns>CalibrationParameters object, null if there is a problem loading.</returns>
        public static CalibrationParameters Load(string fileName)
        {
            try
            {
                var reader = new XmlSerializer(typeof(CalibrationParameters), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());
                using var file = new StreamReader(fileName);
                using var xmlreader = XmlReader.Create(file);
                return (CalibrationParameters)reader.Deserialize(xmlreader);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.WriteLine("Error loading calibration. " + ex);
                throw;
            }
            catch (InvalidOperationException ex) when (ex.InnerException is System.Xml.XmlException)
            {
                System.Diagnostics.Trace.WriteLine("Error loading calibration. It is probably from an old version and cannot be loaded. " + ex);
                throw new InvalidOperationException("Error loading calibration. It is probably from an old version and cannot be loaded. " + ex.Message, ex.InnerException);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Trace.WriteLine("Error loading calibration. " + ex);
                throw;
            }
        }

        /// <summary>
        /// Saves the parameters of the calibration to a default file with a timestamp.
        /// </summary>
        public void Save(string folder, string sessionName)
        {
            Save($"{ folder }\\{ sessionName }-{DateTime.Now.ToString("yyyyMMMdd-HHmmss")}.cal");
        }

        /// <summary>
        /// Saves the parameters to a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void Save(string fileName)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    System.Diagnostics.Trace.WriteLine("Calibration could not be saved. The folder does not exist.");
                    return;
                }

                var writer = new XmlSerializer(typeof(CalibrationParameters), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());
                using var file = new StreamWriter(fileName);

                writer.Serialize(file, this);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving the calibration file.", ex);
            }
        }

        /// <summary>
        /// Copies the calibration parameters.
        /// </summary>
        /// <returns></returns>
        public CalibrationParameters Copy()
        {
            return new CalibrationParameters()
            {
                EyeCalibrationParameters = new EyeCollection<EyeCalibration>(
                    EyeCalibrationParameters[Eye.Left].Copy(),
                    EyeCalibrationParameters[Eye.Right].Copy()),
                HeadCalibrationParameters = HeadCalibrationParameters?.Copy(),
                TrackingSettings = TrackingSettings,
                CalibrationModelParameters = CalibrationModelParameters,
            };
        }

        /// <summary>
        /// Gets the calibrated data from the raw data. The angles are the Fick angles of the rotation of
        /// the eye from the reference (EyeRotationMath): Listing's law with Listing's plane perpendicular
        /// to the line of sight at the reference, plus the torsion when it is known.
        /// </summary>
        /// <param name="eyeData">Raw data.</param>
        /// <param name="mmPerPix">Size of a pixel at the eye, in mm, for the pupil area. NaN if unknown.</param>
        /// <returns>Calibrated data.</returns>
        public CalibratedEyeData GetCalibratedEyeData(EyeData? eyeData, double mmPerPix = double.NaN)
        {
            var data = new CalibratedEyeData
            {
                HorizontalPosition = double.NaN,
                VerticalPosition = double.NaN,
                TorsionalPosition = double.NaN
            };

            if (eyeData is null) return data;

            var calibrationInfo = EyeCalibrationParameters[eyeData.WhichEye];

            if (eyeData.ProcessFrameResult == ProcessFrameResult.Good)
            {
                // In the pixels of the calibration's images, if the window of the camera moved since
                // (#61). A copy only then.
                var shift = calibrationInfo.ShiftTo(eyeData.Intrinsics);
                if (!shift.IsEmpty) eyeData = eyeData.Translated(new System.Drawing.SizeF(-shift.Width, -shift.Height));

                var eyeModel = EyeCalibrationParameters[eyeData.WhichEye].EyePhysicalModel;
                if (!calibrationInfo.HasEyeModel)
                {
                    eyeModel = new EyePhysicalModel(eyeData.Pupil.Center, (float)(eyeData.Iris.Radius * 2));
                }

                var referenceX = calibrationInfo.ReferenceData.Pupil.Center.X;
                var referenceY = calibrationInfo.ReferenceData.Pupil.Center.Y;
                if (!calibrationInfo.HasReference)
                {
                    referenceX = EyeCalibrationParameters[eyeData.WhichEye].ImageSize.Width / 2;
                    referenceY = EyeCalibrationParameters[eyeData.WhichEye].ImageSize.Height / 2;
                }

                // The rotation of the eye from the reference, and its Fick angles. Torsion is NaN
                // when it isn't measured (the rotation then follows Listing's law).
                var rotation = EyeRotationMath.FromLinesOfSight(
                    EyeRotationMath.LineOfSight(new System.Drawing.PointF(referenceX, referenceY), eyeModel, eyeData.WhichEye),
                    EyeRotationMath.LineOfSight(eyeData.Pupil.Center, eyeModel, eyeData.WhichEye),
                    eyeData.TorsionAngle);
                var (horizontal, vertical, torsion) = EyeRotationMath.FickAngles(rotation);
                data.HorizontalPosition = horizontal;
                data.VerticalPosition = vertical;
                data.TorsionalPosition = double.IsNaN(eyeData.TorsionAngle) ? double.NaN : torsion;

                // Width and height are the full axes of the ellipse, so its area is π·w·h/4.
                data.PupilArea = Math.PI / 4 * eyeData.Pupil.Size.Width * eyeData.Pupil.Size.Height * mmPerPix * mmPerPix;

                if (calibrationInfo.HasReference)
                {
                    data.DataQuality = eyeData.DataQuality / calibrationInfo.ReferenceData.DataQuality * 100;

                    if (calibrationInfo.ReferenceData.Eyelids?.Upper != null && calibrationInfo.ReferenceData.Eyelids?.Lower != null)
                    {
                        if (eyeData.Eyelids != null)
                        {
                            data.PercentOpening =
                                (double)((eyeData.Eyelids.Lower[0].Y + eyeData.Eyelids.Lower[1].Y + eyeData.Eyelids.Lower[2].Y + eyeData.Eyelids.Lower[3].Y) -
                                (eyeData.Eyelids.Upper[0].Y + eyeData.Eyelids.Upper[1].Y + eyeData.Eyelids.Upper[2].Y + eyeData.Eyelids.Upper[3].Y))
                                /
                                (double)((calibrationInfo.ReferenceData.Eyelids.Lower[0].Y + calibrationInfo.ReferenceData.Eyelids.Lower[1].Y + calibrationInfo.ReferenceData.Eyelids.Lower[2].Y + calibrationInfo.ReferenceData.Eyelids.Lower[3].Y) -
                                (calibrationInfo.ReferenceData.Eyelids.Upper[0].Y + calibrationInfo.ReferenceData.Eyelids.Upper[1].Y + calibrationInfo.ReferenceData.Eyelids.Upper[2].Y + calibrationInfo.ReferenceData.Eyelids.Upper[3].Y))
                                * 100.0;
                        }

                        if (data.PercentOpening > 120)
                        {
                            data.PercentOpening = 120;
                        }
                    }
                }
                else
                {
                    data.DataQuality = double.NaN;
                }

                // What the calibration found beyond the eye model (for example the map of Remote
                // Linear) changes the data as that calibration needs.
                if (calibrationInfo.CalibrationData is EyeCalibrationData calibrationData)
                {
                    data = calibrationData.Adjust(eyeData, data);
                }
            }

            return data;
        }

        /// <summary>
        /// Gets the calibrated eye data for both eyes.
        /// </summary>
        /// <param name="eyeDataRaw">Eye data raw.</param>
        /// <param name="mmPerPix">Size of a pixel at the eye, in mm, for the pupil area. NaN if unknown.</param>
        /// <returns></returns>
        public EyeCollection<CalibratedEyeData> GetCalibratedEyeData(EyeCollection<EyeData?> eyeDataRaw, double mmPerPix = double.NaN)
        {
            if (eyeDataRaw is null) throw new ArgumentNullException(nameof(eyeDataRaw));

            return new EyeCollection<CalibratedEyeData>(
                GetCalibratedEyeData(eyeDataRaw[Eye.Left], mmPerPix),
                GetCalibratedEyeData(eyeDataRaw[Eye.Right], mmPerPix));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="calibratedEyeData"></param>
        /// <returns></returns>
        public double[]? GetRotationVector(CalibratedEyeData calibratedEyeData)
        {
            return null;
        }

        /// <summary>
        /// Gets the calibrated data from the raw data.
        /// </summary>
        /// <param name="headData">Head data.</param>
        /// <returns>Calibrated data.</returns>
        public CalibratedHeadData GetCalibratedHeadData(HeadData? headData)
        {
            // Without a head sensor, all 0 (Atan2 of zeros would give ±180°).
            if (headData is null || headData.IsEmpty) return new CalibratedHeadData();

            // Not yet checked against the convention of CalibratedHeadData (see #70). Written for
            // the Micromedical head sensor: Yaw can't really come from the accelerometer. The
            // gyroscope and accelerometer are in the data file, raw.

            return new CalibratedHeadData
            {
                Roll = Math.Atan2(-headData.AccelerometerZ, -headData.AccelerometerX) * 180.0 / Math.PI,
                Pitch = Math.Atan2(headData.AccelerometerY, -headData.AccelerometerX) * 180.0 / Math.PI,
                Yaw = Math.Atan2(-headData.AccelerometerZ, headData.AccelerometerY) * 180.0 / Math.PI,
            };
        }


        //TryGetFixationPoint Gets the point represents the convergence of the line of site for both eyes.
        //TryGetLeftEyeOpenAmount Gets a value that represents the how far the left eye is open.
        //TryGetLeftEyePosition Gets the Vector3 that describes the position of the left eye.
        //TryGetLeftEyeRotation Gets the Quaternion that describes the rotation of the left eye.
        //TryGetRightEyeOpenAmount Gets a value that represents the how far the right eye is open.
        //TryGetRightEyePosition Gets the Vector3 that describes the position of the right eye.
        //TryGetRightEyeRotation Gets the Quaternion that describes the rotation of the right eye.
    }
}
