//-----------------------------------------------------------------------
// <copyright file="EyeTrackerLog.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System.Drawing;

    /// <summary>
    /// Listener for Trace messages.
    /// </summary>
    public class LogTraceListener : TraceListener
    {
        private RichTextBox richTextBox1, richTextBoxLogLarge;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerLog class.
        /// </summary>
        public LogTraceListener(RichTextBox richTextBox1, RichTextBox richTextBoxLogLarge)
        {
            this.richTextBox1 = richTextBox1;
            this.richTextBoxLogLarge = richTextBoxLogLarge;

            Trace.Listeners.Add(this);
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// Writes a log message.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        public override void Write(string message)
        {
            if (richTextBox1.IsDisposed) return;
            if (richTextBoxLogLarge.IsDisposed) return;

            if (this.richTextBox1.InvokeRequired)
            {
                this.richTextBox1.BeginInvoke((Action)(()=> Write(message)));
                return;
            }
                
            var messageWithTime = DateTime.Now.ToString("HH:mm:ss") + " - " + message;

            if (message.ToUpper().Contains("ERROR"))
            {
                this.richTextBox1.AppendText(message, Color.Red);
                this.richTextBoxLogLarge.AppendText(messageWithTime, Color.Red);
            }
            else
            {
                this.richTextBox1.AppendText(message);
                this.richTextBoxLogLarge.AppendText(messageWithTime);
            }
            this.richTextBox1.ScrollToCaret();
            this.richTextBoxLogLarge.ScrollToCaret();
        } 

        /// <summary>
        /// Writes a log message with a line break at the end.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        public override void WriteLine(string message) => this.Write(message + Environment.NewLine);
    }
}