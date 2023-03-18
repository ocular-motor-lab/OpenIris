//-----------------------------------------------------------------------
// <copyright file="EyeTrackerLog.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;

    /// <summary>
    /// Listener for Trace messages that will write them to a log file in the same folder as the executable.
    /// </summary>
    public class EyeTrackerLog : TextWriterTraceListener
    {
        /// <summary>
        /// Initializes a new instance of the EyeTrackerLog class.
        /// </summary>
        /// <param name="path">Path o the file.</param>
        private EyeTrackerLog(string path) : base(path)
        {
            Trace.Listeners.Add(this);
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerLog class.
        /// </summary>
        /// <param name="path">Path o the file.</param>
        public static EyeTrackerLog Create(string path)
        {
           return new EyeTrackerLog(path);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Close()
        {
            if (Trace.Listeners.Contains(this))
            {
                Trace.Listeners.Remove(this);
            }
            base.Close();
        }

        /// <summary>
        /// Writes a log message.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        public override void Write(string message) => base.Write(DateTime.Now.ToString("HH:mm:ss") + " - " + message);

        /// <summary>
        /// Writes a log message with a line break at the end.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        public override void WriteLine(string message) => this.Write(message + Environment.NewLine);
    }
}