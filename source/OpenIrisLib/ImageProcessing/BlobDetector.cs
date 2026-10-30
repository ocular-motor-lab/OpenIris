//-----------------------------------------------------------------------
// <copyright file="BlobDetector.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageProcessing
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// A connected group of non-zero pixels in a binary image.
    /// </summary>
    public readonly struct Blob
    {
        /// <summary>
        /// Initializes a blob.
        /// </summary>
        public Blob(int area, PointF centroid, Rectangle boundingBox)
        {
            Area = area;
            Centroid = centroid;
            BoundingBox = boundingBox;
        }

        /// <summary>
        /// Number of pixels.
        /// </summary>
        public int Area { get; }

        /// <summary>
        /// Mean position of the pixels.
        /// </summary>
        public PointF Centroid { get; }

        /// <summary>
        /// Smallest rectangle that contains all the pixels.
        /// </summary>
        public Rectangle BoundingBox { get; }
    }

    /// <summary>
    /// Finds the blobs (8-connected groups of non-zero pixels) in a binary image.
    /// </summary>
    /// <remarks>
    /// Replaces the Emgu.CV.Cvb blob detector, which later EmguCV versions no longer include. It gives
    /// the same blobs in the same order (by their first pixel, scanning rows from the top), which
    /// matters when two blobs tie in the trackers' selection. OpenCV's ConnectedComponentsWithStats
    /// gives the same blobs too, but in a different order and several times slower on the small
    /// images the trackers use.
    ///
    /// One pass over the image: each row is split into runs of non-zero pixels, and runs that touch
    /// runs of the previous row (including diagonally) are joined with union-find. Background is
    /// skipped 8 bytes at a time. Not thread safe: use one instance per thread.
    /// </remarks>
    public sealed class BlobDetector
    {
        // Runs of the previous and current rows: first and last column, and label.
        private int[] previousStart = Array.Empty<int>();
        private int[] previousEnd = Array.Empty<int>();
        private int[] previousLabel = Array.Empty<int>();
        private int[] currentStart = Array.Empty<int>();
        private int[] currentEnd = Array.Empty<int>();
        private int[] currentLabel = Array.Empty<int>();

        // Per provisional label: union-find parent and statistics. Index 0 is unused.
        private int[] parent = new int[64];
        private int[] area = new int[64];
        private long[] sumX = new long[64];
        private long[] sumY = new long[64];
        private int[] minX = new int[64];
        private int[] minY = new int[64];
        private int[] maxX = new int[64];
        private int[] maxY = new int[64];

        /// <summary>
        /// Finds the blobs in a binary image. Coordinates are relative to the image ROI.
        /// </summary>
        /// <param name="binaryImage">Binary image; non-zero pixels are foreground.</param>
        /// <returns>The blobs, ordered by their first pixel.</returns>
        public unsafe List<Blob> Detect(Image<Gray, byte> binaryImage)
        {
            if (binaryImage is null) throw new ArgumentNullException(nameof(binaryImage));

            var roi = binaryImage.ROI;
            int width = roi.Width;
            int height = roi.Height;
            var blobs = new List<Blob>();
            if (width == 0 || height == 0) return blobs;

            // A row has at most (width + 1) / 2 runs.
            int maxRuns = (width + 1) / 2;
            if (currentStart.Length < maxRuns)
            {
                previousStart = new int[maxRuns];
                previousEnd = new int[maxRuns];
                previousLabel = new int[maxRuns];
                currentStart = new int[maxRuns];
                currentEnd = new int[maxRuns];
                currentLabel = new int[maxRuns];
            }

            var iplImage = binaryImage.MIplImage;
            int stride = iplImage.WidthStep;
            byte* origin = (byte*)iplImage.ImageData + (roi.Y * stride) + roi.X;

            int labelCount = 0;
            int previousCount = 0;

            for (int y = 0; y < height; y++)
            {
                byte* pixels = origin + (y * stride);
                int currentCount = 0;
                int p = 0; // First run of the previous row that may still touch a run of this row.
                int x = 0;

                while (x < width)
                {
                    // Skip background, 8 pixels at a time while they are all zero.
                    while (x + 8 <= width && *(ulong*)(pixels + x) == 0) x += 8;
                    while (x < width && pixels[x] == 0) x++;
                    if (x == width) break;

                    int start = x;
                    while (x < width && pixels[x] != 0) x++;
                    int end = x - 1;

                    // Join with the runs of the previous row that touch this one, diagonals included.
                    while (p < previousCount && previousEnd[p] < start - 1) p++;
                    int label = 0;
                    for (int k = p; k < previousCount && previousStart[k] <= end + 1; k++)
                    {
                        label = Merge(label, previousLabel[k]);
                    }

                    if (label == 0)
                    {
                        // First pixel of a new blob (or of a part that joins one later).
                        label = ++labelCount;
                        if (label == parent.Length) Grow();
                        parent[label] = label;
                        area[label] = 0;
                        sumX[label] = sumY[label] = 0;
                        minX[label] = start;
                        maxX[label] = end;
                        minY[label] = y;
                    }

                    int length = end - start + 1;
                    area[label] += length;
                    sumX[label] += (long)(start + end) * length / 2;
                    sumY[label] += (long)y * length;
                    if (start < minX[label]) minX[label] = start;
                    if (end > maxX[label]) maxX[label] = end;
                    maxY[label] = y;

                    currentStart[currentCount] = start;
                    currentEnd[currentCount] = end;
                    currentLabel[currentCount] = label;
                    currentCount++;
                }

                Swap(ref previousStart, ref currentStart);
                Swap(ref previousEnd, ref currentEnd);
                Swap(ref previousLabel, ref currentLabel);
                previousCount = currentCount;
            }

            // The loop above reads the pixels through a raw pointer, which does not stop the GC
            // from freeing the image. Keep it alive until here.
            GC.KeepAlive(binaryImage);

            // Fold each label's statistics into its root. Roots keep the smallest label of their
            // blob, which is the label of the blob's first pixel, so going up in label order lists
            // the blobs in scanning order.
            for (int label = labelCount; label >= 1; label--)
            {
                int root = Find(label);
                if (root == label) continue;

                area[root] += area[label];
                sumX[root] += sumX[label];
                sumY[root] += sumY[label];
                minX[root] = Math.Min(minX[root], minX[label]);
                minY[root] = Math.Min(minY[root], minY[label]);
                maxX[root] = Math.Max(maxX[root], maxX[label]);
                maxY[root] = Math.Max(maxY[root], maxY[label]);
            }

            for (int label = 1; label <= labelCount; label++)
            {
                if (parent[label] != label) continue;

                blobs.Add(new Blob(
                    area[label],
                    new PointF((float)((double)sumX[label] / area[label]), (float)((double)sumY[label] / area[label])),
                    new Rectangle(minX[label], minY[label], maxX[label] - minX[label] + 1, maxY[label] - minY[label] + 1)));
            }

            return blobs;
        }

        /// <summary>
        /// Joins the blobs of two labels (the first may be 0 for none) and returns the root label.
        /// </summary>
        private int Merge(int a, int b)
        {
            if (a == 0) return Find(b);

            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA < rootB)
            {
                parent[rootB] = rootA;
                return rootA;
            }

            parent[rootA] = rootB;
            return rootB;
        }

        private int Find(int label)
        {
            int root = label;
            while (parent[root] != root) root = parent[root];

            // Point the whole path at the root so later searches are short.
            while (parent[label] != root)
            {
                int next = parent[label];
                parent[label] = root;
                label = next;
            }

            return root;
        }

        private void Grow()
        {
            int size = parent.Length * 2;
            Array.Resize(ref parent, size);
            Array.Resize(ref area, size);
            Array.Resize(ref sumX, size);
            Array.Resize(ref sumY, size);
            Array.Resize(ref minX, size);
            Array.Resize(ref minY, size);
            Array.Resize(ref maxX, size);
            Array.Resize(ref maxY, size);
        }

        private static void Swap(ref int[] a, ref int[] b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
    }
}
