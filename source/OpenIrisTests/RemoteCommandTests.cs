//-----------------------------------------------------------------------
// <copyright file="RemoteCommandTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks the text commands received over UDP and TCP. A command that fails must not throw:
    /// the exception would end the listener and remote control would stop working. Over TCP,
    /// commands end with a newline.
    /// </summary>
    [TestClass]
    public class RemoteCommandTests
    {
        // No eye tracker: commands that need one fail, which is what these tests need.
        private readonly EyeTrackerRemote remote = new EyeTrackerRemote(null!);

        [TestMethod]
        public void TestCommandReplies()
        {
            Assert.AreEqual("D", Send("TESEMPTY"));
            Assert.AreEqual("D", Send("tesempty"), "Commands are not case sensitive.");
        }

        [TestMethod]
        public void UnknownCommandIsIgnored()
        {
            Assert.AreEqual(string.Empty, Send("NOSUCHCOMMAND"));
            Assert.AreEqual("D", Send("TESEMPTY"), "Commands must still work after an unknown one.");
        }

        [TestMethod]
        public void FailingCommandIsIgnored()
        {
            // RECORDEVENT throws without an eye tracker.
            Assert.AreEqual(string.Empty, Send("RECORDEVENT|test"));
            Assert.AreEqual("D", Send("TESEMPTY"), "Commands must still work after one failed.");
        }

        [TestMethod]
        public void CommandEndingInNewlineWorks()
        {
            Assert.AreEqual("D", Send("TESEMPTY\n"));
            Assert.AreEqual("D", Send("TESEMPTY\r\n"));
        }

        /// <summary>
        /// TCP doesn't keep messages apart: commands end with a newline, so several can arrive in one
        /// read, or one in several reads.
        /// </summary>
        [TestMethod]
        public void TcpCommandsAreSplitAtNewlines()
        {
            var pending = string.Empty;
            CollectionAssert.AreEqual(new[] { "SETSESSIONNAME|a", "STARTRECORDING" }, EyeTrackerTcpListener.SplitCommands("SETSESSIONNAME|a\nSTARTRECORDING\n", ref pending));
            Assert.AreEqual(string.Empty, pending);

            // One command in pieces, and Windows line endings.
            CollectionAssert.AreEqual(new[] { "STOPRECORDING" }, EyeTrackerTcpListener.SplitCommands("STOPRECORDING\r\nRECORDEV", ref pending));
            CollectionAssert.AreEqual(new string[0], EyeTrackerTcpListener.SplitCommands("ENT|a long", ref pending));
            CollectionAssert.AreEqual(new[] { "RECORDEVENT|a long message" }, EyeTrackerTcpListener.SplitCommands(" message\n", ref pending));
            Assert.AreEqual(string.Empty, pending);

            // Older clients send a command without a newline: one read, one command.
            CollectionAssert.AreEqual(new[] { "TESEMPTY" }, EyeTrackerTcpListener.SplitCommands("TESEMPTY", ref pending));
        }

        /// <summary>
        /// Two commands in one write to the TCP port get a reply each.
        /// </summary>
        [TestMethod]
        [Timeout(20000)]
        public void TcpCommandsSentTogetherBothRun()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var listener = new EyeTrackerTcpListener(null!, port);
            listener.Start();
            try
            {
                using var client = Connect(port);
                var stream = client.GetStream();
                var message = Encoding.ASCII.GetBytes("TESEMPTY\nTESEMPTY\n");
                stream.Write(message, 0, message.Length);

                var reply = string.Empty;
                var buffer = new byte[16];
                while (reply.Length < 2) reply += Encoding.ASCII.GetString(buffer, 0, stream.Read(buffer, 0, buffer.Length));
                Assert.AreEqual("DD", reply);
            }
            finally
            {
                listener.Stop();
            }
        }

        private static TcpClient Connect(int port)
        {
            // The listener starts on another thread: retry until it accepts connections.
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return new TcpClient("127.0.0.1", port);
                }
                catch (SocketException) when (attempt < 50)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private string Send(string command) =>
            Encoding.ASCII.GetString(remote.ParseAndExecuteStringMessage(Encoding.ASCII.GetBytes(command)));
    }
}
