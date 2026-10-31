//-----------------------------------------------------------------------
// <copyright file="EyeTrackingSystemSubjective.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using OpenIris.ImageGrabbing;

    /// <summary>
    /// Subjective eye tracking: a person labels the videos by clicking on the eye images. It replaces
    /// the old SubjectiveTracker program.
    ///
    /// Play the videos with this system and a window opens to step through the frames and place the
    /// markers. The labels are saved next to the left video (*-labels.csv). Each marker can go into the
    /// data as the pupil or a corneal reflection, replacing what the tracking pipeline found, or into
    /// the extra columns of the data file (Double0-7, Int0-7). Choose the pipeline "Nothing" for data
    /// with only the labels, or a real one to label next to its tracking. Processing the videos with this
    /// system writes the data file with the labels.
    ///
    /// The labels are applied after the tracking, in PostProcessImagesAndData, which OpenIris calls
    /// for every frame, also again and again for the same frame while the video is paused. So a new
    /// label shows up in the data right away.
    /// </summary>
    [Export(typeof(EyeTrackingSystemBase)), PluginDescriptionEyeTrackingSystem("Subjective", typeof(EyeTrackingSystemSettingsSubjective))]
    public sealed class EyeTrackingSystemSubjective : EyeTrackingSystemBase
    {
        private SubjectiveLabels? labels;
        private SubjectiveLabelingForm? form;
        private bool pausedAtStart;

        /// <summary>
        /// Markers of the labels: the ones in the saved settings of this system (the settings this
        /// system gets for videos are the defaults).
        /// </summary>
        internal EyeTrackingSystemSettingsSubjective SubjectiveSettings =>
            (EyeTracker.Create().Settings.AllEyeTrackerSystemSettings.TryGetValue("Subjective", out var saved) ? saved : Settings)
                as EyeTrackingSystemSettingsSubjective ?? new EyeTrackingSystemSettingsSubjective();

        /// <summary>
        /// Only videos: there is nothing to label live.
        /// </summary>
        protected override EyeCollection<CameraEye?>? CreateAndStartCameras() =>
            throw new OpenIrisException("The Subjective system labels videos: use Play video, not Start tracking.");

        /// <summary>
        /// Opens the videos, loads their labels and, unless the videos are being processed, opens the
        /// labeling window. Runs on the UI thread.
        /// </summary>
        protected override EyeCollection<VideoEye?> CreateVideos(EyeCollection<string?> fileNames)
        {
            var videos = base.CreateVideos(fileNames);

            var firstVideo = fileNames.FirstOrDefault(f => !string.IsNullOrEmpty(f)) ?? throw new ArgumentException("No video.");
            labels = SubjectiveLabels.LoadForVideo(firstVideo);

            if (!EyeTracker.Create().PostProcessing)
            {
                form = new SubjectiveLabelingForm(this, labels);
                form.Show();
            }

            return videos;
        }

        /// <summary>
        /// Adds the labels of the frame to the data.
        /// </summary>
        public override EyeTrackerImagesAndData PostProcessImagesAndData(EyeTrackerImagesAndData processedImages)
        {
            if (labels is null || processedImages.Data is null) return processedImages;

            // To label, start paused at the first frame. Here, at the first processed frame, and not
            // in the window: with a fast pipeline a short video could be over before the window gets
            // its first frame.
            if (form != null && !pausedAtStart && EyeTracker.Create().VideoPlayer is VideoPlayer player)
            {
                pausedAtStart = true;
                player.Pause();
                player.Scroll((ulong)player.FrameRange.Begin);
            }

            var markers = new Dictionary<string, SubjectiveMarker>();
            foreach (var m in SubjectiveSettings.Markers ?? new SubjectiveMarker[0]) markers[m.Name ?? ""] = m;
            var data = processedImages.Data;
            var eyeDataRaw = data.EyeDataRaw ?? new EyeCollection<EyeData?>(null, null);
            var extra = data.ExtraData;
            var changedEyeData = false;

            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                var image = processedImages.Images[eye];
                if (image is null) continue;

                foreach (var label in labels.Get(image.TimeStamp.FrameNumber, eye))
                {
                    if (!markers.TryGetValue(label.Marker, out var marker)) continue;

                    var target = eye == Eye.Left ? marker.LeftEye : marker.RightEye;
                    if (target == SubjectiveMarkerTarget.None) continue;

                    if (target <= SubjectiveMarkerTarget.CR4)
                    {
                        eyeDataRaw[eye] = SetEyeData(eyeDataRaw[eye], image, target, label.Location);
                        changedEyeData = true;
                    }
                    else
                    {
                        extra = SetExtraData(extra, target, label.Location);
                    }
                }
            }

            data.ExtraData = extra;
            if (changedEyeData)
            {
                data.EyeDataRaw = eyeDataRaw;
                data.EyeDataCalibrated = processedImages.Calibration?.GetCalibratedEyeData(eyeDataRaw);
            }

            form?.ShowFrame(processedImages);

            return processedImages;
        }

        /// <summary>
        /// Copy of the eye data of the pipeline (or new eye data, with the pipeline Nothing) with the
        /// pupil or a corneal reflection at the label.
        /// </summary>
        private static EyeData SetEyeData(EyeData? pipelineData, ImageEye image, SubjectiveMarkerTarget target, PointF location)
        {
            var data = pipelineData?.Copy() ?? new EyeData
            {
                WhichEye = image.WhichEye,
                Timestamp = image.TimeStamp,
                ImageSize = image.Size,
            };
            data.ProcessFrameResult = ProcessFrameResult.Good;

            if (target == SubjectiveMarkerTarget.Pupil)
            {
                // A click gives the center only. Keep the size the pipeline found, if any.
                var size = pipelineData is null || pipelineData.Pupil.IsEmpty ? new SizeF(float.NaN, float.NaN) : pipelineData.Pupil.Size;
                data.Pupil = new PupilData(location, size, pipelineData?.Pupil.Angle ?? 0);
            }
            else
            {
                var index = target - SubjectiveMarkerTarget.CR1;
                var crs = (data.CornealReflections ?? new CornealReflectionData[0]).ToList();
                while (crs.Count <= index) crs.Add(new CornealReflectionData());
                crs[index] = new CornealReflectionData(location, new SizeF(1, 1), 0);
                data.CornealReflections = crs.ToArray();
            }

            return data;
        }

        private static ExtraData SetExtraData(ExtraData extra, SubjectiveMarkerTarget target, PointF location)
        {
            switch (target)
            {
                case SubjectiveMarkerTarget.Double0and1: extra.Double0 = location.X; extra.Double1 = location.Y; break;
                case SubjectiveMarkerTarget.Double2and3: extra.Double2 = location.X; extra.Double3 = location.Y; break;
                case SubjectiveMarkerTarget.Double4and5: extra.Double4 = location.X; extra.Double5 = location.Y; break;
                case SubjectiveMarkerTarget.Double6and7: extra.Double6 = location.X; extra.Double7 = location.Y; break;
                case SubjectiveMarkerTarget.Int0and1: extra.Int0 = (int)Math.Round(location.X); extra.Int1 = (int)Math.Round(location.Y); break;
                case SubjectiveMarkerTarget.Int2and3: extra.Int2 = (int)Math.Round(location.X); extra.Int3 = (int)Math.Round(location.Y); break;
                case SubjectiveMarkerTarget.Int4and5: extra.Int4 = (int)Math.Round(location.X); extra.Int5 = (int)Math.Round(location.Y); break;
                case SubjectiveMarkerTarget.Int6and7: extra.Int6 = (int)Math.Round(location.X); extra.Int7 = (int)Math.Round(location.Y); break;
            }
            return extra;
        }

        /// <summary>
        /// Saves the labels and closes the window.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                labels?.Save();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ERROR saving the subjective labels: " + ex.Message);
            }

            var closingForm = form;
            form = null;
            if (closingForm != null && !closingForm.IsDisposed)
            {
                if (closingForm.InvokeRequired) closingForm.BeginInvoke(new Action(closingForm.Close));
                else closingForm.Close();
            }

            base.Dispose();
        }
    }

    /// <summary>
    /// Where a marker goes in the data.
    /// </summary>
    public enum SubjectiveMarkerTarget
    {
        /// <summary>Only in the labels file.</summary>
        None,
        /// <summary>Center of the pupil (LeftPupilX/Y or RightPupilX/Y), instead of the pipeline's.</summary>
        Pupil,
        /// <summary>Corneal reflection 1 (LeftCR1X/Y...), instead of the pipeline's.</summary>
        CR1,
        /// <summary>Corneal reflection 2.</summary>
        CR2,
        /// <summary>Corneal reflection 3.</summary>
        CR3,
        /// <summary>Corneal reflection 4.</summary>
        CR4,
        /// <summary>X in Double0 and Y in Double1.</summary>
        Double0and1,
        /// <summary>X in Double2 and Y in Double3.</summary>
        Double2and3,
        /// <summary>X in Double4 and Y in Double5.</summary>
        Double4and5,
        /// <summary>X in Double6 and Y in Double7.</summary>
        Double6and7,
        /// <summary>X in Int0 and Y in Int1 (rounded).</summary>
        Int0and1,
        /// <summary>X in Int2 and Y in Int3 (rounded).</summary>
        Int2and3,
        /// <summary>X in Int4 and Y in Int5 (rounded).</summary>
        Int4and5,
        /// <summary>X in Int6 and Y in Int7 (rounded).</summary>
        Int6and7,
    }

    /// <summary>
    /// A kind of label, with its name and where it goes in the data for each eye.
    /// </summary>
    [Serializable]
    public class SubjectiveMarker
    {
        /// <summary>Name of the marker, as in the labels file.</summary>
        [Description("Name of the marker, as in the labels file.")]
        public string Name { get; set; } = "Marker";

        /// <summary>Where the marker goes in the data when placed on the left eye.</summary>
        [Description("Where the marker goes in the data when placed on the left eye.")]
        public SubjectiveMarkerTarget LeftEye { get; set; } = SubjectiveMarkerTarget.None;

        /// <summary>Where the marker goes in the data when placed on the right eye.</summary>
        [Description("Where the marker goes in the data when placed on the right eye.")]
        public SubjectiveMarkerTarget RightEye { get; set; } = SubjectiveMarkerTarget.None;

        /// <inheritdoc/>
        public override string ToString() => Name;
    }

    /// <summary>
    /// Settings of the Subjective system.
    /// </summary>
    [Serializable]
    public class EyeTrackingSystemSettingsSubjective : EyeTrackingSystemSettings
    {
        /// <summary>
        /// The markers. They can also be edited in the labeling window. An array, not a list: when
        /// loading the settings, the XML serializer would add the saved markers to the default ones.
        /// </summary>
        [Category("Subjective"), Description("The markers to place, and where each goes in the data. They can also be edited in the labeling window.")]
        public SubjectiveMarker[] Markers { get => markers; set => SetProperty(ref markers, value, nameof(Markers)); }
        private SubjectiveMarker[] markers =
        {
            new SubjectiveMarker { Name = "Pupil", LeftEye = SubjectiveMarkerTarget.Pupil, RightEye = SubjectiveMarkerTarget.Pupil },
            new SubjectiveMarker { Name = "CR", LeftEye = SubjectiveMarkerTarget.CR1, RightEye = SubjectiveMarkerTarget.CR1 },
        };
    }
}
