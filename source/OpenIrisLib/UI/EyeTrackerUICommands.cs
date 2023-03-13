//-----------------------------------------------------------------------
// <copyright file="EyeTrackerUICommands.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    using OpenIris.UI;
    using OpenIris.ImageGrabbing;
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Windows.Input;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// IDEAS from:
    /// http://www.ageektrapped.com/blog/using-the-command-pattern-in-windows-forms-clients/ http://www.codeguru.com/csharp/.net/net_general/patterns/article.php/c15663/Implement-a-Command-Pattern-Using-C.htm
    /// 2019
    /// Modified a lot to use delegates instead of a single class per command
    /// </remarks>
    public class EyeTrackerUICommands
    {
        public EyeTrackerUICommand StartTrackingCommand;
        public EyeTrackerUICommand StopCommand;
        public EyeTrackerUICommand PlayVideoCommand;
        public EyeTrackerUICommand ProcessVideoCommand;
        public EyeTrackerUICommand BatchProcessVideoCommand;
        public EyeTrackerUICommand StartRecordingCommand;
        public EyeTrackerUICommand StopRecordingCommand;
        public EyeTrackerUICommand StartStopRecordingCommand;
        public EyeTrackerUICommand StartCalibrationCommand;
        public EyeTrackerUICommand CancelCalibrationCommand;
        public EyeTrackerUICommand ResetCalibrationCommand;
        public EyeTrackerUICommand ResetReferenceCommand;
        public EyeTrackerUICommand LoadCalibrationCommand;
        public EyeTrackerUICommand SaveCalibrationCommand;
        public EyeTrackerUICommand EditSettingsCommand;
        public EyeTrackerUICommand StartCancelCalibrationCommand;
        public EyeTrackerUICommand CenterCamerasCommand;
        public EyeTrackerUICommand MoveCamerasCommand;
        public EyeTrackerUICommand ChangeDataFolderCommand;
        public EyeTrackerUICommand TrimVideosCommand;
        public EyeTrackerUICommand ConvertVideoToRGBCommand;
        public EyeTrackerUICommand ConvertVideoToMp4;

        public EyeTrackerUICommands(EyeTracker eyeTracker)
        {
            StartTrackingCommand = new EyeTrackerUICommand(
                    execute: async _ =>
                    {
                        // If the eye tracking system has an specific, UI open it.
                        using var trackingTask = eyeTracker.StartTracking();

                        using Form? eyeTrackingSystemUI = eyeTracker.EyeTrackingSystem?.OpenEyeTrackingSystemUI;

                        await trackingTask;
                    },
                    canExecute: () => eyeTracker.NotStarted);

            StopCommand = new EyeTrackerUICommand(
                execute: async _ => eyeTracker.StopTracking(),
                canExecute: () => eyeTracker.Tracking && !(eyeTracker.RecordingSession?.Stopping ?? false));
            
            PlayVideoCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    PlayVideoOptions? options = SelectVideoDialog.SelectVideo(eyeTracker.Settings);
                    if (options is null) return;
                    await eyeTracker.PlayVideo(options);
                },
                canExecute: () => eyeTracker.NotStarted); 
            
            ProcessVideoCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    ProcessVideoOptions? options = SelectVideoDialog.SelectVideoForProcessing(eyeTracker.Settings);
                    if (options is null) return;
                    await eyeTracker.ProcessVideo(options);
                },
                canExecute: () => eyeTracker.NotStarted);

            BatchProcessVideoCommand = new EyeTrackerUICommand(
                execute: async _ => (new BatchProcessing(eyeTracker)).Show(),
                canExecute: () => eyeTracker.NotStarted);

            StartRecordingCommand = new EyeTrackerUICommand(
                execute: async _ => await eyeTracker.StartRecording(),
                canExecute: () => eyeTracker.Tracking && !eyeTracker.Recording && !(eyeTracker.RecordingSession?.Stopping ?? false));

            StopRecordingCommand = new EyeTrackerUICommand(
                execute: async _ => eyeTracker.StopRecording(),
                canExecute: () => eyeTracker.Recording && !eyeTracker.PostProcessing); 
            
            StartStopRecordingCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (eyeTracker.Recording)
                        StopRecordingCommand.Execute(sender);
                    else
                        StartRecordingCommand.Execute(sender);
                },
                canExecute: () => eyeTracker.Tracking && !eyeTracker.PostProcessing && !(eyeTracker.RecordingSession?.Stopping ?? false));

            StartCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => await eyeTracker.StartCalibration(),
                canExecute: () => eyeTracker.Tracking && !eyeTracker.Calibrating);

            CancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => eyeTracker.CancelCalibration(),
                canExecute: () => eyeTracker.Calibrating);

            ResetCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => eyeTracker.ResetCalibration(),
                canExecute: () => eyeTracker.Tracking && !eyeTracker.Calibrating);

            ResetReferenceCommand = new EyeTrackerUICommand(
                    execute: async _ => await eyeTracker.ResetReference(),
                    canExecute: () => eyeTracker.Tracking);
            LoadCalibrationCommand = new EyeTrackerUICommand(
                    execute: async _ =>
                    {
                        using OpenFileDialog dialog = new OpenFileDialog
                        {
                            AddExtension = true,
                            Filter = "Calibration files (*.cal)|*.cal"
                        };
                        if (dialog.ShowDialog() != DialogResult.OK) return;

                        eyeTracker.LoadCalibration(dialog.FileName);
                    },
                    canExecute: () => !eyeTracker.Calibrating);

            LoadCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using OpenFileDialog dialog = new OpenFileDialog
                    {
                        AddExtension = true,
                        Filter = "Calibration files (*.cal)|*.cal"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    eyeTracker.LoadCalibration(dialog.FileName);
                },
                canExecute: () => !eyeTracker.Calibrating);

            SaveCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using SaveFileDialog dialog = new SaveFileDialog();
                    dialog.AddExtension = true;
                    dialog.Filter = "Calibration files (*.cal)|*.cal";
                    DialogResult result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return;
                    }

                    await Task.Run(() => eyeTracker.Calibration.Save(dialog.FileName));
                },
                canExecute: () => !eyeTracker.Calibrating);

            EditSettingsCommand = new EyeTrackerUICommand(
                execute: async _ => EyeTrackerSettingsForm.Show(eyeTracker.Settings),
                canExecute: () => true);

            StartCancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (eyeTracker.Calibrating)
                        CancelCalibrationCommand.Execute(sender);
                    else
                        StartCalibrationCommand.Execute(sender);
                },
                canExecute: () => eyeTracker.Tracking);

            CenterCamerasCommand = new EyeTrackerUICommand(
                execute: async _ => eyeTracker.CenterEyes(),
                canExecute: () => (eyeTracker.ImageGrabber?.CamerasMovable ?? false) && !eyeTracker.Recording);

            MoveCamerasCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (sender is not Button button) return;
                    (Eye eye, MovementDirection direction) = ((Eye, MovementDirection))button.Tag;
                    eyeTracker.MoveCamera(eye, direction);
                },
                canExecute: () => (eyeTracker.ImageGrabber?.CamerasMovable ?? false) && !eyeTracker.Recording);

            ChangeDataFolderCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using FolderBrowserDialog f = new FolderBrowserDialog
                    {
                        SelectedPath = eyeTracker.Settings.DataFolder,
                        ShowNewFolderButton = false
                    };
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        eyeTracker.Settings.DataFolder = f.SelectedPath;
                    }
                },
                canExecute: () => true);

            TrimVideosCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    var options = SelectVideoDialog.SelectVideoForProcessing(eyeTracker.Settings);
                    if (options?.VideoFileNames is null) return;

                    await VideoTools.TrimVideosCommandExecute(options);
                },
                canExecute: () => true);

            ConvertVideoToRGBCommand = new EyeTrackerUICommand(
                execute: _ => VideoTools.ConvertVideoToRGB(),
                canExecute: () => true);

            ConvertVideoToMp4 = new EyeTrackerUICommand(
                execute: _ => VideoTools.ConvertToMP4(),
                canExecute: () => true);
        }

        /// <summary>
        /// Parent abstract class for all eye tracker commands. The main objective of the command pattern
        /// here is to combine in one place the execution of the command and the control of when the
        /// command can be executed or not. The user interface elements will bind to the command to start
        /// its execution but will also be enabled or disabled automatically.
        /// </summary>
        public class EyeTrackerUICommand : ICommand
        {
            private static readonly Dictionary<object, EyeTrackerUICommand> bindings = new Dictionary<object, EyeTrackerUICommand>();

            static EyeTrackerUICommand()
            {
                Application.Idle += (o, e) =>
                {
                    foreach (var command in EyeTrackerUICommand.bindings.Values)
                    {
                        command.CanExecute();
                    }
                };
            }

            private readonly Func<object?, Task> executeMethod;
            private readonly Func<bool> canExecuteMethod;

            private bool enabled;

            /// <summary>
            /// Initializes an instance of the class EyeTrackerCommand for commands that do not need parameters
            /// </summary>
            /// <param name="execute">Execute method.</param>
            /// <param name="canExecute">Can execute method.</param>
            public EyeTrackerUICommand(Func<object?, Task> execute, Func<bool> canExecute)
            {
                executeMethod = execute;
                canExecuteMethod = canExecute;
            }

            /// <summary>
            /// Event from when the can execute changed.
            /// </summary>
            public event EventHandler? CanExecuteChanged;

            /// <summary>
            /// Binds the command to a control.
            /// </summary>
            /// <param name="control"></param>
            public void Bind(Control control)
            {
                if (control is null) throw new ArgumentNullException(nameof(control));

                bindings.Add(control, this);

                control.Enabled = enabled;
                control.Click += (o, e) => Execute(o);

                CanExecuteChanged += (ob, e) => control.Enabled = enabled;
            }

            /// <summary>
            /// Binds the command to a menu item.
            /// </summary>
            /// <param name="menuItem"></param>
            public void Bind(ToolStripMenuItem menuItem)
            {
                if (menuItem is null) throw new ArgumentNullException(nameof(menuItem));

                bindings.Add(menuItem, this);

                menuItem.Enabled = enabled;
                menuItem.Click += (o, e) => Execute(o);

                CanExecuteChanged += (ob, e) => menuItem.Enabled = enabled;
            }


            /// <summary>
            /// Checks if the command can be executed.
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns></returns>
            public bool CanExecute(object? parameter = null)
            {
                var value = canExecuteMethod();

                if (enabled != value)
                {
                    enabled = value;
                    CanExecuteChanged?.Invoke(this, new EventArgs());
                }

                return value;
            }

            /// <summary>
            /// Execute the command.
            /// </summary>
            /// <param name="parameter"></param>
            public async void Execute(object? parameter = null)
            {
                try
                {
                    await executeMethod(parameter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "ERROR: " + ex.Message,
                        "ERROR",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    System.Diagnostics.Trace.WriteLine("ERROR: " + ex.ToString());
                }
            }
        }
    }
}