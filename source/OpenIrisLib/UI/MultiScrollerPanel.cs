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
    public partial class MultiScrollerPanel : UserControl
    {
        public event EventHandler? ValueChanged;

        public MultiScrollerPanel()
        {
            InitializeComponent();

            var sliderPupil = new SliderTextControl();
            sliderPupil.Text = "Pupil";
            sliderPupil.Range = new OpenIris.Range(0, 255);
            sliderPupil.ValueChanged += (o, e) => this.ValueChanged?.Invoke(o, e);
            sliderPupil.Dock = DockStyle.Fill;

            var sliderCR = new SliderTextControl();
            sliderCR.Text = "CR";
            sliderCR.Range = new OpenIris.Range(0, 255);
            sliderCR.ValueChanged += (o, e) => this.ValueChanged?.Invoke(o, e);
            sliderCR.Dock = DockStyle.Fill;

            var table = new TableLayoutPanel();
            table.RowCount = 2;
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            table.Dock = DockStyle.Fill;
            table.Controls.Add(sliderPupil, 0, 0);
            table.Controls.Add(sliderCR, 1, 0);


            Controls.Add(table);
        }

        private void UpdateSettings()
        {

        }
    }
}
