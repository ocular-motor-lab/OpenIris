//-----------------------------------------------------------------------
// <copyright file="Baseline.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Compares pipeline output with a reference CSV file stored in the Baselines folder.
    /// </summary>
    /// <remarks>
    /// To accept new output as correct (after an intended change), run the tests with the
    /// environment variable OPENIRIS_UPDATE_BASELINES=1 and commit the changed CSV files.
    /// </remarks>
    internal static class Baseline
    {
        private static readonly string[] Columns =
        {
            "frame", "result",
            "pupilX", "pupilY", "pupilWidth", "pupilHeight", "pupilAngle",
            "irisX", "irisY", "irisRadius",
            "reflections", "reflectionX", "reflectionY",
            "torsion", "quality",
        };

        /// <summary>
        /// Largest difference accepted for any numeric value (pixels, degrees or quality units).
        /// Small enough to catch any real change, large enough to absorb floating point differences
        /// between processors.
        /// </summary>
        public const double Tolerance = 0.01;

        public static bool UpdateRequested => Environment.GetEnvironmentVariable("OPENIRIS_UPDATE_BASELINES") == "1";

        /// <summary>
        /// Fails the test if the data differs from the stored baseline.
        /// </summary>
        public static void Check(string name, IEnumerable<EyeData> data)
        {
            var actual = ToCsv(data);
            var baselineFile = Path.Combine(PipelineHarness.SourceFolder, "Baselines", name + ".csv");

            if (UpdateRequested)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(baselineFile));
                File.WriteAllText(baselineFile, actual);
                Assert.Inconclusive($"Baseline written: {baselineFile}");
            }

            if (!File.Exists(baselineFile))
            {
                Assert.Fail($"No baseline at {baselineFile}. Run the tests with OPENIRIS_UPDATE_BASELINES=1 to create it.");
            }

            var differences = Compare(File.ReadAllText(baselineFile), actual);
            if (differences.Count > 0)
            {
                var actualFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "actual", name + ".csv");
                Directory.CreateDirectory(Path.GetDirectoryName(actualFile));
                File.WriteAllText(actualFile, actual);

                Assert.Fail(
                    $"{differences.Count} values differ from {baselineFile} (tolerance {Tolerance}). First ones:{Environment.NewLine}" +
                    string.Join(Environment.NewLine, differences.Take(15)) + Environment.NewLine +
                    $"Full output written to {actualFile}. If the change is intended, update the baselines (see Baseline.cs).");
            }
        }

        public static string ToCsv(IEnumerable<EyeData> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Columns));
            foreach (var d in data)
            {
                var reflection = d.CornealReflections?.FirstOrDefault(cr => !cr.IsEmpty) ?? default;
                var values = new object[]
                {
                    d.Timestamp.FrameNumber, d.ProcessFrameResult,
                    d.Pupil.Center.X, d.Pupil.Center.Y, d.Pupil.Size.Width, d.Pupil.Size.Height, d.Pupil.Angle,
                    d.Iris.Center.X, d.Iris.Center.Y, d.Iris.Radius,
                    d.CornealReflections?.Count(cr => !cr.IsEmpty) ?? 0, reflection.Center.X, reflection.Center.Y,
                    d.TorsionAngle, d.DataQuality,
                };
                sb.AppendLine(string.Join(",", values.Select(Format)));
            }

            return sb.ToString();
        }

        private static string Format(object value) => value switch
        {
            float f => f.ToString("0.####", CultureInfo.InvariantCulture),
            double d => d.ToString("0.####", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture),
        };

        private static List<string> Compare(string expectedCsv, string actualCsv)
        {
            var differences = new List<string>();
            var expected = SplitLines(expectedCsv);
            var actual = SplitLines(actualCsv);

            if (expected.Length != actual.Length)
            {
                differences.Add($"expected {expected.Length - 1} frames, got {actual.Length - 1}");
            }

            for (int row = 1; row < Math.Min(expected.Length, actual.Length); row++)
            {
                var e = expected[row].Split(',');
                var a = actual[row].Split(',');
                for (int col = 0; col < Columns.Length; col++)
                {
                    bool same = double.TryParse(e[col], NumberStyles.Float, CultureInfo.InvariantCulture, out var ev)
                             && double.TryParse(a[col], NumberStyles.Float, CultureInfo.InvariantCulture, out var av)
                        ? Math.Abs(ev - av) <= Tolerance || (double.IsNaN(ev) && double.IsNaN(av))
                        : e[col] == a[col];

                    if (!same)
                    {
                        differences.Add($"  frame {e[0]}, {Columns[col]}: expected {e[col]}, got {a[col]}");
                    }
                }
            }

            return differences;
        }

        private static string[] SplitLines(string text) =>
            text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
