//-----------------------------------------------------------------------
// <copyright file="EyelidData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------    
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.Text;

    /// <summary>
    /// Class containing all the data related to one frame. No images.
    /// </summary>
    public class EyeTrackerData
    {
        /// <summary>
        /// Initializes an instance of the class EyeTrackerData.
        /// </summary>
        public EyeTrackerData()
        {
            HeadDataRaw = new HeadData();
            TimeProcessed = EyeTrackerDebug.TimeElapsed.TotalSeconds;
        }

        /// <summary>
        /// Frame number of the data.
        /// </summary>
        public long FrameNumber
        {
            get
            {
                if (EyeDataRaw is null) return 0;

                foreach (var data in EyeDataRaw)
                {
                    if (data != null)
                    {
                        return (long)data.Timestamp.FrameNumber;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Frame rate the data was recorded at.
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// Gets or sets the raw data from the right eye.
        /// </summary>
        public EyeCollection<EyeData?>? EyeDataRaw { get; set; }

        /// <summary>
        /// GEts or sets the calibrated data for the left eye.
        /// </summary>
        public EyeCollection<CalibratedEyeData>? EyeDataCalibrated { get; set; }

        /// <summary>
        /// Gets or sets the raw data from the head sensor.
        /// </summary>
        public HeadData? HeadDataRaw { get; set; }

        /// <summary>
        /// Gets or sets the calibrated head data.
        /// </summary>
        public CalibratedHeadData? HeadDataCalibrated { get; set; }

        /// <summary>
        /// Extra generic data for different systems to use as they please.
        /// </summary>
        public ExtraData ExtraData { get; set; }

        /// <summary>
        /// Time in seconds since the start of the eye tracker when the frame was processed.
        /// </summary>
        public double TimeProcessed { get; set; }

        /// <summary>
        /// Gets a string line that can be used to save the data to a text file.
        /// </summary>
        /// <returns>The string with all the formated data.</returns>
        public string GetStringLine()
        {
            var eyelidsLeft = EyeDataRaw?[Eye.Left]?.Eyelids;
            var eyelidsRight = EyeDataRaw?[Eye.Right]?.Eyelids;

            var leftUpperEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Upper[0].Y + eyelidsLeft.Upper[1].Y + eyelidsLeft.Upper[2].Y + eyelidsLeft.Upper[3].Y;
            var leftLowerEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Lower[0].Y + eyelidsLeft.Lower[1].Y + eyelidsLeft.Lower[2].Y + eyelidsLeft.Lower[3].Y;
            var rightUpperEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Upper[0].Y + eyelidsRight.Upper[1].Y + eyelidsRight.Upper[2].Y + eyelidsRight.Upper[3].Y;
            var rightLowerEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Lower[0].Y + eyelidsRight.Lower[1].Y + eyelidsRight.Lower[2].Y + eyelidsRight.Lower[3].Y;

            var lcr = EyeDataRaw?[Eye.Left]?.CornealReflections;
            var rcr = EyeDataRaw?[Eye.Right]?.CornealReflections;

            var dataLeft = EyeDataRaw?[Eye.Left] ?? new EyeData();
            var dataRight = EyeDataRaw?[Eye.Right] ?? new EyeData();

            var headData = HeadDataRaw ?? new HeadData();

            var extraData = ExtraData;

            var notTracked = new CalibratedEyeData { HorizontalPosition = double.NaN, VerticalPosition = double.NaN, TorsionalPosition = double.NaN };
            var calibratedLeft = EyeDataCalibrated?[Eye.Left] ?? notTracked;
            var calibratedRight = EyeDataCalibrated?[Eye.Right] ?? notTracked;
            var calibratedHead = HeadDataCalibrated ?? new CalibratedHeadData();

            // One StringBuilder for the whole line. Each value is formatted from its own type (float,
            // double, integer), as always: the text of the columns must not change.
            var line = new StringBuilder(1024);

            Add(line, dataLeft.Timestamp.FrameNumber);
            Add(line, dataLeft.Timestamp.FrameNumberRaw);
            Add(line, dataLeft.Timestamp.Seconds);
            Add(line, dataLeft.Pupil.Center.X);
            Add(line, dataLeft.Pupil.Center.Y);
            Add(line, dataLeft.Pupil.Size.Width);
            Add(line, dataLeft.Pupil.Size.Height);
            Add(line, dataLeft.Pupil.Angle);
            Add(line, dataLeft.Iris.Radius);
            Add(line, dataLeft.TorsionAngle);
            Add(line, leftUpperEyelid);
            Add(line, leftLowerEyelid);
            Add(line, dataLeft.DataQuality);
            for (int i = 0; i < 5; i++)
            {
                Add(line, CornealReflectionCenter(lcr, i).X);
                Add(line, CornealReflectionCenter(lcr, i).Y);
            }

            Add(line, dataRight.Timestamp.FrameNumber);
            Add(line, dataRight.Timestamp.FrameNumberRaw);
            Add(line, dataRight.Timestamp.Seconds);
            Add(line, dataRight.Pupil.Center.X);
            Add(line, dataRight.Pupil.Center.Y);
            Add(line, dataRight.Pupil.Size.Width);
            Add(line, dataRight.Pupil.Size.Height);
            Add(line, dataRight.Pupil.Angle);
            Add(line, dataRight.Iris.Radius);
            Add(line, dataRight.TorsionAngle);
            Add(line, rightUpperEyelid);
            Add(line, rightLowerEyelid);
            Add(line, dataRight.DataQuality);
            for (int i = 0; i < 5; i++)
            {
                Add(line, CornealReflectionCenter(rcr, i).X);
                Add(line, CornealReflectionCenter(rcr, i).Y);
            }

            Add(line, headData.AccelerometerX);
            Add(line, headData.AccelerometerY);
            Add(line, headData.AccelerometerZ);
            Add(line, headData.GyroX);
            Add(line, headData.GyroY);
            Add(line, headData.GyroZ);
            Add(line, headData.MagnetometerX);
            Add(line, headData.MagnetometerY);
            Add(line, headData.MagnetometerZ);

            Add(line, extraData.Int0);
            Add(line, extraData.Int1);
            Add(line, extraData.Int2);
            Add(line, extraData.Int3);
            Add(line, extraData.Int4);
            Add(line, extraData.Int5);
            Add(line, extraData.Int6);
            Add(line, extraData.Int7);

            Add(line, extraData.Double0);
            Add(line, extraData.Double1);
            Add(line, extraData.Double2);
            Add(line, extraData.Double3);
            Add(line, extraData.Double4);
            Add(line, extraData.Double5);
            Add(line, extraData.Double6);
            Add(line, extraData.Double7);

            Add(line, dataLeft.Timestamp.TimeGrabbed);
            Add(line, dataRight.Timestamp.TimeGrabbed);
            Add(line, TimeProcessed);

            // Columns added in 0.2, after the original ones so files and readers of older versions
            // keep their column order.
            Add(line, calibratedLeft.HorizontalPosition);
            Add(line, calibratedLeft.VerticalPosition);
            Add(line, calibratedLeft.TorsionalPosition);
            Add(line, calibratedLeft.PupilArea);
            Add(line, calibratedLeft.PercentOpening);
            Add(line, calibratedRight.HorizontalPosition);
            Add(line, calibratedRight.VerticalPosition);
            Add(line, calibratedRight.TorsionalPosition);
            Add(line, calibratedRight.PupilArea);
            Add(line, calibratedRight.PercentOpening);

            Add(line, calibratedHead.Roll);
            Add(line, calibratedHead.Pitch);
            Add(line, calibratedHead.Yaw);
            Add(line, calibratedHead.RollVelocity);
            Add(line, calibratedHead.PitchVelocity);
            Add(line, calibratedHead.YawVelocity);
            Add(line, calibratedHead.XAcceleration);
            Add(line, calibratedHead.YAcceleration);
            Add(line, calibratedHead.ZAcceleration);
            Add(line, calibratedHead.X);
            Add(line, calibratedHead.Y);
            Add(line, calibratedHead.Z);

            // ROI of each eye image in the camera image, in pixels (X, Y, width, height). Placeholders:
            // no system passes its ROI along with the images yet.
            for (int i = 0; i < 8; i++)
            {
                Add(line, 0.0);
            }

            return line.ToString();
        }

        // Write a value of the line, separated by a space from the previous one.
        private static void Add(StringBuilder line, double value) => Separate(line).Append(value.ToString("0.0000"));

        private static void Add(StringBuilder line, float value) => Separate(line).Append(value.ToString("0.0000"));

        private static void Add(StringBuilder line, ulong value) => Separate(line).Append(value.ToString());

        private static void Add(StringBuilder line, int value) => Separate(line).Append(value.ToString());

        private static StringBuilder Separate(StringBuilder line) => line.Length == 0 ? line : line.Append(' ');

        /// <summary>
        /// Center of a corneal reflection, or 0, 0 if there are fewer.
        /// </summary>
        private static PointF CornealReflectionCenter(CornealReflectionData[]? reflections, int index)
            => reflections != null && index < reflections.Length ? reflections[index].Center : PointF.Empty;

        /// <summary>
        /// Gets the data of this frame as an array of doubles, in the same column order as
        /// <see cref="GetStringLine"/> and <see cref="GetStringHeader"/>. Used to stream the
        /// data through channels (e.g. LSL) keeping the same layout as the recorded data files.
        /// </summary>
        /// <returns>The array of values, one per column of the data file.</returns>
        public double[] GetDataArray()
        {
            var eyelidsLeft = EyeDataRaw?[Eye.Left]?.Eyelids;
            var eyelidsRight = EyeDataRaw?[Eye.Right]?.Eyelids;

            var leftUpperEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Upper[0].Y + eyelidsLeft.Upper[1].Y + eyelidsLeft.Upper[2].Y + eyelidsLeft.Upper[3].Y;
            var leftLowerEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Lower[0].Y + eyelidsLeft.Lower[1].Y + eyelidsLeft.Lower[2].Y + eyelidsLeft.Lower[3].Y;
            var rightUpperEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Upper[0].Y + eyelidsRight.Upper[1].Y + eyelidsRight.Upper[2].Y + eyelidsRight.Upper[3].Y;
            var rightLowerEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Lower[0].Y + eyelidsRight.Lower[1].Y + eyelidsRight.Lower[2].Y + eyelidsRight.Lower[3].Y;

            var lcr = EyeDataRaw?[Eye.Left]?.CornealReflections;
            var rcr = EyeDataRaw?[Eye.Right]?.CornealReflections;

            var dataLeft = EyeDataRaw?[Eye.Left] ?? new EyeData();
            var dataRight = EyeDataRaw?[Eye.Right] ?? new EyeData();

            var headData = HeadDataRaw ?? new HeadData();

            var extraData = ExtraData;

            var notTracked = new CalibratedEyeData { HorizontalPosition = double.NaN, VerticalPosition = double.NaN, TorsionalPosition = double.NaN };
            var calibratedLeft = EyeDataCalibrated?[Eye.Left] ?? notTracked;
            var calibratedRight = EyeDataCalibrated?[Eye.Right] ?? notTracked;
            var calibratedHead = HeadDataCalibrated ?? new CalibratedHeadData();

            var values = new double[ColumnCount];
            int n = 0;

            values[n++] = dataLeft.Timestamp.FrameNumber;
            values[n++] = dataLeft.Timestamp.FrameNumberRaw;
            values[n++] = dataLeft.Timestamp.Seconds;
            values[n++] = dataLeft.Pupil.Center.X;
            values[n++] = dataLeft.Pupil.Center.Y;
            values[n++] = dataLeft.Pupil.Size.Width;
            values[n++] = dataLeft.Pupil.Size.Height;
            values[n++] = dataLeft.Pupil.Angle;
            values[n++] = dataLeft.Iris.Radius;
            values[n++] = dataLeft.TorsionAngle;
            values[n++] = leftUpperEyelid;
            values[n++] = leftLowerEyelid;
            values[n++] = dataLeft.DataQuality;
            for (int i = 0; i < 5; i++)
            {
                values[n++] = CornealReflectionCenter(lcr, i).X;
                values[n++] = CornealReflectionCenter(lcr, i).Y;
            }

            values[n++] = dataRight.Timestamp.FrameNumber;
            values[n++] = dataRight.Timestamp.FrameNumberRaw;
            values[n++] = dataRight.Timestamp.Seconds;
            values[n++] = dataRight.Pupil.Center.X;
            values[n++] = dataRight.Pupil.Center.Y;
            values[n++] = dataRight.Pupil.Size.Width;
            values[n++] = dataRight.Pupil.Size.Height;
            values[n++] = dataRight.Pupil.Angle;
            values[n++] = dataRight.Iris.Radius;
            values[n++] = dataRight.TorsionAngle;
            values[n++] = rightUpperEyelid;
            values[n++] = rightLowerEyelid;
            values[n++] = dataRight.DataQuality;
            for (int i = 0; i < 5; i++)
            {
                values[n++] = CornealReflectionCenter(rcr, i).X;
                values[n++] = CornealReflectionCenter(rcr, i).Y;
            }

            values[n++] = headData.AccelerometerX;
            values[n++] = headData.AccelerometerY;
            values[n++] = headData.AccelerometerZ;
            values[n++] = headData.GyroX;
            values[n++] = headData.GyroY;
            values[n++] = headData.GyroZ;
            values[n++] = headData.MagnetometerX;
            values[n++] = headData.MagnetometerY;
            values[n++] = headData.MagnetometerZ;

            values[n++] = extraData.Int0;
            values[n++] = extraData.Int1;
            values[n++] = extraData.Int2;
            values[n++] = extraData.Int3;
            values[n++] = extraData.Int4;
            values[n++] = extraData.Int5;
            values[n++] = extraData.Int6;
            values[n++] = extraData.Int7;

            values[n++] = extraData.Double0;
            values[n++] = extraData.Double1;
            values[n++] = extraData.Double2;
            values[n++] = extraData.Double3;
            values[n++] = extraData.Double4;
            values[n++] = extraData.Double5;
            values[n++] = extraData.Double6;
            values[n++] = extraData.Double7;

            values[n++] = dataLeft.Timestamp.TimeGrabbed;
            values[n++] = dataRight.Timestamp.TimeGrabbed;
            values[n++] = TimeProcessed;

            values[n++] = calibratedLeft.HorizontalPosition;
            values[n++] = calibratedLeft.VerticalPosition;
            values[n++] = calibratedLeft.TorsionalPosition;
            values[n++] = calibratedLeft.PupilArea;
            values[n++] = calibratedLeft.PercentOpening;
            values[n++] = calibratedRight.HorizontalPosition;
            values[n++] = calibratedRight.VerticalPosition;
            values[n++] = calibratedRight.TorsionalPosition;
            values[n++] = calibratedRight.PupilArea;
            values[n++] = calibratedRight.PercentOpening;

            values[n++] = calibratedHead.Roll;
            values[n++] = calibratedHead.Pitch;
            values[n++] = calibratedHead.Yaw;
            values[n++] = calibratedHead.RollVelocity;
            values[n++] = calibratedHead.PitchVelocity;
            values[n++] = calibratedHead.YawVelocity;
            values[n++] = calibratedHead.XAcceleration;
            values[n++] = calibratedHead.YAcceleration;
            values[n++] = calibratedHead.ZAcceleration;
            values[n++] = calibratedHead.X;
            values[n++] = calibratedHead.Y;
            values[n++] = calibratedHead.Z;

            // The 8 ROI values stay 0 (see GetStringLine).
            return values;
        }

        /// <summary>
        /// Number of columns of the data file.
        /// </summary>
        private const int ColumnCount = 104;

        /// <summary>
        /// Gets the header for the data file.
        /// </summary>
        /// <returns></returns>
        public static string GetStringHeader()
        {
            return
                  "LeftFrameNumber " +
                  "LeftFrameNumberRaw " +
                  "LeftSeconds " +
                  "LeftPupilX " +
                  "LeftPupilY " +
                  "LeftPupilWidth " +
                  "LeftPupilHeight " +
                  "LeftPupilAngle " +
                  "LeftIrisRadius " +
                  "LeftTorsion " +
                  "LeftUpperEyelid " +
                  "LeftLowerEyelid " +
                  "LeftDataQuality " +
                  "LeftCR1X " +
                  "LeftCR1Y " +
                  "LeftCR2X " +
                  "LeftCR2Y " +
                  "LeftCR3X " +
                  "LeftCR3Y " +
                  "LeftCR4X " +
                  "LeftCR4Y " +
                  "LeftCR5X " +
                  "LeftCR5Y " +

                  "RightFrameNumber " +
                  "RightFrameNumberRaw " +
                  "RightSeconds " +
                  "RightPupilX " +
                  "RightPupilY " +
                  "RightPupilWidth " +
                  "RightPupilHeight " +
                  "RightPupilAngle " +
                  "RightIrisRadius " +
                  "RightTorsion " +
                  "RightUpperEyelid " +
                  "RightLowerEyelid " +
                  "RightDataQuality " +
                  "RightCR1X " +
                  "RightCR1Y " +
                  "RightCR2X " +
                  "RightCR2Y " +
                  "RightCR3X " +
                  "RightCR3Y " +
                  "RightCR4X " +
                  "RightCR4Y " +
                  "RightCR5X " +
                  "RightCR5Y " +

                  "AccelerometerX " +
                  "AccelerometerY " +
                  "AccelerometerZ " +
                  "GyroX " +
                  "GyroY " +
                  "GyroZ " +
                  "MagnetometerX " +
                  "MagnetometerY " +
                  "MagnetometerZ " +

                  "Int0 " +
                  "Int1 " +
                  "Int2 " +
                  "Int3 " +
                  "Int4 " +
                  "Int5 " +
                  "Int6 " +
                  "Int7 " +

                  "Double0 " +
                  "Double1 " +
                  "Double2 " +
                  "Double3 " +
                  "Double4 " +
                  "Double5 " +
                  "Double6 " +
                  "Double7 " +

                  "DebugTimeGrabbedLeft " +
                  "DebugTimeGrabbedRight " +
                  "DebugTimeProcessed " +

                  "LeftH " +
                  "LeftV " +
                  "LeftT " +
                  "LeftPupilArea " +
                  "LeftEyeOpening " +
                  "RightH " +
                  "RightV " +
                  "RightT " +
                  "RightPupilArea " +
                  "RightEyeOpening " +

                  "HeadRoll " +
                  "HeadPitch " +
                  "HeadYaw " +
                  "HeadRollVelocity " +
                  "HeadPitchVelocity " +
                  "HeadYawVelocity " +
                  "HeadAccelerationX " +
                  "HeadAccelerationY " +
                  "HeadAccelerationZ " +
                  "HeadX " +
                  "HeadY " +
                  "HeadZ " +

                  "LeftRoiX " +
                  "LeftRoiY " +
                  "LeftRoiWidth " +
                  "LeftRoiHeight " +
                  "RightRoiX " +
                  "RightRoiY " +
                  "RightRoiWidth " +
                  "RightRoiHeight";
        }
    }

    /// <summary>
    /// Structure for the generic extra data.
    /// </summary>
    public struct ExtraData
    {
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int0 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int1 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int2 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int3 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int4 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int5 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int6 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int7 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int8 { get; set; }
        /// <summary>
        /// Gets some integer data.
        /// </summary>
        public int Int9 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double0 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double1 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double2 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double3 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double4 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double5 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double6 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double7 { get; set; }

        /// <summary>
        /// Gets some double data.
        /// </summary>
        public double Double8 { get; set; }
    }

    /// <summary>
    /// Class containing all the data related to one frame and the images of the frame.
    /// </summary>
    public class EyeTrackerImagesAndData
    {
        /// <summary>
        /// Initializes an instance of the class EyeTrackerDataAndImages.
        /// </summary>
        /// <param name="images">Images for the current frame.</param>
        /// <param name="calibration">Calibration used to processed this images.</param>
        /// <param name="pipelineName">Name of the processing pipeline to use.</param>
        /// <param name="trackingSettings">Tracking settings used to process this image.</param>
        public EyeTrackerImagesAndData(EyeCollection<ImageEye?> images, CalibrationParameters calibration, string pipelineName, EyeTrackingPipelineSettings trackingSettings)
        {
            Images = images;
            Calibration = calibration;
            EyeTrackingPipelineName = pipelineName;
            TrackingSettings = trackingSettings;

            FrameNumber = Images.GetFrameNumber();
        }

        /// <summary>
        /// Gets the Images for the current frame.
        /// </summary>
        public EyeCollection<ImageEye?> Images { get; private set; }

        /// <summary>
        /// Gets the data for the current frame.
        /// </summary>
        public EyeTrackerData? Data { get; set; }

        /// <summary>
        /// Gets the Frame number for the current frame.
        /// </summary>
        public long FrameNumber { get; private set; }

        /// <summary>
        /// </summary>
        public string EyeTrackingPipelineName { get; private set; }

        /// <summary>
        /// Gets the calibration parameters at the time the images were processed.
        /// </summary>
        public CalibrationParameters Calibration { get; set; }

        /// <summary>
        /// Gets the tracking settings at the time the images were processed.
        /// </summary>
        public EyeTrackingPipelineSettings TrackingSettings { get; set; }

        /// <summary>
        /// Update the images.
        /// </summary>
        public void UpdateImages(EyeCollection<ImageEye?> images)
        {
            Images = images;
        }
    }
}
