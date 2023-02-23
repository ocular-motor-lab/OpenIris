//-----------------------------------------------------------------------
// <copyright file="CalibrationParameters.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
        public static CalibrationParameters Default => new CalibrationParameters();

        /// <summary>
        /// Initializes a new instance of the CalibrationParameters class;
        /// </summary>
        public CalibrationParameters()
        {
            EyeCalibrationParameters = new EyeCollection<EyeCalibration>(
                                                        new EyeCalibration(Eye.Left),
                                                        new EyeCalibration(Eye.Right));
            HeadCalibrationParameters = new HeadCalibration();
        }

        /// <summary>
        /// Calibration parameters for Eye data.
        /// </summary>
        public EyeCollection<EyeCalibration> EyeCalibrationParameters { get; set; }

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
        /// Additional parameters that can be specific for each calibration method. Make sure this is serializable.
        /// </summary>
        public object? AdditionalParameters { get; set; }

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
                var calibration = (CalibrationParameters)reader.Deserialize(xmlreader);
                if (calibration.EyeCalibrationParameters.Count == 4)
                {
                    // TODO: HORRIBLE QUICK FIX. Needs to be done because the current initalization while serializing creates two empty items. Then, when deserializing it adds two more.
                    calibration.EyeCalibrationParameters = new EyeCollection<EyeCalibration>(calibration.EyeCalibrationParameters[2], calibration.EyeCalibrationParameters[3]);
                }
                return calibration;
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
                HeadCalibrationParameters = HeadCalibrationParameters,
                TrackingSettings = TrackingSettings,
            };
        }

        /// <summary>
        /// Gets the calibrated data from the raw data.
        /// </summary>
        /// <param name="eyeData">Raw data.</param>
        /// <returns>Calibrated data.</returns>
        public CalibratedEyeData GetCalibratedEyeData(EyeData? eyeData)
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

                // Get the angle position relative to the reference
                var referenceXDeg = Math.Asin((referenceX - eyeModel.Center.X) / eyeModel.Radius) * 180 / Math.PI;
                var referenceYDeg = Math.Asin((referenceY - eyeModel.Center.Y) / eyeModel.Radius) * 180 / Math.PI;

                var xDeg = Math.Asin((eyeData.Pupil.Center.X - eyeModel.Center.X) / eyeModel.Radius) * 180 / Math.PI;
                var yDeg = Math.Asin((eyeData.Pupil.Center.Y - eyeModel.Center.Y) / eyeModel.Radius) * 180 / Math.PI;

                data.HorizontalPosition = -(xDeg - referenceXDeg);
                data.VerticalPosition = -(yDeg - referenceYDeg);

                data.TorsionalPosition = eyeData.TorsionAngle;

                if (double.IsNaN(eyeData.TorsionAngle))
                {
                    data.TorsionalPosition = double.NaN;
                }

                // Pupil area in mm2 asuming 24 mm eyeblobe
                if (!calibrationInfo.EyePhysicalModel.IsEmpty)
                {
                    data.PupilArea = (eyeData.Pupil.Size.Width / calibrationInfo.EyePhysicalModel.Radius * 12) * (eyeData.Pupil.Size.Height / calibrationInfo.EyePhysicalModel.Radius * 12) * Math.PI;
                }
                else
                {
                    data.PupilArea = (eyeData.Pupil.Size.Width / eyeData.Iris.Radius * 12) * (eyeData.Pupil.Size.Height / eyeData.Iris.Radius * 12) * Math.PI;
                }

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
            }

            return data;
        }

        /// <summary>
        /// Gets the calibrated eye data for both eyes.
        /// </summary>
        /// <param name="eyeDataRaw">Eye data raw.</param>
        /// <returns></returns>
        public EyeCollection<CalibratedEyeData> GetCalibratedEyeData(EyeCollection<EyeData?> eyeDataRaw)
        {
            if (eyeDataRaw is null) throw new ArgumentNullException(nameof(eyeDataRaw));

            return new EyeCollection<CalibratedEyeData>(
                GetCalibratedEyeData(eyeDataRaw[Eye.Left]),
                GetCalibratedEyeData(eyeDataRaw[Eye.Right]));
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
        public CalibratedHeadData GetCalibratedHeadData(HeadData headData)
        {
            var data = new CalibratedHeadData();

            if (headData != null)
            {
                data.XAcceleration = headData.AccelerometerX;
                data.YAcceleration = headData.AccelerometerY;
                data.ZAcceleration = headData.AccelerometerZ;
                data.Roll = Math.Atan2(-headData.AccelerometerZ, -headData.AccelerometerX) * 180.0 / Math.PI;
                data.Pitch = Math.Atan2(headData.AccelerometerY, -headData.AccelerometerX) * 180.0 / Math.PI;
                data.Yaw = Math.Atan2(-headData.AccelerometerZ, headData.AccelerometerY) * 180.0 / Math.PI;
                data.YawVelocity = headData.GyroX;
                data.PitchVelocity = headData.GyroY;
                data.RollVelocity = headData.GyroZ;
            }

            return data;
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
