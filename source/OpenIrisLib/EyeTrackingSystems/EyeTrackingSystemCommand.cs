//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemCommand.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;

    /// <summary>
    /// An action of an eye tracking system that the user can run, for example "Auto exposure once".
    /// It doesn't depend on any user interface library: the Windows user interface shows it in the
    /// System menu, and other interfaces can show it their own way.
    /// </summary>
    public sealed class EyeTrackingSystemCommand
    {
        /// <summary>
        /// Creates a command.
        /// </summary>
        /// <param name="name">Name shown to the user.</param>
        /// <param name="execute">What the command does. Runs on the user interface thread.</param>
        /// <param name="shortcut">Optional keyboard shortcut, such as "F10" or "Ctrl+E".</param>
        public EyeTrackingSystemCommand(string name, Action execute, string? shortcut = null)
        {
            Name = name;
            Execute = execute;
            Shortcut = shortcut;
        }

        /// <summary>
        /// Name shown to the user.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// What the command does.
        /// </summary>
        public Action Execute { get; }

        /// <summary>
        /// Keyboard shortcut, such as "F10" or "Ctrl+E", or null.
        /// </summary>
        public string? Shortcut { get; }
    }
}
