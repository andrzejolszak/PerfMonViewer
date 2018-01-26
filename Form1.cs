using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Charting;
using System.Reactive;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Humanizer;
using Tx.Windows;

namespace PerfMonViewerDeluxe
{
    public partial class Form1 : Form
    {
        private Dictionary<string, List<PerformanceSample>> _performanceData;
        private Dictionary<string, FastLine> _trends = new Dictionary<string, FastLine>();
        private DateTime _start = new DateTime(2999, 12, 12);
        private DateTime _end = new DateTime(1024, 12, 12);

        public Form1()
        {
            InitializeComponent();

            SetupZoomInteractions(this._overviewChart);
            SetupDefaultVisualStyles(this._overviewChart);
            _overviewChart.FormatNumber += FormatNumber;
        }

        public static void SetupZoomInteractions(System.Windows.Forms.DataVisualization.Charting.Chart targetChart)
        {
            foreach (System.Windows.Forms.DataVisualization.Charting.ChartArea area in targetChart.ChartAreas)
            {
                area.CursorX.IsUserEnabled = true;
                area.CursorX.IsUserSelectionEnabled = true;
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisX.ScrollBar.IsPositionedInside = true;

                targetChart.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        area.AxisX.ScaleView.ZoomReset();
                    }
                };
            }
        }

        public static void SetupDefaultVisualStyles(System.Windows.Forms.DataVisualization.Charting.Chart targetChart)
        {
            foreach (System.Windows.Forms.DataVisualization.Charting.ChartArea area in targetChart.ChartAreas)
            {
                area.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
                area.AxisX.MinorGrid.LineColor = System.Drawing.Color.Gainsboro;
                area.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
                area.AxisY.MinorGrid.LineColor = System.Drawing.Color.Gainsboro;
            }
        }

        private void FormatNumber(object sender, FormatNumberEventArgs e)
        {
            e.LocalizedValue = e.Value.ToMetric();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            this.openFileDialog1.FileName = "*.blg";
            this.openFileDialog1.Filter = "PerfMon Binary Logs|*.blg";
            this.openFileDialog1.Multiselect = true;

            DialogResult res = this.openFileDialog1.ShowDialog(this);

            if (res == DialogResult.OK)
            {
                string fileName = this.openFileDialog1.FileName;

                this._performanceData = new Dictionary<string, List<PerformanceSample>>();

                var playback = new Playback();
                playback.AddPerfCounterTraces(fileName);
                playback.GetObservable<PerformanceSample>().Subscribe(StoreData);
                playback.Run();

                this.listBox1.Items.Clear();
                foreach (string id in this._performanceData.Keys.OrderBy(x => x).ToList())
                {
                    this.listBox1.Items.Add(id);
                }

                this.Text = this._start.ToShortDateString() + " - " + this._end.ToShortDateString() + ": " + fileName;
            }
        }

        private void StoreData(PerformanceSample sample)
        {
            string id = sample.CounterSet + "->" + sample.CounterName + "->" + sample.Instance;
            List<PerformanceSample> list;
            if (!this._performanceData.TryGetValue(id, out list))
            {
                list = new List<PerformanceSample>();
                this._performanceData.Add(id, list);
            }

            list.Add(sample);

            if (sample.Timestamp > this._end)
            {
                this._end = sample.Timestamp;
            }

            if (sample.Timestamp < this._start)
            {
                this._start = sample.Timestamp;
            }
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            _overviewChart.Series.Clear();
            this._trends.Clear();

            double maxVal = 0;
            foreach (object id in this.listBox1.SelectedItems)
            {
                string ids = id.ToString();
                List<PerformanceSample> samples = this._performanceData[ids];

                foreach (PerformanceSample s in samples)
                {
                    FastLine trend;
                    if (!_trends.TryGetValue(ids, out trend))
                    {
                        trend = new FastLine { LegendText = ids };
                        trend.BorderWidth = 3;
                        _trends.Add(ids, trend);
                        _overviewChart.Series.Add(trend);
                    }

                    trend.Add(s.Timestamp.ToLongTimeString(), new FastLine.DataPoint(s.Value));

                    if (s.Value > maxVal)
                    {
                        maxVal = s.Value;
                    }
                }
            }

            //_overviewChart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            _overviewChart.ChartAreas[0].AxisY.Maximum = maxVal;

            _overviewChart.Invalidate();
        }
    }
}