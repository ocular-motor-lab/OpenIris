//-----------------------------------------------------------------------
// <copyright file="PluginViews.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Forms;
    using OpenIris.Calibration;

    /// <summary>
    /// Window of an eye tracking system in the Windows user interface. Plugins export one
    /// ([Export(typeof(IEyeTrackingSystemWindow))]) to give their system a window, and the system
    /// itself stays free of any user interface library. Derive from <see cref="EyeTrackingSystemWindow{TSystem}"/>.
    /// </summary>
    public interface IEyeTrackingSystemWindow
    {
        /// <summary>
        /// Whether this is the window of the system.
        /// </summary>
        bool IsFor(EyeTrackingSystemBase system);

        /// <summary>
        /// Opens the window when tracking starts. OpenIris closes it when tracking stops.
        /// </summary>
        /// <returns>The window, or null to show none.</returns>
        Form? Open(EyeTrackingSystemBase system);
    }

    /// <summary>
    /// Window of an eye tracking system of type TSystem. Export it with
    /// [Export(typeof(IEyeTrackingSystemWindow))].
    /// </summary>
    public abstract class EyeTrackingSystemWindow<TSystem> : IEyeTrackingSystemWindow
        where TSystem : EyeTrackingSystemBase
    {
        /// <inheritdoc/>
        public bool IsFor(EyeTrackingSystemBase system) => system is TSystem;

        /// <inheritdoc/>
        public Form? Open(EyeTrackingSystemBase system) => Open((TSystem)system);

        /// <summary>
        /// Opens the window when tracking starts. OpenIris closes it when tracking stops.
        /// </summary>
        /// <returns>The window, or null to show none.</returns>
        protected abstract Form? Open(TSystem system);
    }

    /// <summary>
    /// Panel of a calibration method, shown in the Calibration tab while calibrating. Plugins export
    /// one ([Export(typeof(ICalibrationPanel))]). Derive from <see cref="CalibrationPanel{TPipeline}"/>.
    /// </summary>
    public interface ICalibrationPanel
    {
        /// <summary>
        /// Whether this is the panel of the calibration method.
        /// </summary>
        bool IsFor(CalibrationPipelineBase pipeline);

        /// <summary>
        /// Creates the panel. It must be a UserControl.
        /// </summary>
        ICalibrationUIControl? Create(CalibrationPipelineBase pipeline);
    }

    /// <summary>
    /// Panel of a calibration method of type TPipeline. Export it with [Export(typeof(ICalibrationPanel))].
    /// </summary>
    public abstract class CalibrationPanel<TPipeline> : ICalibrationPanel
        where TPipeline : CalibrationPipelineBase
    {
        /// <inheritdoc/>
        public bool IsFor(CalibrationPipelineBase pipeline) => pipeline is TPipeline;

        /// <inheritdoc/>
        public ICalibrationUIControl? Create(CalibrationPipelineBase pipeline) => Create((TPipeline)pipeline);

        /// <summary>
        /// Creates the panel. It must be a UserControl.
        /// </summary>
        protected abstract ICalibrationUIControl? Create(TPipeline pipeline);
    }

    /// <summary>
    /// Finds the windows and panels that OpenIris and the plugins provide for the Windows user interface.
    /// </summary>
    public static class PluginViews
    {
        private static readonly ICalibrationPanel[] builtInCalibrationPanels =
        {
            new CalibrationPipelineManualPanel(),
            new CalibrationRemoteLinearPanel(),
        };

        /// <summary>
        /// Opens the window of the eye tracking system, if it has one.
        /// </summary>
        public static Form? OpenSystemWindow(EyeTrackingSystemBase? system)
        {
            if (system is null) return null;

            return EyeTrackerPluginManager.GetExports<IEyeTrackingSystemWindow>()
                .FirstOrDefault(window => window.IsFor(system))?
                .Open(system);
        }

        /// <summary>
        /// Creates the panel of the calibration, if its method has one.
        /// </summary>
        public static ICalibrationUIControl? CreateCalibrationPanel(CalibrationSession? session)
        {
            if (session is null) return null;

            // Calibration methods can still create their panel themselves.
            return session.GetCalibrationUI()
                ?? builtInCalibrationPanels
                    .Concat(EyeTrackerPluginManager.GetExports<ICalibrationPanel>())
                    .FirstOrDefault(panel => panel.IsFor(session.Pipeline))?
                    .Create(session.Pipeline);
        }

        /// <summary>
        /// Creates the menu items for the commands of the eye tracking system.
        /// </summary>
        public static ToolStripMenuItem[]? CreateMenuItems(EyeTrackingSystemBase? system)
        {
            var commands = system?.GetCommands();
            if (commands is null || commands.Length == 0) return null;

            return commands.Select(command =>
            {
                var item = new ToolStripMenuItem(command.Name);
                if (!string.IsNullOrEmpty(command.Shortcut))
                {
                    try
                    {
                        item.ShortcutKeys = (Keys)new KeysConverter().ConvertFromInvariantString(command.Shortcut);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidEnumArgumentException)
                    {
                        Trace.WriteLine($"ERROR: shortcut '{command.Shortcut}' of '{command.Name}' is not valid.");
                    }
                }
                item.Click += (o, e) => command.Execute();
                return item;
            }).ToArray();
        }
    }
}
