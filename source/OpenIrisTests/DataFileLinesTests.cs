//-----------------------------------------------------------------------
// <copyright file="DataFileLinesTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks that the lines of the data file and the values streamed through LSL don't change: the
    /// same frames must give exactly the same text and numbers as in Baselines\data-file-lines.txt.
    /// Run with OPENIRIS_UPDATE_BASELINES=1 to rewrite it after an intended change.
    /// </summary>
    [TestClass]
    public class DataFileLinesTests
    {
        [TestMethod]
        public void LinesAndStreamedValuesDontChange()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var random = new Random(1);
                var actual = new StringBuilder();
                for (int i = 0; i < 200; i++)
                {
                    var data = RandomFrame(random, i);
                    actual.AppendLine(data.GetStringLine());
                    actual.AppendLine(string.Join(" ", data.GetDataArray().Select(v => v.ToString("R"))));
                }

                var baselineFile = Path.Combine(PipelineHarness.SourceFolder, "Baselines", "data-file-lines.txt");
                if (Baseline.UpdateRequested)
                {
                    File.WriteAllText(baselineFile, actual.ToString());
                    Assert.Inconclusive($"Baseline written: {baselineFile}");
                }

                var expected = File.ReadAllLines(baselineFile);
                var lines = actual.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None).Take(expected.Length).ToArray();
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.AreEqual(expected[i], lines[i], $"Line {i + 1} differs.");
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        /// <summary>
        /// A frame with every field set, and now and then a missing eye, fewer reflections, no
        /// eyelids, NaN or large values.
        /// </summary>
        private static EyeTrackerData RandomFrame(Random random, int i)
        {
            float F(double scale) => random.Next(20) == 0 ? float.NaN : (float)((random.NextDouble() - 0.3) * scale);
            double D(double scale) => random.Next(20) == 0 ? double.NaN : (random.NextDouble() - 0.3) * scale;
            PointF P() => new PointF(F(1000), F(1000));

            EyeData? Eye()
            {
                if (random.Next(10) == 0) return null;
                return new EyeData
                {
                    Timestamp = new ImageEyeTimestamp(D(1e5), (ulong)random.Next(), (ulong)random.Next()) { TimeGrabbed = D(1e4) },
                    Pupil = new PupilData(P(), new SizeF(F(100), F(100)), F(360)),
                    Iris = new IrisData(P(), F(200)),
                    TorsionAngle = D(20),
                    DataQuality = D(100),
                    CornealReflections = Enumerable.Range(0, random.Next(6)).Select(_ => new CornealReflectionData(P(), new SizeF(F(10), F(10)), F(90))).ToArray(),
                    Eyelids = random.Next(4) == 0 ? null : new EyelidData
                    {
                        Upper = Enumerable.Range(0, 4).Select(_ => P()).ToArray(),
                        Lower = Enumerable.Range(0, 4).Select(_ => P()).ToArray(),
                    },
                };
            }

            CalibratedEyeData Calibrated() => new CalibratedEyeData
            {
                HorizontalPosition = D(40), VerticalPosition = D(40), TorsionalPosition = D(10),
                PupilArea = D(1e4), PercentOpening = D(100), DataQuality = D(100),
            };

            return new EyeTrackerData
            {
                EyeDataRaw = new EyeCollection<EyeData?>(Eye(), Eye()),
                EyeDataCalibrated = random.Next(5) == 0 ? null : new EyeCollection<CalibratedEyeData>(Calibrated(), Calibrated()),
                HeadDataRaw = random.Next(5) == 0 ? null : new HeadData
                {
                    AccelerometerX = D(10), AccelerometerY = D(10), AccelerometerZ = D(10),
                    GyroX = D(500), GyroY = D(500), GyroZ = D(500),
                    MagnetometerX = D(1), MagnetometerY = D(1), MagnetometerZ = D(1),
                },
                HeadDataCalibrated = random.Next(5) == 0 ? (CalibratedHeadData?)null : new CalibratedHeadData
                {
                    Roll = D(90), Pitch = D(90), Yaw = D(90),
                    RollVelocity = D(300), PitchVelocity = D(300), YawVelocity = D(300),
                    XAcceleration = D(10), YAcceleration = D(10), ZAcceleration = D(10),
                    X = D(100), Y = D(100), Z = D(100),
                },
                ExtraData = new ExtraData
                {
                    Int0 = random.Next(-1000, 1000), Int1 = random.Next(), Int2 = i, Int3 = -i,
                    Int4 = random.Next(2), Int5 = random.Next(), Int6 = 0, Int7 = int.MaxValue,
                    Double0 = D(1), Double1 = D(1e6), Double2 = D(1e-3), Double3 = 0,
                    Double4 = D(10), Double5 = D(10), Double6 = D(10), Double7 = D(10),
                },
                TimeProcessed = D(1e4),
            };
        }
    }
}
