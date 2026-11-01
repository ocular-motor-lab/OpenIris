//-----------------------------------------------------------------------
// <copyright file="RoiTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Where each eye image is in the camera's image (ImageEye.Roi) reaches the RoiX, RoiY, RoiWidth
    /// and RoiHeight columns of the data file (GitHub issue #58).
    /// </summary>
    [TestClass]
    public class RoiTests
    {
        [TestMethod]
        public void DataFileHasTheRoiOfEachEye()
        {
            var data = new EyeTrackerData
            {
                EyeDataRaw = new EyeCollection<EyeData?>(
                    new EyeData { Roi = new Rectangle(10, 20, 720, 450) },
                    new EyeData { Roi = new Rectangle(30, 40, 640, 480) }),
            };

            var columns = EyeTrackerData.GetStringHeader().Split(' ');
            var line = data.GetStringLine().Split(' ');
            var values = data.GetDataArray();
            double Column(string name) => double.Parse(line[Array.IndexOf(columns, name)], CultureInfo.InvariantCulture);
            double Value(string name) => values[Array.IndexOf(columns, name)];

            var expected = new[] { ("LeftRoiX", 10), ("LeftRoiY", 20), ("LeftRoiWidth", 720), ("LeftRoiHeight", 450), ("RightRoiX", 30), ("RightRoiY", 40), ("RightRoiWidth", 640), ("RightRoiHeight", 480) };
            foreach (var (name, value) in expected)
            {
                Assert.AreEqual(value, Column(name), name);
                Assert.AreEqual(value, Value(name), name + " (streamed)");
            }
        }

        /// <summary>
        /// A part cut out of an image is at the image's ROI plus the part's position.
        /// </summary>
        [TestMethod]
        public void CopyOfAPartKeepsTrackOfTheRoi()
        {
            using var image = new ImageEye(new Image<Gray, byte>(400, 300), Eye.Both, ImageEyeTimestamp.Empty, null) { Roi = new Rectangle(100, 50, 400, 300) };

            Assert.AreEqual(new Rectangle(110, 70, 200, 150), image.Copy(new Rectangle(10, 20, 200, 150)).Roi);
            Assert.AreEqual(image.Roi, image.Copy().Roi);

            using var unknown = new ImageEye(new Image<Gray, byte>(400, 300), Eye.Both, ImageEyeTimestamp.Empty, null);
            Assert.IsTrue(unknown.Copy(new Rectangle(10, 20, 200, 150)).Roi.IsEmpty, "An unknown ROI stays unknown.");
        }
    }
}
