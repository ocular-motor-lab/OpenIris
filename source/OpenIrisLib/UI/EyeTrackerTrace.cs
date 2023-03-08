//-----------------------------------------------------------------------
// <copyright file="EyeTrackerTrace.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using OpenIris;
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;
    
    /// <summary>
    /// Control to show the traces of the data.
    /// </summary>
    public partial class EyeTrackerTrace : UserControl
    {
        private double lastSampleRate = double.NaN;
        private double lastSpan = double.NaN;
        private EyeTrackerSettings? settings;

        /// <summary>
        /// True if already initialized.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerTrace class.
        /// </summary>
        public EyeTrackerTrace()
        {
            Initialized = false;
            InitializeComponent();

            var chartAreaHorizontal = new TraceChartArea(tracesChart, DataStream.H, "Horizontal")
            {
                DefaultRange = new Range(-20, 20)
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHorizontal, Eye.Left));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHorizontal, Eye.Right));

            var chartAreaVertical = new TraceChartArea(tracesChart, DataStream.V, "Vertical")
            {
                DefaultRange = new Range(-20, 20)
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaVertical, Eye.Left));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaVertical, Eye.Right));

            var chartAreaTorsion = new TraceChartArea(tracesChart, DataStream.T, "Torsion")
            {
                DefaultRange = new Range(-20, 20)
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaTorsion, Eye.Left));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaTorsion, Eye.Right));

            var chartAreaPupil = new TraceChartArea(tracesChart, DataStream.P, "Pupil")
            {
                DefaultRange = new Range(0, 200),
                ShouldZoom = false
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaPupil, Eye.Left));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaPupil, Eye.Right));

            var chartAreaEyelid = new TraceChartArea(tracesChart, DataStream.E, "EyeLids")
            {
                DefaultRange = new Range(-10, 110),
                ShouldZoom = false
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaEyelid, Eye.Left));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaEyelid, Eye.Right));

            var chartAreaHeadVelocity = new TraceChartArea(tracesChart, DataStream.HVR, "HeadVelocity")
            {
                DefaultRange = new Range(-500, 500),
                ShouldZoom = false
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadVelocity, Eye.Right, Color.Blue));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadVelocity, Eye.Right, Color.Red));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadVelocity, Eye.Right, Color.Green));

            var chartAreaHeadAcceleration = new TraceChartArea(tracesChart, DataStream.HR, "HeadAcceleration")
            {
                DefaultRange = new Range(-2, 2),
                ShouldZoom = false
            };
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadAcceleration, Eye.Right, Color.Blue));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadAcceleration, Eye.Right, Color.Red));
            tracesChart.Series.Add(new EyeTraceSeries(chartAreaHeadAcceleration, Eye.Right, Color.Green));

            foreach (var chartArea in tracesChart.ChartAreas)
            {
                if (chartArea is TraceChartArea traceChart)
                {
                    if (traceChart.Visible)
                    {
                        traceChart.ResetAxis();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the graph.
        /// </summary>
        public void Init(EyeTrackerSettings settings)
        {
            this.settings = settings;

            if (Initialized) return;

            try
            { 
                tracesChart.SuspendLayout();

                UpdateLayout();
                UpdateVisibleCharts();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                tracesChart.ResumeLayout();
            }

            Initialized = true;
        }
        
        /// <summary>
        /// Updates the traces with new data.
        /// </summary>
        /// <param name="dataBuffer">Data buffer.</param>
        public void Update(EyeTrackerDataBuffer dataBuffer)
        {
            try
            {
                if (settings == null) throw new Exception();

                var rawFrameRate = dataBuffer.CurrentFrameRate;

                if (double.IsNaN(rawFrameRate)) return;

                tracesChart.SuspendLayout();
                tracesChart.Series.SuspendUpdates();

                // If framerate has changed update the text label and the 
                // x coordinate of the points.
                var frameRate = Math.Round(rawFrameRate / 5) * 5;

                if (lastSampleRate != frameRate || lastSpan != settings.TraceSpan)
                {
                    toolStripLabelFrameRate.Text = $"[{frameRate:0} fps]";
                    lastSampleRate = frameRate;
                    lastSpan = settings.TraceSpan;
                }

                UpdateLayout();

                foreach (var series in tracesChart.Series)
                {
                    if (series is EyeTraceSeries traceSeries  && traceSeries.traceChartArea.Visible)
                    {
                        traceSeries.Update(dataBuffer, frameRate, settings.TraceSpan);
                    }
                }

                tracesChart.Series.ResumeUpdates();
                tracesChart.ResumeLayout();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }

        private void UpdateLayout()
        {
            UpdateTimeSpan();
            UpdateYAxis();
        }

        private void UpdateYAxis()
        {
            foreach (var chartArea in tracesChart.ChartAreas)
            {
                if (chartArea is TraceChartArea traceChartArea)
                {
                    if (!double.IsNaN(traceChartArea.MovingAverage))
                    {
                        var offset = -traceChartArea.MovingAverage;
                        if (!moveVerticalAxisWithDataToolStripMenuItem.Checked)
                        {
                            offset = 0;
                        }

                        traceChartArea.AxisY.Maximum = -offset + (traceChartArea.DataRange) / 2.0;
                        traceChartArea.AxisY.Minimum = -offset - (traceChartArea.DataRange) / 2.0;



                        var intervalOffset = offset % traceChartArea.AxisY.Interval;
                        traceChartArea.AxisY.Interval = traceChartArea.DataRange / 4;
                        traceChartArea.AxisY.IntervalOffset = intervalOffset;

                        traceChartArea.AxisY.MinorGrid.IntervalOffset = intervalOffset;
                        traceChartArea.AxisY.MajorGrid.IntervalOffset = intervalOffset;
                        traceChartArea.AxisY.LabelStyle.IntervalOffset = intervalOffset;

                    }
                }
            }
        }

        private void UpdateTimeSpan()
        {
            if (settings == null) throw new Exception();

            var timeSpan = 10.0;

            if (double.TryParse(toolStripComboBoxSpan.Text, out timeSpan))
            {
                if (timeSpan > 50)
                {
                    timeSpan = 50;
                }

                if (timeSpan != settings.TraceSpan)
                {
                    settings.TraceSpan = timeSpan;
                    toolStripComboBoxSpan.Text = settings.TraceSpan.ToString();

                    foreach (TraceChartArea traceChartArea in tracesChart.ChartAreas)
                    {
                        traceChartArea.AxisX.Minimum = 0;
                        traceChartArea.AxisX.Maximum = timeSpan;
                    }
                }
            }
            else
            {
                timeSpan = settings.TraceSpan;
                toolStripComboBoxSpan.Text = timeSpan.ToString();

                foreach (TraceChartArea traceChartArea in tracesChart.ChartAreas)
                {
                    traceChartArea.AxisX.Minimum = 0;
                    traceChartArea.AxisX.Maximum = timeSpan;
                }
            }
        }

        private void UpdateVisibleCharts()
        {
            tracesChart.ChartAreas["H"].Visible = horizontalToolStripMenuItem.Checked;
            tracesChart.ChartAreas["V"].Visible = verticalToolStripMenuItem.Checked;
            tracesChart.ChartAreas["T"].Visible = torsionToolStripMenuItem.Checked;
            tracesChart.ChartAreas["HVR"].Visible = headToolStripMenuItem.Checked;
            tracesChart.ChartAreas["HR"].Visible = headAccelerationToolStripMenuItem.Checked;
            tracesChart.ChartAreas["P"].Visible = pupilToolStripMenuItem.Checked;
            tracesChart.ChartAreas["E"].Visible = eyeLidsToolStripMenuItem.Checked;

            var menuPercent = (float)horizontalToolStripMenuItem.Height / this.Height*100f;

            var visibleChartAreas = tracesChart.ChartAreas.Where(c => c.Visible).ToArray();
            var h = menuPercent;
            if (visibleChartAreas.Length > 0)
            {
                foreach (var chartarea in visibleChartAreas)
                {
                    chartarea.AlignWithChartArea = visibleChartAreas.First().Name;
                    chartarea.Position.Width = 100;
                    chartarea.Position.Height = (100- menuPercent) / visibleChartAreas.Length;
                    chartarea.Position.Y = h;
                    chartarea.AxisX.LabelStyle.Enabled = false;
                    h += chartarea.Position.Height;
                }

                visibleChartAreas.Last().AxisX.LabelStyle.Enabled = true;
            }
        }

        private void ZoomIn()
        {
            TraceChartArea.ZoomLevel++;
        }

        private void ZoomOut()
        {
            TraceChartArea.ZoomLevel--;
        }
        
        private void Chart1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ZoomIn();
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ZoomOut();
            }
        }

        private void SelectPlots_Click(object sender, EventArgs e)
        {
            UpdateVisibleCharts();
        }

        private void visibleChartsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateVisibleCharts();
        }
        
        private void toolStripComboBoxSpan_TextUpdate(object sender, EventArgs e)
        {
            int timeSpan = 10;
            int.TryParse(toolStripComboBoxSpan.Text, out timeSpan);

            if (timeSpan < 10)
            {
                timeSpan = 10;
            }
            if (timeSpan > 50)
            {
                timeSpan = 50;
            }
            toolStripComboBoxSpan.Text = timeSpan.ToString();

            if (settings != null) settings.TraceSpan = timeSpan;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                tracesChart.SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                Clipboard.SetImage(bm);
            }
        }

        private void toolStripButtonZoomIn_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void toolStripButtonZoomOut_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void resetPlotsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var chartArea in tracesChart.ChartAreas)
            {
                if (chartArea is TraceChartArea traceChartArea)
                {
                    traceChartArea.ResetAxis();
                }
            }
        }

        private void headAccelerationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateVisibleCharts();
        }
    }

    class TraceChartArea : ChartArea
    {
        public DataStream DataSource { get; private set; }
        private static int zoomLevel = 5;
        public static double lastSampleRate = 100;

        public static int ZoomLevel
        {
            get
            {
                return zoomLevel;
            }

            set
            {
                if (value >= 0 && value < 20)
                {
                    zoomLevel = value;
                }
            }
        }

        public double[] zoomLevels = new double[] { 200, 100, 80, 60, 40, 20, 15, 10, 5, 4, 3, 2, 1, 0.5, 0.4, 0.3, 0.2, 0.1, 0.05, 0.01 };

        public Range DefaultRange { get; set; }

        public double MovingAverage { get; set; }

        public double DataRange
        {
            get
            {
                if (ShouldZoom)
                {
                    return zoomLevels[zoomLevel];
                }
                else
                {
                    return DefaultRange.End - DefaultRange.Begin;
                }
            }
        }

        public bool ShouldZoom { get; set; }
        
        public TraceChartArea(Chart chart, DataStream signal, string yLabel)
            : base(signal.ToString())
        {
            DataSource = signal;
            ShouldZoom = true;

            chart.ChartAreas.Add(this);

            // Set up axes
            AxisY.Title = yLabel;
            AxisY.TitleFont = new Font(FontFamily.GenericSansSerif, 16);
            AxisY.LineColor = Color.Gray;
            AxisY.LineWidth = 0;

            AxisY.MinorGrid.Enabled = true;
            AxisY.MinorGrid.LineColor = Color.FromArgb(240, 240, 240);

            AxisY.MajorTickMark.Enabled = false;
            AxisY.MajorGrid.Enabled = true;
            AxisY.MajorGrid.LineColor = Color.FromArgb(200, 200, 200);
            
            AxisX.MajorGrid.LineColor = Color.FromArgb(200, 200, 200);
            AxisX.TitleFont = new Font(FontFamily.GenericSansSerif, 16);

            AxisX.LineWidth = 0;
            AxisX.MajorTickMark.Enabled = false;

            ResetAxis();
        }

        public void ResetAxis()
        {
            MovingAverage = (DefaultRange.End + DefaultRange.Begin) / 2.0;
        }
    }

    class EyeTraceSeries : Series
    {
        public TraceChartArea traceChartArea;
        private readonly Eye whichEye;
        private readonly DataStream dataSource;

        public static int numberOfPoints = 500;

        public EyeTraceSeries(TraceChartArea chartArea, Eye whichEye)
            :this(chartArea, whichEye, (whichEye == Eye.Left) ? Color.RoyalBlue : Color.Red)
        {
        }

        public EyeTraceSeries(TraceChartArea chartArea, Eye whichEye, Color color)
        {
            traceChartArea = chartArea;
            this.whichEye = whichEye;
            dataSource = chartArea.DataSource;

            ChartArea = chartArea.Name;

            ChartType = SeriesChartType.FastLine;
            BorderWidth = 1;
            Color = color;

            for (int i = 0; i < numberOfPoints; i++)
            {
                Points.AddXY(i / 100.0, 0);
            }

            traceChartArea.MovingAverage = double.NaN;
        }

        public void SetPoint(int x, double y)
        {
            if (double.IsNaN(y) || double.IsInfinity(y))
            {
                Points[x].IsEmpty = true;
                y = 0;
            }
            else
            {
                Points[x].IsEmpty = false;

                if (double.IsNaN(traceChartArea.MovingAverage))
                {
                    traceChartArea.MovingAverage = y;
                }
                else
                {
                    traceChartArea.MovingAverage = traceChartArea.MovingAverage * 0.99 + y * 0.01;
                }
            }

            Points[x].YValues.SetValue(y, 0);
        }

        private int lastPointUpdated = -1;
        private long lastFrameUpdated = -1;
        private double lastSampleRate = double.NaN;
        private double lastSpan = double.NaN;

        public void Update(EyeTrackerDataBuffer dataBuffer, double frameRate, double traceSpan)
        {
            // If the frame rate or the time span has changed reset the time series
            // Also if the current frame is smaller than the last frame updated it 
            // must be that the cameras have restarted or the video has restarted
            // So we must reset all the points.
            if (lastSampleRate != frameRate || lastSpan != traceSpan || lastFrameUpdated > dataBuffer.LastFrameUpdated)
            {
                // Reset the time
                for (int i = 0; i < EyeTraceSeries.numberOfPoints; i++)
                {
                    Points[i].XValue = i / (double)EyeTraceSeries.numberOfPoints * traceSpan;
                    for (int j = 0; j < Points[i].YValues.Count(); j++)
                    {
                        Points[i].YValues[j] = 0;
                    }
                }
                
                lastSampleRate = frameRate;
                lastSpan = traceSpan;
                lastPointUpdated = -1;
            }

            var span = traceSpan;
            var framesPerPoint = frameRate * span / EyeTraceSeries.numberOfPoints;
            var pointsPerFrame = 1 / framesPerPoint;
            
            // the first point to update will be the next to the last updated.
            // the last point to update will depend on the last frame in the buffer.
            // Here we do not take into account circularity of the point sequence
            // because we need it for the for loop. Then inside the loop we correct
            // for circularity. Add a delay of 5 so it does not try to update points when not
            // all data is there.
            var lastFrameToUpdate = dataBuffer.LastFrameUpdated - 5;
            var lastPointToUpdate = (long)Math.Round(lastFrameToUpdate * pointsPerFrame);
            lastFrameUpdated = lastFrameToUpdate;

            for (long currentPoint = lastPointUpdated + 1; currentPoint <= lastPointToUpdate; currentPoint++)
            {
                var seriesIdx = (int)currentPoint % EyeTraceSeries.numberOfPoints;
                var frameIdx = (int)Math.Floor((currentPoint * framesPerPoint) % dataBuffer.Length);

                lastPointUpdated = (int)currentPoint;

                if (dataBuffer[frameIdx] != null)
                {
                    SetPoint(seriesIdx, dataBuffer[frameIdx, whichEye, dataSource]);
                }
                else
                {
                    SetPoint(seriesIdx, double.NaN);
                }
            }
        }
    }
}
