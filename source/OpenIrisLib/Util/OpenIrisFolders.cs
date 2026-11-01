//-----------------------------------------------------------------------
// <copyright file="OpenIrisFolders.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.IO;

    /// <summary>
    /// The folders of an OpenIris installation:
    /// <code>
    /// OpenIris.exe, OpenIris.exe.config, EyeTrackerSettings.xml
    /// lib\           the libraries (OpenIrisLib.dll, EmguCV...), found through probing in OpenIris.exe.config
    /// Plugins\       the plugins
    /// Logs\          the log files
    /// SampleVideos\  the videos the Simulation system plays
    /// </code>
    /// </summary>
    public static class OpenIrisFolders
    {
        /// <summary>
        /// Folder of the program (OpenIris.exe), or of the tests.
        /// </summary>
        public static string Program => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Folder of the libraries, where OpenIrisLib.dll is.
        /// </summary>
        public static string Libraries => Path.GetDirectoryName(typeof(OpenIrisFolders).Assembly.Location);

        /// <summary>
        /// Folder of the plugins.
        /// </summary>
        public static string Plugins => Path.Combine(Program, "Plugins");

        /// <summary>
        /// Folder of the log files. Created if needed.
        /// </summary>
        public static string Logs => Directory.CreateDirectory(Path.Combine(Program, "Logs")).FullName;
    }
}
