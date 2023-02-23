//-----------------------------------------------------------------------
// <copyright file="EyeTrackerClient.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates a client that can control remotely (or locally from a different program) the eye tracker.
    /// </summary>
    public class EyeTrackerClient
    {
        private readonly IEyeTrackerService proxy;

        public EyeTrackerClient(string hostname, int port)
        {
            var binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            binding.Security.Mode = SecurityMode.None;

            this.proxy = ChannelFactory<IEyeTrackerService>.CreateChannel(
                binding,
                new EndpointAddress("net.tcp://" + hostname + ":" + port + "/EyeTrackerEndpoint"));

            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "IP.txt"), hostname);
        }

        public EyeTrackerStatusSummary Status
        {
            get { return this.proxy.Status; }
        }

        public EyeTrackingPipelineSettings Settings
        {
            get { return this.proxy.Settings; }
        }

        public bool ChangeSetting(string settingName, object value)
        {
            return this.proxy.ChangeSetting(settingName, value);
        }

        public ImagesAndData GetCurrentImagesAndData()
        {
            return this.proxy.GetCurrentImagesAndData();
        }

        public EyeCalibrationParamteres GetCalibrationParameters()
        {
            return this.proxy.GetCalibrationParameters();
        }

        public void StartRecording()
        {
            this.proxy.StartRecording();
        }
        
        public void StopRecording()
        {
            this.proxy.StopRecording();
        }

        public void ResetReference()
        {
            this.proxy.ResetReference();
        }

        public long RecordEvent(string message)
        {
           return this.proxy.RecordEvent(message);
        }

        public EyeCollection<EyeData?>? GetCurrentData()
        {
            return this.proxy.GetCurrentData();
        }
        
        public EyeCollection<EyeData?>? WaitForNewData()
        {
            return this.proxy.WaitForNewData();
        }

        public void ChangeThreshold(bool increase, bool dark, Eye whichEye)
        {
            this.proxy.changeThreshold(increase, dark, whichEye);
        }

        public List<string> DownloadFile()
        {
            var task = Task.Run(async () => await this.DownloadFileAsync());
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> DownloadFileAsync()
        {
            try
            {
                byte[] buffer = new byte[6500];
                int bytesRead = 0;

                var pathToExecutable = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var downloadPath = System.IO.Path.Combine(pathToExecutable, "DownloadedData");
                if (!System.IO.Directory.Exists(downloadPath))
                {
                    System.IO.Directory.CreateDirectory(downloadPath);
                }

                var remoteFilesInfo = new RemoteFileInfo[3];
                var localFiles = new List<string>();
                // data file
                remoteFilesInfo[0] = await this.proxy.DownloadLastFile();
                var req = new DownloadRequest();
                // calibraiton file
                req.FileName = remoteFilesInfo[0].FileName.Replace(".txt", ".cal");
                remoteFilesInfo[1] = await this.proxy.DownloadFile(req);
                // events file
                req.FileName = remoteFilesInfo[0].FileName.Replace(".txt", "-events.txt");
                remoteFilesInfo[2] = await this.proxy.DownloadFile(req);

                foreach (var remoteFileInfo in remoteFilesInfo)
                {
                    if (!string.IsNullOrEmpty(remoteFileInfo.FileName))
                    {
                        var file = System.IO.Path.Combine(downloadPath, System.IO.Path.GetFileName(remoteFileInfo.FileName));
                        localFiles.Add(file);
                        using (var fileStream = System.IO.File.Create(file))
                        {
                            bytesRead = remoteFileInfo.FileByteStream.Read(buffer, 0, buffer.Length);
                            while (bytesRead > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                                fileStream.Flush();

                                buffer = new byte[6500];
                                bytesRead = remoteFileInfo.FileByteStream.Read(buffer, 0, buffer.Length);
                            }
                        }
                        remoteFileInfo.FileByteStream.Close();
                    }
                }

                return localFiles;
            }
            catch(Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }
    }
}
