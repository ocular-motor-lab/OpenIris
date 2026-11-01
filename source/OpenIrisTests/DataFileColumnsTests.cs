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
    /// Checks the columns of the data file: the header, the lines and the LSL samples must match,
    /// and the columns of older versions must keep their names and positions, because analysis code
    /// reads them.
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
            Assert.AreEqual(header.Length, data.GetDataArray().Length);
            Assert.AreEqual(header.Length, header.Distinct().Count(), "Column names must be unique.");
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
            Assert.AreEqual("LeftH", header[ColumnsUntil0_1]);
        }

        private static string[] Split(string line) => line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
