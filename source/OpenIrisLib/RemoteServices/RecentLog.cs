//-----------------------------------------------------------------------
// <copyright file="RecentLog.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The last lines of the log, for the web client (GET /api/log): what the log tab of the
    /// OpenIris window shows. Each line has a number, so a client asks only for the new ones.
    /// </summary>
    internal sealed class RecentLog : TraceListener
    {
        private const int MaxLines = 500;

        private readonly object gate = new object();
        private readonly Queue<LogLine> lines = new Queue<LogLine>();
        private readonly StringBuilder partial = new StringBuilder();
        private long lastNumber;

        public RecentLog()
        {
            Trace.Listeners.Add(this);
        }

        /// <summary>
        /// A line of the log. Error when it says so, as the OpenIris window shows it in red.
        /// </summary>
        public sealed class LogLine
        {
            public LogLine(long number, DateTime time, string text)
            {
                Number = number;
                Time = time.ToString("HH:mm:ss");
                Text = text;
                Error = text.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            public long Number { get; }
            public string Time { get; }
            public string Text { get; }
            public bool Error { get; }
        }

        /// <summary>
        /// The lines after the given number (all that are kept, with 0).
        /// </summary>
        public LogLine[] After(long number)
        {
            lock (gate) return lines.Where(line => line.Number > number).ToArray();
        }

        public override void Write(string? message)
        {
            if (string.IsNullOrEmpty(message)) return;

            lock (gate)
            {
                partial.Append(message);
                var text = partial.ToString();
                var end = text.LastIndexOf('\n');
                if (end < 0) return;

                partial.Clear().Append(text, end + 1, text.Length - end - 1);
                foreach (var line in text.Substring(0, end).Split('\n'))
                {
                    lines.Enqueue(new LogLine(++lastNumber, DateTime.Now, line.TrimEnd('\r')));
                    if (lines.Count > MaxLines) lines.Dequeue();
                }
            }
        }

        public override void WriteLine(string? message) => Write(message + "\n");

        protected override void Dispose(bool disposing)
        {
            if (disposing) Trace.Listeners.Remove(this);
            base.Dispose(disposing);
        }
    }
}
