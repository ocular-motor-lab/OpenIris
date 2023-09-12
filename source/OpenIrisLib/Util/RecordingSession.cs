﻿//-----------------------------------------------------------------------
// <copyright file="EyeTrackerRecorder.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using Emgu.CV;
    using Emgu.CV.Ocl;
    using Emgu.CV.Structure;
    using OpenIris.UI;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    /// Class that controls data and video recording. The object has to take care of syncronizing all
    /// the recordings. There are 5 main threads interacting.
    /// 1) Thread recording raw frames to disk
    /// 2) Thread recording processed frames to disk
    /// 3) Grabbing thread sending the raw frames through the eye tracker event
    /// 4) Processing thread sending the frames through the eye tracker event
    /// 5) Main eye tracker thread sending the start and stop recording commands The raw video
    /// recording and the data recording should have the same number of frames. At least, start and
    /// stop with the same frame. There can be frames dropped in the data that were not dropped in
    /// the video.
    /// </summary>
    public sealed class RecordingSession : IDisposable
    {
        private readonly RecordingOptions options;

        private readonly Consumer<(ImageEye?[] images, double frameRate)> rawFramesRecorder;
        private readonly Consumer<(EyeTrackerImagesAndData images, double frameRate)> processedFramesRecorder;
        private readonly Consumer<EyeTrackerEvent> eventRecorder;

        private VideoWriter?[]? rawVideoWriters;
        private VideoWriter? processedVideoWriter;
        private StreamWriter? eventFile;
        private StreamWriter? dataFile;
        private EyeTrackerLog? log;

        private Range RangeToRecord;
        private Task? recordingTask;
        private bool started;

        /// <summary>
        /// Initializes a recording.
        /// </summary>
        /// <param name="recordingOptions">Options of the recording.</param>
        internal RecordingSession(RecordingOptions recordingOptions)
        {
            options = recordingOptions;

            // Prepare file names
            TimeRecordingStarted = DateTime.Now;
            var session = $"{options.SessionName}-{TimeRecordingStarted:yyyyMMMdd-HHmmss}";
            DataFileName = Path.Combine(options.DataFolder, session, session + ".txt");

            // Initialize consumers early so items can be added to the queue as soon as possible
            rawFramesRecorder = new (RecordGrabbedImages, 1000);
            processedFramesRecorder = new (RecordDataAndProcessedImages, 1000);
            eventRecorder = new (RecordEvent, 1000);
        }

        /// <summary>
        /// Filename of the datafile.
        /// </summary>
        public string DataFileName { get; }

        /// <summary>
        /// Gets the total time spent recording.
        /// </summary>
        public DateTime TimeRecordingStarted { get; }

        /// <summary>
        /// Gets the total time spent recording.
        /// </summary>
        public DateTime TimeRecordingStopped { get; private set; }

        /// <summary>
        /// Gets the total time since the recording started.
        /// </summary>
        public TimeSpan TimeRecorded => (!Stopping ? DateTime.Now : TimeRecordingStopped) - TimeRecordingStarted;

        /// <summary>
        /// Gets a value indicating weather the recording is stopping.
        /// </summary>
        public bool Stopping { get; private set; }

        /// <summary>
        /// Gets the total number of frames recorded.
        /// </summary>
        public int NumberFramesRecordedInVideo => rawFramesRecorder.TryAddedCount;

        /// <summary>
        /// Gets the total number of frames dropped.
        /// </summary>
        public int NumberFramesDroppedInVideo => rawFramesRecorder.DroppedCount;

        /// <summary>
        /// Gets the total number of frames recorded in data file.
        /// </summary>
        public int NumberFramesRecordedInDataFile => processedFramesRecorder.TryAddedCount;

        /// <summary>
        /// Gets the total number of frames dropped in data file.
        /// </summary>
        public int NumberFramesDroppedInDataFile => processedFramesRecorder.DroppedCount;

        /// <summary>
        /// Gets the number of the last frame recorded in the video.
        /// </summary>
        public int NumberFrameLastInVideo => (int)(rawFramesRecorder.LastItemAdded);

        /// <summary>
        /// Gets the number of the last frame recorded in the data file.
        /// </summary>
        public int NumberFrameLastInDataFile => (int)(processedFramesRecorder.LastItemAdded);

        /// <summary>
        /// Gets a string with a status message regarding the recording.
        /// </summary>
        public string RecordingStatus
        {
            get => $"Recording {TimeRecorded:hh\\:mm\\:ss\\.f} "+
                $"[Video drops:{NumberFramesDroppedInVideo}, Data drops:{NumberFramesDroppedInDataFile}]";
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        /// <param name="calibration">Current calibration.</param>
        /// <param name="settings">Current settings of the eye tracker.</param>
        internal async Task Start(CalibrationParameters calibration, EyeTrackerSettings settings)
        {
            if (started) throw new InvalidOperationException("Cannot start a new recording. Another recording is in process.");
            started = true;

            settings.LastRecordedFile = DataFileName;

            // RangeToRecord will keep track fo the range of frame numbers that need to be recorded.
            // Initially is unknown. As soon as the first image frame comes that will be the begining
            // of the range the end of the range might be the last image frame recorded at the time
            // the order to stop recording is recieved. Or it can be set specifically at the stop
            // recording for some future frame number.
            RangeToRecord = new Range();

            try
            {
                var errorHandler = new TaskErrorHandler(Stop);
                log = EyeTrackerLog.Create(DataFileName.Replace(".txt", "-log.log"));

                // Create new folder and save calibration and settings
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DataFileName));
                    calibration.Save(DataFileName.Replace(".txt", ".cal"));
                    settings.Save(DataFileName.Replace(".txt", "-settings.xml"));
                });

                // Start the consumers. 
                recordingTask = Task.WhenAll(
                        rawFramesRecorder.Start().ContinueWith(errorHandler.HandleError),
                        processedFramesRecorder.Start().ContinueWith(errorHandler.HandleError),
                        eventRecorder.Start().ContinueWith(errorHandler.HandleError));

                await recordingTask;

                errorHandler.CheckForErrors();

                Trace.WriteLine($"Type of Recorder -> (Frames to record >> Frames not dropped >> Fames recorded)");
                Trace.WriteLine($"RawFramesRecorder -> ({rawFramesRecorder.TryAddedCount} >> {rawFramesRecorder.AddedCount} >> {rawFramesRecorder.ConsumedCount})");
                Trace.WriteLine($"ProcessedFramesRecorder -> ({processedFramesRecorder.TryAddedCount} >> {processedFramesRecorder.AddedCount} >> {processedFramesRecorder.ConsumedCount})");
                Trace.WriteLine($"EventRecorder -> ({eventRecorder.TryAddedCount} >> {eventRecorder.AddedCount} >> {eventRecorder.ConsumedCount})");
            }
            catch
            {
                Stop();
                throw;
            }
            finally
            {
                // No need for dispose method because all the disposable objects are created here or by threads started here
                // so if we get to this point or there is an error all objects will be disposed correctly.

                // Create new folder and save calibration and settings
                // Use a different thread so the UI does not freeze.
                await Task.Run(() =>
                {
                    if (rawVideoWriters != null) Array.ForEach(rawVideoWriters, v => v?.Dispose());
                    rawVideoWriters = null;

                    dataFile?.Dispose();
                    dataFile = null;

                    processedVideoWriter?.Dispose();
                    processedVideoWriter = null;

                    eventFile?.Dispose();
                    eventFile = null;

                    log?.Close();
                    log?.Dispose();
                    log = null;
                });

                Stopping = false;
            }
        }

        /// <summary>
        /// Waits for the recording to finish.
        /// </summary>
        /// <returns></returns>
        internal async Task Wait()
        {
            await (recordingTask ?? Task.CompletedTask);
        }

        /// <summary>
        /// Stops recording as soon as possible. Only use in case of errors.
        /// Always preferred to use the other stop method that specifies the last frame number
        /// to record. That way all the files will contain the same number of frames.
        /// </summary>
        internal void Stop()
        {
            if (rawFramesRecorder is null) return;

            RangeToRecord = new Range(RangeToRecord.Begin, rawFramesRecorder.LastItemAdded);

            TimeRecordingStopped = DateTime.Now;
            Stopping = true;

            rawFramesRecorder?.Stop();
            processedFramesRecorder?.Stop();
            eventRecorder?.Stop();
        }

        /// <summary>
        /// Stop the recording when an specific frame number is reached.
        /// </summary>
        /// <param name="lastFrameNumberToRecord">Last frame number to be recorded.</param>
        internal void Stop(long lastFrameNumberToRecord)
        {
            if (rawFramesRecorder is null) return;

            RangeToRecord = new Range(RangeToRecord.Begin, lastFrameNumberToRecord);

            TimeRecordingStopped = DateTime.Now;
            Stopping = true;

            rawFramesRecorder?.Stop(RangeToRecord.End);
            processedFramesRecorder?.Stop(RangeToRecord.End);
            eventRecorder?.Stop();
        }

        /// <summary>
        /// Records new raw frames. If buffer is full it will drop the frame.
        /// </summary>
        /// <param name="images">Frames to be recorded.</param>
        /// <param name="frameRate">Frame rate of the image grabber.</param>
        /// <returns>True if the frames were recording, false if dropped.</returns>
        internal bool TryRecordImages(EyeCollection<ImageEye?> images, double frameRate)
        {
            if (rawFramesRecorder is null) throw new InvalidOperationException("Recorder not ready.");

            var frameNumber = images.GetFrameNumber();

            // If starting recording we must wait until we get the first raw image because they come
            // before the processed images by definition. If the range is not sent from outside.
            // Then, the begining of the range to record will be the current frame number. The end of
            // the range to record will be set when the stop recording arrives.
            if (RangeToRecord.IsEmpty) RangeToRecord = new Range(frameNumber, long.MaxValue);

            if (RangeToRecord.DoesNotContain(frameNumber)) return false;

            // Only recorded one of every decimateRatio frames
            if ((frameNumber % options.DecimateRatioRawVideo) != 0) return false;

            // Copy the images to avoid memory conflicts
            // with other threads working with the same images
            EyeCollection<ImageEye?> imagesCopy = new(images.Select(im => im?.Copy()));

            return rawFramesRecorder.TryAdd((imagesCopy, frameRate), frameNumber);
        }

        /// <summary>
        /// Records new processed images.
        /// </summary>
        /// <param name="newDataAndImages">Processed images to be saved.</param>
        /// <param name="frameRate">Frame rate of the image grabber.</param>
        /// <returns>True if the frames were recording, false if dropped.</returns>
        internal bool TryRecordImagesAndData(EyeTrackerImagesAndData newDataAndImages, double frameRate)
        {
            if (processedFramesRecorder is null) throw new InvalidOperationException("Recorder not ready.");

            if (RangeToRecord.DoesNotContain(newDataAndImages.FrameNumber)) return false;
            
            return processedFramesRecorder.TryAdd((newDataAndImages, frameRate), newDataAndImages.FrameNumber);
        }


        /// <summary>
        /// Records new event.
        /// </summary>
        /// <param name="eventMessage"></param>
        /// <param name="frameNumber"></param>
        /// <param name="data"></param>
        /// <returns>True if it could be added to the buffer.</returns>
        internal bool TryRecordEvent(string eventMessage, long frameNumber, object? data = null)
        {
            if (eventRecorder is null) throw new InvalidOperationException("Recorder not ready.");

            if (RangeToRecord.DoesNotContain(frameNumber)) return false;
            
            return eventRecorder.TryAdd(new EyeTrackerEvent(eventMessage, frameNumber, data), frameNumber);
        }

        private bool RecordGrabbedImages((ImageEye?[] images, double frameRate) imagesEye)
        {
            if (imagesEye.images == null) throw new ArgumentNullException(nameof(imagesEye));

            if (!options.SaveRawVideo) return false;
            
            // Create the videowriters the first time an image is consumed This is better so the set
            // up time of the consumer is fast and it can receive items in the queue very quickly.
            // Also, it allows for reading the frame size directly from the images, no need to pass
            // it as a parameter.
            rawVideoWriters ??= new EyeCollection<VideoWriter?>(
                    imagesEye.images.Select(im => (im is null) ? null :
                        new VideoWriter(
                           fileName: DataFileName.Replace(".txt", "-" + im.WhichEye + ".avi"),
                           compressionCode: 0,
                           fps: imagesEye.frameRate / options.DecimateRatioRawVideo,
                           size: imagesEye.images.GetFrameSize(),
                           isColor: false)).ToArray());

            // Record the images
            for (int i = 0; i < imagesEye.images.Length; i++)
            {
                var image = imagesEye.images[i];

                if (image is null) continue;

                rawVideoWriters[i]?.Write(image.Image.Mat);
            }
            return true;
        }

        private bool RecordDataAndProcessedImages((EyeTrackerImagesAndData dataAndImages, double frameRate) images)
        {
            //
            // Save the data to text file
            // 
            if (dataFile is null)
            {
                // Open the file and write the header the first time
                dataFile = new StreamWriter(DataFileName);
                dataFile.WriteLine(EyeTrackerData.GetStringHeader());
            }

            dataFile.WriteLine(images.dataAndImages.Data?.GetStringLine() ?? string.Empty);

            //
            // Save the images to a video file with data overlay
            // 

           // var images = dataAndImages.Images ?? new EyeCollection<ImageEye?>(null, null);
            var processedVideoOptions = options as ProcessedRecordingOptions;

            if (processedVideoOptions?.SaveProcessedVideo is true)
            {
                // Open the video
                processedVideoWriter ??= new VideoWriter(
                            fileName: DataFileName.Replace(".txt", ".avi"),
                            compressionCode: 0,
                            fps: images.frameRate,
                            size: images.dataAndImages.Images.GetFrameSize(),
                            isColor: true);

                // Save the processed video
                var imagesColor = images.dataAndImages.Images
                    .Where(im => im is not null)
                    .Where(im => processedVideoOptions.WhichEye == Eye.Both || im!.WhichEye == processedVideoOptions.WhichEye)
                    .Select(im => (Image: im!.Image.Convert<Bgr, byte>(), im!.EyeData)).ToArray();
                foreach ((var imageColor, var eyeData) in imagesColor)
                {
                    if (processedVideoOptions.IncreaseContrast) imageColor._EqualizeHist();

                    if (eyeData is null) continue;

                    var eyeModel = images.dataAndImages.Calibration.EyeCalibrationParameters[eyeData.WhichEye].EyePhysicalModel;
                    if (processedVideoOptions.AddPupilCross) ImageEyeBox.DrawCross(imageColor, eyeData);
                    if (processedVideoOptions.AddCRCross) ImageEyeBox.DrawCR(imageColor, eyeData);
                    if (processedVideoOptions.AddEyelids) ImageEyeBox.DrawEyelids(imageColor, eyeData, eyeModel);
                }

                using var imageForVideo = imagesColor.Count() switch
                {
                    1 => imagesColor[0].Image,
                    2 => CombineImages(imagesColor[0].Image, imagesColor[1].Image),
                    _ => throw new InvalidOperationException("Invalid option"),
                };

                processedVideoWriter.Write(imageForVideo.Mat);
            }
            return true;
        }

        private bool RecordEvent(EyeTrackerEvent eyeTrackerEvent)
        {
            // The first time open the file
            eventFile ??= new StreamWriter(DataFileName.Replace(".txt", "-events.txt"));

            eventFile.WriteLine(eyeTrackerEvent?.GetStringLine() ?? string.Empty);
            return true;
        }

        private static Image<Bgr, byte> CombineImages(Image<Bgr, byte> imageLeft, Image<Bgr, byte> imageRight)
        {
            var combinedSize = new Size(imageLeft.Size.Width * 2, imageLeft.Size.Height);
            var imgBoth = new Image<Bgr, byte>(combinedSize);

            imageLeft.CopyTo(imgBoth, new Rectangle(imageLeft.Width, 0, imageLeft.Size.Width, imageLeft.Size.Height));
            imageRight.CopyTo(imgBoth, imageRight.ROI);

            return imgBoth;
        }

        public void Dispose()
        {
            rawFramesRecorder.Dispose();
            processedFramesRecorder.Dispose();
            eventRecorder.Dispose();
            if (rawVideoWriters != null) Array.ForEach(rawVideoWriters, v => v?.Dispose());
            processedVideoWriter?.Dispose();
            eventFile?.Dispose();
            dataFile?.Dispose();
            log?.Dispose();

            rawVideoWriters = null;
            processedVideoWriter = null;
            processedVideoWriter = null;
            eventFile = null;
            dataFile = null;
            log = null;
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class RecordingOptions
    {
        public string? SessionName { get; set; }
        public string? DataFolder { get; set; }
        public bool SaveRawVideo { get; set; }
        public int DecimateRatioRawVideo { get; set; } = 1;
    }

    public class ProcessedRecordingOptions : RecordingOptions
    {
        public bool SaveProcessedVideo { get; set; }
        public Eye WhichEye { get; set; }
        public bool IncreaseContrast { get; set; }
        public bool AddPupilCross { get; set; }
        public bool AddCRCross { get; set; }
        public bool AddEyelids { get; set; }
    }
}