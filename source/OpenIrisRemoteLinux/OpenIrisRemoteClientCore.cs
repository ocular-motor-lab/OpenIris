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
    public class OpenIrisRemoteClientCore
    {
        private readonly IEyeTrackerServiceCore proxy;

        public OpenIrisRemoteClientCore(string hostname, int port)
        {
            var binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            binding.Security.Mode = SecurityMode.None;

            var channelFactory = new ChannelFactory<IEyeTrackerServiceCore>(binding, new EndpointAddress("net.tcp://" + hostname + ":" + port + "/EyeTrackerEndpoint"));
            this.proxy = channelFactory.CreateChannel();
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
    }
}
