using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenIris.UI
{
    public partial class DebugImagesPanel : UserControl
    {
        public DebugImagesPanel()
        {
            InitializeComponent();
        }

        public void UpdateUI()
        {

            try
            {
                /* var CROSSCORR = true;
                 var CURVATURE = false;
                 if (CROSSCORR)
                 {
                     // Cross correlations
                     var maxTorsion = eyeTrackerViewModel.Settings.Tracking.MaxTorsion;
                     var xcorrSize = (int)Math.Round(maxTorsion * 10);
                     // Initialize charts
                     if (chartBottom.Series.Count == 0)
                     {
                         var seriesLeft = new System.Windows.Forms.DataVisualization.Charting.Series("Left");
                         seriesLeft.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                         var seriesRight = new System.Windows.Forms.DataVisualization.Charting.Series("Right");
                         seriesRight.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                         chartBottom.Series.Add(seriesLeft);
                         chartBottom.Series.Add(seriesRight);

                         for (int i = 0; i < xcorrSize; i++)
                         {
                             chartBottom.Series[0].Points.AddXY((i - xcorrSize / 2.0) / xcorrSize * maxTorsion * 2.0, 0);
                             chartBottom.Series[1].Points.AddXY((i - xcorrSize / 2.0) / xcorrSize * maxTorsion * 2.0, 0);
                         }

                         chartBottom.Series[0].Color = Color.RoyalBlue;
                         chartBottom.Series[1].Color = Color.Red;

                         chartBottom.ChartAreas[0].AxisX.MajorTickMark.Interval = 5;
                         chartBottom.ChartAreas[0].AxisX.LabelStyle.Interval = 5;
                         chartBottom.ChartAreas[0].AxisX.MajorGrid.Interval = 5;
                         chartBottom.ChartAreas[0].AxisX.Minimum = -maxTorsion;
                         chartBottom.ChartAreas[0].AxisX.Maximum = +maxTorsion;
                         chartBottom.ChartAreas[0].AxisX.MinorGrid.Interval = 1;
                         chartBottom.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
                         chartBottom.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.Gray;

                         chartBottom.ChartAreas[0].AxisY.Maximum = 255;
                         chartBottom.ChartAreas[0].AxisY.Minimum = 0;
                     }

                     var imageCorrLeft = EyeTrackerDebug.DebugImageManager.GetImage("crosscorrelation", Eye.Left);
                     if (imageCorrLeft != null)
                     {
                         var leftXcorr = imageCorrLeft.Resize(1, xcorrSize, Emgu.CV.CvEnum.Inter.Linear);

                         for (int i = 0; i < xcorrSize; i++)
                         {
                             chartBottom.Series[0].Points[i].SetValueY(leftXcorr.Data[i, 0, 0]);
                         }
                     }

                     var imageCorrRight = EyeTrackerDebug.DebugImageManager.GetImage("crosscorrelation", Eye.Right);
                     if (imageCorrRight != null)
                     {
                         var rightXcorr = imageCorrRight.Resize(1, xcorrSize, Emgu.CV.CvEnum.Inter.Linear);
                         for (int i = 0; i < xcorrSize; i++)
                         {
                             chartBottom.Series[1].Points[i].SetValueY(rightXcorr.Data[i, 0, 0]);
                         }
                     }

                     chartBottom.Invalidate();
                 }
                if (CURVATURE)
                {
                }*/

                foreach (var key in EyeTrackerDebug.Images.Keys.OrderBy(k => k).ToArray())
                {
                    if (!listBox1.Items.Contains(key))
                    {
                        listBox1.Items.Add(key);
                    }
                }

                var imagesLeft = new System.Collections.Generic.List<Emgu.CV.UI.ImageBox>(new Emgu.CV.UI.ImageBox[4] {
                        imageBoxLeft1,imageBoxLeft2,imageBoxLeft3,imageBoxLeft4
                    });
                var imagesRight = new System.Collections.Generic.List<Emgu.CV.UI.ImageBox>(new Emgu.CV.UI.ImageBox[4] {
                        imageBoxRight1,imageBoxRight2,imageBoxRight3,imageBoxRight4
                    });

                // Only allow 4 selected items
                var a = listBox1.SelectedIndices;
                for (int i = 4; i < a.Count; i++)
                {
                    listBox1.SetSelected(a[i], false);
                }

                var selectedKeys = listBox1.SelectedItems;
                for (int i = 0; i < 4; i++)
                {
                    if (i < selectedKeys.Count)
                    {
                        imagesLeft[i].Image = EyeTrackerDebug.Images[(string)selectedKeys[i]][Eye.Left];
                        imagesRight[i].Image = EyeTrackerDebug.Images[(string)selectedKeys[i]][Eye.Right];
                    }
                    else
                    {
                        imagesLeft[i].Image = null;
                        imagesRight[i].Image = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}
