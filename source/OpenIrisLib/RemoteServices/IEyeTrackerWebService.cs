//-----------------------------------------------------------------------
// <copyright file="IEyeTrackerService.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Drawing;
    using System.ServiceModel;
    using System.ServiceModel.Web;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Interface contract for the remote interface of the eye tracker.
    /// </summary>
    [ServiceContract(Namespace = "http://OpenIris.org/torsion")]
    public interface IEyeTrackerWebService
    {
        EyeTrackerStatusSummary StatusSummary
        {
            [OperationContract]
            [WebGet]
            get;
        }

        EyeTrackingPipelineSettings? PipelineSettings
        {
            [OperationContract]
            [WebGet]
            get;
        }


        [OperationContract, WebGet(UriTemplate = "/")]
        public System.IO.Stream GetWebsite();

        [OperationContract]
        [WebGet]
        bool ChangeSetting(string settingName, object value);

        [OperationContract]
        [WebGet]
        void StartRecording();

        [OperationContract]
        [WebGet]
        void StopRecording();

        [OperationContract]
        [WebGet]
        void ResetReference();

        [OperationContract]
        [WebGet]
        long RecordEvent(string message);

        [OperationContract]
        [WebGet]
        System.IO.Stream GetFileWeb();

    }
}
