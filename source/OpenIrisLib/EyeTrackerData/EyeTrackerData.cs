//-----------------------------------------------------------------------
// <copyright file="EyelidData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------    
namespace OpenIris
{
#nullable enable

    using System;
    using System.Linq;

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
        public HeadData HeadDataRaw { get; set; }

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
            var data = this;

            var eyelidsLeft = data?.EyeDataRaw?[Eye.Left]?.Eyelids;
            var eyelidsRight = data?.EyeDataRaw?[Eye.Right]?.Eyelids;

            var leftUpperEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Upper[0].Y + eyelidsLeft.Upper[1].Y + eyelidsLeft.Upper[2].Y + eyelidsLeft.Upper[3].Y;
            var leftLowerEyelid = (eyelidsLeft is null) ? 0 : eyelidsLeft.Lower[0].Y + eyelidsLeft.Lower[1].Y + eyelidsLeft.Lower[2].Y + eyelidsLeft.Lower[3].Y;
            var rightUpperEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Upper[0].Y + eyelidsRight.Upper[1].Y + eyelidsRight.Upper[2].Y + eyelidsRight.Upper[3].Y;
            var rightLowerEyelid = (eyelidsRight is null) ? 0 : eyelidsRight.Lower[0].Y + eyelidsRight.Lower[1].Y + eyelidsRight.Lower[2].Y + eyelidsRight.Lower[3].Y;

            var lcr = data?.EyeDataRaw?[Eye.Left]?.CornealReflections ?? new CornealReflectionData[5];
            var rcr = data?.EyeDataRaw?[Eye.Right]?.CornealReflections ?? new CornealReflectionData[5];
            lcr = lcr.Concat(new CornealReflectionData[5 - lcr.Length]).ToArray();
            rcr = rcr.Concat(new CornealReflectionData[5 - rcr.Length]).ToArray();

            var dataLeft = data?.EyeDataRaw?[Eye.Left] ?? new EyeData();
            var dataRight = data?.EyeDataRaw?[Eye.Right] ?? new EyeData();

            var headData = data?.HeadDataRaw ?? new HeadData();

            var extraData = data?.ExtraData ?? new ExtraData();

            string line =
                string.Format("{0}", dataLeft.Timestamp.FrameNumber) + " " +
                string.Format("{0}", dataLeft.Timestamp.FrameNumberRaw) + " " +
                string.Format("{0:0.0000}", dataLeft.Timestamp.Seconds) + " " +
                string.Format("{0:0.0000}", dataLeft.Pupil.Center.X) + " " +
                string.Format("{0:0.0000}", dataLeft.Pupil.Center.Y) + " " +
                string.Format("{0:0.0000}", dataLeft.Pupil.Size.Width) + " " +
                string.Format("{0:0.0000}", dataLeft.Pupil.Size.Height) + " " +
                string.Format("{0:0.0000}", dataLeft.Pupil.Angle) + " " +
                string.Format("{0:0.0000}", dataLeft.Iris.Radius) + " " +
                string.Format("{0:0.0000}", dataLeft.TorsionAngle) + " " +
                string.Format("{0:0.0000}", leftUpperEyelid) + " " +
                string.Format("{0:0.0000}", leftLowerEyelid) + " " +
                string.Format("{0:0.0000}", dataLeft.DataQuality) + " " +
                string.Format("{0:0.0000}", lcr[0].Center.X) + " " +
                string.Format("{0:0.0000}", lcr[0].Center.Y) + " " +
                string.Format("{0:0.0000}", lcr[1].Center.X) + " " +
                string.Format("{0:0.0000}", lcr[1].Center.Y) + " " +
                string.Format("{0:0.0000}", lcr[2].Center.X) + " " +
                string.Format("{0:0.0000}", lcr[2].Center.Y) + " " +
                string.Format("{0:0.0000}", lcr[3].Center.X) + " " +
                string.Format("{0:0.0000}", lcr[3].Center.Y) + " " +
                string.Format("{0:0.0000}", lcr[4].Center.X) + " " +
                string.Format("{0:0.0000}", lcr[4].Center.Y) + " " +

