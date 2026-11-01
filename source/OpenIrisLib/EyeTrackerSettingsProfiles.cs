//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettingsProfiles.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Settings profiles: several configurations of OpenIris on one computer, to switch between
    /// without changing each setting. In the folder OpenIris starts in:
    /// <code>
    /// OpenIrisSettings.xml   the settings of this computer (marked [SharedSetting]) and the active profile
    /// Settings\NAME.xml      one file per profile, with all the other settings
    /// </code>
    /// </summary>
    public sealed partial class EyeTrackerSettings
    {
        /// <summary>
        /// Name of the file with the shared settings and the active profile.
        /// </summary>
        public const string FileName = "OpenIrisSettings.xml";

        /// <summary>
        /// Name of the folder of the profiles.
        /// </summary>
        public const string ProfilesFolderName = "Settings";

        /// <summary>
        /// Name of the profile made from the settings of an older version, or from the defaults.
        /// </summary>
        public const string DefaultProfileName = "Default";

        /// <summary>
        /// Settings file of OpenIris 0.2.0-alpha.1 and earlier, with all the settings.
        /// </summary>
        private const string OldFileName = "EyeTrackerSettings.xml";

        private static XmlSerializer? profileSerializer;
        private static List<Type>? profileSerializerTypes;

        /// <summary>
        /// Folder with OpenIrisSettings.xml. Empty if these settings weren't loaded with
        /// <see cref="Load()"/>, and then <see cref="Save()"/> does nothing.
        /// </summary>
        private string folder = string.Empty;

        /// <summary>
        /// Name of the active profile.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public string Profile { get; private set; } = DefaultProfileName;

        /// <summary>
        /// Names of the profiles, sorted.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public string[] Profiles
        {
            get
            {
                var profiles = Directory.Exists(ProfilesFolder)
                    ? Directory.GetFiles(ProfilesFolder, "*.xml").Select(Path.GetFileNameWithoutExtension).ToList()
                    : new List<string>();
                if (!profiles.Contains(Profile, StringComparer.OrdinalIgnoreCase)) profiles.Add(Profile);
                return profiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
            }
        }

        private string ProfilesFolder => Path.Combine(folder, ProfilesFolderName);

        private string ProfileFile(string name) => Path.Combine(ProfilesFolder, name + ".xml");

        /// <summary>
        /// Loads the settings from the folder OpenIris starts in: the active profile and the shared settings.
        /// </summary>
        public static EyeTrackerSettings Load() => LoadFromFolder(Directory.GetCurrentDirectory());

        /// <summary>
        /// Loads the settings from a folder: a profile and the shared settings. The first time, the
        /// settings of an older version (a single file) become the profile Default.
        /// </summary>
        /// <param name="folder">Folder with OpenIrisSettings.xml.</param>
        /// <param name="profile">Profile to load. If null or missing, the active profile.</param>
        public static EyeTrackerSettings LoadFromFolder(string folder, string? profile = null)
        {
            var sharedFile = Path.Combine(folder, FileName);
            var shared = ReadSharedFile(sharedFile);
            var profilesFolder = Path.Combine(folder, ProfilesFolderName);

            EyeTrackerSettings settings;
            if (shared is null || !Directory.Exists(profilesFolder) || !Directory.EnumerateFiles(profilesFolder, "*.xml").Any())
            {
                settings = LoadOlderVersion(folder);
            }
            else
            {
                var name = profile ?? shared.ActiveProfile;
                var file = Path.Combine(profilesFolder, name + ".xml");
                if (!File.Exists(file))
                {
                    Trace.WriteLine($"ERROR: there is no settings profile '{name}'. Using '{shared.ActiveProfile}'.");
                    name = shared.ActiveProfile;
                    file = Path.Combine(profilesFolder, name + ".xml");
                }

                settings = ReadProfile(file) ?? new EyeTrackerSettings();
                settings.Profile = name;
                settings.SetSharedValues(shared);
            }

            settings.folder = folder;
            Trace.WriteLine($"Settings profile '{settings.Profile}'.");
            return settings;
        }

        /// <summary>
        /// Saves the active profile and the shared settings.
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                Directory.CreateDirectory(ProfilesFolder);
                using (var file = new StreamWriter(ProfileFile(Profile)))
                {
                    ProfileSerializer().Serialize(file, this);
                }

                var serializer = new XmlSerializer(typeof(SharedSettingsFile));
                using (var file = new StreamWriter(Path.Combine(folder, FileName)))
                {
                    serializer.Serialize(file, GetSharedValues());
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                Trace.WriteLine("ERROR saving the settings: " + ex);
            }
        }

        /// <summary>
        /// Switches to another profile, saving the current one first. Only while not tracking: the
        /// eye tracking system may change.
        /// </summary>
        /// <param name="name">Name of the profile.</param>
        public void SwitchProfile(string name)
        {
            if (string.Equals(name, Profile, StringComparison.OrdinalIgnoreCase)) return;
            if (!File.Exists(ProfileFile(name))) throw new ArgumentException($"There is no settings profile '{name}'.");

            Save();

            var other = ReadProfile(ProfileFile(name)) ?? throw new InvalidOperationException($"The settings profile '{name}' could not be read. See the log.");
            CopyProfileValues(other);
            Profile = Path.GetFileNameWithoutExtension(ProfileFile(name));
            Save();

            Trace.WriteLine($"Switched to the settings profile '{Profile}'.");
            OnPropertyChanged(this, nameof(Profile));
        }

        /// <summary>
        /// Saves the current settings as a new profile, which becomes the active one.
        /// </summary>
        /// <param name="name">Name of the new profile.</param>
        public void SaveProfileAs(string name)
        {
            CheckNewProfileName(name);

            Save();
            Profile = name.Trim();
            Save();

            Trace.WriteLine($"Saved the settings as the profile '{Profile}'.");
            OnPropertyChanged(this, nameof(Profile));
        }

        /// <summary>
        /// Renames a profile.
        /// </summary>
        public void RenameProfile(string name, string newName)
        {
            if (!Profiles.Contains(name, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"There is no settings profile '{name}'.");
            if (!string.Equals(name.Trim(), newName.Trim(), StringComparison.OrdinalIgnoreCase)) CheckNewProfileName(newName);

            var active = string.Equals(name, Profile, StringComparison.OrdinalIgnoreCase);
            if (active) Save();

            File.Move(ProfileFile(name), ProfileFile(newName.Trim()));

            if (active)
            {
                Profile = newName.Trim();
                Save();
                OnPropertyChanged(this, nameof(Profile));
            }
        }

        /// <summary>
        /// Deletes a profile. Not the active one.
        /// </summary>
        public void DeleteProfile(string name)
        {
            if (string.Equals(name, Profile, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The active settings profile can't be deleted. Switch to another one first.");
            if (!File.Exists(ProfileFile(name))) throw new ArgumentException($"There is no settings profile '{name}'.");

            File.Delete(ProfileFile(name));
            OnPropertyChanged(this, nameof(Profiles));
        }

        private void CheckNewProfileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name of the profile is empty.");
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException($"The name of a profile can't have any of these characters: \\ / : * ? \" < > |");
            if (Profiles.Contains(name.Trim(), StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"There is already a settings profile '{name.Trim()}'.");
        }

        /// <summary>
        /// The settings of a version without profiles: OpenIrisSettings.xml of 0.2.0-alpha.2, or
        /// EyeTrackerSettings.xml of older ones, both with all the settings. Or the defaults.
        /// </summary>
        private static EyeTrackerSettings LoadOlderVersion(string folder)
        {
            foreach (var name in new[] { FileName, OldFileName })
            {
                var file = Path.Combine(folder, name);
                if (File.Exists(file) && RootElement(file) == "EyeTrackerSettings")
                {
                    Trace.WriteLine($"Reading the settings from {name}. They become the profile '{DefaultProfileName}'.");
                    var settings = Load(file);
                    settings.Profile = DefaultProfileName;
                    return settings;
                }
            }

            Trace.WriteLine("No settings yet, using the default settings.");
            return new EyeTrackerSettings();
        }

        private static string? RootElement(string file)
        {
            try
            {
                using var reader = XmlReader.Create(file);
                reader.MoveToContent();
                return reader.LocalName;
            }
            catch (Exception ex) when (ex is IOException || ex is XmlException)
            {
                return null;
            }
        }

        private static SharedSettingsFile? ReadSharedFile(string file)
        {
            if (!File.Exists(file) || RootElement(file) != SharedSettingsFile.RootName) return null;

            try
            {
                using var reader = new StreamReader(file);
                return (SharedSettingsFile)new XmlSerializer(typeof(SharedSettingsFile)).Deserialize(reader);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
            {
                Trace.WriteLine($"ERROR reading {file}, using the default shared settings. " + ex);
                return null;
            }
        }

        private static EyeTrackerSettings? ReadProfile(string file)
        {
            try
            {
                using var reader = new StreamReader(file);
                var settings = (EyeTrackerSettings)ProfileSerializer().Deserialize(reader);

                // As in Load(file): select again to add the defaults of any plugin whose settings were skipped.
                settings.EyeTrackerSystem = settings.EyeTrackerSystem;
                settings.EyeTrackingPipeline = settings.EyeTrackingPipeline;
                settings.CalibrationMethod = settings.CalibrationMethod;
                return settings;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is PluginManagerException)
            {
                Trace.WriteLine($"ERROR reading the settings profile {file}. " + ex);
                return null;
            }
        }

        /// <summary>
        /// Serializer of the profiles: all the settings but the shared ones. Kept, because a serializer
        /// with overrides is a new generated assembly each time.
        /// </summary>
        private static XmlSerializer ProfileSerializer()
        {
            var types = EyeTrackerPluginManager.ExtraSettingsTypesForXML;
            if (profileSerializer is null || profileSerializerTypes != types)
            {
                var overrides = new XmlAttributeOverrides();
                foreach (var property in SharedProperties)
                {
                    overrides.Add(typeof(EyeTrackerSettings), property.Name, new XmlAttributes { XmlIgnore = true });
                }

                profileSerializer = new XmlSerializer(typeof(EyeTrackerSettings), overrides, types?.ToArray() ?? new Type[0], null, null);
                profileSerializerTypes = types;
            }

            return profileSerializer;
        }

        private static IEnumerable<PropertyInfo> SharedProperties => typeof(EyeTrackerSettings).GetProperties()
            .Where(p => p.GetCustomAttribute<SharedSettingAttribute>() != null);

        private SharedSettingsFile GetSharedValues() => new SharedSettingsFile
        {
            ActiveProfile = Profile,
            Settings = SharedProperties
                .Select(p => new SharedSettingsFile.Setting { Name = p.Name, Value = TypeDescriptor.GetConverter(p.PropertyType).ConvertToInvariantString(p.GetValue(this)) ?? string.Empty })
                .ToList(),
        };

        private void SetSharedValues(SharedSettingsFile shared)
        {
            foreach (var property in SharedProperties)
            {
                var setting = shared.Settings.FirstOrDefault(s => s.Name == property.Name);
                if (setting is null) continue;

                try
                {
                    property.SetValue(this, TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(setting.Value));
                }
                catch (Exception ex) when (ex is NotSupportedException || ex is FormatException || ex is ArgumentException)
                {
                    Trace.WriteLine($"ERROR in the shared setting {property.Name}='{setting.Value}', using {property.GetValue(this)}.");
                }
            }
        }

        /// <summary>
        /// Takes the values of another profile, keeping this object, which the user interface and the
        /// eye tracker are subscribed to. The shared settings stay.
        /// </summary>
        private void CopyProfileValues(EyeTrackerSettings other)
        {
            // The selected plugins first, without events: the settings of the new ones are in the new
            // lists, so anything reacting to the lists below finds them.
            eyeTrackerSystem = other.eyeTrackerSystem;
            eyeTrackingPipeline = other.eyeTrackingPipeline;
            calibrationMethod = other.calibrationMethod;

            AllEyeTrackerSystemSettings = other.AllEyeTrackerSystemSettings;
            AllTrackingPipelinesSettings = other.AllTrackingPipelinesSettings;
            AllCalibrationImplementations = other.AllCalibrationImplementations;

            var selections = new[] { nameof(EyeTrackerSystem), nameof(EyeTrackingPipeline), nameof(CalibrationMethod) };
            var lists = new[] { nameof(AllEyeTrackerSystemSettings), nameof(AllTrackingPipelinesSettings), nameof(AllCalibrationImplementations) };
            var properties = typeof(EyeTrackerSettings).GetProperties()
                .Where(p => p.CanRead && p.GetSetMethod() != null && p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<XmlIgnoreAttribute>() is null && p.GetCustomAttribute<SharedSettingAttribute>() is null)
                .Where(p => !selections.Contains(p.Name) && !lists.Contains(p.Name));
            foreach (var property in properties)
            {
                property.SetValue(this, property.GetValue(other));
            }

            // Select again, for the events and the mm per pixel of the tracking settings.
            EyeTrackerSystem = eyeTrackerSystem;
            EyeTrackingPipeline = eyeTrackingPipeline;
            CalibrationMethod = calibrationMethod;
            foreach (var name in selections) OnPropertyChanged(this, name);
        }
    }

    /// <summary>
    /// Contents of OpenIrisSettings.xml: the active profile and the shared settings.
    /// </summary>
    [XmlRoot(RootName)]
    public sealed class SharedSettingsFile
    {
        /// <summary>
        /// Name of the root element, which tells this file from the settings files of older versions.
        /// </summary>
        public const string RootName = "OpenIrisSettings";

        /// <summary>
        /// Name of the active profile.
        /// </summary>
        public string ActiveProfile { get; set; } = EyeTrackerSettings.DefaultProfileName;

        /// <summary>
        /// The shared settings, as text.
        /// </summary>
        [XmlArray("SharedSettings")]
        [XmlArrayItem("Setting")]
        public List<Setting> Settings { get; set; } = new List<Setting>();

        /// <summary>
        /// One shared setting.
        /// </summary>
        public sealed class Setting
        {
            /// <summary>
            /// Name of the setting.
            /// </summary>
            [XmlAttribute]
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// Value of the setting.
            /// </summary>
            [XmlText]
            public string Value { get; set; } = string.Empty;
        }
    }
}
