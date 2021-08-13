using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlyCapture2Managed;

namespace PointGreyTest
{
    public partial class Form1 : Form
    {
        private ManagedCamera camera;
        private long FrameCounter;
        private long LastFrameCounter;
        private long DroppedFrameCounter;

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await this.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Stop();
        }

        private async Task Start()
        {
            try
            {
                this.StartCamera();

                // Setup the queue and the threads
                this.LastFrameCounter = -1;

                await Task.Run(GrabLoop);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Stop()
        {
            this.camera?.StopCapture();
        }
        
        private void GrabLoop()
        {
            while (true)
            {
                try
                {
                    var image = new ManagedImage();
                    camera.RetrieveBuffer(image);
                    this.FrameCounter++;
                    if (this.LastFrameCounter > 0) this.DroppedFrameCounter += image.imageMetadata.embeddedFrameCounter - this.LastFrameCounter - 1;
                    this.LastFrameCounter = image.imageMetadata.embeddedFrameCounter;
                    //var image2 = new ManagedImage();
                    //image.Convert(PixelFormat.PixelFormatRaw8, image2);
                    image.Dispose();
                }
                catch(FC2Exception)
                {
                    break;
                }
            }
        }

        private void StartCamera()
        {
            // Initialize left camera if necessary
            try
            {
                using (var busManager = new ManagedBusManager())
                {
                    var cameraPGRGuid = busManager.GetCameraFromIndex(0u);

                    this.camera = new ManagedCamera();
                    this.camera.Connect(cameraPGRGuid);

                    // Set ROI
                    //Format7ImageSettings newFmt7Settings = new Format7ImageSettings();
                    //newFmt7Settings.mode = Mode.Mode0;
                    //newFmt7Settings.offsetX = Convert.ToUInt32(0);
                    //newFmt7Settings.offsetY = Convert.ToUInt32(426);
                    //newFmt7Settings.width = Convert.ToUInt32(1920);
                    //newFmt7Settings.height = Convert.ToUInt32(350);
                    //var good = true;
                    //var fmt7PacketInfo = this.camera.ValidateFormat7Settings(newFmt7Settings, ref good);
                    //this.camera.SetFormat7Configuration(newFmt7Settings, fmt7PacketInfo.recommendedBytesPerPacket);

                    //// Set frame rate
                    //var prop = this.camera.GetProperty(PropertyType.FrameRate);
                    //prop.absControl = true;
                    //prop.absValue = (float)100;
                    //prop.autoManualMode = false;
                    //prop.onOff = true;
                    //this.camera.SetProperty(prop);

                    // Set buffer mode
                    FC2Config config = this.camera.GetConfiguration();
                    config.grabMode = GrabMode.BufferFrames;
                    config.numBuffers = (uint)100;
                    config.highPerformanceRetrieveBuffer = true;
                    this.camera.SetConfiguration(config);

                    this.camera.StartCapture();
                }
            }
            catch (FC2Exception ex)
            {
                if (this.camera != null)
                {
                    this.camera.StopCapture();
                }

                throw new InvalidOperationException("Error starting cameras captures or setting GPIOs.", ex);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.label1.Text = this.FrameCounter.ToString();
            this.label3.Text = this.DroppedFrameCounter.ToString();
        }
    }
}
