//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationNPoint.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris.Calibration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Windows.Forms;
    using Emgu.CV;
    using Emgu.CV.UI;
    using Emgu.CV.Structure;
    using OpenIris.ImageProcessing;
    using OpenIris.UI;

    public partial class EyeCalibrationNPointUI : UserControl, ICalibrationUIControl
    {
        private EyeCalibrationNPoint calibration;

        private EyePhysicalModel leftModel;
        private EyePhysicalModel rightModel;


        public EyeCalibrationNPointUI(EyeCalibrationNPoint calibration)
        {
            this.calibration = calibration;

            InitializeComponent();

        }
        
        #region ICalibrationUI Members

        public void UpdateUI()
        {
            var imageBoxes = new EyeCollection<ImageBox>(this.imageBoxLeftEye, this.imageBoxRightEye);

            foreach (var eye in new Eye[] { Eye.Left, Eye.Right })
            {
                var im = calibration.ScatterImages[eye];
                im._EqualizeHist();
                var im2 = im.Convert<Bgr, byte>();

                foreach (var point in calibration.CalibrationPoints[eye])
                {
                    im2.Draw(new Cross2DF(point, 10, 10), new Bgr(Color.Red), 1);
                }

                imageBoxes[eye].Image = im2;
            }
        }

        #endregion

        private void buttonLeftNext_Click(object sender, EventArgs e)
        {

        }

        private void buttonLeftBack_Click(object sender, EventArgs e)
        {
            calibration.CalibrationPoints[Eye.Left].RemoveAt(calibration.CalibrationPoints[Eye.Left].Count - 1);
        }

        private void imageBoxLeftEye_MouseDown(object sender, MouseEventArgs e)
        {
            // Distance to board 106cm
            // 16.9 and 35.5 cm for horizontal targets
            // 13 and 25.2 cm for the vertical targets

            // At that distance 1 cm = 0.54 deg
            // Thus targets are at 9.1 19.2 deg for h and 7 and 13.6 for v

            if (e.Button == MouseButtons.Left)
            {
                var eye = Eye.Left;

                if (sender == this.imageBoxLeftEye)
                {
                    var mousePosition = e.Location.ConvertCoordinates(this.imageBoxLeftEye);

                    if (this.calibration.CalibrationPoints[eye].Count < 9)
                    {
                        this.calibration.CalibrationPoints[eye].Add(mousePosition);
                    }

                    if (this.calibration.CalibrationPoints[eye].Count == 9)
                    {
                        var p = this.calibration.CalibrationPoints[eye];
                        var xPixPerDeg = ((p[2].X - p[0].X) + (p[4].X - p[2].X)) / 2.0 / 19.2 + ((p[2].X - p[1].X) + (p[3].X - p[2].X)) / 2.0 / 9.1;
                        var yPixPerDeg = ((p[2].Y - p[5].Y) + (p[8].Y - p[2].Y)) / 2.0 / 13.6 + ((p[2].Y - p[6].Y) + (p[7].Y - p[2].Y)) / 2.0 / 7;

                        var xRad = 15.0 * xPixPerDeg;
                        var yRad = 15.0 * yPixPerDeg;

                        this.leftModel = new EyePhysicalModel(p[2], (float)xRad);
                    }
                }
                else
                {
                    eye = Eye.Right;
                    var mousePosition = e.Location.ConvertCoordinates(this.imageBoxRightEye);

                    if (this.calibration.CalibrationPoints[eye].Count < 9)
                    {
                        this.calibration.CalibrationPoints[eye].Add(mousePosition);
                    }

                    if (this.calibration.CalibrationPoints[eye].Count == 9)
                    {
                        var p = this.calibration.CalibrationPoints[eye];
                        var xPixPerDeg = ((p[2].X - p[0].X) + (p[4].X - p[2].X)) / 2.0 / 19.2 + ((p[2].X - p[1].X) + (p[3].X - p[2].X)) / 2.0 / 9.1;
                        var yPixPerDeg = ((p[2].Y - p[5].Y) + (p[8].Y - p[2].Y)) / 2.0 / 13.6 + ((p[2].Y - p[6].Y) + (p[7].Y - p[2].Y)) / 2.0 / 7;

                        var xRad = 15.0 * xPixPerDeg;
                        var yRad = 15.0 * yPixPerDeg;

                        this.rightModel = new EyePhysicalModel(p[2], (float)xRad);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            calibration.SetPhysicalModelsFromUI(leftModel, rightModel);
        }
    }
}
