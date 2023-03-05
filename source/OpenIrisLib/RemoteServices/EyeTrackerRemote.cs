//-----------------------------------------------------------------------
// <copyright file="EyeTrackerRemote.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.ServiceModel;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class functions as a wrapper on top of the EyeTracker class to expose only 
    /// the methods that are exposable remotely.
    /// In order to use one of the ServiceHost constructors that takes a service instance,
    /// the InstanceContextMode of the service must be set to InstanceContextMode.Single.
    /// This can be configured via the ServiceBehaviorAttribute.  Otherwise, please
    /// consider using the ServiceHost constructors that take a Type argument.
    /// https://learn.microsoft.com/en-us/dotnet/api/system.servicemodel.instancecontextmode?view=netframework-4.8.1
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class EyeTrackerRemote : IEyeTrackerService, IEyeTrackerWebService
    {
        protected EyeTracker eyeTracker;

        public EyeTrackerRemote(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
        }

        /// <summary>
        /// Summary status
        /// </summary>
        public EyeTrackerStatusSummary StatusSummary
        {
            get
            {
                if (eyeTracker != null)
                {
                    return new EyeTrackerStatusSummary
                    {
                        NotStarted = eyeTracker.NotStarted,
                        Tracking = eyeTracker.Tracking,
                        Processing = eyeTracker.PostProcessing,
                        Recording = eyeTracker.Recording,
                        Calibrating = eyeTracker.Calibrating,

                        ProcessorStatus = eyeTracker.ImageProcessor?.ProcessingStatus ?? "Not tracking",
                        GrabberStatus = eyeTracker.ImageGrabber?.GrabbingStatus ?? "Not tracking" + " " + eyeTracker.HeadTracker?.Status ?? "[No head tracking]",
                        RecorderStatus = eyeTracker.RecordingSession?.RecordingStatus ?? "Not recording",
                    };
                }
                else
                {
                    return new EyeTrackerStatusSummary();
                }
            }
        }

        /// <summary>
        /// Gets the settings of the current pipeline.
        /// </summary>
        public EyeTrackingPipelineSettings? PipelineSettings => eyeTracker?.Settings.TrackingPipelineSettings;

        /// <summary>
        /// Starts the recording.
        /// </summary>
        public void StartRecording()
        {
            _ = eyeTracker.StartRecording();
        }

        /// <summary>
        /// Stops the recording.
        /// </summary>
        public void StopRecording()
        {
            eyeTracker.StopRecording();
        }

        /// <summary>
        /// Resets the reference of the calibration.
        /// </summary>
        public void ResetReference()
        {
            _ = eyeTracker.ResetReference();
        }

        /// <summary>
        /// Changes the threshold.
        /// </summary>
        /// <param name="increase"></param>
        /// <param name="dark"></param>
        /// <param name="whichEye"></param>
        public void changeThreshold(bool increase, bool dark, Eye whichEye)
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            var change = increase ? 1 : -1;

            // TODO: this is not ideal. The remote only works for EyeTrackingPipelineJOMSettings
            var trackingSettings = eyeTracker.Settings.TrackingPipelineSettings as EyeTrackingPipelineJOMSettings;

            if (trackingSettings == null) return;

            switch ((dark, whichEye))
            {
                case (true, Eye.Left):
                    trackingSettings.DarkThresholdLeftEye = Math.Max(Math.Min(trackingSettings.DarkThresholdLeftEye + change, 255), 0);
                    break;
                case (true, Eye.Right):
                    trackingSettings.DarkThresholdRightEye = Math.Max(Math.Min(trackingSettings.DarkThresholdRightEye + change, 255), 0);
                    break;
                case (true, Eye.Both):
                    trackingSettings.DarkThresholdLeftEye = Math.Max(Math.Min(trackingSettings.DarkThresholdLeftEye + change, 255), 0);
                    trackingSettings.DarkThresholdRightEye = Math.Max(Math.Min(trackingSettings.DarkThresholdRightEye + change, 255), 0);
                    break;
                case (false, Eye.Left):
                    trackingSettings.BrightThresholdLeftEye = Math.Max(Math.Min(trackingSettings.BrightThresholdLeftEye + change, 255), 0);
                    break;
                case (false, Eye.Right):
                    trackingSettings.BrightThresholdRightEye = Math.Max(Math.Min(trackingSettings.BrightThresholdRightEye + change, 255), 0);
                    break;
                case (false, Eye.Both):
                    trackingSettings.BrightThresholdLeftEye = Math.Max(Math.Min(trackingSettings.BrightThresholdLeftEye + change, 255), 0);
                    trackingSettings.BrightThresholdRightEye = Math.Max(Math.Min(trackingSettings.BrightThresholdRightEye + change, 255), 0);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Changes a setting. It accepts the notation. Setting.subsetting
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ChangeSetting(string settingName, object value)
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                if (settingName.IndexOf('.') > 0)
                {
                    var settingParts = settingName.Split('.');
                    switch (settingParts[0])
                    {
                        case "Tracking":
                            typeof(EyeTrackingPipelineSettings).GetProperty(settingParts[1]).SetValue(eyeTracker.Settings.TrackingPipelineSettings, value, null);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    typeof(EyeTrackerSettings).GetProperty(settingName).SetValue(eyeTracker.Settings, value, null);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error changing setting remotely: " + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the current images and data.
        /// </summary>
        /// <returns></returns>
        public ImagesAndData GetCurrentImagesAndData()
        {
            var imagesAndData = new ImagesAndData();

            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            if (eyeTracker.LastImagesAndData != null)
            {
                try
                {
                    imagesAndData.RawData = new EyeCollection<EyeData?>(
                        eyeTracker.LastImagesAndData.Data?.EyeDataRaw?[Eye.Left],
                        eyeTracker.LastImagesAndData.Data?.EyeDataRaw?[Eye.Right]);

                    imagesAndData.CalibratedData = new EyeCollection<CalibratedEyeData>(
                        eyeTracker.LastImagesAndData.Data?.EyeDataCalibrated?[Eye.Left] ?? new CalibratedEyeData(),
                        eyeTracker.LastImagesAndData.Data?.EyeDataCalibrated?[Eye.Right] ?? new CalibratedEyeData()
                        );
                    imagesAndData.Image = new EyeCollection<Bitmap?>(
                        eyeTracker.LastImagesAndData.Images[Eye.Left]?.Image.Bitmap,
                        eyeTracker.LastImagesAndData.Images[Eye.Right]?.Image.Bitmap
                        );

                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error processing remote request for data: " + ex.Message);
                }
            }

            return imagesAndData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EyeCollection<EyeData?>? GetCurrentData()
        {
            return eyeTracker?.LastImagesAndData?.Data?.EyeDataRaw;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EyeCollection<EyeData?>? WaitForNewData()
        {
            var dataWait = new AutoResetEvent(true);
            EyeCollection<EyeData?>? data = null;

            if (eyeTracker is null) return null;

            EventHandler<EyeTrackerImagesAndData>? eventHandler = (_, o) =>
            {
                data = o?.Data?.EyeDataRaw;
                dataWait.Set();
            };

            try
            {
                eyeTracker.NewDataAndImagesAvailable += eventHandler;

                dataWait.WaitOne(1000);
            }
            finally
            {
                eyeTracker.NewDataAndImagesAvailable -= eventHandler;
            }

            return data;
        }

        /// <summary>
        /// Gets the calibration parameters.
        /// </summary>
        /// <returns></returns>
        public EyeCalibrationParamteres GetCalibrationParameters()
        {
            var calibrationParameters = new EyeCalibrationParamteres();
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            try
            {
                if (eyeTracker.Calibration is null) return calibrationParameters;

                calibrationParameters.PhysicalModel = new EyeCollection<EyePhysicalModel>(
                    eyeTracker.Calibration.EyeCalibrationParameters[Eye.Left].EyePhysicalModel,
                    eyeTracker.Calibration.EyeCalibrationParameters[Eye.Right].EyePhysicalModel
                    );
                calibrationParameters.ReferenceData = new EyeCollection<EyeData?>(
                    eyeTracker.Calibration.EyeCalibrationParameters[Eye.Left].ReferenceData,
                    eyeTracker.Calibration.EyeCalibrationParameters[Eye.Right].ReferenceData
                    );
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote request for calibration parameters: " + ex.Message);
            }

            return calibrationParameters;
        }

        /// <summary>
        /// Records an event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public long RecordEvent(string message)
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            return eyeTracker.RecordEvent(message, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<RemoteFileInfo> DownloadLastFile()
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            var request = new DownloadRequest();
            try
            {
                Trace.WriteLine("Request to download last file");
                request.FileName = eyeTracker.Settings.LastRecordedFile;
                Trace.WriteLine("Request to download last file finished");
                return await this.DownloadFile(request);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR with file download request: " + ex);
            }

            var result = new RemoteFileInfo();
            result.FileName = string.Empty;
            result.FileByteStream = FileStream.Null;
            return result;
        }

        /// <summary>
        /// Downloads 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<RemoteFileInfo> DownloadFile(DownloadRequest request)
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            RemoteFileInfo result = new RemoteFileInfo();
            try
            {
                var filename = request.FileName;

                if (!Path.IsPathRooted(request.FileName))
                {
                    filename = Path.Combine(eyeTracker.Settings.DataFolder, request.FileName);
                }

                await Task.Run(async () =>
                {
                    while (eyeTracker.Recording)
                    {
                        Trace.WriteLine("Remote download file waiting for recording to finish.");
                        await Task.Delay(1000);
                    }
                });

                FileInfo fileInfo = new FileInfo(filename);

                // check if exists
                if (fileInfo.Exists)
                {
                    // open stream
                    FileStream stream = new FileStream(
                        fileInfo.FullName,
                        FileMode.Open,
                        FileAccess.Read);

                    // return result 
                    result.FileName = request.FileName;
                    result.Length = fileInfo.Length;
                    result.FileByteStream = stream;
                }
                else
                {
                    result.FileName = string.Empty;
                    result.FileByteStream = FileStream.Null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            return result;
        }

        /// <summary>
        /// Escute a command based on a string message.
        /// </summary>
        /// <param name="bytes">String message.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[] ParseAndExecuteStringMessage(byte[] bytes)
        {
            byte[] bytesToSend = new byte[0];
            var stringMessage = Encoding.ASCII.GetString(bytes);
            // var stringMessage = Encoding.ASCII.GetString(bytes, 0, i); TODO think how to optimize TCP

            var msg = stringMessage.Split('|');
            switch (msg[0].ToUpper())
            {
                case "STARTRECORDING":
                    StartRecording();
                    return bytesToSend;
                case "STOPRECORDING":
                    StopRecording();
                    return bytesToSend;
                case "GETDATA":
                    var eyedata = GetCurrentData();
                    var leftData = eyedata?[Eye.Left] ?? new EyeData();
                    var rightData = eyedata?[Eye.Left] ?? new EyeData();
                    var eyedatamsg =
                    $"{leftData.Timestamp.FrameNumberRaw};{leftData.Timestamp.Seconds};{leftData.Pupil.Center.X};{leftData.Pupil.Center.Y};" +
                    $"{rightData.Timestamp.FrameNumberRaw};{rightData.Timestamp.Seconds};{rightData.Pupil.Center.X};{rightData.Pupil.Center.Y};";
                    return Encoding.ASCII.GetBytes(eyedatamsg);
                case "RECORDEVENT":
                    var frameNumber = RecordEvent(msg[1]);
                    return Encoding.ASCII.GetBytes(frameNumber.ToString());
                case "TESEMPTY":
                    return new byte[1] { 68 };
                default:
                    throw new NotImplementedException("This command does not exist");
            }
        }
    }
}