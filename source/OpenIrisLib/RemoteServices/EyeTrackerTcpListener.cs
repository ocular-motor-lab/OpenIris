
namespace OpenIris
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

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
                Thread.CurrentThread.Name = "EyeTracker:TCP server";
                Thread.CurrentThread.IsBackground = true; // Stops the thread from preventing the application from closing

                try
                {
                    // TcpListener server = new TcpListener(port);
                    server = new TcpListener(IPAddress.Any, port);

                    // Start listening for client requests.
                    server.Start();

                    // Buffer for reading data
                    Byte[] bytes = new Byte[1024];

                    // Enter the listening loop.
                    while (true)
                    {
                        Trace.WriteLine($"TCP server Waiting for a connection in port {port} ...");

                        // Perform a blocking call to accept requests.
                        // You could also use server.AcceptSocket() here.
                        using TcpClient client = server.AcceptTcpClient();
                        Trace.WriteLine("TCP server Connected!");

                        // Get a stream object for reading and writing
                        NetworkStream stream = client.GetStream();

                        int i;
                        var pending = string.Empty;

                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            foreach (var command in SplitCommands(Encoding.ASCII.GetString(bytes, 0, i), ref pending))
                            {
                                var bytesToSend = eyeTracker.ParseAndExecuteStringMessage(Encoding.ASCII.GetBytes(command));

                                if (bytesToSend.Length > 0)
                                {
                                    // Send back a response.
                                    stream.Write(bytesToSend, 0, bytesToSend.Length);
                                }
                            }
                        }
                    }
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted)
                {
                    // Stop() was called: OpenIris is closing.
                }
                catch (InvalidOperationException)
                {
                    // Stop() was called while a client was connected, so waiting for the next
                    // connection found the server already stopped.
                }
                catch (SocketException e)
                {
                    Trace.WriteLine("ERROR: TCP server: " + e.Message);
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
            var tempTask = task;
            task = null;
            tempTask?.Wait();
            tempTask?.Dispose();
        }

        /// <summary>
        /// Splits the text received into commands. TCP doesn't keep messages apart, so commands end
        /// with a newline: then several commands can arrive together, or one in pieces. Text without
        /// any newline is one whole command, as older clients send it.
        /// </summary>
        /// <param name="received">Text received in one read.</param>
        /// <param name="pending">The start of a command whose newline hasn't arrived yet. Pass the same
        /// variable on every read of a connection, starting empty.</param>
        /// <returns>The complete commands, in order.</returns>
        internal static List<string> SplitCommands(string received, ref string pending)
        {
            var text = pending + received;
            var lastNewline = text.LastIndexOf('\n');
            if (lastNewline < 0)
            {
                // The rest of a command that arrived in pieces, or a command without a newline.
                if (pending.Length > 0)
                {
                    pending = text;
                    return new List<string>();
                }

                return text.Length > 0 ? new List<string> { text } : new List<string>();
            }

            pending = text.Substring(lastNewline + 1);
            return text.Substring(0, lastNewline)
                .Split('\n')
                .Select(command => command.TrimEnd('\r'))
                .Where(command => command.Length > 0)
                .ToList();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    task.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
