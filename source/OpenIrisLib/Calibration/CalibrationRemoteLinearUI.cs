using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;

namespace OpenIris.Calibration
{
#nullable enable

    using System.Windows.Forms;
    using OpenIris;

    public partial class CalibrationRemoteLinearUI : UserControl, ICalibrationUIControl
    {

        #region ICalibrationUI Members

        public ICalibrationUIControl? GetCalibrationUI() => this;

        public String status = "Waiting";
        public List<FixationGridViewRow> fix_data = new List<FixationGridViewRow>();

        /// <summary>
        /// Updates the UI with the image from last frame and current calibration.
        /// </summary>
        public void UpdateUI()
        {
            this.label1.Text = $"Status: {status}";
            this.label2.Text = $"Num Fixations: {fix_data.Count}";
            this.dataGridView1.DataSource = this.fix_data;
        }

        #endregion ICalibrationUI Members
        public CalibrationRemoteLinearUI()
        {
            InitializeComponent();
        }

        private void RemoteCalibrationUI_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }

    public class FixationGridViewRow
    {
        public int FixNum { get; set; }
        public float FixX { get; set; }
        public float FixY { get; set; }
        public float? LeftX { get; set; }
        public float? LeftY { get; set; }
        public float? RightX { get; set; }
        public float? RightY { get; set; }
    }
}
