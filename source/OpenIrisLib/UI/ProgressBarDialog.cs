using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenIris.UI
{
#nullable enable

    /// <summary>
    /// 
    /// </summary>
    public partial class ProgressBarDialog : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public ProgressBarDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Progress
        {
            set
            {
                if (value >= 0 && value <= 100)
                    this.BeginInvoke((Action)(() => this.progressBar1.Value = value));
                ;
            }
            get { return this.progressBar1.Value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Cancelled?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler? Cancelled;
    }
}
