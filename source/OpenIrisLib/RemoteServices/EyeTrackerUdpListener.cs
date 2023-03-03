
namespace OpenIris
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    internal class EyeTrackerUdpListener : IDisposable
    {
        private EyeTrackerRemote eyeTracker;
        private Int32 port;
        private UdpClient server = null;
        private Task task;
        private bool disposedValue;

        public EyeTrackerUdpListener(EyeTracker eyeTracker, int port)
        {
            this.port = port;
            this.eyeTracker = new EyeTrackerRemote(eyeTracker);
        }

        public void Start()
        {
            task = Task.Run(() =>
            {
                try
                {
                    // https://stackoverflow.com/questions/20038943/simple-udp-example-to-send-and-receive-data-from-same-socket
                    server = new UdpClient(port);

                    // Buffer for reading data
                    Byte[] bytes;
                    String data = null;

                    Trace.WriteLine($"UDP server Waiting for requests in port {port}.");

                    // Enter the listening loop.
                    while (true)
                    {

                        var remoteEP = new IPEndPoint(IPAddress.Any, port);

                        bytes = server.Receive(ref remoteEP);
                        //Console.Write("receive data from " + remoteEP.ToString());

                        var bytesToSend = eyeTracker.ParseAndExecuteStringMessage(bytes);
                        if (bytesToSend.Length > 0)
                        {
                            // Send back a response.
                            server.Send(bytesToSend, bytesToSend.Length, remoteEP); 
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                finally
                {
                    server.Close();
                }
            });
        }

        public void Stop()
        {
            server.Close();
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
