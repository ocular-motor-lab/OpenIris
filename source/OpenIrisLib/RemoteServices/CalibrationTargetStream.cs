//-----------------------------------------------------------------------
// <copyright file="CalibrationTargetStream.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using EmbedIO.WebSockets;
    using OpenIris.Calibration;

    /// <summary>
    /// WebSocket for the displays of the N-point calibration in the RemoteDisplay mode (every page
    /// of the web client, targets.js): OpenIris decides the targets and this sends them, about
    /// 60 times a second while there is one:
    ///     {"type":"target","x":5,"y":-3,"scale":1.4,"recording":false,"size":0.8,"screenWidthMm":530,"distanceMm":570,"number":2,"count":9}
    /// x and y in degrees, positive right and up; size, the diameter in degrees, drawn scale times bigger.
    /// The screen's width and distance are the eye tracking system's settings, so the display converts
    /// degrees to its pixels. When there is no target: {"type":"idle"}.
    /// </summary>
    internal sealed class CalibrationTargetStream : WebSocketModule
    {
        private const int SendIntervalMs = 16;

        private readonly EyeTracker eyeTracker;
        private readonly Timer sendTimer;
        private string lastMessage = string.Empty;
        private int sending;

        public CalibrationTargetStream(string urlPath, EyeTracker eyeTracker)
            : base(urlPath, true)
        {
            this.eyeTracker = eyeTracker;
            sendTimer = new Timer(_ => Send(), null, SendIntervalMs, SendIntervalMs);
        }

        /// <summary>
        /// The message for the current target, or idle.
        /// </summary>
        private string Message()
        {
            if (eyeTracker.CalibrationSession?.Pipeline is not CalibrationPipelineRemoteLinear pipeline ||
                pipeline.Mode != NPointMode.RemoteDisplay ||
                pipeline.Target is not NPointTarget target)
            {
                return "{\"type\":\"idle\"}";
            }

            var settings = (CalibrationSettingsRemoteLinear)pipeline.Settings;
            var system = pipeline.SystemSettings;
            string N(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
            return "{\"type\":\"target\"" +
                $",\"x\":{N(target.Position.X)},\"y\":{N(target.Position.Y)},\"scale\":{N(target.Scale)}" +
                $",\"recording\":{(target.Recording ? "true" : "false")},\"size\":{N(settings.TargetSize)}" +
                $",\"screenWidthMm\":{N(system?.ScreenWidthMm ?? 530)},\"distanceMm\":{N(system?.ViewingDistanceMm ?? 570)}" +
                $",\"number\":{target.Number},\"count\":{target.Count}}}";
        }

        private async void Send()
        {
            // Skip a tick if the previous message is still going out.
            if (Interlocked.Exchange(ref sending, 1) == 1) return;
            try
            {
                if (ActiveContexts.Count == 0) return;

                var message = Message();
                if (message == lastMessage) return;
                lastMessage = message;
                await BroadcastAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("ERROR sending the calibration target: " + ex.Message);
            }
            finally
            {
                sending = 0;
            }
        }

        // A new display gets the current state right away.
        protected override Task OnClientConnectedAsync(IWebSocketContext context) => SendAsync(context, Message());

        // Displays don't send anything.
        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result) => Task.CompletedTask;

        protected override void Dispose(bool disposing)
        {
            if (disposing) sendTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
