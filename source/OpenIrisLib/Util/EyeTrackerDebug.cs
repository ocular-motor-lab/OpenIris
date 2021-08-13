//-----------------------------------------------------------------------
// <copyright file="EyeTrackerDebug.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using static OpenIris.EyeTracker;

    /// <summary>
    /// Class with diagnostic information about the eye tracker. Timing, errors, etc.
    /// </summary>
    public static class EyeTrackerDebug
    {
        /// <summary>
        /// Initializes the static variables for this class.
        /// </summary>
        static EyeTrackerDebug()
        {
            deltaTimes = new ConcurrentDictionary<string, double>();
            previousMessages = new (double, string, double)[50];
            stopWatch = Stopwatch.StartNew();
            Images = new ConcurrentDictionary<string, EyeCollection<Image<Bgr, byte>?>>();
        }

        /// <summary>
        /// Stopwatch to measure the time it takes to process.
        /// </summary>
        private static readonly Stopwatch stopWatch;

        /// <summary>
        /// Dictionary of deltaTimes. Keeps a moving average of a given delta time.
        /// </summary>
        private static readonly ConcurrentDictionary<string, double> deltaTimes;

        /// <summary>
        /// For each thread (ThreadID is the key of the dictionary) saves
        /// the last message tracked, the time it happened, and the time of the begining of the frame.
        /// </summary>
        private static readonly (double beginTime, string message, double time)[] previousMessages;

        /// <summary>
        /// Collection of debug images with a name associated to them.
        /// </summary>
        public static ConcurrentDictionary<string, EyeCollection<Image<Bgr, byte>?>> Images { get; }

        /// <summary>
        /// Elapsed time since starting the application.
        /// </summary>
        public static TimeSpan TimeElapsed { get => stopWatch.Elapsed; }

        /// <summary>
        /// Adds an image for debugging.
        /// </summary>
        /// <param name="name">Name of the image.</param>
        /// <param name="whichEye">Left or righ eye.</param>
        /// <param name="image">Image to save.</param>
        public static void AddImage(string name, Eye whichEye, Image<Gray, byte> image)
        {
            if (!DEBUG) return;
            if (image == null) return;

            AddImage(name, whichEye, image.Convert<Bgr, byte>());
        }

        /// <summary>
        /// Adds an image for debugging.
        /// </summary>
        /// <param name="name">Name of the image.</param>
        /// <param name="whichEye">Left or righ eye.</param>
        /// <param name="image">Image to save.</param>
        public static void AddImage(string name, Eye whichEye, Image<Gray, float> image)
        {
            if (!DEBUG) return;
            if (image == null) return;

            AddImage(name, whichEye, image.Convert<Bgr, byte>());
        }

        /// <summary>
        /// Adds an image for debugging.
        /// </summary>
        /// <param name="name">Name of the image.</param>
        /// <param name="whichEye">Left or righ eye.</param>
        /// <param name="image">Image to save.</param>
        public static void AddImage(string name, Eye whichEye, Image<Bgr, byte> image)
        {
            if (!DEBUG) return;
            if (image == null) return;

            if (!Images.ContainsKey(name))
            {
                Images.TryAdd(name, new EyeCollection<Image<Bgr, byte>?>(null, null));
            }

            Images[name][whichEye] = image;
        }

        /// <summary>
        /// Converts the Timer data to a string.
        /// </summary>
        /// <returns>The string with all the timing info.</returns>
        public static string GetDeltaTimesText()
        {
            string s = string.Empty;
            foreach (var time in deltaTimes)
            {
                s = s + time.Key.PadRight(50, '.') + " : " + string.Format("{0:0.00}", time.Value) + "\r\n";
            }

            return s;
        }

        /// <summary>
        /// Tracks a partial time.
        /// </summary>
        /// <param name="message">Message describing the partial time step.</param>
        public static void TrackTime(string message)
        {
            if (DEBUG)
            {
                var threadID = Thread.CurrentThread.ManagedThreadId;
                var previousMessage = previousMessages[threadID];
                var time = EyeTrackerDebug.TimeElapsed.TotalMilliseconds;
                var newMessage = (previousMessage.beginTime, message, time);

                switch (message)
                {
                    case "FRAME START":
                        newMessage.beginTime = EyeTrackerDebug.TimeElapsed.TotalMilliseconds;
                        break;
                    case "FRAME FINISH":
                        var deltaTime = newMessage.time - previousMessage.beginTime;

                        var deltaMessage = "TOTAL";

                        if (!deltaTimes.ContainsKey(deltaMessage)) deltaTimes.TryAdd(deltaMessage, deltaTime);

                        deltaTimes[deltaMessage] = (deltaTimes[deltaMessage] * 0.95) + (deltaTime * 0.05);

                        // Remove the previous times for this thread
                        newMessage = (0.0, "", 0.0);
                        break;
                    default:
                        // Delta message serves a code for a section of the code
                        // the times with the same delta message will be averaged and displayed
                        // together
                        deltaMessage = (previousMessage.message ?? "").PadRight(20) + "->" + message;
                        deltaTime = newMessage.time - previousMessage.time;

                        if (!deltaTimes.ContainsKey(deltaMessage))    deltaTimes.TryAdd(deltaMessage, deltaTime);

                        deltaTimes[deltaMessage] = (deltaTimes[deltaMessage] * 0.99) + (deltaTime * 0.01);
                        break;
                }
                
                previousMessages[threadID] = newMessage;
            }
        }

        /// <summary>
        /// Tracks the time from the begining of the frame processing.
        /// </summary>
        public static void TrackTimeBeginingFrame() => TrackTime("FRAME START");

        /// <summary>
        /// Tracks the time to the end of the frame processing.
        /// </summary>
        public static void TrackTimeEndFrame() => TrackTime("FRAME FINISH");
    }
}