//-----------------------------------------------------------------------
// <copyright file="VideoPlayerUI.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.Windows.Forms;

    /// <summary>
    /// In the UI the scroll bar goes from Frame 1 to Frame totalNumberOfFrames, in the video you go from 0 to totalNumberOfFrames-1
    /// </summary>
    public partial class VideoPlayerUI : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public enum ProgressDisplay
        {
            /// <summary>
            /// 
            /// </summary>
            FrameNumber,

            /// <summary>
            /// 
            /// </summary>
            Time,
        }

        private VideoPlayer? videoPlayer;

        /// <summary>
        /// 
        /// </summary>
        public VideoPlayerUI()
        {
            InitializeComponent();

            ProgressDisplayType = ProgressDisplay.FrameNumber;
        }
 
        /// <summary>
        /// 
        /// </summary>
        public ProgressDisplay ProgressDisplayType { get; set; }

        /// <summary>
        /// Update the control with the information from the videoPlayer
        /// </summary>
        /// <param name="videoPlayer"></param>
        public void Update(VideoPlayer videoPlayer)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<VideoPlayer>(Update), videoPlayer);
            }
            else
            {
                this.videoPlayer = videoPlayer;

                // Update the player bar
                if (videoPlayer != null)
                {
                    buttonVideoPauseResume.Enabled = true;

                    if (videoPlayer.Playing)
                    {
                        buttonVideoPauseResume.Text = "Pause";
                    }
                    else
                    {
                        buttonVideoPauseResume.Text = "Resume";
                    }
                    hScrollBarPlayBack.Minimum = 1;
                    hScrollBarPlayBack.Maximum = (int)videoPlayer.FrameCount;
                    hScrollBarPlayBack.SmallChange = 1;
                    hScrollBarPlayBack.LargeChange = 10;

                    // Add 1 because they are number from 0

                    switch (ProgressDisplayType)
                    {
                        case ProgressDisplay.FrameNumber:
                            labelFrameNumber.Text =
                                (videoPlayer.CurrentFrameNumber + 1) +
                                "/ " +
                                videoPlayer.FrameCount;
                            break;
                        case ProgressDisplay.Time:
                            var timeElapsed = TimeSpan.FromSeconds((videoPlayer.CurrentFrameNumber + 1) / videoPlayer.FrameRate);
                            var timeTotal = TimeSpan.FromSeconds(videoPlayer.FrameCount / videoPlayer.FrameRate);
                            labelFrameNumber.Text =
                                timeElapsed.ToString(@"mm\:ss\.F") + "/" + timeTotal.ToString(@"mm\:ss\.F");
                            break;
                    }
                    hScrollBarPlayBack.Value = (int)videoPlayer.CurrentFrameNumber + 1;

                    Invalidate();
                    Refresh();
                }
            }
        }

        private void HScrollBarPlayBack_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }

            videoPlayer?.Scroll(e.NewValue - 1);
        }

        private void ButtonVideoPauseResume_Click(object sender, EventArgs e)
        {
            if (videoPlayer != null)
            {
                if (videoPlayer.Playing)
                {
                    videoPlayer.Pause();
                }
                else
                {
                    videoPlayer.Play();
                }
            }
        }

        private void LabelFrameNumber_Click(object sender, EventArgs e)
        {
            if ( ProgressDisplayType== ProgressDisplay.FrameNumber)
            {
                ProgressDisplayType = ProgressDisplay.Time;
            }
            else
            {
                ProgressDisplayType = ProgressDisplay.FrameNumber;
            }

            if (videoPlayer is null) return;

            Update(videoPlayer);
        }
    }
}
