//-----------------------------------------------------------------------
// <copyright file="EyeTrackerRemote.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text;
    using System.Text.Json;
    using System.Threading;

    /// <summary>
    /// Runs the text commands that come by UDP and TCP (see EyeTrackerUdpListener and
    /// EyeTrackerTcpListener, and docs/user-manual/remote-control.md).
    /// </summary>
    public class EyeTrackerRemote
    {
        protected EyeTracker eyeTracker;

        public EyeTrackerRemote(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
        }

        /// <summary>
        /// Starts the recording.
        /// </summary>
        public void StartRecording()
        {
            _ = eyeTracker.StartRecording();
        }

        /// <summary>
        /// Stops the recording.
        /// </summary>
        public void StopRecording()
        {
            eyeTracker.StopRecording();
        }

        /// <summary>
        /// Waits for the next processed frame (up to 1 s) and returns all its data.
        /// </summary>
        /// <returns></returns>
        private EyeTrackerData? WaitForNewTrackerData()
        {
            var dataWait = new AutoResetEvent(false);
            EyeTrackerData? data = null;

            if (eyeTracker is null) return null;

            EventHandler<EyeTrackerImagesAndData>? eventHandler = (_, o) =>
            {
                data = o?.Data;
                dataWait.Set();
            };

            try
            {
                eyeTracker.NewDataAndImagesAvailable += eventHandler;

                dataWait.WaitOne(1000);
            }
            finally
            {
                eyeTracker.NewDataAndImagesAvailable -= eventHandler;
            }

            return data;
        }

        /// <summary>
        /// Records an event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public long RecordEvent(string message)
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            return eyeTracker.RecordEvent(message, null);
        }

        /// <summary>
        /// Handles a CALIBRATION message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string CalibrationMessage(string message)
        {
            if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

            return eyeTracker.CalibrationMessage(message);
        }

        /// <summary>
        /// A JSON serializeable class representing eyedata.
        /// </summary>
        public class JsonEyeData
        {
            public JsonEyeData(EyeData data)
            {
                FrameNumber = data.Timestamp.FrameNumberRaw;
                Seconds = data.Timestamp.Seconds;
                Pupil = data.Pupil;

                var crs = data.CornealReflections ?? new CornealReflectionData[] { };
                var crCenters = new List<PointF>();
                foreach (var cr in crs)
                {
                    crCenters.Add(cr.Center);
                }

                CRs = crCenters.ToArray();
            }
            public ulong? FrameNumber { get; set; }
            public double? Seconds { get; set; }
            public PupilData? Pupil { get; set; }
            public PointF[]? CRs { get; set; }

        }

        /// <summary>
        /// A JSON serializeable class representing both left and right eyedata.
        /// </summary>
        public class JsonData
        {
            public JsonData(EyeData leftData, EyeData rightData, ExtraData extraData)
            {
                Left = new JsonEyeData(leftData);
                Right = new JsonEyeData(rightData);
                Extra = extraData;
            }
            public JsonEyeData? Left { get; set; }
            public JsonEyeData? Right { get; set; }

            public ExtraData? Extra { get; set; }
        }


        /// <summary>
        /// Escute a command based on a string message.
        /// </summary>
        /// <param name="bytes">String message.</param>
        /// <returns>The reply to send back, empty if there is none (also if the command failed).</returns>
        public byte[] ParseAndExecuteStringMessage(byte[] bytes)
        {
            // Errors are logged and the message is ignored. Letting them through would end the
            // UDP or TCP listener, and remote control would stop working until OpenIris restarts.
            try
            {
                return ExecuteStringMessage(bytes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ERROR: remote command '{Encoding.ASCII.GetString(bytes)}' failed: {ex.Message}");
                return new byte[0];
            }
        }

        private byte[] ExecuteStringMessage(byte[] bytes)
        {
            byte[] bytesToSend = new byte[0];
            var stringMessage = Encoding.ASCII.GetString(bytes);
            // var stringMessage = Encoding.ASCII.GetString(bytes, 0, i); TODO think how to optimize TCP

            var bar_idx = stringMessage.IndexOf('|');

            var command = bar_idx == -1 ? stringMessage : stringMessage.Substring(0, bar_idx);
            var data = bar_idx == -1 ? string.Empty : stringMessage.Substring(bar_idx + 1);
            switch (command.ToUpper())
            {
                case "STARTRECORDING":
                    StartRecording();
                    return bytesToSend;
                case "STOPRECORDING":
                    StopRecording();
                    return bytesToSend;
                case "GETDATA":
                    var eyedata = eyeTracker?.LastImagesAndData?.Data;
                    var leftData = eyedata?.EyeDataRaw?[Eye.Left] ?? new EyeData();
                    var rightData = eyedata?.EyeDataRaw?[Eye.Right] ?? new EyeData();
                    var extraData = eyedata?.ExtraData ?? new ExtraData();
                    var eyedatamsg = JsonSerializer.Serialize(new JsonData(leftData, rightData, extraData));
                    return Encoding.ASCII.GetBytes(eyedatamsg);

                case "WAITFORDATA":
                    eyedata = WaitForNewTrackerData();
                    leftData = eyedata?.EyeDataRaw?[Eye.Left] ?? new EyeData();
                    rightData = eyedata?.EyeDataRaw?[Eye.Right] ?? new EyeData();
                    extraData = eyedata?.ExtraData ?? new ExtraData();
                    eyedatamsg = JsonSerializer.Serialize(new JsonData(leftData, rightData, extraData));
                    return Encoding.ASCII.GetBytes(eyedatamsg);

                case "RECORDEVENT":
                    var frameNumber = RecordEvent(data);
                    return Encoding.ASCII.GetBytes(frameNumber.ToString());

                case "CALIBRATION":
                    var result = CalibrationMessage(data);
                    return Encoding.ASCII.GetBytes(result);

                case "SETSESSIONNAME":
                    if (eyeTracker != null)
                    {
                        eyeTracker.Settings.SessionName = data;
                    }
                    return bytesToSend;

                case "TESEMPTY":
                    return new byte[1] { 68 };
                default:
                    throw new NotImplementedException($"Unknown command '{command}'.");
            }
        }
    }
}