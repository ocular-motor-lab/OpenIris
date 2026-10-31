//-----------------------------------------------------------------------
// <copyright file="EyeTrackerWebServer.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using EmbedIO;
    using EmbedIO.Actions;
    using EmbedIO.WebApi;

    /// <summary>
    /// Web server for remote control: the REST API (/api), the live data stream (/ws/data), the
    /// web client (/) and the URLs of the old WCF web endpoint (/EyeTrackerEndpoint/Web/...).
    /// It uses its own HTTP implementation (EmbedIO), so unlike HttpListener and WCF it doesn't
    /// need a "netsh http add urlacl" from an administrator to accept connections from other computers.
    /// </summary>
    public sealed class EyeTrackerWebServer : IDisposable
    {
        private readonly EyeTracker eyeTracker;
        private readonly SynchronizationContext? uiContext;
        private readonly WebServer server;
        private readonly EyeTrackerDataStream dataStream;

        /// <summary>
        /// JSON options of every reply. NaN (eye not found) goes as null, which JSON has and
        /// MATLAB's jsondecode turns back into NaN.
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(), new NanAsNullConverter(), new NanAsNullFloatConverter() },
        };

        /// <summary>
        /// Creates and starts the server. Create it on the UI thread: commands that change the
        /// state of the eye tracker run there, as they do when they come from the OpenIris window.
        /// </summary>
        /// <param name="eyeTracker">Eye tracker to control.</param>
        /// <param name="port">Port to listen on, in all network interfaces.</param>
        public EyeTrackerWebServer(EyeTracker eyeTracker, int port)
        {
            this.eyeTracker = eyeTracker;
            uiContext = SynchronizationContext.Current;
            dataStream = new EyeTrackerDataStream("/ws/data", eyeTracker);

            Swan.Logging.Logger.NoLogging();

            server = new WebServer(o => o
                    .WithUrlPrefix($"http://*:{port}/")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(dataStream)
                .WithModule(new WebApiModule("/api", SendJsonAsync)
                    .WithController(() => new EyeTrackerWebApi(eyeTracker, this)))
                .WithModule(new ActionModule("/EyeTrackerEndpoint/Web", HttpVerbs.Any, OldWebEndpointAsync))
                .WithEmbeddedResources("/", typeof(EyeTrackerWebServer).Assembly, "OpenIris.RemoteServices.Web");

            server.OnHttpException = async (context, ex) =>
                await SendErrorAsync(context, ex.StatusCode, ex.Message ?? HttpStatusDescription.Get(ex.StatusCode));
            server.OnUnhandledException = async (context, ex) =>
            {
                // Commands that can't run in the current state (start recording while not tracking...).
                var conflict = ex is InvalidOperationException || ex is OpenIrisException;
                if (!conflict) Trace.WriteLine($"ERROR: web request {context.Request.HttpMethod} {context.RequestedPath} failed: {ex}");
                await SendErrorAsync(context, conflict ? 409 : 500, ex.Message);
            };

            server.Start();
            Trace.WriteLine($"Remote control web client and API at http://localhost:{port}/");
        }

        /// <summary>
        /// Runs an action on the UI thread and waits for it.
        /// </summary>
        internal T OnUiThread<T>(Func<T> action)
        {
            if (uiContext is null) return action();

            T result = default!;
            Exception? error = null;
            uiContext.Send(_ =>
            {
                try { result = action(); }
                catch (Exception ex) { error = ex; }
            }, null);

            if (error != null) throw error;
            return result;
        }

        /// <summary>
        /// Runs an action on the UI thread and waits for it.
        /// </summary>
        internal void OnUiThread(Action action) => OnUiThread(() => { action(); return 0; });

        /// <summary>
        /// Starts a long command (recording, calibration...) on the UI thread without waiting for it
        /// to finish. If it fails right away, for example because of the state of the eye tracker,
        /// the error goes to the client. Later errors go to the log.
        /// </summary>
        internal void StartOnUiThread(string name, Func<Task> command)
        {
            var task = OnUiThread(command);
            if (task.IsFaulted) throw task.Exception!.InnerException!;

            task.ContinueWith(t => Trace.WriteLine($"ERROR: remote {name} failed: {t.Exception?.InnerException?.Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task SendJsonAsync(IHttpContext context, object? data)
        {
            context.Response.ContentType = MimeType.Json;
            using var text = context.OpenResponseText(new UTF8Encoding(false));
            await text.WriteAsync(JsonSerializer.Serialize(data, JsonOptions)).ConfigureAwait(false);
        }

        private static Task SendErrorAsync(IHttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            return SendJsonAsync(context, new { error = message });
        }

        /// <summary>
        /// The URLs of the old WCF web endpoint (http://host:9001/EyeTrackerEndpoint/Web/StartRecording...),
        /// so links and scripts that used them keep working.
        /// </summary>
        private async Task OldWebEndpointAsync(IHttpContext context)
        {
            var api = new EyeTrackerWebApi(eyeTracker, this);
            switch (context.RequestedPath.Trim('/').ToUpperInvariant())
            {
                case "":
                    throw HttpException.Redirect("/");
                case "STARTRECORDING":
                    api.StartRecording();
                    break;
                case "STOPRECORDING":
                    api.StopRecording();
                    break;
                case "RESETREFERENCE":
                    api.ResetReference();
                    break;
                case "RECORDEVENT":
                    await SendJsonAsync(context, eyeTracker.RecordEvent(context.Request.QueryString["message"] ?? "", null));
                    return;
                case "STATUSSUMMARY":
                    await SendJsonAsync(context, api.GetStatus());
                    return;
                case "GETFILEWEB":
                    var file = eyeTracker.Settings.LastRecordedFile;
                    if (eyeTracker.Recording) throw new HttpException(409, "Still recording.");
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) throw HttpException.NotFound("No recorded file.");
                    await EyeTrackerWebApi.SendFileAsync(context, file);
                    return;
                default:
                    throw HttpException.NotFound();
            }

            await SendJsonAsync(context, new { ok = true });
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Dispose()
        {
            dataStream.Dispose();
            server.Dispose();
        }

        /// <summary>
        /// Writes NaN and infinity, which JSON doesn't have, as null.
        /// </summary>
        private sealed class NanAsNullConverter : JsonConverter<double>
        {
            public override bool HandleNull => true;

            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                reader.TokenType == JsonTokenType.Null ? double.NaN : reader.GetDouble();

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) writer.WriteNullValue();
                else writer.WriteNumberValue(value);
            }
        }

        private sealed class NanAsNullFloatConverter : JsonConverter<float>
        {
            public override bool HandleNull => true;

            public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                reader.TokenType == JsonTokenType.Null ? float.NaN : reader.GetSingle();

            public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) writer.WriteNullValue();
                else writer.WriteNumberValue(value);
            }
        }
    }
}
