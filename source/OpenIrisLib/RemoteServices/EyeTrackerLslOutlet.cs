//-----------------------------------------------------------------------
// <copyright file="EyeTrackerLslOutlet.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;

    /// <summary>
    /// Core service that streams every processed frame of eye tracking data through a Lab
    /// Streaming Layer (LSL) outlet. It is a sibling of <see cref="EyeTrackerUdpListener"/> and
    /// <see cref="EyeTrackerTcpListener"/>: an always-available output that runs in parallel with
    /// whatever eye tracking system is selected. It is enabled / disabled at runtime through the
    /// <see cref="EyeTrackerSettings.StreamToLsl"/> setting.
    ///
    /// It listens to <see cref="EyeTracker.NewDataAndImagesAvailable"/> and pushes one sample per
    /// frame. The outlet is created lazily the first time streaming is enabled and torn down when
    /// it is disabled, so toggling the setting takes effect without restarting tracking.
    /// </summary>
    internal class EyeTrackerLslOutlet : IDisposable
    {
        private readonly EyeTracker eyeTracker;
        private readonly object outletLock = new object();

        private IntPtr outlet = IntPtr.Zero;
        private int channelCount;
        private bool nativeLibraryMissing;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackerLslOutlet"/> class.
        /// </summary>
        /// <param name="eyeTracker">The eye tracker whose data should be streamed.</param>
        public EyeTrackerLslOutlet(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
        }

        /// <summary>
        /// Starts listening for new data. The actual LSL outlet is only created once streaming is
        /// enabled in the settings.
        /// </summary>
        public void Start()
        {
            eyeTracker.NewDataAndImagesAvailable += OnNewDataAndImagesAvailable;
        }

        /// <summary>
        /// Stops listening and tears down the outlet if it exists.
        /// </summary>
        public void Stop()
        {
            eyeTracker.NewDataAndImagesAvailable -= OnNewDataAndImagesAvailable;
            DestroyOutlet();
        }

        private void OnNewDataAndImagesAvailable(object sender, EyeTrackerImagesAndData imagesAndData)
        {
            // If the native library is not available there is nothing we can do, do not keep trying.
            if (nativeLibraryMissing) return;

            var data = imagesAndData?.Data;
            if (data is null) return;

            if (!eyeTracker.Settings.StreamToLsl)
            {
                // Streaming disabled: make sure no outlet is advertised.
                if (outlet != IntPtr.Zero) DestroyOutlet();
                return;
            }

            var sample = data.GetDataArray();

            lock (outletLock)
            {
                if (outlet == IntPtr.Zero)
                {
                    if (!TryCreateOutlet(sample.Length)) return;
                }

                // The number of channels is fixed once the outlet is created. This should never
                // change frame to frame, but guard against it just in case.
                if (sample.Length != channelCount) return;

                try
                {
                    Lsl.lsl_push_sample_dtp(outlet, sample, Lsl.lsl_local_clock(), 1);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error pushing LSL sample: " + ex.Message);
                }
            }
        }

        private bool TryCreateOutlet(int numberOfChannels)
        {
            try
            {
                var name = string.IsNullOrEmpty(eyeTracker.Settings.LslStreamName) ? "OpenIris" : eyeTracker.Settings.LslStreamName;
                var sourceId = name + "@" + Environment.MachineName;

                // Irregular sample rate: the frame rate is not guaranteed to be constant.
                var info = Lsl.lsl_create_streaminfo(name, "Gaze", numberOfChannels, 0.0, Lsl.ChannelFormat.Double64, sourceId);
                if (info == IntPtr.Zero)
                {
                    Trace.WriteLine("Error creating LSL stream info.");
                    return false;
                }

                AppendChannelLabels(info, numberOfChannels);

                outlet = Lsl.lsl_create_outlet(info, 0, 360);
                channelCount = numberOfChannels;

                // The outlet keeps its own copy of the stream info.
                Lsl.lsl_destroy_streaminfo(info);

                if (outlet == IntPtr.Zero)
                {
                    Trace.WriteLine("Error creating LSL outlet.");
                    return false;
                }

                Trace.WriteLine($"LSL outlet '{name}' created with {numberOfChannels} channels.");
                return true;
            }
            catch (DllNotFoundException ex)
            {
                Trace.WriteLine("LSL native library (lsl.dll) not found, LSL streaming disabled: " + ex.Message);
                nativeLibraryMissing = true;
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error creating LSL outlet: " + ex.Message);
                return false;
            }
        }

        private static void AppendChannelLabels(IntPtr info, int numberOfChannels)
        {
            var labels = EyeTrackerData.GetStringHeader().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var desc = Lsl.lsl_get_desc(info);
            var channels = Lsl.lsl_append_child(desc, "channels");

            for (var i = 0; i < numberOfChannels; i++)
            {
                var channel = Lsl.lsl_append_child(channels, "channel");
                Lsl.lsl_append_child_value(channel, "label", i < labels.Length ? labels[i] : "Channel" + i);
            }
        }

        private void DestroyOutlet()
        {
            lock (outletLock)
            {
                if (outlet != IntPtr.Zero)
                {
                    try
                    {
                        Lsl.lsl_destroy_outlet(outlet);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Error destroying LSL outlet: " + ex.Message);
                    }
                    finally
                    {
                        outlet = IntPtr.Zero;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
