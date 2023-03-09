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

        public EyeTrackerUICommands(EyeTracker eyeTracker)
        {
            StartTrackingCommand = new EyeTrackerUICommand(
                    execute: async _ =>
                    {
                        // If the eye tracking system has an specific, UI open it.
                        using Form? eyeTrackingSystemUI = eyeTracker.EyeTrackingSystem?.OpenEyeTrackingSystemUI;
                        await eyeTracker.StartTracking();
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
                execute: _ => VideoTools.ConvertVideoToRGBCommandExecute(),
                canExecute: () => true);
        }
    }
}