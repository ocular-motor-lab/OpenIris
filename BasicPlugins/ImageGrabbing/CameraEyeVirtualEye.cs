//-----------------------------------------------------------------------
// <copyright file="CameraEyeVirtualEye.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.ImageGrabbing
{
#nullable enable

    using System;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.Structure;
    
    public class CameraEyeVirtualEye : CameraEye
    {
        private DateTime initialTime;
        private DateTime timeLastImage;
        private long numberFramesGrabbed;
        private EyeTrackingSystems.FakeEyeControlUI UI;

        private Image<Gray, byte> baseImage;
        private EyeTrackingSystems.GazeOrientation gaze;

        private PointF pupilCenter = new PointF(200, 200);
        private float pupilR = 20;
        private float irisR = 80;
        EyePhysicalModel eyeGlobe = new EyePhysicalModel(new PointF(200, 200), 160);

        /// <summary>
        /// Initializes an instance of the CameraEyeVirtualEye class.
        /// </summary>
        public CameraEyeVirtualEye(Eye whichEye, EyeTrackingSystems.FakeEyeControlUI controller)
        {
            WhichEye = whichEye;
            FrameSize = new Size(400, 400);
            UI = controller;
            initialTime = DateTime.Now;

            controller.NewGaze += controller_NewGaze;

            baseImage = new Image<Gray, byte>(FrameSize);

            baseImage.SetValue(new Gray(128));
            baseImage.Draw(new CircleF(eyeGlobe.Center, eyeGlobe.Radius), new Gray(230), 0);
            baseImage.Draw(new CircleF(pupilCenter, irisR), new Gray(100), 0);

            var Nsegments = 500;
            for (int i = 0; i < Nsegments; i++)
            {
                //var theta = 2 * Math.PI / Nsegments * i + gaze.Torsion * Math.PI / 4 + 0*Math.Round(3.0 * Math.Sin(7069 * (double)i / Nsegments * 2.0 * Math.PI));

                //baseImage.Draw(
                //    new LineSegment2D(
                //        new Point((int)(Math.Cos(theta) * irisR + pupilCenter.X), (int)(Math.Sin(theta) * irisR + pupilCenter.Y)),
                //        new Point((int)pupilCenter.X, (int)pupilCenter.Y)),
                //    new Gray(200), (int)Math.Round(4.0 + 3.0 * Math.Sin(7069 * (double)i / Nsegments * 2.0 * Math.PI)));


                var theta = 2 * Math.PI / Nsegments * i + gaze.Torsion * Math.PI / 4 + 4 * Math.Round(30.0 * Math.Sin(7069 * (double)i / Nsegments * 2.0 * Math.PI));

                int offset = 0;
                Math.DivRem(7069 * i, 10, out offset);

                var a = 0.2f + offset / 12f;
                var b = 0f + offset / 12f;

                baseImage.Draw(
                    new LineSegment2D(
                        new Point((int)(Math.Cos(theta) * irisR * a + pupilCenter.X), (int)(Math.Sin(theta) * irisR * a + pupilCenter.Y)),
                        new Point((int)(Math.Cos(theta) * irisR * b + pupilCenter.X), (int)(Math.Sin(theta) * irisR * b + pupilCenter.Y))),
                    new Gray(120 + 50 * Math.Sin(7069 * (double)i / Nsegments * 2.0 * Math.PI)), 4);
            }

            baseImage.Draw(new CircleF(pupilCenter, pupilR), new Gray(10), 0);
        }


        void controller_NewGaze(object sender, EyeTrackingSystems.GazeOrientation e)
        {
            gaze = e;
        }

        protected override ImageEye GrabImageFromCamera()
        {
            // ROTATE THE SPHERE
            var x1 = (int)Math.Floor(eyeGlobe.Center.X - eyeGlobe.Radius);
            var x2 = (int)Math.Ceiling(eyeGlobe.Center.X + eyeGlobe.Radius);
            var y1 = (int)Math.Max(0, Math.Floor(eyeGlobe.Center.Y - eyeGlobe.Radius));
            var y2 = (int)Math.Min(FrameSize.Height - 1, Math.Ceiling(eyeGlobe.Center.Y + eyeGlobe.Radius));

            var mapx = new Image<Gray, float>(FrameSize);
            var mapy = new Image<Gray, float>(FrameSize);

            var angle = gaze.Angle; // axis of rotation
            var ecc = gaze.Eccentricity; // ammount of rotation
            var q = new Quaternions(Math.Cos(ecc / 2), -Math.Sin(angle) * Math.Sin(ecc / 2), Math.Cos(angle) * Math.Sin(ecc / 2), 0);

            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    double r = Math.Sqrt((x - eyeGlobe.Center.X) * (x - eyeGlobe.Center.X) + (y - eyeGlobe.Center.Y) * (y - eyeGlobe.Center.Y)) / eyeGlobe.Radius;

                    if (r <= 1)
                    {
                        var z = Math.Sqrt(1 - r * r) * eyeGlobe.Radius;

                        var p = new MCvPoint3D64f(x - eyeGlobe.Center.X, y - eyeGlobe.Center.Y, z);

                        var p2 = q.RotatePoint(p);


                        mapx.Data[y, x, 0] = (float)p2.X + eyeGlobe.Center.X;
                        mapy.Data[y, x, 0] = (float)p2.Y + eyeGlobe.Center.Y;
                    }
                }
            }


            var image = new Image<Gray, byte>(FrameSize);

            CvInvoke.Remap(baseImage, image, mapx, mapy, Emgu.CV.CvEnum.Inter.Cubic, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(255));

            image.Draw(new LineSegment2D(new Point(0, (int)pupilCenter.Y + 200), new Point(400, (int)pupilCenter.Y + 200)), new Gray(80), 40);
            image.Draw(new LineSegment2D(new Point(0, (int)pupilCenter.Y - 200), new Point(400, (int)pupilCenter.Y - 200)), new Gray(80), 40);

            ImageEyeTimestamp t = new ImageEyeTimestamp
            {
                FrameNumber = (ulong)numberFramesGrabbed,
                FrameNumberRaw = (ulong)numberFramesGrabbed,
                Seconds = (DateTime.Now - initialTime).TotalSeconds,
            };

            numberFramesGrabbed++;
            timeLastImage = t.DateTimeGrabbed;

            System.Threading.Thread.Sleep((int)Math.Max(0, 10 - (t.DateTimeGrabbed - timeLastImage).TotalMilliseconds));
            var newImage = new ImageEye(image, WhichEye, t, null);
            return newImage;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            initialTime = DateTime.Now;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Stop()
        {
            if (!UI.IsDisposed)
            {
                UI.Close();
                UI.Dispose();
            }
        }
    }
}
