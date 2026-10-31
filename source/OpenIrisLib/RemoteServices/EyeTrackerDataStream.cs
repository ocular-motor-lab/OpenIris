//-----------------------------------------------------------------------
// <copyright file="EyeTrackerDataStream.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using EmbedIO.WebSockets;

    /// <summary>
    /// WebSocket that streams every frame of data, with the columns of the data file (see
    /// docs/user-manual/data-file.md). When a client connects it gets the column names:
    ///     {"type":"columns","columns":["LeftFrameNumber",...]}
    /// and then, about 30 times a second, the frames since the last message:
    ///     {"type":"data","rows":[[...],[...]]}
    /// NaN (eye not found) goes as null.
    /// </summary>
    internal sealed class EyeTrackerDataStream : WebSocketModule
    {
        private const int SendIntervalMs = 33;

        // About 10 s at 500 Hz. If clients can't keep up, older frames are dropped.
        private const int MaxQueuedFrames = 5000;

        private readonly EyeTracker eyeTracker;
        private readonly ConcurrentQueue<double[]> frames = new ConcurrentQueue<double[]>();
        private readonly Timer sendTimer;
        private readonly string columnsMessage;
        private int sending;

        public EyeTrackerDataStream(string urlPath, EyeTracker eyeTracker)
            : base(urlPath, true)
        {
            this.eyeTracker = eyeTracker;

            var columns = EyeTrackerData.GetStringHeader().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            columnsMessage = JsonSerializer.Serialize(new { type = "columns", columns });

            eyeTracker.NewDataAndImagesAvailable += OnNewData;
            sendTimer = new Timer(_ => SendQueuedFrames(), null, SendIntervalMs, SendIntervalMs);
        }

        private void OnNewData(object? sender, EyeTrackerImagesAndData imagesAndData)
        {
            if (ActiveContexts.Count == 0 || imagesAndData?.Data is null) return;

            frames.Enqueue(imagesAndData.Data.GetDataArray());
            while (frames.Count > MaxQueuedFrames) frames.TryDequeue(out _);
        }

        private async void SendQueuedFrames()
        {
            // Skip a tick if the previous message is still going out.
            if (Interlocked.Exchange(ref sending, 1) == 1) return;
            try
            {
                if (frames.IsEmpty) return;
                if (ActiveContexts.Count == 0)
                {
                    while (frames.TryDequeue(out _)) { }
                    return;
                }

                var message = new StringBuilder("{\"type\":\"data\",\"rows\":[");
                var first = true;
                while (frames.TryDequeue(out var row))
                {
                    if (!first) message.Append(',');
                    first = false;
                    AppendRow(message, row);
                }
                message.Append("]}");

                await BroadcastAsync(message.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("ERROR sending the live data: " + ex.Message);
            }
            finally
            {
                sending = 0;
            }
        }

        private static void AppendRow(StringBuilder message, double[] row)
        {
            message.Append('[');
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0) message.Append(',');
                var value = row[i];
                // Same precision as the data file.
                message.Append(double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("0.####", CultureInfo.InvariantCulture));
            }
            message.Append(']');
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context) => SendAsync(context, columnsMessage);

        // Clients don't send anything.
        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result) => Task.CompletedTask;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                eyeTracker.NewDataAndImagesAvailable -= OnNewData;
                sendTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
