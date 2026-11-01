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

        private readonly CalibrationPipelineRemoteLinear pipeline;

        /// <summary>
        /// Updates the UI with the state of the calibration.
        /// </summary>
        public void UpdateUI()
        {
            var fixations = pipeline.Fixations;
            this.label1.Text = $"Status: {pipeline.Status}";
            this.label2.Text = $"Num Fixations: {fixations.Count}";
            if (this.dataGridView1.DataSource != fixations) this.dataGridView1.DataSource = fixations;
        }

        #endregion ICalibrationUI Members
        public CalibrationRemoteLinearUI(CalibrationPipelineRemoteLinear pipeline)
        {
            this.pipeline = pipeline;
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

    /// <summary>
    /// Shows the remote linear calibration in its tab.
    /// </summary>
    internal sealed class CalibrationRemoteLinearPanel : UI.CalibrationPanel<CalibrationPipelineRemoteLinear>
    {
        protected override ICalibrationUIControl Create(CalibrationPipelineRemoteLinear pipeline) => new CalibrationRemoteLinearUI(pipeline);
    }
}
