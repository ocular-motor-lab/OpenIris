using System;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using System.Windows.Forms;

namespace OpenIris.UI
{
    internal class VideoTools
    {
        public static async Task TrimVideosCommandExecute(ProcessVideoOptions options)
        {
            if (options is null || options.VideoFileNames is null)
            {
                return;
            }
            var videoLeft = options.VideoFileNames[Eye.Left];
            var videoRight = options.VideoFileNames[Eye.Right];



            var cancelled = false;


            using var progressDialog = new ProgressBarDialog();

            using var videoReaderLeft = new VideoCapture(videoLeft);
            using var videoReaderRight = new VideoCapture(videoRight);
            using var videoWriterLeft = new VideoWriter(videoLeft + "trim.avi", 100, new Size(videoReaderLeft.Width, videoReaderLeft.Height), true);
            using var videoWriterRight = new VideoWriter(videoRight + "trim.avi", 100, new Size(videoReaderRight.Width, videoReaderRight.Height), true);


            progressDialog.Cancelled += (o, es) => cancelled = true;
            progressDialog.Show();

            var range = options.CustomRange;
            if (range.IsEmpty)
            {
                range = new Range(0, (long)Math.Round(videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount)));
            }

            var taskLeft = Task.Run(() =>
            {
                videoReaderLeft.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, (double)options.CustomRange.Begin);
                while (!cancelled)
                {
                    var img = videoReaderLeft.QueryFrame();
                    if (img is null)
                    {
                        return;
                    }

                    videoWriterLeft.Write(img);

                    if (videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) > range.End)
                        break;

                    var percent = (int)Math.Round((videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) - range.Begin) / (range.End - range.Begin) * 100);

                    progressDialog.Progress = percent;
                }
            });
            var taskRight = Task.Run(() =>
            {
                videoReaderLeft.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, (double)options.CustomRange.Begin);
                while (!cancelled)
                {
                    var img = videoReaderRight.QueryFrame();
                    if (img is null)
                    {
                        return;
                    }

                    videoWriterRight.Write(img);
                    if (videoReaderLeft.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames) > range.End)
                        break;
                }
            });

            await Task.WhenAll(taskLeft, taskRight);
            progressDialog.Close();
        }

        public static async Task ConvertVideoToRGB()
        {
            using (var dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                using (var videoReader = new VideoCapture(dialog.FileName))
                using (var videoWriter = new VideoWriter(dialog.FileName + "out2.avi", 100, new Size(videoReader.Width, videoReader.Height), true))
                {
                    while (true)
                    {
                        var img = videoReader.QueryFrame();
                        if (img is null)
                        {
                            return;
                        }

                        videoWriter.Write(img);
                    }
                }
            }
        }

        public static async Task ConvertToMP4()
        {
            using (var dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                // TODO: it does not work
                using (var videoReader = new VideoCapture(dialog.FileName))
                using (var videoWriter = new VideoWriter(dialog.FileName + "out2.mp4", 100, new Size(videoReader.Width, videoReader.Height), true))
                {
                    while (true)
                    {
                        var img = videoReader.QueryFrame();
                        if (img is null)
                        {
                            return;
                        }

                        videoWriter.Write(img);
                    }
                }
            }
        }
    }
}
