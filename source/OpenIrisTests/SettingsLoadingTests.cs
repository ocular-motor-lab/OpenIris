//-----------------------------------------------------------------------
// <copyright file="SettingsLoadingTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Checks that a settings file saved with a plugin that is no longer there (removed, moved to
    /// another repository, or missing its camera SDK) still loads, keeping everything else.
    /// </summary>
    [TestClass]
    public class SettingsLoadingTests
    {
        [TestMethod]
        public void SettingsOfMissingPluginsAreSkipped()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var path = Path.Combine(Path.GetTempPath(), "OpenIrisSettingsTest.xml");

            try
            {
                var original = new EyeTrackerSettings { SessionName = "KeepMe" };
                original.Save(path);

                // As if saved while the plugins "Gone system" and "Gone pipeline" were installed and
                // selected. The entry of the missing system comes first, so reading has to carry on
                // after skipping it.
                var xml = File.ReadAllText(path)
                    .Replace("<EyeTrackerSystem>Simulation</EyeTrackerSystem>", "<EyeTrackerSystem>Gone system</EyeTrackerSystem>")
                    .Replace("<EyeTrackingPipeline>JOM</EyeTrackingPipeline>", "<EyeTrackingPipeline>Gone pipeline</EyeTrackingPipeline>")
                    .Replace(
                        "<AllEyeTrackerSystemSettings>",
                        "<AllEyeTrackerSystemSettings><item><key><string>Gone system</string></key><value>" +
                        "<EyeTrackingSystemSettings xsi:type=\"EyeTrackingSystemSettingsGone\"><Gain>3</Gain></EyeTrackingSystemSettings>" +
                        "</value></item>");
                File.WriteAllText(path, xml);

                var loaded = EyeTrackerSettings.Load(path);

                Assert.AreEqual("KeepMe", loaded.SessionName, "The other settings must be kept.");
                Assert.AreEqual("Simulation", loaded.EyeTrackerSystem, "A missing system falls back to the default.");
                Assert.AreEqual("JOM", loaded.EyeTrackingPipeline, "A missing pipeline falls back to the default.");
                Assert.IsFalse(loaded.AllEyeTrackerSystemSettings.ContainsKey("Gone system"));
                Assert.IsTrue(loaded.AllEyeTrackerSystemSettings.ContainsKey("Simulation"), "Entries after the skipped one must be read.");
                Assert.IsNotNull(loaded.EyeTrackingSystemSettings);
                Assert.IsNotNull(loaded.TrackingPipelineSettings);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void SelectedSystemWithoutSavedSettingsGetsDefaults()
        {
            PipelineHarness.EnsurePluginsLoaded();
            var path = Path.Combine(Path.GetTempPath(), "OpenIrisSettingsTest2.xml");

            try
            {
                new EyeTrackerSettings().Save(path);

                // The selected system (Simulation) is installed, but its settings are not in the file.
                var xml = File.ReadAllText(path);
                var start = xml.IndexOf("<AllEyeTrackerSystemSettings>");
                var end = xml.IndexOf("</AllEyeTrackerSystemSettings>");
                File.WriteAllText(path, xml.Substring(0, start) + "<AllEyeTrackerSystemSettings>" + xml.Substring(end));

                var loaded = EyeTrackerSettings.Load(path);

                Assert.AreEqual("Simulation", loaded.EyeTrackerSystem);
                Assert.IsNotNull(loaded.EyeTrackingSystemSettings);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
