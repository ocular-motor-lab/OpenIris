//-----------------------------------------------------------------------
// <copyright file="EyeTrackerEvent.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;

    /// <summary>
    /// Class to encapsulate any kind of event
    /// </summary>
    public class EyeTrackerEvent
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventMessage"></param>
        /// <param name="frameNumber"></param>
        /// <param name="data"></param>
        public EyeTrackerEvent(string eventMessage, long frameNumber, object? data = null)
        {
            this.EventMessage = eventMessage;
            this.ComputerTime = DateTime.Now;
            this.FrameNumber = frameNumber;
            this.Data = data ?? string.Empty;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public DateTime ComputerTime { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public long FrameNumber { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string EventMessage { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetStringLine()
        {
            return $"Time={this.ComputerTime.ToString("yyyy-MM-dd-HH:mm:ss.fff")} FrameNumber={FrameNumber}  Message={EventMessage} Data={this.Data.ToString()}";   
        }
    }
}
