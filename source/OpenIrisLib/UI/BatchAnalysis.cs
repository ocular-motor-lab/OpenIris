//-----------------------------------------------------------------------
// <copyright file="BatchAnalysis.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// Class to do batch processing of multiple files.
    /// </summary>
    public partial class BatchProcessing : Form
    {
        private bool closing = false;
        private EyeTracker eyeTracker;
        private List<ProcessVideoOptions>? optionsList;
        private Task? batchTask;

        /// <summary>
        /// Initializes the form.
        /// </summary>
        /// <param name="eyeTracker"></param>
        public BatchProcessing(EyeTracker eyeTracker)
        {
            this.eyeTracker = eyeTracker;
            InitializeComponent();
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            var options = SelectVideoDialog.SelectVideoForProcessing(eyeTracker.Settings);

            if (options is null) return;

            listView1.Items.Add(new ProcessItem(options));
        }

        private void buttonLoadBatchDescription_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Batch description files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;

            var result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(openFileDialog1.FileName);

                var folder = lines[0];
                var system = lines[1];
                var calibrationFile = lines[2];

                for (int i = 3; i < lines.Length; i++)
                {
                    if (lines[i].Length > 0)
                    {
                        var options = new ProcessVideoOptions();
                        options.EyeTrackingSystem = system;
                        if (calibrationFile.Trim().Length > 0)
                        {
                            options.CalibrationFileName = Path.Combine(folder, Path.GetFileName(calibrationFile));
                        }
                        else
                        {
                            options.CalibrationFileName = string.Empty;
                        }
                        options.VideoFileNames = new EyeCollection<string?>(
                            Path.Combine(folder, Path.GetFileName(lines[i] + "-Left.avi")),
                            Path.Combine(folder, Path.GetFileName(lines[i] + "-Right.avi")));
                        //options.CustomRange = ;

                        options.SaveProcessedVideo = false;

                        var item = new ProcessItem(options);
                        listView1.Items.Add(item);
                    }
                }
            }
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            foreach (var item in listView1.SelectedItems)
            {
                listView1.Items.Remove((ListViewItem)item);
            }
        }

        private void buttonSaveBatchDescription_Click(object sender, EventArgs e)
        {
            using SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Batch description files (*.txt)|*.txt";
            saveFileDialog1.FilterIndex = 1;

            var result = saveFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string[] lines = new string[3 + listView1.Items.Count];
                var processItem = listView1.Items[0] as ProcessItem ?? throw new Exception();
                var options = processItem.Options;

                var file = (options.VideoFileNames?[Eye.Left] != null)
                    ? options.VideoFileNames?[Eye.Left]
                    : options.VideoFileNames?[Eye.Right] ?? throw new Exception();

                lines[0] = Path.GetDirectoryName(file);
                lines[1] = options.EyeTrackingSystem?.ToString() ?? "";
                if (options.CalibrationFileName?.Length > 0)
                {
                    lines[2] = Path.GetFileName(options.CalibrationFileName);
                }
                else
                {
                    lines[2] = " ";
                }

                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    var optionsItem = ((ProcessItem)listView1.Items[i]).Options;

                    lines[3 + i] = Path.GetFileName(file).Replace("-Left.avi", "");
                }

                File.WriteAllLines(saveFileDialog1.FileName, lines);
            }
        }

        private async void ButtonStart_Click(object sender, EventArgs e)
        {
            var fileList = listView1;
            if (fileList.Items.Count > 0)
            {
                optionsList = new List<ProcessVideoOptions>();

                foreach (var item in fileList.Items)
                {
                    optionsList.Add(((ProcessItem)item).Options);
                }

                panel1.Enabled = false;
                buttonLoadBatchDescription.Enabled = false;

                for (int currentItem = 0; currentItem < listView1.Items.Count; currentItem++)
                {
                    listView1.Items[currentItem].SubItems[2].Text = "Processing";
                    var options = (ProcessVideoOptions)optionsList.ElementAt(currentItem);

                    try
                    {
                        using (batchTask = eyeTracker.ProcessVideo(options))
                        {
                            await batchTask;
                        }
                    }
                    catch (Exception)
                    {
                        closing = true;
                        eyeTracker.StopTracking();
                    }

                    if (closing)
                    {
                        return;
                    }

                    listView1.Items[currentItem].SubItems[2].Text = "Completed";
                }

                panel1.Enabled = true;
                buttonLoadBatchDescription.Enabled = true;
            }
        }

        private void BatchProcessing_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (batchTask is null) return;

            var result = MessageBox.Show(
                "Are you sure you want to close? This will cancel the batch processing.",
                "Closing",
                MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            closing = true;
            eyeTracker.StopTracking();
        }
    }

    /// <summary>
    /// Batch processing items.
    /// </summary>
    [Serializable]
    internal class ProcessItem : ListViewItem
    {
        /// <summary>
        /// Initializes a new instance of the ProcessItem class.
        /// </summary>
        /// <param name="options">Options for the batch processing of the recording.</param>
        public ProcessItem(ProcessVideoOptions options)
            : base(options.VideoFileNames?[0])
        {
            Options = options;
            SubItems.Add(options.CalibrationFileName);
            SubItems.Add("Pending");
        }

        /// <summary>
        /// Gets the options for the batch processing of one recording.
        /// </summary>
        public ProcessVideoOptions Options { get; }
    }
}