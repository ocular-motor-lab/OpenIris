//-----------------------------------------------------------------------
// <copyright file="ManualLabels.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// One label: a marker placed on one eye in one frame, in pixels of the eye image.
    /// </summary>
    public readonly struct ManualLabel
    {
        public ManualLabel(ulong frameNumber, Eye eye, string marker, PointF location)
        {
            FrameNumber = frameNumber;
            Eye = eye;
            Marker = marker;
            Location = location;
        }

        public ulong FrameNumber { get; }
        public Eye Eye { get; }
        public string Marker { get; }
        public PointF Location { get; }
    }

    /// <summary>
    /// The labels of a video, saved next to it as a CSV file: Frame,Eye,Marker,X,Y. Each marker is
    /// at most once per eye and frame.
    /// </summary>
    public sealed class ManualLabels
    {
        private readonly object sync = new object();
        private readonly Dictionary<(ulong frame, Eye eye), List<ManualLabel>> labels = new Dictionary<(ulong, Eye), List<ManualLabel>>();
        private bool changed;

        private ManualLabels(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// The labels file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Raised when labels are added or removed.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Name of the labels file of a video: next to it, VIDEO-labels.csv.
        /// </summary>
        public static string FileNameForVideo(string videoFile) =>
            Path.Combine(Path.GetDirectoryName(videoFile) ?? "", Path.GetFileNameWithoutExtension(videoFile) + "-labels.csv");

        /// <summary>
        /// Loads the labels of a video, or starts empty if it has none yet.
        /// </summary>
        public static ManualLabels LoadForVideo(string videoFile)
        {
            var result = new ManualLabels(FileNameForVideo(videoFile));
            if (!File.Exists(result.FileName)) return result;

            foreach (var line in File.ReadLines(result.FileName).Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length < 5) continue;
                // The marker name could have commas: it's everything between the eye and X.
                var marker = string.Join(",", parts.Skip(2).Take(parts.Length - 4));
                if (ulong.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frame)
                    && Enum.TryParse<Eye>(parts[1], out var eye)
                    && float.TryParse(parts[parts.Length - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    && float.TryParse(parts[parts.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    result.SetWithoutEvent(new ManualLabel(frame, eye, marker, new PointF(x, y)));
                }
            }
            return result;
        }

        /// <summary>
        /// Labels of one eye in one frame.
        /// </summary>
        public ManualLabel[] Get(ulong frameNumber, Eye eye)
        {
            lock (sync)
            {
                return labels.TryGetValue((frameNumber, eye), out var list) ? list.ToArray() : new ManualLabel[0];
            }
        }

        /// <summary>
        /// Places a marker, replacing the same marker on that eye and frame.
        /// </summary>
        public void Set(ManualLabel label)
        {
            SetWithoutEvent(label);
            OnChanged();
        }

        private void SetWithoutEvent(ManualLabel label)
        {
            lock (sync)
            {
                if (!labels.TryGetValue((label.FrameNumber, label.Eye), out var list))
                {
                    labels[(label.FrameNumber, label.Eye)] = list = new List<ManualLabel>();
                }
                list.RemoveAll(l => l.Marker == label.Marker);
                list.Add(label);
            }
        }

        /// <summary>
        /// Removes the label closest to a point, on that eye and frame.
        /// </summary>
        public void RemoveClosest(ulong frameNumber, Eye eye, PointF point)
        {
            lock (sync)
            {
                if (!labels.TryGetValue((frameNumber, eye), out var list) || list.Count == 0) return;
                var closest = list.OrderBy(l => Math.Pow(l.Location.X - point.X, 2) + Math.Pow(l.Location.Y - point.Y, 2)).First();
                list.Remove(closest);
            }
            OnChanged();
        }

        /// <summary>
        /// Removes all the labels of a frame.
        /// </summary>
        public void RemoveFrame(ulong frameNumber)
        {
            lock (sync)
            {
                labels.Remove((frameNumber, Eye.Left));
                labels.Remove((frameNumber, Eye.Right));
            }
            OnChanged();
        }

        /// <summary>
        /// Frames with at least one label, in order.
        /// </summary>
        public ulong[] LabeledFrames()
        {
            lock (sync)
            {
                return labels.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key.frame).Distinct().OrderBy(f => f).ToArray();
            }
        }

        /// <summary>
        /// Saves the file if anything changed.
        /// </summary>
        public void Save()
        {
            string text;
            lock (sync)
            {
                if (!changed) return;
                var builder = new StringBuilder("Frame,Eye,Marker,X,Y\r\n");
                foreach (var label in labels.Values.SelectMany(l => l).OrderBy(l => l.FrameNumber).ThenBy(l => l.Eye).ThenBy(l => l.Marker))
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:0.##},{4:0.##}\r\n",
                        label.FrameNumber, label.Eye, label.Marker, label.Location.X, label.Location.Y));
                }
                text = builder.ToString();
                changed = false;
            }

            // Write to a temporary file first, so a crash while saving doesn't lose the labels.
            var temporary = FileName + ".tmp";
            File.WriteAllText(temporary, text);
            if (File.Exists(FileName)) File.Delete(FileName);
            File.Move(temporary, FileName);
        }

        private void OnChanged()
        {
            lock (sync) changed = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
