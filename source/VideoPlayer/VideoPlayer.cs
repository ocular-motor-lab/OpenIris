

namespace VideoPlayer
{
#nullable enable

    using System;
    using System.Windows.Forms;
    using OpenIris;
    using OpenIris.UI;

    /// <summary>
    /// 
    /// </summary>
    public partial class VideoPlayer : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public VideoPlayer()
        {
            InitializeComponent();
        }

        private OpenIris.VideoPlayer? videoPlayer;
        private EyeTrackerSettings? settings;
        private EyeCollection<ImageEye?>? images;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                settings = EyeTrackerSettings.Load();

                var timer = new Timer();
                timer.Interval = 30;
                timer.Tick += (o, e) =>
                {
                    if (videoPlayer is null) return;

                    eyeTrackerImageEyeBox1.UpdateImageEyeBox(images?[Eye.Right]);
                    eyeTrackerImageEyeBox2.UpdateImageEyeBox(images?[Eye.Left]);
                    videoPlayerUI1?.Update(videoPlayer);
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR" + ex.Message);
            }
        }

        private void ButtonOpen_Click(object sender, EventArgs e)
        {
            if (settings is null) throw new InvalidOperationException("Null settings");

            var options = SelectVideoDialog.SelectVideo(settings, SelectVideoDialog.Mode.Simple);

            if (options is null) return;

            // Initialize the video grabbing
            videoPlayer = new OpenIris.VideoPlayer(options?.VideoFileNames[Eye.Left], options?.VideoFileNames[Eye.Right]);
            videoPlayer.ImagesGrabbed += (_, e) => images = e;

            videoPlayer.Play();
        }
    }
}
