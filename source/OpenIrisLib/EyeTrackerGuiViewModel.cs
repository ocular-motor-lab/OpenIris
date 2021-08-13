//-----------------------------------------------------------------------
// <copyright file="EyeTrackerGuiViewModel.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
    public sealed class EyeTrackerGuiViewModel : IDisposable
    {
        public EyeTracker EyeTracker { get; }

        public EyeTrackerSettings Settings => EyeTracker.Settings;

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
        public EyeTrackerGuiViewModel()
        {
            try
            {
                EyeTracker = EyeTracker.Start();
            }
            catch (PluginManagerException ex)
            {
                MessageBox.Show("Error initializing, trying safe mode (no plugins). " + ex.Message);
                EyeTracker = EyeTracker.Start(safeMode: true);
            }

            EyeTracker.NewDataAndImagesAvailable += (o, dataAndimages) => LastDataAndImages = dataAndimages;
            EyeTracker.Settings.PropertyChanged += (o, e) =>
            {
                var needsRestartingAttributes = typeof(EyeTrackerSettings).GetProperty(e.PropertyName)?.
                    GetCustomAttributes(typeof(NeedsRestartingAttribute), false) as NeedsRestartingAttribute[];

                var needsRestarting = (needsRestartingAttributes?.Length > 0)
                                    ? needsRestartingAttributes[0].Value
                                    : false;

                if (needsRestarting && !EyeTracker.NotStarted)
                {
                    var result = MessageBox.Show(
                          "Changing the setting " + e.PropertyName + " requires to stop the tracking. Do you want to stop?",
                          "Do you want to stop?",
                          MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        EyeTracker.StopTracking();
                        return;
                    }
                }
            };

            StartTrackingCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    // If the eye tracking system has an specific, UI open it.
                    using var eyeTrackingSystemUI = EyeTracker.EyeTrackingSystem?.OpenEyeTrackingSystemUI;
                    await EyeTracker.StartTracking();
                },
                canExecute: () => EyeTracker.NotStarted);

            StopCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTracker.StopTracking(),
                canExecute: () => EyeTracker.Tracking && !(EyeTracker.RecordingSession?.Stopping ?? false));

            PlayVideoCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    var options = SelectVideoDialog.SelectVideo(EyeTracker.Settings);
                    if (options is null) return;
                    await EyeTracker.PlayVideo(options);
                },
                canExecute: () => EyeTracker.NotStarted);

            ProcessVideoCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    var options = SelectVideoDialog.SelectVideoForProcessing(EyeTracker.Settings);
                    if (options is null) return;
                    await EyeTracker.ProcessVideo(options);
                },
                canExecute: () => EyeTracker.NotStarted);

            BatchProcessVideoCommand = new EyeTrackerUICommand(
                execute: async (_) => (new BatchProcessing(EyeTracker)).Show(),
                canExecute: () => EyeTracker.NotStarted);

            StartRecordingCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    var options = new RecordingOptions()
                    {
                        SessionName = EyeTracker.Settings.SessionName,
                        SaveRawVideo = EyeTracker.Settings.RecordVideo,
                        DecimateRatioRawVideo = EyeTracker.Settings.DecimateVideoRatio,
                        DataFolder = Settings?.DataFolder ?? "",
                        FrameRate = EyeTracker.ImageGrabber?.FrameRate ?? 0.0,
                    };

                    await EyeTracker.StartRecording(options);
                },
                canExecute: () => EyeTracker.Tracking && !EyeTracker.Recording && !(EyeTracker.RecordingSession?.Stopping ?? false));

            StopRecordingCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTracker.StopRecording(),
                canExecute: () => EyeTracker.Recording && !EyeTracker.PostProcessing);

            StartStopRecordingCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (EyeTracker.Recording)
                        StopRecordingCommand.Execute(sender);
                    else
                        StartRecordingCommand.Execute(sender);
                },
                canExecute: () => EyeTracker.Tracking && !EyeTracker.PostProcessing && !(EyeTracker.RecordingSession?.Stopping ?? false));

            StartCalibrationCommand = new EyeTrackerUICommand(
                execute: async (_) => await EyeTracker.StartCalibration(),
                canExecute: () => EyeTracker.Tracking && !EyeTracker.Calibrating);

            CancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTracker.CancelCalibration(),
                canExecute: () => EyeTracker.Calibrating);

            StartCancelCalibrationCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (EyeTracker.Calibrating)
                        CancelCalibrationCommand.Execute(sender);
                    else
                        StartCalibrationCommand.Execute(sender);
                },
                canExecute: () => EyeTracker.Tracking);

            ResetCalibrationCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTracker.ResetCalibration(),
                canExecute: () => EyeTracker.Tracking && !EyeTracker.Calibrating);

            ResetReferenceCommand = new EyeTrackerUICommand(
                execute: async (_) => await EyeTracker.ResetReference(),
                canExecute: () => EyeTracker.Tracking);

            LoadCalibrationCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        AddExtension = true,
                        Filter = "Calibration files (*.cal)|*.cal"
                    };
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    EyeTracker.LoadCalibration(dialog.FileName);
                },
                canExecute: () => !EyeTracker.Calibrating);

            SaveCalibrationCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    using var dialog = new SaveFileDialog();
                    dialog.AddExtension = true;
                    dialog.Filter = "Calibration files (*.cal)|*.cal";
                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return;
                    }

                    await Task.Run(() => EyeTracker.Calibration.Save(dialog.FileName));
                },
                canExecute: () => !EyeTracker.Calibrating);

            EditSettingsCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTrackerSettingsForm.Show(EyeTracker.Settings),
                canExecute: () => true);

            CenterCamerasCommand = new EyeTrackerUICommand(
                execute: async (_) => EyeTracker.CenterEyes(),
                canExecute: () => (EyeTracker.ImageGrabber?.CamerasMovable ?? false) && !EyeTracker.Recording);

            MoveCamerasCommand = new EyeTrackerUICommand(
                execute: async (object? sender) =>
                {
                    if (!(sender is Button button)) return;
                    (var eye, var direction) = ((Eye, MovementDirection))button.Tag;
                    EyeTracker.MoveCamera(eye, direction);
                },
                canExecute: () => (EyeTracker.ImageGrabber?.CamerasMovable ?? false) && !EyeTracker.Recording);

            ChangeDataFolderCommand = new EyeTrackerUICommand(
                execute: async (_) =>
                {
                    using var f = new FolderBrowserDialog
                    {
                        SelectedPath = EyeTracker.Settings.DataFolder,
                        ShowNewFolderButton = false
                    };
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        EyeTracker.Settings.DataFolder = f.SelectedPath;
                    }
                },
                canExecute: () => true);

            TrimVideosCommand = new EyeTrackerUICommand(
                execute: TrimVideosCommandExecute,
                canExecute: () => true);

            ConvertVideoToRGBCommand = new EyeTrackerUICommand(
                execute: ConvertVideoToRGBCommandExecute,
                canExecute: () => true);
        }

        /// <summary>
        /// Disposes reserouces.
        /// </summary>
        public void Dispose()
        {
            EyeTracker?.Dispose();
        }
        
        private async Task TrimVideosCommandExecute(object? sender)
        {
            var options = SelectVideoDialog.SelectVideoForProcessing(EyeTracker.Settings);

            if (options is null || options.VideoFileNames is null)
            {
                return;
            }

            var cancelled = false;

            var videoLeft = options.VideoFileNames[Eye.Left];
            var videoRight = options.VideoFileNames[Eye.Right];

            using var progressDialog = new ProgressBarDialog();

            using var videoReaderLeft = new VideoCapture(videoLeft);
            using var videoReaderRight = new VideoCapture(videoRight);
            using var videoWriterLeft = new VideoWriter(videoLeft + "trim.avi", 100, new Size(videoReaderLeft.Width, videoReaderLeft.Height), true);
            using var videoWriterRight = new VideoWriter(videoRight + "trim.avi", 100, new Size(videoReaderRight.Width, videoReaderRight.Height), true);


            progressDialog.Cancelled += (o, es) => cancelled = true;
            progressDialog.Show();

            var range = options.CustomRange;
            if (range.IsEmpty)
            {
                range = new Range(0, (long)Math.Round(videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount)));
            }

            var taskLeft = Task.Run(() =>
            {
                videoReaderLeft.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, (double)options.CustomRange.Begin);
                while (!cancelled)
                {
                    var img = videoReaderLeft.QueryFrame();
                    if (img is null)
                    {
                        return;
                    }

                    videoWriterLeft.Write(img);

                    if (videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) > range.End)
                        break;

                    var percent = (int)Math.Round((videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) - range.Begin) / (range.End - range.Begin) * 100);

                    progressDialog.Progress = percent;
                }
            });
            var taskRight = Task.Run(() =>
            {
                videoReaderLeft.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, (double)options.CustomRange.Begin);
                while (!cancelled)
                {
                    var img = videoReaderRight.QueryFrame();
                    if (img is null)
                    {
                        return;
                    }

                    videoWriterRight.Write(img);
                    if (videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) > range.End)
                        break;
                }
            });

            await Task.WhenAll(taskLeft, taskRight);
            progressDialog.Close();
        }

        private async Task ConvertVideoToRGBCommandExecute(object? sender)
        {
            using (var dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                using (var videoReader = new VideoCapture(dialog.FileName))
                using (var videoWriter = new VideoWriter(dialog.FileName + "out2.avi", 100, new Size(videoReader.Width, videoReader.Height), true))
                {
                    while (true)
                    {
                        var img = videoReader.QueryFrame();
                        if (img is null)
                        {
                            return;
                        }

                        videoWriter.Write(img);
                    }
                }
            }
        }
    }
}