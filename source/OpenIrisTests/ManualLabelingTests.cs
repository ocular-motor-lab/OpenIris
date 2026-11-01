//-----------------------------------------------------------------------
// <copyright file="ManualLabelingTests.cs">
//     Copyright (c) 2014-2026 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.UI;

    /// <summary>
    /// The labels file of the tool to label videos by hand (Tools menu).
    /// </summary>
    [TestClass]
    public class ManualLabelingTests
    {
        [TestMethod]
        public void LabelsAreSavedAndLoadedAgain()
        {
            var folder = Path.Combine(Path.GetTempPath(), "OpenIrisTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var video = Path.Combine(folder, "Subject-Left.avi");
                var labels = ManualLabels.LoadForVideo(video);
                Assert.AreEqual(Path.Combine(folder, "Subject-Left-labels.csv"), labels.FileName);

                labels.Set(new ManualLabel(5, Eye.Left, "Pupil", new PointF(100.5f, 80.25f)));
                labels.Set(new ManualLabel(5, Eye.Left, "Pupil", new PointF(101, 81)));   // Replaces the one before.
                labels.Set(new ManualLabel(5, Eye.Right, "Pupil", new PointF(110, 90)));
                labels.Set(new ManualLabel(7, Eye.Left, "Tip, upper", new PointF(40, 41))); // A comma in the name.
                labels.Set(new ManualLabel(7, Eye.Left, "CR", new PointF(60, 61)));
                labels.RemoveClosest(7, Eye.Left, new PointF(59, 60));
                labels.Save();

                var loaded = ManualLabels.LoadForVideo(video);
                CollectionAssert.AreEqual(new ulong[] { 5, 7 }, loaded.LabeledFrames());

                var left5 = loaded.Get(5, Eye.Left).Single();
                Assert.AreEqual("Pupil", left5.Marker);
                Assert.AreEqual(101, left5.Location.X, 1e-3);
                Assert.AreEqual(81, left5.Location.Y, 1e-3);
                Assert.AreEqual(110, loaded.Get(5, Eye.Right).Single().Location.X, 1e-3);

                var left7 = loaded.Get(7, Eye.Left).Single();
                Assert.AreEqual("Tip, upper", left7.Marker, "The closest label (CR) was removed, the other kept.");

                loaded.RemoveFrame(5);
                CollectionAssert.AreEqual(new ulong[] { 7 }, loaded.LabeledFrames());
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
