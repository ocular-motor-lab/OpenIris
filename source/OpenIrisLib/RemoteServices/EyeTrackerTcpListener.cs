
namespace OpenIris
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.ServiceModel;

    internal class EyeTrackerTcpListener : IDisposable
    {
        private EyeTrackerRemote eyeTracker;
        private Int32 port;
        private TcpListener server = null;
        private Task task;
        private bool disposedValue;

        public EyeTrackerTcpListener(EyeTracker eyeTracker, int port)
        {
            this.port = port;
            this.eyeTracker = new EyeTrackerRemote(eyeTracker);
        }

        public void Start()
        {
            task = Task.Run(() =>
            {
                System.Threading.Thread.CurrentThread.Name = "EyeTracker:TCP server";

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
                        Trace.WriteLine($"TCP server Waiting for a connection in port {port} ...");

                        // Perform a blocking call to accept requests.
                        // You could also use server.AcceptSocket() here.
                        using TcpClient client = server.AcceptTcpClient();
                        Trace.WriteLine("TCP server Connected!");

                        data = null;

                        // Get a stream object for reading and writing
                        NetworkStream stream = client.GetStream();

                        int i;

                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            
                            var bytesToSend = eyeTracker.ParseAndExecuteStringMessage(bytes);
                            if (bytesToSend.Length > 0)
                            {
                                // Send back a response.
                                stream.Write(bytesToSend, 0, bytesToSend.Length);
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
    }
}
