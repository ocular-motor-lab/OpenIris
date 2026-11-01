//-----------------------------------------------------------------------
// <copyright file="SettingsProfilesTests.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Settings profiles: OpenIrisSettings.xml with the shared settings and the active profile, and
    /// one file per profile in Settings\.
    /// </summary>
    [TestClass]
    public class SettingsProfilesTests
    {
        private string folder = string.Empty;

        [TestInitialize]
        public void CreateFolder()
        {
            PipelineHarness.EnsurePluginsLoaded();
            folder = Path.Combine(Path.GetTempPath(), "OpenIrisProfilesTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TestCleanup]
        public void DeleteFolder() => Directory.Delete(folder, true);

        [TestMethod]
        public void SettingsOfOlderVersionsBecomeTheDefaultProfile()
        {
            foreach (var oldFile in new[] { "EyeTrackerSettings.xml", EyeTrackerSettings.FileName })
            {
                foreach (var f in Directory.GetFileSystemEntries(folder)) { if (File.Exists(f)) File.Delete(f); else Directory.Delete(f, true); }
                new EyeTrackerSettings { SessionName = "Old", DataFolder = @"D:\OldData" }.Save(Path.Combine(folder, oldFile));
                var oldText = File.ReadAllText(Path.Combine(folder, oldFile));

                var settings = EyeTrackerSettings.LoadFromFolder(folder);
                Assert.AreEqual(EyeTrackerSettings.DefaultProfileName, settings.Profile, oldFile);
                Assert.AreEqual("Old", settings.SessionName, oldFile);
                Assert.AreEqual(@"D:\OldData", settings.DataFolder, oldFile);

                settings.Save();
                var profile = File.ReadAllText(Path.Combine(folder, "Settings", "Default.xml"));
                StringAssert.Contains(profile, "<SessionName>Old</SessionName>");
                Assert.IsFalse(profile.Contains("DataFolder"), "Shared settings aren't in the profiles.");
                StringAssert.Contains(File.ReadAllText(Path.Combine(folder, EyeTrackerSettings.FileName)), @"<Setting Name=""DataFolder"">D:\OldData</Setting>");
                if (oldFile != EyeTrackerSettings.FileName)
                {
                    Assert.AreEqual(oldText, File.ReadAllText(Path.Combine(folder, oldFile)), "The old file stays for the older version.");
                }
            }
        }

        [TestMethod]
        public void SwitchingProfilesKeepsTheSharedSettings()
        {
            var pipelines = EyeTrackerPluginManager.EyeTrackingPipelineFactory!.ClassesAvaiable.Select(c => c.Name).ToArray();
            var settings = EyeTrackerSettings.LoadFromFolder(folder);
            settings.SessionName = "First";
            settings.EyeTrackingPipeline = pipelines[0];
            settings.DataFolder = @"D:\Data1";

            settings.SaveProfileAs("Second");
            settings.SessionName = "SecondSession";
            settings.EyeTrackingPipeline = pipelines[1];

            var changed = new List<string>();
            settings.PropertyChanged += (o, e) => changed.Add(e.PropertyName);
            settings.SwitchProfile(EyeTrackerSettings.DefaultProfileName);

            Assert.AreEqual(EyeTrackerSettings.DefaultProfileName, settings.Profile);
            Assert.AreEqual("First", settings.SessionName);
            Assert.AreEqual(pipelines[0], settings.EyeTrackingPipeline);
            Assert.AreEqual(@"D:\Data1", settings.DataFolder);
            CollectionAssert.Contains(changed, nameof(EyeTrackerSettings.Profile));
            CollectionAssert.Contains(changed, nameof(EyeTrackerSettings.EyeTrackingPipeline));
            Assert.AreEqual(pipelines[0], settings.TrackingPipelineSettings == settings.AllTrackingPipelinesSettings[pipelines[0]] ? pipelines[0] : "wrong pipeline settings");

            settings.DataFolder = @"D:\Data2";
            settings.SwitchProfile("Second");
            Assert.AreEqual("SecondSession", settings.SessionName);
            Assert.AreEqual(pipelines[1], settings.EyeTrackingPipeline);
            Assert.AreEqual(@"D:\Data2", settings.DataFolder, "Shared: changed in one profile, seen in the other.");
            CollectionAssert.AreEqual(new[] { "Default", "Second" }, settings.Profiles);

            // Next start: the last profile, with the shared settings.
            settings.Save();
            var next = EyeTrackerSettings.LoadFromFolder(folder);
            Assert.AreEqual("Second", next.Profile);
            Assert.AreEqual("SecondSession", next.SessionName);
            Assert.AreEqual(@"D:\Data2", next.DataFolder);

            // Or a profile chosen at the start (--profile).
            var chosen = EyeTrackerSettings.LoadFromFolder(folder, "Default");
            Assert.AreEqual("First", chosen.SessionName);
            Assert.AreEqual(@"D:\Data2", chosen.DataFolder);
            Assert.AreEqual("Second", EyeTrackerSettings.LoadFromFolder(folder, "Missing").Profile, "A missing profile falls back to the active one.");
        }

        [TestMethod]
        public void ProfilesCanBeRenamedAndDeleted()
        {
            var settings = EyeTrackerSettings.LoadFromFolder(folder);
            settings.SaveProfileAs("Lab rig");
            settings.SaveProfileAs("Webcam");

            Assert.ThrowsException<ArgumentException>(() => settings.SaveProfileAs("lab RIG"));
            Assert.ThrowsException<ArgumentException>(() => settings.SaveProfileAs("a/b"));
            Assert.ThrowsException<ArgumentException>(() => settings.SaveProfileAs(" "));
            Assert.ThrowsException<InvalidOperationException>(() => settings.DeleteProfile("Webcam"));

            settings.RenameProfile("Lab rig", "Patient room");
            settings.RenameProfile("Webcam", "Webcam pilot");
            Assert.AreEqual("Webcam pilot", settings.Profile);
            settings.DeleteProfile("Default");
            CollectionAssert.AreEqual(new[] { "Patient room", "Webcam pilot" }, settings.Profiles);

            Assert.AreEqual("Webcam pilot", EyeTrackerSettings.LoadFromFolder(folder).Profile);
        }
    }
}
