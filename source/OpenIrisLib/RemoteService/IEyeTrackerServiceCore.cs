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
    using System.IO;
    using System.ServiceModel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Interface contract for the remote interface of the eye tracker.
    /// </summary>
    [ServiceContract(Namespace = "http://OpenIris.org/torsion")]
    public interface IEyeTrackerServiceCore
    {

        //[OperationContract]
        //EyeCollection<EyeData?>? GetCurrentData();

        //[OperationContract]
        //EyeCollection<EyeData?>? WaitForNewData();

        //[OperationContract]
        //EyeCalibrationParamteres GetCalibrationParameters();

        [OperationContract]
        void StartRecording();

        [OperationContract]
        void StopRecording();

        [OperationContract]
        void ResetReference();

        [OperationContract]
        long RecordEvent(string message);
    }
}
