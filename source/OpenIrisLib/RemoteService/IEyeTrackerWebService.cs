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
    using System.ServiceModel;
    using System.ServiceModel.Web;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Interface contract for the remote interface of the eye tracker.
    /// </summary>
    [ServiceContract(Namespace = "http://OpenIris.org/torsion")]
    public interface IEyeTrackerWebService
    {
        EyeTrackerStatusSummary Status
        {
            [OperationContract]
            [WebGet]
            get;
        }

        EyeTrackingAlgorithmSettings? Settings
        {
            [OperationContract]
            [WebGet]
            get;
        }

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

    }
}
