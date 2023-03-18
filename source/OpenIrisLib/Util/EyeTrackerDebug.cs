//-----------------------------------------------------------------------
// <copyright file="EyeTrackerDebug.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
            var numberOfTables = 50;

            deltaTimes = new ConcurrentDictionary<string, (long, double)>();
            previousPeriods = new (string, double,  double, double)[numberOfTables];
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
        private static readonly ConcurrentDictionary<string, (long count, double avgTime)> deltaTimes;

        /// <summary>
        /// For each thread (ThreadID is the key of the dictionary) saves
        /// the last message tracked, the time it happened, and the time of the begining of the frame.
        /// </summary>
        private static readonly (string periodName, double beginTime,  double endTime, double frameStartTime)[] previousPeriods;

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
            var maxCount = deltaTimes.Max(d => d.Value.count);

            //var sortedDict = from entry in deltaTimes orderby entry.Value.avgBeginTime ascending select entry;

            string s = string.Format("{0,50} : {1,8:0.0}", "Interva from -> to", "Avg time (ms)") + "\r\n" + "\r\n";
            foreach (var time in deltaTimes)
            {
                if (time.Value.count > 0.2 * maxCount | time.Key == "TOTAL processing")
                {
                    s = s + string.Format("{0,50} : {1,8:0.0} ", time.Key, time.Value.avgTime) + "\r\n";
                }
            }

            return s;
        }

        /// <summary>
        /// Tracks a partial time.
        /// </summary>
        /// <param name="timePeriodName">Message describing the partial time step.</param>
        public static void TrackProcessingTime(string timePeriodName)
        {
            if (DEBUG)
            {
                var ID = Thread.CurrentThread.ManagedThreadId % previousPeriods.Length;

                var newPeriod = (
                    periodName: timePeriodName,
                    beginTime: previousPeriods[ID].endTime,
                    endTime: EyeTrackerDebug.TimeElapsed.TotalMilliseconds,
                    previousPeriods[ID].frameStartTime);

                var deltaTime = newPeriod.endTime - newPeriod.beginTime;
                var deltaMessage = String.Format("{0,-23} -> {1,23}", previousPeriods[ID].periodName, timePeriodName);

                if (!deltaTimes.ContainsKey(deltaMessage))
                {
                    deltaTimes.TryAdd(deltaMessage, (1, deltaTime));
                }

                var avgTime = deltaTimes[deltaMessage].avgTime * 0.99 + deltaTime * 0.01;
                var count = deltaTimes[deltaMessage].count + 1;

                deltaTimes[deltaMessage] = (count, avgTime);

                previousPeriods[ID] = newPeriod;
            }
        }

        /// <summary>
        /// Tracks the time from the begining of the frame processing.
        /// </summary>
        public static void TrackTimeBeginPipeline(Eye whichEye, ImageEyeTimestamp timestamp)
        {
            var ID = Thread.CurrentThread.ManagedThreadId % previousPeriods.Length;

            previousPeriods[ID] = (
                "FRAME GRABBED", 
                timestamp.TimeGrabbed * 1000.0, 
                timestamp.TimeGrabbed * 1000.0, 
                EyeTrackerDebug.TimeElapsed.TotalMilliseconds);

            TrackProcessingTime("PIPELINE START " + whichEye);
        }

        /// <summary>
        /// Tracks the time to the end of the frame processing.
        /// </summary>
        public static void TrackTimeEndPipeline()
        {
            var ID = Thread.CurrentThread.ManagedThreadId % previousPeriods.Length;

            previousPeriods[ID] = (
                "PIPELINE START",
                previousPeriods[ID].frameStartTime,
                previousPeriods[ID].frameStartTime,
                previousPeriods[ID].frameStartTime);

            TrackProcessingTime("PIPELINE FINISH");
        }
    }
}