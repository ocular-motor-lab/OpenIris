

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

        OpenIris.VideoPlayer videoPlayer;
        private EyeTrackerSettings settings;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.settings = EyeTrackerSettings.Load();

                var timer = new Timer();
                timer.Interval = 30;
                timer.Tick += Timer_Tick;
                timer.Start();

            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR" + ex.Message);
            }
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            if (this.images != null)
            {
                this.eyeTrackerImageEyeBox1.UpdateImageEyeBox(this.images[Eye.Right]);
                this.eyeTrackerImageEyeBox2.UpdateImageEyeBox(this.images[Eye.Left]);
            };

            if (this.videoPlayer != null)
            {
                this.videoPlayerUI1.Update(this.videoPlayer);
            }
        }

        EyeCollection<ImageEye> images;

        void VideoPlayer_ImagesGrabbed(object sender, EyeCollection<ImageEye?> e)
        {
            this.images = e;
        }
        
        private void ButtonOpen_Click(object sender, EventArgs e)
        {
            var options = SelectVideoDialog.SelectVideo(this.settings, SelectVideoDialog.Mode.Simple);
            
            if (options is null)
            {
                return;
            }

            // Initialize the video grabbing
            this.videoPlayer = new OpenIris.VideoPlayer(options?.VideoFileNames[Eye.Left], options?.VideoFileNames[Eye.Right]);
            this.videoPlayer.ImagesGrabbed += VideoPlayer_ImagesGrabbed;

            this.videoPlayer.Play();
        }
    }
}
