using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenIris.EyeTrackingSystems
{
    public partial class FakeEyeControlUI : Form
    {
        public GazeOrientation currentOrientation = new GazeOrientation();

        public event EventHandler<GazeOrientation> NewGaze;

        public FakeEyeControlUI()
        {
            InitializeComponent();

        }

        private void FakeEyeControlUI_Load(object sender, EventArgs ea)
        {
            this.ListingAngle.Text = "Angle";
            this.ListingAngle.Range = new Range(-180, 180);
            this.ListingAngle.Value = 0;
            this.ListingAngle.ValueChanged += ListingValueChanged;
            this.ListingEccentricity.Text = "Eccentricity";
            this.ListingEccentricity.Range = new Range(0, 90);
            this.ListingEccentricity.Value = 0;
            this.ListingEccentricity.ValueChanged += ListingValueChanged;
            this.ListingTorsion.Text = "Torsion";
            this.ListingTorsion.Range = new Range(-90, 90);
            this.ListingTorsion.Value = 0;
            this.ListingTorsion.ValueChanged += ListingValueChanged;

            this.FickHorizontal.Text = "Horizontal";
            this.FickHorizontal.Range = new Range(-90, 90);
            this.FickHorizontal.Value = 0;
            this.FickHorizontal.ValueChanged += Fick_ValueChanged;
            this.FickVertical.Text = "Vertical";
            this.FickVertical.Range = new Range(-90, 90);
            this.FickVertical.Value = 0;
            this.FickVertical.ValueChanged += (o, e) => { };
            this.FickTorsion.Text = "Torsion";
            this.FickTorsion.Range = new Range(-90, 90);
            this.FickTorsion.Value = 0;
            this.FickTorsion.ValueChanged += (o, e) => { };

            this.HelmholtzHorizontal.Text = "Horizontal";
            this.HelmholtzHorizontal.Range = new Range(-90, 90);
            this.HelmholtzHorizontal.Value = 0;
            this.HelmholtzHorizontal.ValueChanged += (o, e) => { };
            this.HelmholtzVertical.Text = "Vertical";
            this.HelmholtzVertical.Range = new Range(-90, 90);
            this.HelmholtzVertical.Value = 0;
            this.HelmholtzVertical.ValueChanged += (o, e) => { };
            this.HelmholtzTorsion.Text = "Torsion";
            this.HelmholtzTorsion.Range = new Range(-90, 90);
            this.HelmholtzTorsion.Value = 0;
            this.HelmholtzTorsion.ValueChanged += (o, e) => { };

            this.HelmholtzHorizontal.Enabled = false;
            this.HelmholtzVertical.Enabled = false;
            this.HelmholtzTorsion.Enabled = false;

            this.FickHorizontal.Enabled = false;
            this.FickVertical.Enabled = false;
            this.FickTorsion.Enabled = false;

            this.HessHorizontal.Text = "Horizontal";
            this.HessHorizontal.Range = new Range(-90, 90);
            this.HessHorizontal.Value = 0;
            this.HessHorizontal.ValueChanged += Hess_ValueChanged;
            this.HessVertical.Text = "Vertical";
            this.HessVertical.Range = new Range(-90, 90);
            this.HessVertical.Value = 0;
            this.HessVertical.ValueChanged += Hess_ValueChanged;
            this.HessTorsion.Text = "Torsion";
            this.HessTorsion.Range = new Range(-90, 90);
            this.HessTorsion.Value = 0;
            this.HessTorsion.ValueChanged += Hess_ValueChanged;

            this.HarmsHorizontal.Text = "Horizontal";
            this.HarmsHorizontal.Range = new Range(-90, 90);
            this.HarmsHorizontal.Value = 0;
            this.HarmsHorizontal.ValueChanged += Harms_ValueChanged;
            this.HarmsVertical.Text = "Vertical";
            this.HarmsVertical.Range = new Range(-90, 90);
            this.HarmsVertical.Value = 0;
            this.HarmsVertical.ValueChanged += Harms_ValueChanged;
            this.HarmsTorsion.Text = "Torsion";
            this.HarmsTorsion.Range = new Range(-90, 90);
            this.HarmsTorsion.Value = 0;
            this.HarmsTorsion.ValueChanged += Harms_ValueChanged;

            this.RotationVectorX.Text = "X (line of sight axis)";
            this.RotationVectorX.Range = new Range(-1, 1);
            this.RotationVectorX.Value = 0;
            //this.RotationVectorX.ValueChanged += RotationVector_ValueChanged;
            this.RotationVectorY.Text = "Y (Interaural axis)";
            this.RotationVectorY.Range = new Range(-1, 1);
            this.RotationVectorY.Value = 0;
            //this.RotationVectorY.ValueChanged += RotationVector_ValueChanged;
            this.RotationVectorZ.Text = "Z (Vertical axis)";
            this.RotationVectorZ.Range = new Range(-1, 1);
            this.RotationVectorZ.Value = 0;
            //this.RotationVectorZ.ValueChanged += RotationVector_ValueChanged;
        }


        private void ListingValueChanged(object sender, EventArgs e)
        {
            currentOrientation.Angle = this.ListingAngle.Value / 180 * Math.PI;
            currentOrientation.Eccentricity = this.ListingEccentricity.Value / 180 * Math.PI;
            currentOrientation.Torsion = this.ListingTorsion.Value;

            OnNewGaze();
        }

        private void Fick_ValueChanged(object sender, EventArgs e)
        {

        }

        void RotationVector_ValueChanged(object sender, EventArgs e)
        {
            var x = this.RotationVectorX.Value;
            var y = this.RotationVectorY.Value;
            var z = this.RotationVectorZ.Value;

            // Asuming torsion zero
            currentOrientation.Angle = Math.Atan2(y, z);
            currentOrientation.Eccentricity = 2 * Math.Atan(Math.Sqrt( y * y + z * z));
            currentOrientation.Torsion = 2 * Math.Atan(x);

            OnNewGaze();
        }

        void Hess_ValueChanged(object sender, EventArgs e)
        {
            var h = HessHorizontal.Value/180*Math.PI;
            var v = HessVertical.Value / 180 * Math.PI;
            currentOrientation.Angle = Math.Atan2(v, h);
            currentOrientation.Eccentricity = Math.Sqrt(h * h + v * v);
            currentOrientation.Torsion = HessTorsion.Value/180*Math.PI;

            OnNewGaze();
        }

        private void Harms_ValueChanged(object sender, EventArgs ea)
        {
            var h = HarmsHorizontal.Value / 180 * Math.PI;
            var v = HarmsVertical.Value / 180 * Math.PI;

            currentOrientation.Angle = Math.Atan2(Math.Tan(v), Math.Tan(h));
            currentOrientation.Eccentricity = Math.Atan(Math.Sqrt(Math.Tan(h) * Math.Tan(h) + Math.Tan(v) * Math.Tan(v)));
            currentOrientation.Torsion = HessTorsion.Value / 180 * Math.PI;

            var a = currentOrientation.Angle;
            var e = currentOrientation.Eccentricity;
            var hh = Math.Atan(Math.Cos(a) * Math.Tan(e)) * 180 / Math.PI;
            var vv = Math.Atan(Math.Sin(a) * Math.Tan(e)) * 180 / Math.PI;

            OnNewGaze();
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                var x = -(e.X - panel1.Width / 2.0) / panel1.Width * 2.0;
                var y = -(e.Y - panel1.Height / 2.0) / panel1.Height * 2.0;
                if (Math.Sqrt(x * x + y * y) < 1)
                {
                    currentOrientation.Angle = Math.Atan2(y, x);
                    currentOrientation.Eccentricity = Math.Asin(Math.Sqrt(x * x + y * y));

                    OnNewGaze();
                }
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                currentOrientation.Torsion = -(e.Y - panel1.Height / 2.0) / panel1.Height * 2.0;

                OnNewGaze();
            }
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            this.currentOrientation = new GazeOrientation();

            OnNewGaze();
        }

        protected void OnNewGaze()
        {
            var a = currentOrientation.Angle;
            var e = currentOrientation.Eccentricity;
            var t = currentOrientation.Torsion;

            this.ListingAngle.EnableValueChangedEvent = false;
            this.ListingEccentricity.EnableValueChangedEvent = false;
            this.ListingTorsion.EnableValueChangedEvent = false;
            this.ListingAngle.Value = (int)Math.Round( a * 180.0 / Math.PI);
            this.ListingEccentricity.Value = (int)Math.Round(e * 180 / Math.PI);
            this.ListingTorsion.Value = (int)Math.Round(t * 180 / Math.PI);
            this.ListingAngle.EnableValueChangedEvent = true;
            this.ListingEccentricity.EnableValueChangedEvent = true;
            this.ListingTorsion.EnableValueChangedEvent = true;

            this.HessHorizontal.EnableValueChangedEvent = false;
            this.HessVertical.EnableValueChangedEvent = false;
            this.HessTorsion.EnableValueChangedEvent = false;
            this.HessHorizontal.Value = (int)Math.Round(Math.Cos(a) * e * 180 / Math.PI);
            this.HessVertical.Value = (int)Math.Round(Math.Sin(a) * e * 180 / Math.PI);
            this.HessTorsion.Value = (int)Math.Round(t * 180 / Math.PI);
            this.HessHorizontal.EnableValueChangedEvent = true;
            this.HessVertical.EnableValueChangedEvent = true;
            this.HessTorsion.EnableValueChangedEvent = true;

            this.HarmsHorizontal.EnableValueChangedEvent = false;
            this.HarmsVertical.EnableValueChangedEvent = false;
            this.HarmsTorsion.EnableValueChangedEvent = false;
            this.HarmsHorizontal.Value = (int)Math.Round(Math.Atan(Math.Cos(a) * Math.Tan(e)) * 180 / Math.PI);
            this.HarmsVertical.Value = (int)Math.Round(Math.Atan(Math.Sin(a) * Math.Tan(e)) * 180 / Math.PI);
            this.HarmsTorsion.Value = (int)Math.Round(t * 180 / Math.PI);
            this.HarmsHorizontal.EnableValueChangedEvent = true;
            this.HarmsVertical.EnableValueChangedEvent = true;
            this.HarmsTorsion.EnableValueChangedEvent = true;

            this.FickHorizontal.EnableValueChangedEvent = false;
            this.FickVertical.EnableValueChangedEvent = false;
            this.FickTorsion.EnableValueChangedEvent = false;
            this.FickHorizontal.Value = this.HarmsHorizontal.Value;
            this.FickVertical.Value = this.HessVertical.Value;
            this.FickTorsion.Value = (int)Math.Round(t * 180 / Math.PI);
            this.FickHorizontal.EnableValueChangedEvent = true;
            this.FickVertical.EnableValueChangedEvent = true;
            this.FickTorsion.EnableValueChangedEvent = true;

            this.HelmholtzHorizontal.EnableValueChangedEvent = false;
            this.HelmholtzVertical.EnableValueChangedEvent = false;
            this.HelmholtzTorsion.EnableValueChangedEvent = false;
            this.HelmholtzHorizontal.Value = this.HessHorizontal.Value;
            this.HelmholtzVertical.Value = this.HarmsVertical.Value;
            this.HelmholtzTorsion.Value = (int)Math.Round(t * 180 / Math.PI);
            this.HelmholtzHorizontal.EnableValueChangedEvent = true;
            this.HelmholtzVertical.EnableValueChangedEvent = true;
            this.HelmholtzTorsion.EnableValueChangedEvent = true;

            //this.RotationVectorX.EnableValueChangedEvent = false;
            //this.RotationVectorY.EnableValueChangedEvent = false;
            //this.RotationVectorZ.EnableValueChangedEvent = false;
            //this.RotationVectorX.Value = Math.Tan(currentOrientation.Torsion/2);
            //this.RotationVectorY.Value = this.HarmsVertical.Value;
            //this.RotationVectorZ.Value = Math.Sqrt(Math.Pow(Math.Tan(currentOrientation.Eccentricity/2),2)/(1+Math.Pow(Math.Tan(currentOrientation.Angle),2)));
            //this.RotationVectorX.EnableValueChangedEvent = true;
            //this.RotationVectorY.EnableValueChangedEvent = true;
            //this.RotationVectorZ.EnableValueChangedEvent = true;

            //// Asuming torsion zero
            //currentOrientation.Angle = Math.Atan2(y, z);
            //currentOrientation.Eccentricity = 2 * Math.Atan(Math.Sqrt(y * y + z * z));
            //currentOrientation.Torsion = 2 * Math.Atan(x);


            NewGaze(this, this.currentOrientation);
        }

        private void sliderTextControl3_Load(object sender, EventArgs e)
        {

        }

    }

    /// <summary>
    /// Gaze orientation in listing coordinates.
    /// </summary>
    public struct GazeOrientation
    {
        /// <summary>
        /// Direction of gaze in radians.
        /// </summary>
        public double Angle;

        /// <summary>
        /// Eccentricity of gaze along the direction in radians.
        /// </summary>
        public double Eccentricity;

        /// <summary>
        /// Rotation around the line of sight in radians.
        /// </summary>
        public double Torsion;
    }
}
