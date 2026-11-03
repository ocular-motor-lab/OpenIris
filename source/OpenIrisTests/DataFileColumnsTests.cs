//-----------------------------------------------------------------------
// <copyright file="DataFileColumnsTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks the columns of the data files: the header, the lines and the LSL samples must match,
    /// and the columns of older versions must keep their names and positions, because analysis code
    /// reads them. The calibrated columns of 0.2 alphas moved to the calibrated data file.
    /// </summary>
    [TestClass]
    public class DataFileColumnsTests
    {
        private const int ColumnsUntil0_1 = 74;

        [TestMethod]
        public void HeaderLineAndSamplesHaveTheSameColumns()
        {
            var header = Split(EyeTrackerData.GetStringHeader());
            var data = new EyeTrackerData();

            Assert.AreEqual(header.Length, Split(data.GetStringLine()).Length);
            Assert.AreEqual(header.Length, header.Distinct().Count(), "Column names must be unique.");

            var calibrated = Split(EyeTrackerData.GetCalibratedStringHeader());
            Assert.AreEqual(calibrated.Length, Split(data.GetCalibratedStringLine()).Length);
            Assert.AreEqual(calibrated.Length, calibrated.Distinct().Count(), "Column names must be unique.");

            // The streams: the data file, then the calibrated file without its frame number and time.
            var streamed = Split(EyeTrackerData.GetStreamedHeader());
            Assert.AreEqual(streamed.Length, data.GetDataArray().Length);
            Assert.AreEqual(streamed.Length, streamed.Distinct().Count(), "Column names must be unique.");
            CollectionAssert.AreEqual(header.Concat(calibrated.Skip(2)).ToArray(), streamed);
        }

        [TestMethod]
        public void ColumnsOfOlderVersionsKeepTheirPlace()
        {
            var header = Split(EyeTrackerData.GetStringHeader());

            Assert.AreEqual("LeftFrameNumber", header[0]);
            Assert.AreEqual("RightFrameNumber", header[23]);
            Assert.AreEqual("AccelerometerX", header[46]);
            Assert.AreEqual("Int0", header[55]);
            Assert.AreEqual("Double0", header[63]);
            Assert.AreEqual("DebugTimeProcessed", header[ColumnsUntil0_1 - 1]);
            Assert.AreEqual("LeftRoiX", header[ColumnsUntil0_1]);
        }

        [TestMethod]
        public void CalibratedFileStartsWithTheFrameNumber()
        {
            var header = Split(EyeTrackerData.GetCalibratedStringHeader());

            Assert.AreEqual("FrameNumber", header[0]);
            Assert.AreEqual("Seconds", header[1]);
            Assert.AreEqual("LeftH", header[2]);
        }

        private static string[] Split(string line) => line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
