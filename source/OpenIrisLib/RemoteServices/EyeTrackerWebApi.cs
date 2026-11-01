//-----------------------------------------------------------------------
// <copyright file="EyeTrackerWebApi.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using EmbedIO;
    using EmbedIO.Routing;
    using EmbedIO.WebApi;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    /// <summary>
    /// The REST API, under /api. Documented in docs/user-manual/remote-control.md.
    /// Replies are JSON; errors are {"error": "message"} with status 400 (bad request),
    /// 404 (not found), 409 (not possible in the current state, e.g. recording while not tracking)
    /// or 500.
    /// </summary>
    internal sealed class EyeTrackerWebApi : WebApiController
    {
        private readonly EyeTracker eyeTracker;
        private readonly EyeTrackerWebServer server;

        public EyeTrackerWebApi(EyeTracker eyeTracker, EyeTrackerWebServer server)
        {
            this.eyeTracker = eyeTracker;
            this.server = server;
        }

        #region Status

        [Route(HttpVerbs.Get, "/status")]
        public object GetStatus()
        {
            var settings = eyeTracker.Settings;
            return new
            {
                eyeTracker.NotStarted,
                eyeTracker.Tracking,
                Processing = eyeTracker.PostProcessing,
                eyeTracker.Recording,
                eyeTracker.Calibrating,
                GrabberStatus = eyeTracker.ImageGrabber?.GrabbingStatus() ?? "Not tracking",
                HeadTrackerStatus = eyeTracker.HeadTracker?.Status ?? "",
                ProcessorStatus = eyeTracker.ImageProcessor?.ProcessingStatus ?? "Not tracking",
                RecorderStatus = eyeTracker.RecordingSession?.RecordingStatus ?? "Not recording",
                FrameRate = eyeTracker.ImageGrabber?.FrameRateMeasured ?? double.NaN,
                settings.EyeTrackerSystem,
                Eye = settings.EyeTrackingSystemSettings.Eye,
                settings.EyeTrackingPipeline,
                settings.CalibrationMethod,
                settings.Profile,
                settings.SessionName,
                settings.DataFolder,
                settings.LastRecordedFile,
                Version = Version,
            };
        }

        /// <summary>
        /// Version of OpenIris, as GitVersion stamps it on OpenIris.exe (0.2.0-alpha.5...).
        /// </summary>
        private static readonly string Version =
            (Assembly.GetEntryAssembly()?.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .FirstOrDefault() as AssemblyInformationalVersionAttribute)?.InformationalVersion
            ?? typeof(EyeTracker).Assembly.GetName().Version?.ToString() ?? "";

        #endregion

        #region Tracking, recording and events

        [Route(HttpVerbs.Post, "/tracking/start")]
        public object StartTracking()
        {
            if (!eyeTracker.NotStarted) throw new InvalidOperationException("Already tracking.");
            server.StartOnUiThread("start tracking", () => eyeTracker.StartTracking());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/tracking/stop")]
        public object StopTracking()
        {
            server.OnUiThread(() => eyeTracker.StopTracking());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/recording/start")]
        public object StartRecording()
        {
            if (!eyeTracker.Tracking) throw new InvalidOperationException("Can't record: not tracking.");
            server.StartOnUiThread("start recording", () => eyeTracker.StartRecording());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/recording/stop")]
        public object StopRecording()
        {
            server.OnUiThread(() => eyeTracker.StopRecording());
            return Ok();
        }

        /// <summary>
        /// Saves an event in the events file of the recording. The body is the message, as text.
        /// Replies with the frame number of the event.
        /// </summary>
        [Route(HttpVerbs.Post, "/events")]
        public async Task<object> RecordEvent()
        {
            var message = await HttpContext.GetRequestBodyAsStringAsync().ConfigureAwait(false);
            return new { FrameNumber = eyeTracker.RecordEvent(message ?? "", null) };
        }

        #endregion

        #region Calibration

        [Route(HttpVerbs.Post, "/reference/reset")]
        public object ResetReference()
        {
            if (!eyeTracker.Tracking) throw new InvalidOperationException("Can't reset the reference: not tracking.");
            if (eyeTracker.Calibrating) throw new InvalidOperationException("Can't reset the reference while calibrating.");
            server.StartOnUiThread("reset reference", () => eyeTracker.ResetReference());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/calibration/start")]
        public object StartCalibration()
        {
            if (!eyeTracker.Tracking) throw new InvalidOperationException("Can't calibrate: not tracking.");
            server.StartOnUiThread("calibration", () => eyeTracker.StartCalibration());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/calibration/cancel")]
        public object CancelCalibration()
        {
            server.OnUiThread(() => eyeTracker.CancelCalibration());
            return Ok();
        }

        [Route(HttpVerbs.Post, "/calibration/reset")]
        public object ResetCalibration()
        {
            server.OnUiThread(() => eyeTracker.ResetCalibration());
            return Ok();
        }

        /// <summary>
        /// Sends a message to the running calibration (for example FixStart:x,y for the Remote Linear
        /// method). The body is the message, as text. Replies with the answer of the calibration.
        /// </summary>
        [Route(HttpVerbs.Post, "/calibration/message")]
        public async Task<object> CalibrationMessage()
        {
            var message = await HttpContext.GetRequestBodyAsStringAsync().ConfigureAwait(false);
            if (!eyeTracker.Calibrating) throw new InvalidOperationException("Not calibrating.");
            return new { Reply = server.OnUiThread(() => eyeTracker.CalibrationMessage(message ?? "")) };
        }

        #endregion

        #region Data and images

        /// <summary>
        /// Data of the last frame: raw and calibrated, for each eye, plus head and extra data.
        /// </summary>
        [Route(HttpVerbs.Get, "/data")]
        public object GetData() => DataReply(eyeTracker.LastImagesAndData?.Data);

        /// <summary>
        /// Waits for the next frame (up to 1 s) and replies with its data.
        /// </summary>
        [Route(HttpVerbs.Get, "/data/next")]
        public async Task<object> GetNextData()
        {
            var next = new TaskCompletionSource<EyeTrackerData?>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnNewData(object? sender, EyeTrackerImagesAndData e) => next.TrySetResult(e?.Data);

            eyeTracker.NewDataAndImagesAvailable += OnNewData;
            try
            {
                var finished = await Task.WhenAny(next.Task, Task.Delay(1000)).ConfigureAwait(false);
                return DataReply(finished == next.Task ? next.Task.Result : null);
            }
            finally
            {
                eyeTracker.NewDataAndImagesAvailable -= OnNewData;
            }
        }

        private static object DataReply(EyeTrackerData? data)
        {
            var left = data?.EyeDataRaw?[Eye.Left];
            var right = data?.EyeDataRaw?[Eye.Right];
            return new
            {
                FrameNumber = (left ?? right)?.Timestamp.FrameNumber,
                Seconds = (left ?? right)?.Timestamp.Seconds,
                FrameRate = data?.FrameRate,
                Left = left,
                Right = right,
                LeftCalibrated = data?.EyeDataCalibrated?[Eye.Left],
                RightCalibrated = data?.EyeDataCalibrated?[Eye.Right],
                Head = data?.HeadDataRaw,
                HeadCalibrated = data?.HeadDataCalibrated,
                Extra = data?.ExtraData,
            };
        }

        /// <summary>
        /// Last image of one eye (left or right) as JPEG, by default with the tracking drawn on top
        /// as in the OpenIris window. ?overlay=false for the plain image.
        /// </summary>
        [Route(HttpVerbs.Get, "/images/{eye}")]
        public async Task GetImage(string eye, [QueryField] string? overlay)
        {
            if (!Enum.TryParse<Eye>(eye, true, out var whichEye) || whichEye == Eye.Both)
                throw HttpException.BadRequest("The eye must be left or right.");

            var imagesAndData = eyeTracker.LastImagesAndData;
            var imageEye = imagesAndData?.Images[whichEye];
            if (imagesAndData is null || imageEye is null) throw HttpException.NotFound("No image.");

            byte[] jpeg;
            try
            {
                using var image = overlay == "false"
                    ? imageEye.Image.Convert<Bgr, byte>()
                    : EyeImageOverlay.DrawAllData(imageEye, imagesAndData.Calibration.EyeCalibrationParameters[whichEye], imagesAndData.TrackingSettings);
                if (image is null) throw HttpException.NotFound("No image.");

                using var buffer = new VectorOfByte();
                CvInvoke.Imencode(".jpg", image, buffer, new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 80));
                jpeg = buffer.ToArray();
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is NullReferenceException || ex is AccessViolationException)
            {
                // The frame was released while copying it. The next request gets the next one.
                throw new HttpException(503, "The image changed while reading it, try again.");
            }

            HttpContext.Response.ContentType = "image/jpeg";
            HttpContext.Response.Headers.Add("Cache-Control", "no-store");
            using var stream = HttpContext.OpenResponseStream(false, false);
            await stream.WriteAsync(jpeg, 0, jpeg.Length).ConfigureAwait(false);
        }

        #endregion

        #region Settings

        /// <summary>
        /// All the settings shown in the settings window, in groups: General (the main settings),
        /// System (the eye tracking system), Tracking (the tracking pipeline) and Calibration.
        /// Values are numbers, booleans or text (as the settings window shows them).
        /// </summary>
        [Route(HttpVerbs.Get, "/settings")]
        public object GetSettings() => server.OnUiThread(() =>
            SettingsGroups().SelectMany(g => TypeDescriptor.GetProperties(g.settings, new Attribute[] { BrowsableAttribute.Yes })
                .Cast<PropertyDescriptor>()
                .Select(p => DescribeSetting(g.group, g.settings, p))
                .Where(s => s != null))
            .ToList());

        /// <summary>
        /// Changes a setting. The name is Group.Name (Tracking.DarkThresholdLeftEye), or just the name
        /// for the General group (SessionName). The body is the new value as JSON (a number, true/false
        /// or a string) or as plain text.
        /// </summary>
        [Route(HttpVerbs.Put, "/settings/{name}")]
        public async Task<object> ChangeSetting(string name)
        {
            var text = await ReadTextAsync().ConfigureAwait(false);
            server.OnUiThread(() => SetSetting(name, text));
            return Ok();
        }

        /// <summary>
        /// The body of the request as text: a JSON value, or plain text.
        /// </summary>
        private async Task<string> ReadTextAsync()
        {
            var body = await HttpContext.GetRequestBodyAsStringAsync().ConfigureAwait(false) ?? "";
            try
            {
                using var json = JsonDocument.Parse(body);
                return json.RootElement.ValueKind == JsonValueKind.String ? json.RootElement.GetString() ?? "" : json.RootElement.GetRawText();
            }
            catch (JsonException)
            {
                return body;
            }
        }

        #region Settings profiles

        /// <summary>
        /// The settings profiles: the active one and all of them.
        /// </summary>
        [Route(HttpVerbs.Get, "/profiles")]
        public object GetProfiles() => server.OnUiThread(() => new { Active = eyeTracker.Settings.Profile, Profiles = eyeTracker.Settings.Profiles });

        /// <summary>
        /// Switches to another profile. The body is its name. Only while not tracking.
        /// </summary>
        [Route(HttpVerbs.Put, "/profiles/active")]
        public async Task<object> SwitchProfile()
        {
            var name = await ReadTextAsync().ConfigureAwait(false);
            server.OnUiThread(() => ProfileCommand(() => eyeTracker.SwitchSettingsProfile(name)));
            return GetProfiles();
        }

        /// <summary>
        /// Saves the current settings as a new profile, which becomes the active one. The body is its name.
        /// </summary>
        [Route(HttpVerbs.Post, "/profiles")]
        public async Task<object> SaveProfileAs()
        {
            var name = await ReadTextAsync().ConfigureAwait(false);
            server.OnUiThread(() => ProfileCommand(() => eyeTracker.Settings.SaveProfileAs(name)));
            return GetProfiles();
        }

        private static void ProfileCommand(Action command)
        {
            try
            {
                command();
            }
            catch (ArgumentException ex)
            {
                throw HttpException.BadRequest(ex.Message);
            }
        }

        #endregion

        /// <summary>
        /// Sets a setting from its text, converted as the settings window does. Also used by the
        /// old ChangeSetting remote command.
        /// </summary>
        internal void SetSetting(string name, string text)
        {
            var dot = name.IndexOf('.');
            var group = dot > 0 ? name.Substring(0, dot) : "General";
            var propertyName = dot > 0 ? name.Substring(dot + 1) : name;

            var settings = SettingsGroups().FirstOrDefault(g => string.Equals(g.group, group, StringComparison.OrdinalIgnoreCase)).settings
                ?? throw HttpException.NotFound($"No settings group '{group}'. The groups are General, System, Tracking and Calibration.");
            var property = TypeDescriptor.GetProperties(settings).Find(propertyName, true);
            if (property is null || !property.IsBrowsable) throw HttpException.NotFound($"No setting '{name}'.");
            if (property.IsReadOnly) throw HttpException.BadRequest($"The setting '{name}' is read only.");

            object? value;
            try
            {
                // Text settings take the text as it is. Some have a converter only for their list
                // of options (the plugin lists), which can't convert from text.
                value = property.PropertyType == typeof(string) ? text : property.Converter.ConvertFromInvariantString(text);
            }
            catch (Exception ex)
            {
                throw HttpException.BadRequest($"'{text}' is not a valid value for {name}: {ex.Message}");
            }
            property.SetValue(settings, value);
        }

        private IEnumerable<(string group, object settings)> SettingsGroups()
        {
            var settings = eyeTracker.Settings;
            yield return ("General", settings);
            yield return ("System", settings.EyeTrackingSystemSettings);
            yield return ("Tracking", settings.TrackingPipelineSettings);
            yield return ("Calibration", settings.CalibrationSettings);
        }

        private static object? DescribeSetting(string group, object settings, PropertyDescriptor property)
        {
            var converter = property.Converter;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object? value = property.GetValue(settings);

            string kind;
            if (type == typeof(bool)) kind = "boolean";
            else if (type.IsEnum) kind = "enum";
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) kind = "integer";
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) kind = "number";
            else if (type == typeof(string) || (converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))) kind = "text";
            else return null;

            if (kind == "enum" || (kind == "text" && type != typeof(string))) value = converter.ConvertToInvariantString(value);

            string[]? options = null;
            if (converter.GetStandardValuesSupported())
            {
                options = converter.GetStandardValues()?.Cast<object>().Select(o => converter.ConvertToInvariantString(o)).ToArray();
            }

            return new
            {
                Name = group == "General" ? property.Name : group + "." + property.Name,
                Group = group,
                property.Category,
                property.Description,
                Type = kind,
                Value = value,
                Options = options,
                property.IsReadOnly,
            };
        }

        #endregion

        #region Files

        /// <summary>
        /// The files of the last recording that exist: data, calibration and events.
        /// </summary>
        [Route(HttpVerbs.Get, "/files/last")]
        public object GetLastFiles()
        {
            var dataFile = eyeTracker.Settings.LastRecordedFile;
            if (string.IsNullOrEmpty(dataFile)) return new { Recording = eyeTracker.Recording, Files = new string[0] };

            var files = new[] { dataFile, dataFile.Replace(".txt", ".cal"), dataFile.Replace(".txt", "-events.txt") }
                .Where(File.Exists)
                .Select(Path.GetFileName)
                .ToArray();
            return new { Recording = eyeTracker.Recording, Files = files };
        }

        /// <summary>
        /// Downloads a file of the last recording, or of the data folder. Only file names, no folders.
        /// </summary>
        [Route(HttpVerbs.Get, "/files/{name}")]
        public async Task GetFile(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(".."))
                throw HttpException.BadRequest("Only the name of a file in the data folder.");

            // Each recording has its own folder inside the data folder.
            var lastRecording = eyeTracker.Settings.LastRecordedFile;
            var file = new[] { string.IsNullOrEmpty(lastRecording) ? null : Path.GetDirectoryName(lastRecording), eyeTracker.Settings.DataFolder }
                .Where(folder => !string.IsNullOrEmpty(folder))
                .Select(folder => Path.Combine(folder, name))
                .FirstOrDefault(File.Exists)
                ?? throw HttpException.NotFound($"No file '{name}' in the last recording or the data folder.");
            if (eyeTracker.Recording && string.Equals(Path.GetFileNameWithoutExtension(file), Path.GetFileNameWithoutExtension(eyeTracker.Settings.LastRecordedFile), StringComparison.OrdinalIgnoreCase))
                throw new HttpException(409, "Still recording this file.");

            await SendFileAsync(HttpContext, file).ConfigureAwait(false);
        }

        internal static async Task SendFileAsync(IHttpContext context, string file)
        {
            context.Response.ContentType = file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? MimeType.PlainText : MimeType.Default;
            context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(file)}\"");
            using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var stream = context.OpenResponseStream(false, true);
            await source.CopyToAsync(stream).ConfigureAwait(false);
        }

        #endregion

        private static object Ok() => new { ok = true };
    }
}
