//-----------------------------------------------------------------------
// <copyright file="EyeTrackerGuiViewModel.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    using System;
    using System.Drawing;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;
    using Emgu.CV;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// IDEAS from:
    /// http://www.ageektrapped.com/blog/using-the-command-pattern-in-windows-forms-clients/ http://www.codeguru.com/csharp/.net/net_general/patterns/article.php/c15663/Implement-a-Command-Pattern-Using-C.htm
    /// 2019
    /// Modified a lot to use delegates instead of a single class per command
    /// </remarks>
    public sealed class EyeTrackerGuiViewModel
    {
        private EyeTracker eyeTracker;

        public EyeTrackerSettings Settings => eyeTracker.Settings;

        public EyeTrackerUICommand StartTrackingCommand { get; }
        public EyeTrackerUICommand PlayVideoCommand { get; }
        public EyeTrackerUICommand ProcessVideoCommand { get; }
        public EyeTrackerUICommand BatchProcessVideoCommand { get; }
        public EyeTrackerUICommand StopCommand { get; }
        public EyeTrackerUICommand StartRecordingCommand { get; }
        public EyeTrackerUICommand StopRecordingCommand { get; }
        public EyeTrackerUICommand StartCalibrationCommand { get; }
        public EyeTrackerUICommand CancelCalibrationCommand { get; }
        public EyeTrackerUICommand ResetCalibrationCommand { get; }
        public EyeTrackerUICommand ResetReferenceCommand { get; }
        public EyeTrackerUICommand LoadCalibrationCommand { get; }
        public EyeTrackerUICommand SaveCalibrationCommand { get; }
        public EyeTrackerUICommand EditSettingsCommand { get; }
        public EyeTrackerUICommand StartCancelCalibrationCommand { get; }
        public EyeTrackerUICommand StartStopRecordingCommand { get; }

        public EyeTrackerUICommand CenterCamerasCommand { get; }
        public EyeTrackerUICommand MoveCamerasCommand { get; }

        public EyeTrackerUICommand ChangeDataFolderCommand { get; }

        public EyeTrackerUICommand TrimVideosCommand { get; private set; }
        public EyeTrackerUICommand ConvertVideoToRGBCommand { get; private set; }



        /// <summary>
        /// Data and images from the last frame.
        /// </summary>
        public EyeTrackerImagesAndData? LastDataAndImages { get; private set; }

        /// <summary>
        /// Initializes an instance of the EyeTrackerUIViewModel class.
        /// </summary>
        public EyeTrackerGuiViewModel(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
            this.eyeTracker.NewDataAndImagesAvailable += (o, dataAndimages) => LastDataAndImages = dataAndimages;
            this.eyeTracker.Settings.PropertyChanged += (o, e) =>
            {
                var needsRestartingAttributes = typeof(EyeTrackerSettings).GetProperty(e.PropertyName)?.
                    GetCustomAttributes(typeof(NeedsRestartingAttribute), false) as NeedsRestartingAttribute[];

                var needsRestarting = (needsRestartingAttributes?.Length > 0)
                                    ? needsRestartingAttributes[0].Value
                                    : false;

                if (needsRestarting && !this.eyeTracker.NotStarted)
                {
                    var result = MessageBox.Show(
                          "Changing the setting " + e.PropertyName + " requires to stop the tracking. Do you want to stop?",
                          "Do you want to stop?",
                          MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        this.eyeTracker.StopTracking();
                        return;
                    }
                }
            };

            StartTrackingCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    // If the eye tracking system has an specific, UI open it.
                    using var eyeTrackingSystemUI = this.eyeTracker.EyeTrackingSystem?.OpenEyeTrackingSystemUI;
                    await this.eyeTracker.StartTracking();
                },
                canExecute: () => this.eyeTracker.NotStarted);

            StopCommand = new EyeTrackerUICommand(
                execute: async _ => this.eyeTracker.StopTracking(),
                canExecute: () => this.eyeTracker.Tracking && !(this.eyeTracker.RecordingSession?.Stopping ?? false));

            PlayVideoCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    var options = SelectVideoDialog.SelectVideo(this.eyeTracker.Settings);
                    if (options is null) return;
                    await this.eyeTracker.PlayVideo(options);
                },
                canExecute: () => this.eyeTracker.NotStarted);

            ProcessVideoCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    var options = SelectVideoDialog.SelectVideoForProcessing(this.eyeTracker.Settings);
                    if (options is null) return;
                    await this.eyeTracker.ProcessVideo(options);
                },
                canExecute: () => this.eyeTracker.NotStarted);

            BatchProcessVideoCommand = new EyeTrackerUICommand(
                execute: async _ => (new BatchProcessing(this.eyeTracker)).Show(),
                canExecute: () => this.eyeTracker.NotStarted);

            StartRecordingCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    await this.eyeTracker.StartRecording();
                },
                canExecute: () => this.eyeTracker.Tracking && !this.eyeTracker.Recording && !(this.eyeTracker.RecordingSession?.Stopping ?? false));

            StopRecordingCommand = new EyeTrackerUICommand(
                execute: async _ => this.eyeTracker.StopRecording(),
                canExecute: () => this.eyeTracker.Recording && !this.eyeTracker.PostProcessing);

            StartStopRecordingCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (this.eyeTracker.Recording)
                        StopRecordingCommand.Execute(sender);
                    else
                        StartRecordingCommand.Execute(sender);
                },
                canExecute: () => this.eyeTracker.Tracking && !this.eyeTracker.PostProcessing && !(this.eyeTracker.RecordingSession?.Stopping ?? false));

            StartCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => await this.eyeTracker.StartCalibration(),
                canExecute: () => this.eyeTracker.Tracking && !this.eyeTracker.Calibrating);

            CancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => this.eyeTracker.CancelCalibration(),
                canExecute: () => this.eyeTracker.Calibrating);

            StartCancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (this.eyeTracker.Calibrating)
                        CancelCalibrationCommand.Execute(sender);
                    else
                        StartCalibrationCommand.Execute(sender);
                },
                canExecute: () => this.eyeTracker.Tracking);

            ResetCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ => this.eyeTracker.ResetCalibration(),
                canExecute: () => this.eyeTracker.Tracking && !this.eyeTracker.Calibrating);

            ResetReferenceCommand = new EyeTrackerUICommand(
                execute: async _ => await this.eyeTracker.ResetReference(),
                canExecute: () => this.eyeTracker.Tracking);

            LoadCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        AddExtension = true,
                        Filter = "Calibration files (*.cal)|*.cal"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    this.eyeTracker.LoadCalibration(dialog.FileName);
                },
                canExecute: () => !this.eyeTracker.Calibrating);

            SaveCalibrationCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using var dialog = new SaveFileDialog();
                    dialog.AddExtension = true;
                    dialog.Filter = "Calibration files (*.cal)|*.cal";
                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return;
                    }

                    await Task.Run(() => this.eyeTracker.Calibration.Save(dialog.FileName));
                },
                canExecute: () => !this.eyeTracker.Calibrating);

            EditSettingsCommand = new EyeTrackerUICommand(
                execute: async _ => EyeTrackerSettingsForm.Show(this.eyeTracker.Settings),
                canExecute: () => true);

            CenterCamerasCommand = new EyeTrackerUICommand(
                execute: async _ => this.eyeTracker.CenterEyes(),
                canExecute: () => (this.eyeTracker.ImageGrabber?.CamerasMovable ?? false) && !this.eyeTracker.Recording);

            MoveCamerasCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (!(sender is Button button)) return;
                    (var eye, var direction) = ((Eye, MovementDirection))button.Tag;
                    this.eyeTracker.MoveCamera(eye, direction);
                },
                canExecute: () => (this.eyeTracker.ImageGrabber?.CamerasMovable ?? false) && !this.eyeTracker.Recording);

            ChangeDataFolderCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    using var f = new FolderBrowserDialog
                    {
                        SelectedPath = this.eyeTracker.Settings.DataFolder,
                        ShowNewFolderButton = false
                    };
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        this.eyeTracker.Settings.DataFolder = f.SelectedPath;
                    }
                },
                canExecute: () => true);

            TrimVideosCommand = new EyeTrackerUICommand(
                execute: async _ =>
                {
                    var options = SelectVideoDialog.SelectVideoForProcessing(this.eyeTracker.Settings);

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