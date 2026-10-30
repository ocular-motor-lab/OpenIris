//-----------------------------------------------------------------------
// <copyright file="RemoteCommandTests.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks the text commands received over UDP and TCP. A command that fails must not throw:
    /// the exception would end the listener and remote control would stop working.
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

        private string Send(string command) =>
            Encoding.ASCII.GetString(remote.ParseAndExecuteStringMessage(Encoding.ASCII.GetBytes(command)));
    }
}
