//-----------------------------------------------------------------------
// <copyright file="CameraEyeTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using OpenIris.ImageGrabbing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests of what <see cref="CameraEye.GrabImageEye"/> does with the images the cameras return.
    /// </summary>
    [TestClass]
    public class CameraEyeTests
    {
        [TestMethod]
        public void GrabImageEyeReturnsNullWhenTheCameraHasNoImage()
        {
            // Cameras return null when they are stopped or the grab is cancelled.
            var camera = new TestCamera(() => null);

            Assert.IsNull(camera.GrabImageEye());
        }

        [TestMethod]
        public void GrabImageEyeStampsTheTimeGrabbed()
        {
            var before = DateTime.Now;
            var camera = new TestCamera(() => new ImageEye(new Image<Gray, byte>(200, 100), Eye.Left, new ImageEyeTimestamp(1, 5), null));

            using var image = camera.GrabImageEye();

            Assert.IsNotNull(image);
            Assert.IsTrue(image.TimeStamp.DateTimeGrabbed >= before);
            Assert.AreEqual(5UL, camera.LastFrameNumber);
        }

        private sealed class TestCamera : CameraEye
        {
            private readonly Func<ImageEye?> grab;

            public TestCamera(Func<ImageEye?> grab)
            {
                this.grab = grab;
                WhichEye = Eye.Left;
                FrameSize = new Size(200, 100);
            }

            public override void Start() { }

            public override void Stop() { }

            protected override ImageEye GrabImageFromCamera() => grab()!;
        }
    }
}
