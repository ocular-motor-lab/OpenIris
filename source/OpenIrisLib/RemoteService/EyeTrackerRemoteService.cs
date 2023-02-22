//-----------------------------------------------------------------------
// <copyright file="EyeTrackerService.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.Threading.Tasks;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Service methods to allow remote control of the eye tracker.
    /// </summary>
    public class EyeTrackerRemoteService : IEyeTrackerService, IEyeTrackerWebService
    {
        private static EyeTracker? eyeTracker;
        private static ServiceHost? eyeTrackerHost;
        private static ServiceHost? eyeTrackerHostWeb;
        private static AutoResetEvent dataWait = new AutoResetEvent(true);

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="newEyeTracker">Eye tracker object.</param>
        /// <returns>The service host object.</returns>
        public static void Start(EyeTracker newEyeTracker)
        {
            if (eyeTracker != null) return;

            eyeTracker = newEyeTracker;

            // HTTP SERVICE
            try
            {
                eyeTrackerHostWeb = new ServiceHost(typeof(EyeTrackerRemoteService), new Uri[] { new Uri("http://localhost:" + (eyeTracker.Settings.ServiceListeningPort + 1) + "/EyeTrackerEndpoint") });
                eyeTrackerHostWeb.AddServiceEndpoint(typeof(IEyeTrackerWebService), new BasicHttpBinding(), "Soap");
                var endpoint = eyeTrackerHostWeb.AddServiceEndpoint(typeof(IEyeTrackerWebService), new WebHttpBinding(), "Web");
                endpoint.EndpointBehaviors.Add(new WebHttpBehavior());
                eyeTrackerHostWeb.Open();
                Trace.WriteLine(eyeTrackerHostWeb.State);
                Trace.WriteLine(eyeTrackerHostWeb.Description);
                Trace.WriteLine(eyeTrackerHostWeb.Credentials);
                if (eyeTrackerHostWeb.BaseAddresses.Count > 0)
                    Trace.WriteLine(eyeTrackerHostWeb.BaseAddresses[0].ToString());

                Trace.WriteLine(endpoint.Address);
                Trace.WriteLine(endpoint.ListenUri);
                Trace.WriteLine(endpoint.ListenUriMode);
                Trace.WriteLine(endpoint.EndpointBehaviors[0]);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting WEB service: " + ex.Message);


                Trace.WriteLine("==== INSTRUCTIONS ==========================================================");
                Trace.WriteLine("After running the server for the first time you should open the command line");
                Trace.WriteLine("in administrator mode and run the following command to enable the service.");
                Trace.WriteLine($"netsh http add urlacl url = http://+:{eyeTracker.Settings.ServiceListeningPort + 1}/EyeTrackerEndpoint/ user=username");
                Trace.WriteLine("After running the command and starting the eye tracker you can open the browser ");
                Trace.WriteLine($"http://localhost:{eyeTracker.Settings.ServiceListeningPort + 1}/EyeTrackerEndpoint/Web/StartRecording");
                Trace.WriteLine($"Or from a different computer using the correct IP address");
                Trace.WriteLine("==== END INSTRUCTIONS ==========================================================");


                eyeTrackerHostWeb = null;
            }


            // TCP SERVICE
            try
            {
                eyeTrackerHost = new ServiceHost(typeof(EyeTrackerRemoteService));
                var binding = new NetTcpBinding();
                binding.MaxReceivedMessageSize = 2147483647;
                binding.Security.Mode = SecurityMode.None;

                var address = "net.tcp://localhost:" + eyeTracker.Settings.ServiceListeningPort + "/EyeTrackerEndpoint";
                var e = eyeTrackerHost.AddServiceEndpoint(typeof(IEyeTrackerService), binding, address);

                eyeTrackerHost.Open();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting TCP service: " + ex.Message);
                eyeTrackerHostWeb = null;
            }

            // HTTP SERVICE FROM CLEVELAND DO NOT CHANGE
            //Need to run something like this in the command line. 
            //netsh http add urlacl url = http://+:9001/EyeTrackerEndpoint/ user=jorge
            // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-create-a-basic-wcf-web-http-service
            //
            //var host = new ServiceHost(typeof(EyeTrackerRemoteService), new Uri[] { new Uri("http://10.77.17.88:" + EyeTrackerRemoteService.EyeTracker.Settings.ServiceListeningPort + "/EyeTrackerEndpoint") });
            //host.AddServiceEndpoint(typeof(IEyeTrackerService), new BasicHttpBinding(), "Soap");
            //var endpoint = host.AddServiceEndpoint(typeof(IEyeTrackerService), new WebHttpBinding(), "Web");
            //endpoint.EndpointBehaviors.Add(new WebHttpBehavior());
            //host.Open();

            // END HTTP SERVICE FROM CLEVELAND DO NOT CHANGE
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public static void StopService()
        {
            try
            {
                eyeTrackerHost?.Close();
                eyeTrackerHostWeb?.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error closing the service: " + ex.Message);
            }
        }

        /// <summary>
        /// Summary status
        /// </summary>
        public EyeTrackerStatusSummary Status
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
        public EyeTrackingPipelineSettings? Settings => eyeTracker?.Settings.TrackingpipelineSettings; 

        /// <summary>
        /// Starts the recording.
        /// </summary>
        public void StartRecording()
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                var conf = new RecordingOptions()
                {
                    SessionName = eyeTracker.Settings.SessionName,
                    DataFolder = eyeTracker.Settings.DataFolder,
                    SaveRawVideo = eyeTracker.Settings.RecordVideo,
                    FrameRate = eyeTracker.ImageGrabber?.FrameRate ?? 0.0,
                };

                _ = eyeTracker.StartRecording(conf);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote start recording: " + ex.Message);
            }
        }

        /// <summary>
        /// Stops the recording.
        /// </summary>
        public void StopRecording()
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                eyeTracker.StopRecording();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote start recording: " + ex.Message);
            }
        }

        /// <summary>
        /// Resets the reference of the calibration.
        /// </summary>
        public void ResetReference()
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                _ = eyeTracker.ResetReference();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote start recording: " + ex.Message);
            }
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
            var trackingSettings = eyeTracker.Settings.TrackingpipelineSettings as EyeTrackingPipelineJOMSettings;

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
                            typeof(EyeTrackingPipelineSettings).GetProperty(settingParts[1]).SetValue(eyeTracker.Settings.TrackingpipelineSettings, value, null);
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
                        eyeTracker.LastImagesAndData.Data.EyeDataRaw?[Eye.Left],
                        eyeTracker.LastImagesAndData.Data.EyeDataRaw?[Eye.Right]);

                    imagesAndData.CalibratedData = new EyeCollection<CalibratedEyeData>(
                        eyeTracker.LastImagesAndData.Data.EyeDataCalibrated?[Eye.Left] ?? new CalibratedEyeData(),
                        eyeTracker.LastImagesAndData.Data.EyeDataCalibrated?[Eye.Right] ?? new CalibratedEyeData()
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
        /// Gets the IP addresses
        /// </summary>
        /// <returns></returns>
        public static string GetIPAddresses()
        {
            StringBuilder sb = new StringBuilder();

            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection) 
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                // Read the IP configuration for each network 
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses 
                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now 
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    sb.AppendLine(address.Address.ToString() + " (" + network.Name + ")");
                }
            }

            return sb.ToString();
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
    }
}
