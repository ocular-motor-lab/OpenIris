//-----------------------------------------------------------------------
// <copyright file="EyeCalibrationManualUI.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris.Calibration
{
#nullable enable

    using Emgu.CV.Structure;
    using OpenIris.ImageProcessing;
    using OpenIris.UI;
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Manual calibration where a UI can be used to set the eye model and the reference position.
    /// </summary>
    public partial class CalibrationPipelineManualUI : UserControl, ICalibrationUIControl
    {
        private readonly EyeCollection<Emgu.CV.UI.ImageBox> imageBoxes;
        public EyeCollection<ImageEye?> lastImages;
        public EyeCollection<EyePhysicalModel>? eyeModels;

        public ICalibrationUIControl? GetCalibrationUI() => this;

        /// <summary>
        /// Indicates weather the calibration was cancelled.
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CalibrationPipelineManualUI()
        {
            lastImages = new EyeCollection<ImageEye?>(null, null);

            InitializeComponent();

            imageBoxes = new EyeCollection<Emgu.CV.UI.ImageBox>(imageBoxLeftEye, imageBoxRightEye);


            sliderTextControlLeftEyeGlobeH.Text = "Left Eye globe H";
            sliderTextControlLeftEyeGlobeH.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlLeftEyeGlobeH.Value = 0;

            sliderTextControlLeftEyeGlobeV.Text = "Left Eye globe V";
            sliderTextControlLeftEyeGlobeV.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlLeftEyeGlobeV.Value = 0;

            sliderTextControlLeftEyeGlobeR.Text = "Left Eye globe R";
            sliderTextControlLeftEyeGlobeR.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlLeftEyeGlobeR.Value = 10;

            sliderTextControlRightEyeGlobeH.Text = "Right Eye globe H";
            sliderTextControlRightEyeGlobeH.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlRightEyeGlobeH.Value = 0;

            sliderTextControlRightEyeGlobeV.Text = "Right Eye globe V";
            sliderTextControlRightEyeGlobeV.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlRightEyeGlobeV.Value = 0;

            sliderTextControlRightEyeGlobeR.Text = "Right Eye globe R";
            sliderTextControlRightEyeGlobeR.Range = new OpenIris.RangeDouble(0, 2000);
            sliderTextControlRightEyeGlobeR.Value = 10;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        public EyePhysicalModel GetEyeGlobe(Eye whichEye)
        {
            return whichEye switch
            {
                Eye.Left => new EyePhysicalModel(
                                   new PointF(
                                       (float)sliderTextControlLeftEyeGlobeH.Value,
                                       (float)sliderTextControlLeftEyeGlobeV.Value),
                                   (float)sliderTextControlLeftEyeGlobeR.Value),

                Eye.Right => new EyePhysicalModel(
                                    new PointF(
                                        (float)sliderTextControlRightEyeGlobeH.Value,
                                        (float)sliderTextControlRightEyeGlobeV.Value),
                                    (float)sliderTextControlRightEyeGlobeR.Value),

                _ => new EyePhysicalModel(),
            };
        }

        #region ICalibrationUI Members

        /// <summary>
        /// Updates the UI with the image from last frame and current calibration.
        /// </summary>
        public void UpdateUI()
        {
            foreach (var image in lastImages)
            {
                if (image != null)
                {
                    var imageColor = image.Image.Convert<Bgr, byte>();

                    ImageEyeBox.DrawEyeGlobe(imageColor, GetEyeGlobe(image.WhichEye), true);

                    imageBoxes[image.WhichEye].Image = imageColor;
                }
            }
        }

        #endregion ICalibrationUI Members


        private void buttonAccept_Click(object sender, EventArgs e)
        {
            eyeModels = new EyeCollection<EyePhysicalModel>(GetEyeGlobe(Eye.Left), GetEyeGlobe(Eye.Right));
        }

        private void buttonAuto_Click(object sender, EventArgs e)
        {
            var lastImageLeftEye = lastImages[Eye.Left];
            var lastImageRightEye = lastImages[Eye.Right];

            if (lastImageLeftEye != null)
            {
                sliderTextControlLeftEyeGlobeH.Value = (int)Math.Round(lastImageLeftEye.EyeData?.Pupil.Center.X ?? 0);
                sliderTextControlLeftEyeGlobeV.Value = (int)Math.Round(lastImageLeftEye.EyeData?.Pupil.Center.Y ?? 0);
                sliderTextControlLeftEyeGlobeR.Value = (int)Math.Round(lastImageLeftEye.EyeData?.Iris.Radius * 2.0 ?? 0);
            }

            if (lastImageRightEye != null)
            {
                sliderTextControlRightEyeGlobeH.Value = (int)Math.Round(lastImageRightEye.EyeData?.Pupil.Center.X ?? 0);
                sliderTextControlRightEyeGlobeV.Value = (int)Math.Round(lastImageRightEye.EyeData?.Pupil.Center.Y ?? 0);
                sliderTextControlRightEyeGlobeR.Value = (int)Math.Round(lastImageRightEye.EyeData?.Iris.Radius * 2.0 ?? 0);
            }
        }
        
        private void imageBoxLeftEye_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var mousePosition = e.Location.ConvertCoordinates(imageBoxLeftEye);

                sliderTextControlLeftEyeGlobeH.Value = mousePosition.X;
                sliderTextControlLeftEyeGlobeV.Value = mousePosition.Y;
            }
        }

        private void imageBoxLeftEye_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var mousePosition = e.Location.ConvertCoordinates(imageBoxLeftEye);

                sliderTextControlLeftEyeGlobeH.Value = mousePosition.X;
                sliderTextControlLeftEyeGlobeV.Value = mousePosition.Y;
            }
        }

        private void imageBoxRightEye_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var mousePosition = e.Location.ConvertCoordinates(imageBoxRightEye);

                sliderTextControlRightEyeGlobeH.Value = mousePosition.X;
                sliderTextControlRightEyeGlobeV.Value = mousePosition.Y;
            }
        }

        private void imageBoxRightEye_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var mousePosition = e.Location.ConvertCoordinates(imageBoxRightEye);

                sliderTextControlRightEyeGlobeH.Value = mousePosition.X;
                sliderTextControlRightEyeGlobeV.Value = mousePosition.Y;
            }
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            Cancelled = true;
        }

        private void sliderTextControlLeftEyeGlobeR_Load(object sender, EventArgs e)
        {

        }
    }
}