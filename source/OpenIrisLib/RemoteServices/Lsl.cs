//-----------------------------------------------------------------------
// <copyright file="Lsl.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Minimal P/Invoke bindings for the Lab Streaming Layer native library (lsl.dll).
    ///
    /// Only the small subset of the C API needed to publish an outlet of double samples is
    /// declared here. The native library (lsl.dll) must be present next to the executable.
    /// See https://github.com/sccn/liblsl for the full API.
    /// </summary>
    internal static class Lsl
    {
        /// <summary>
        /// Name of the native library. The 64 bit Windows build of liblsl 1.x ships as "lsl.dll".
        /// </summary>
        private const string LibraryName = "lsl";

        /// <summary>
        /// Channel format constants (lsl_channel_format_t). Only double64 is used here.
        /// </summary>
        public enum ChannelFormat
        {
            Float32 = 1,
            Double64 = 2,
            String = 3,
            Int32 = 4,
            Int16 = 5,
            Int8 = 6,
            Int64 = 7,
            Undefined = 0,
        }

        /// <summary>
        /// Reads the local high resolution clock used to timestamp samples.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double lsl_local_clock();

        /// <summary>
        /// Creates a new stream info object describing the stream.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr lsl_create_streaminfo(string name, string type, int channel_count, double nominal_srate, ChannelFormat channel_format, string source_id);

        /// <summary>
        /// Destroys a previously created stream info object.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lsl_destroy_streaminfo(IntPtr info);

        /// <summary>
        /// Gets the extended description element of a stream info (for adding channel metadata).
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lsl_get_desc(IntPtr info);

        /// <summary>
        /// Appends a child node to an XML element.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr lsl_append_child(IntPtr e, string name);

        /// <summary>
        /// Appends a child node holding a value to an XML element.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr lsl_append_child_value(IntPtr e, string name, string value);

        /// <summary>
        /// Creates an outlet that makes the stream available on the network.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lsl_create_outlet(IntPtr info, int chunk_size, int max_buffered);

        /// <summary>
        /// Destroys an outlet, removing the stream from the network.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lsl_destroy_outlet(IntPtr outlet);

        /// <summary>
        /// Pushes a single sample (array of doubles) with an explicit timestamp.
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lsl_push_sample_dtp(IntPtr outlet, double[] data, double timestamp, int pushthrough);
    }
}
