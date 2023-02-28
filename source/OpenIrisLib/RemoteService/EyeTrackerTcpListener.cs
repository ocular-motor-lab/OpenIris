
namespace OpenIris
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    internal class EyeTrackerTcpListener : IDisposable 
    {
        private Int32 port;
        private IPAddress localAddr;
        private TcpListener server = null;
        private Task task;
        private bool disposedValue;
        private EyeTracker eyeTracker;

        public EyeTrackerTcpListener(EyeTracker eyeTracker, int port)
        {
            this.port = (Int32)port;
            this.eyeTracker = eyeTracker;
        }

        public void Start()
        {
            task = Task.Run(() =>
            {
                try
                {
                    // TcpListener server = new TcpListener(port);
                    server = new TcpListener(IPAddress.Any, port);

                    // Start listening for client requests.
                    server.Start();

                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    String data = null;

                    // Enter the listening loop.
                    while (true)
                    {
                        Trace.Write($"TCP server Waiting for a connection in port {port} ...");

                        // Perform a blocking call to accept requests.
                        // You could also use server.AcceptSocket() here.
                        using TcpClient client = server.AcceptTcpClient();
                        Trace.WriteLine("Connected!");

                        data = null;

                        // Get a stream object for reading and writing
                        NetworkStream stream = client.GetStream();

                        int i;

                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = Encoding.ASCII.GetString(bytes, 0, i);
                            //Trace.WriteLine("Received: {0}", data);

                            var msg = data.Split('|');
                            switch (msg[0].ToUpper())
                            {
                                case "STARTRECORDING":
                                    this.StartRecording();
                                    break;
                                case "STOPRECORDING":
                                    this.StopRecording();
                                    break;
                                case "GETDATA":
                                    var eyedata = this.GetCurrentData();
                                    var eyedatamsg =
                                    $"{eyedata[Eye.Left].Timestamp.FrameNumberRaw};{eyedata[Eye.Left].Timestamp.Seconds};{eyedata[Eye.Left].Pupil.Center.X};{eyedata[Eye.Left].Pupil.Center.Y};" +
                                    $"{eyedata[Eye.Right].Timestamp.FrameNumberRaw};{eyedata[Eye.Right].Timestamp.Seconds};{eyedata[Eye.Right].Pupil.Center.X};{eyedata[Eye.Right].Pupil.Center.Y};";
                                    byte[] bytesToSend = Encoding.ASCII.GetBytes(eyedatamsg);
                                    // Send back a response.
                                    stream.Write(bytesToSend, 0, bytesToSend.Length);
                                    break;
                            }
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                finally
                {
                    server.Stop();
                }
            });
        }

        public void Stop()
        {
            server.Stop();
            //Task.WaitAll(task);
            task.Dispose();
            task = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    task.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~EyeTrackerTcpListener()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// Starts the recording.
        /// </summary>
        public void StartRecording()
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                var conf = new RecordingOptions()
                {
                    SessionName = eyeTracker.Settings.SessionName,
                    DataFolder = eyeTracker.Settings.DataFolder,
                    SaveRawVideo = eyeTracker.Settings.RecordVideo,
                    FrameRate = eyeTracker.ImageGrabber?.FrameRate ?? 0.0,
                };

                _ = eyeTracker.StartRecording(conf);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote start recording: " + ex.Message);
            }
        }

        /// <summary>
        /// Stops the recording.
        /// </summary>
        public void StopRecording()
        {
            try
            {
                if (eyeTracker is null) throw new InvalidOperationException("Eye tracker is null.");

                eyeTracker.StopRecording();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error processing remote start recording: " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EyeCollection<EyeData?>? GetCurrentData()
        {
            return eyeTracker?.LastImagesAndData?.Data?.EyeDataRaw;
        }
    }
}
