//-----------------------------------------------------------------------
// <copyright file="BlobDetectorTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Emgu.CV;
    using Emgu.CV.Cvb;
    using Emgu.CV.Structure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenIris.ImageProcessing;

    /// <summary>
    /// Checks that <see cref="BlobDetector"/> finds exactly the same blobs, in the same order, as the
    /// Emgu.CV.Cvb blob detector it replaced.
    /// </summary>
    /// <remarks>
    /// Emgu.CV.Cvb only exists in EmguCV 4.1 and earlier. Delete this file when upgrading EmguCV; the
    /// pipeline baselines keep guarding the results after that.
    /// </remarks>
    [TestClass]
    public class BlobDetectorTests
    {
        [TestMethod]
        public void SameAsCvBlobOnRandomImages()
        {
            var random = new Random(1234);
            for (int i = 0; i < 300; i++)
            {
                // Odd and even sizes, sparse to dense, so there are many touching, diagonal and edge blobs.
                var size = new Size(random.Next(1, 120), random.Next(1, 120));
                double density = random.NextDouble();
                using var image = new Image<Gray, byte>(size);
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        image.Data[y, x, 0] = random.NextDouble() < density ? (byte)255 : (byte)0;
                    }
                }

                AssertSame(image, $"random image {i} ({size.Width}x{size.Height}, density {density:0.00})");
            }
        }

        [TestMethod]
        public void SameAsCvBlobOnEdgeCases()
        {
            using var empty = new Image<Gray, byte>(50, 40);
            AssertSame(empty, "empty image");

            using var full = new Image<Gray, byte>(50, 40, new Gray(255));
            AssertSame(full, "full image");

            using var onePixel = new Image<Gray, byte>(1, 1, new Gray(255));
            AssertSame(onePixel, "1x1 image");

            using var corners = new Image<Gray, byte>(30, 20);
            corners.Data[0, 0, 0] = corners.Data[0, 29, 0] = corners.Data[19, 0, 0] = corners.Data[19, 29, 0] = 255;
            AssertSame(corners, "pixels in the corners");

            using var diagonal = new Image<Gray, byte>(20, 20);
            for (int i = 0; i < 20; i++) diagonal.Data[i, i, 0] = 255;
            AssertSame(diagonal, "diagonal line (8-connected only)");

            using var gray = new Image<Gray, byte>(20, 20, new Gray(1));
            AssertSame(gray, "non-zero values other than 255");
        }

        [TestMethod]
        public void SameAsCvBlobOnEyeImages()
        {
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                foreach (var frame in SyntheticEye.Frames(eye, 50))
                {
                    using (frame)
                    {
                        AssertSameOnThresholds(frame.Image, $"synthetic {eye} frame {frame.TimeStamp.FrameNumber}");
                    }
                }

                foreach (var frame in PipelineHarness.ReadVideo(PipelineHarness.SampleVideo(eye), eye, 100))
                {
                    using (frame)
                    {
                        AssertSameOnThresholds(frame.Image, $"video {eye} frame {frame.TimeStamp.FrameNumber}");
                    }
                }
            }
        }

        /// <summary>
        /// Thresholds like the pupil (dark) and reflection (bright) trackers do, at a few levels.
        /// </summary>
        private static void AssertSameOnThresholds(Image<Gray, byte> image, string description)
        {
            foreach (var dark in new[] { 30, 60, 90 })
            {
                using var binary = image.ThresholdBinaryInv(new Gray(dark), new Gray(255));
                AssertSame(binary, $"{description}, dark threshold {dark}");
            }

            foreach (var bright in new[] { 200, 250 })
            {
                using var binary = image.ThresholdBinary(new Gray(bright), new Gray(255));
                AssertSame(binary, $"{description}, bright threshold {bright}");
            }
        }

        private static void AssertSame(Image<Gray, byte> image, string description)
        {
            var expected = DetectWithCvBlob(image);
            var detector = new BlobDetector();
            var actual = detector.Detect(image);

            Assert.AreEqual(expected.Count, actual.Count, $"Number of blobs in {description}");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].Area, actual[i].Area, $"Area of blob {i} in {description}");
                Assert.AreEqual(expected[i].BoundingBox, actual[i].BoundingBox, $"Bounding box of blob {i} in {description}");
                Assert.AreEqual(expected[i].Centroid, actual[i].Centroid, $"Centroid of blob {i} in {description}");
            }
        }

        private static List<Blob> DetectWithCvBlob(Image<Gray, byte> image)
        {
            using var detector = new CvBlobDetector();
            using var blobs = new CvBlobs();
            detector.Detect(image, blobs);

            // CvBlobs is keyed by label, which numbers blobs in the order they are found.
            return blobs.OrderBy(b => b.Key)
                .Select(b => new Blob(b.Value.Area, b.Value.Centroid, b.Value.BoundingBox))
                .ToList();
        }
    }
}
