//-----------------------------------------------------------------------
// <copyright file="IEyeTrackerService.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.IO;
    using System.ServiceModel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Interface contract for the remote interface of the eye tracker.
    /// </summary>
    [ServiceContract(Namespace = "http://OpenIris.org/torsion")]
    public interface IEyeTrackerService
    {
        EyeTrackerStatusSummary Status
        {
            [OperationContract]
            get;
        }

        EyeTrackingPipelineSettings? Settings
        {
            [OperationContract]
            get;
        }

        [OperationContract]
        bool ChangeSetting(string settingName, object value);

        [OperationContract]
        ImagesAndData GetCurrentImagesAndData();

        [OperationContract]
        EyeCalibrationParamteres GetCalibrationParameters();

        [OperationContract]
        void StartRecording();

        [OperationContract]
        void StopRecording();

        [OperationContract]
        void ResetReference();

        [OperationContract]
        long RecordEvent(string message);

        [OperationContract]
        void changeThreshold(bool increase, bool dark, Eye whichEye);

        [OperationContract]
        System.Threading.Tasks.Task<RemoteFileInfo> DownloadFile(DownloadRequest request);

        [OperationContract]
        System.Threading.Tasks.Task<RemoteFileInfo> DownloadLastFile();
    }

    /// <summary>
    /// Message to request a filename to be downloaded.
    /// </summary>
    [MessageContract]
    public class DownloadRequest
    {
        [MessageBodyMember]
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Message to download a file.
    /// </summary>
    [MessageContract]
    public sealed class RemoteFileInfo : IDisposable
    {
        [MessageHeader(MustUnderstand = true)]
        public string FileName { get; set; } = string.Empty;

        [MessageHeader(MustUnderstand = true)]
        public long Length { get; set; }

        [MessageBodyMember(Order = 1)]
        public Stream? FileByteStream { get; set; }

        public void Dispose()
        {
            FileByteStream?.Close();
        }
    }

    public class ImagesAndData
    {
        public EyeCollection<Bitmap?> Image { get; set; } = new EyeCollection<Bitmap?>();
        public EyeCollection<EyeData?> RawData { get; set; } = new EyeCollection<EyeData?>();
        public EyeCollection<CalibratedEyeData> CalibratedData { get; set; } = new EyeCollection<CalibratedEyeData>();
    }

    public class EyeCalibrationParamteres
    {
        public EyeCollection<EyePhysicalModel> PhysicalModel { get; set; } = new EyeCollection<EyePhysicalModel>();
        public EyeCollection<EyeData?> ReferenceData { get; set; } = new EyeCollection<EyeData?>();
    }

    public class EyeTrackerStatusSummary
    {
        public bool NotStarted { get; set; }
        public bool Tracking { get; set; }
        public bool Processing { get; set; }

        public bool Recording { get; set; }

        public bool Calibrating { get; set; }

        public string GrabberStatus { get; set; } = "";
        public string ProcessorStatus { get; set; } = "";
        public string RecorderStatus { get; set; } = "";
    }
}