                string.Format("{0}", dataRight.Timestamp.FrameNumber) + " " +
                string.Format("{0}", dataRight.Timestamp.FrameNumberRaw) + " " +
                string.Format("{0:0.0000}", dataRight.Timestamp.Seconds) + " " +
                string.Format("{0:0.0000}", dataRight.Pupil.Center.X) + " " +
                string.Format("{0:0.0000}", dataRight.Pupil.Center.Y) + " " +
                string.Format("{0:0.0000}", dataRight.Pupil.Size.Width) + " " +
                string.Format("{0:0.0000}", dataRight.Pupil.Size.Height) + " " +
                string.Format("{0:0.0000}", dataRight.Pupil.Angle) + " " +
                string.Format("{0:0.0000}", dataRight.Iris.Radius) + " " +
                string.Format("{0:0.0000}", dataRight.TorsionAngle) + " " +
                string.Format("{0:0.0000}", rightUpperEyelid) + " " +
                string.Format("{0:0.0000}", rightLowerEyelid) + " " +
                string.Format("{0:0.0000}", dataRight.DataQuality) + " " +
                string.Format("{0:0.0000}", rcr[0].Center.X) + " " +
                string.Format("{0:0.0000}", rcr[0].Center.Y) + " " +
                string.Format("{0:0.0000}", rcr[1].Center.X) + " " +
                string.Format("{0:0.0000}", rcr[1].Center.Y) + " " +
                string.Format("{0:0.0000}", rcr[2].Center.X) + " " +
                string.Format("{0:0.0000}", rcr[2].Center.Y) + " " +
                string.Format("{0:0.0000}", rcr[3].Center.X) + " " +
                string.Format("{0:0.0000}", rcr[3].Center.Y) + " " +
                string.Format("{0:0.0000}", rcr[4].Center.X) + " " +
                string.Format("{0:0.0000}", rcr[4].Center.Y) + " " +

                string.Format("{0:0.0000}", headData.AccelerometerX) + " " +
                string.Format("{0:0.0000}", headData.AccelerometerY) + " " +
                string.Format("{0:0.0000}", headData.AccelerometerZ) + " " +
                string.Format("{0:0.0000}", headData.GyroX) + " " +
                string.Format("{0:0.0000}", headData.GyroY) + " " +
                string.Format("{0:0.0000}", headData.GyroZ) + " " +
                string.Format("{0:0.0000}", headData.MagnetometerX) + " " +
                string.Format("{0:0.0000}", headData.MagnetometerY) + " " +
                string.Format("{0:0.0000}", headData.MagnetometerZ) + " " +

                string.Format("{0}", extraData.Int0) + " " +
                string.Format("{0}", extraData.Int1) + " " +
                string.Format("{0}", extraData.Int2) + " " +
                string.Format("{0}", extraData.Int3) + " " +
                string.Format("{0}", extraData.Int4) + " " +
                string.Format("{0}", extraData.Int5) + " " +
                string.Format("{0}", extraData.Int6) + " " +
                string.Format("{0}", extraData.Int7) + " " +

                string.Format("{0:0.0000}", extraData.Double0) + " " +
                string.Format("{0:0.0000}", extraData.Double1) + " " +
                string.Format("{0:0.0000}", extraData.Double2) + " " +
                string.Format("{0:0.0000}", extraData.Double3) + " " +
                string.Format("{0:0.0000}", extraData.Double4) + " " +
                string.Format("{0:0.0000}", extraData.Double5) + " " +
                string.Format("{0:0.0000}", extraData.Double6) + " " +
                string.Format("{0:0.0000}", extraData.Double7) + " " +

                string.Format("{0:0.0000}", dataLeft.Timestamp.TimeGrabbed) + " " +
                string.Format("{0:0.0000}", dataRight.Timestamp.TimeGrabbed) + " " +
                string.Format("{0:0.0000}", data?.TimeProcessed ?? 0.0);

            return line;
        }

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
                  "DebugTimeProcessed";
        }
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
