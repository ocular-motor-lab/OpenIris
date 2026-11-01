//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPluginLoader.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// http://stackoverflow.com/questions/11488297/how-do-you-use-exportfactoryt
    /// </summary>
    public static class EyeTrackerPluginManager
    {
        /// <summary>
        /// Init the singleton plugin manager.
        /// </summary>
        public static void Init(bool safeMode)
        {
            try
            {
                if (!resolvingFromPath)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += LoadFromPath;
                    resolvingFromPath = true;
                }

                var pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");

                // An aggregate catalog that combines multiple catalogs
                // Adds all the parts found in all assemblies in the same directory as the executing program
                // and in the plugin directory
                // Kept for GetExports, for the life of the program.
                var catalog = new AggregateCatalog();
                var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                var pluginCatalog = new DirectoryCatalog(pluginFolder);

                catalog.Catalogs.Add(assemblyCatalog);
                if (!safeMode)
                {
                    catalog.Catalogs.Add(pluginCatalog);
                }

                EyeTrackingsyStemFactory = new EyeTrackerPluginLoader<EyeTrackingSystemBase, IEyeTrackingSystemMetadata>(catalog);
                CalibrationPipelineFactory = new EyeTrackerPluginLoader<CalibrationPipelineBase, IEyeTrackerPluginMetadata>(catalog);
                EyeTrackingPipelineFactory = new EyeTrackerPluginLoader<EyeTrackingPipelineBase, IEyeTrackerPluginMetadata>(catalog);
                container = new CompositionContainer(catalog);

                ExtraSettingsTypesForXML = new List<Type>();
                ExtraSettingsTypesForXML.AddRange(EyeTrackingsyStemFactory.ClassesAvaiable.Select(t => t.SettingsType));
                ExtraSettingsTypesForXML.AddRange(EyeTrackingPipelineFactory.ClassesAvaiable.Select(t => t.SettingsType));
                ExtraSettingsTypesForXML.AddRange(CalibrationPipelineFactory.ClassesAvaiable.Select(t => t.SettingsType));
            }
            catch (Exception ex)
            {
                throw new PluginManagerException("ERROR loading plugins", ex);
            }
        }

        private static CompositionContainer? container;

        /// <summary>
        /// Gets every part exported as type T by OpenIris and the plugins. User interfaces use it to
        /// find what plugins provide for them, such as the windows of eye tracking systems, without
        /// the core knowing about any user interface library.
        /// </summary>
        /// <typeparam name="T">Type the parts are exported as.</typeparam>
        public static IEnumerable<T> GetExports<T>()
        {
            try
            {
                return container?.GetExportedValues<T>() ?? Enumerable.Empty<T>();
            }
            catch (Exception ex) when (ex is CompositionException || ex is ReflectionTypeLoadException)
            {
                Trace.WriteLine($"ERROR loading the plugin parts of type {typeof(T).Name}: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        private static bool resolvingFromPath;
        private static readonly HashSet<string> notInPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads an assembly that isn't next to OpenIris or its plugins from the folders in PATH.
        /// Camera SDKs such as Spinnaker install their .NET library in a folder they add to PATH,
        /// next to the native libraries it needs, and the two must be the same version. So plugins
        /// don't ship it: it is loaded from there, whatever version of the SDK is installed.
        /// </summary>
        private static Assembly? LoadFromPath(object sender, ResolveEventArgs args)
        {
            var fileName = new AssemblyName(args.Name).Name + ".dll";

            lock (notInPath)
            {
                if (notInPath.Contains(fileName)) return null;

                foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
                {
                    try
                    {
                        var file = Path.Combine(folder.Trim().Trim('"'), fileName);
                        if (!File.Exists(file)) continue;

                        var assembly = Assembly.LoadFrom(file);
                        Trace.WriteLine($"Loaded {assembly.FullName} from {file}");
                        return assembly;
                    }
                    catch (ArgumentException)
                    {
                        // A PATH entry with invalid characters.
                    }
                }

                notInPath.Add(fileName);
                return null;
            }
        }

        /// <summary>
        /// Extra types that should be given tot he xml serializer to read the settings file.
        /// </summary>
        public static List<Type>? ExtraSettingsTypesForXML { get; private set; }

        /// <summary>
        /// Factory for eye tracking systems.
        /// </summary>
        public static EyeTrackerPluginLoader<EyeTrackingSystemBase, IEyeTrackingSystemMetadata>? EyeTrackingsyStemFactory { get; private set; }

        /// <summary>
        /// Factory for eye tracking calibration methods.
        /// </summary>
        public static EyeTrackerPluginLoader<CalibrationPipelineBase, IEyeTrackerPluginMetadata>? CalibrationPipelineFactory { get; private set; }

        /// <summary>
        /// Factory for eye tracking pipelines.
        /// </summary>
        public static EyeTrackerPluginLoader<EyeTrackingPipelineBase, IEyeTrackerPluginMetadata>? EyeTrackingPipelineFactory { get; private set; }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <param name="t">Type of the plugin.</param>
        /// <returns>Name of the plugin.</returns>
        public static string GetPluginName(Type t)
        {
            return ((PluginDescriptionAttribute)Attribute.GetCustomAttribute(t, typeof(PluginDescriptionAttribute))).Name;
        }
    }

    /// <summary>
    /// Class to load instances of plugins.
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    /// <typeparam name="TPluginMetadata"></typeparam>
    public class EyeTrackerPluginLoader<TPlugin, TPluginMetadata>
        where TPlugin : class
        where TPluginMetadata : IEyeTrackerPluginMetadata
    {
        /// <summary>
        /// Initializzes an instance of EyeTrackerPluginLoader.
        /// </summary>
        /// <param name="catalog"></param>
        public EyeTrackerPluginLoader(AggregateCatalog catalog)
        {
            try
            {
                // Create the CompositionContainer with the parts in the catalog
                var container = new CompositionContainer(catalog);
                container.SatisfyImportsOnce(this);

                Trace.WriteLine($"{typeof(TPlugin).FullName} loader : {this.ClassFactories.Count<ExportFactory<TPlugin, TPluginMetadata>>()} factories available");
            }
            catch (ReflectionTypeLoadException ex)
            {
                var sb = new StringBuilder();
                foreach (var exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    if (exSub is FileNotFoundException exFileNotFound)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }

                string errorMessage = sb.ToString();

                throw new PluginManagerException("Error loading plugins: " + errorMessage, ex);
            }
        }

        [ImportMany]
        private IEnumerable<ExportFactory<TPlugin, TPluginMetadata>>? ClassFactories { get; set; }

        /// <summary>
        /// Gets a list of camera systems available.
        /// </summary>
        public IEnumerable<TPluginMetadata> ClassesAvaiable { get { return ClassFactories.Select(x => x.Metadata); } }

        /// <summary>
        /// Whether a plugin with this name is installed.
        /// </summary>
        /// <param name="name">Name of the plugin.</param>
        public bool IsAvailable(string name) => ClassFactories?.Any(x => x.Metadata.Name == name) ?? false;

        /// <summary>
        /// Generic factory method.
        /// </summary>
        /// <param name="name">Name of the factory. Must match Metadata.Name.</param>
        /// <returns>The matching factory to the name or null if not found.</returns>
        public TPlugin Create(string name)
        {
            try
            {
                lock (this)
                {
                    // Find a factory by its name
                    return ClassFactories.First(x => x.Metadata.Name == name)?.CreateExport().Value ??
                         throw new PluginManagerException($"Plugin '{name}' not found.");
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentNullException)
            {
                throw new PluginManagerException($"ERROR loading plugin '{name}'.", ex);
            }
        }

        /// <summary>
        /// Gets the default settings for the plugin
        /// </summary>
        /// <param name="name">Name of the factory. Must match Metadata.Name.</param>
        /// <returns></returns>
        public EyeTrackerSettingsBase GetDefaultSettings(string name)
        {
            try
            {
                lock (this)
                {
                    // Find a factory by its name
                    var settingsType = ClassFactories.First(x => x.Metadata.Name == name)?.Metadata.SettingsType ??
                        throw new PluginManagerException($"Plugin '{name}' not found.");

                    return Activator.CreateInstance(settingsType) as EyeTrackerSettingsBase ??
                        throw new PluginManagerException("Bad settings");
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentNullException)
            {
                throw new PluginManagerException($" Error loading plugin '{name}'.", ex);
            }
        }

        /// <summary>
        /// Gets the default settings for the plugin given the system object
        /// </summary>
        /// <param name="system">Name of the factory. Must match Metadata.Name.</param>
        /// <returns></returns>
        public EyeTrackerSettingsBase GetDefaultSettings(EyeTrackingSystemBase system)
        {
            var systemAttribute = Attribute.GetCustomAttribute(system.GetType(), typeof(PluginDescriptionEyeTrackingSystemAttribute)) as PluginDescriptionEyeTrackingSystemAttribute;
            var systemName = systemAttribute?.Name ?? String.Empty;

            return GetDefaultSettings(systemName);
        }

    }

    /// <summary>
    /// Interface of metadata for all plugins.
    /// </summary>
    public interface IEyeTrackerPluginMetadata
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of the settings that this pluggin will work with.
        /// </summary>
        public Type SettingsType { get; }

    }

    /// <summary>
    /// Metadata base class for all plugins with name.
    /// </summary>
    [MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginDescriptionAttribute : ExportAttribute, IEyeTrackerPluginMetadata
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the settings that this pluggin will work with.
        /// </summary>
        public Type SettingsType { get; }

        /// <summary>
        /// Initializes an instance of the plugin metadata.
        /// </summary>
        public PluginDescriptionAttribute(string name, Type typeOfSettings)
            : base(typeof(IEyeTrackerPluginMetadata))
        {
            Name = name;
            SettingsType = typeOfSettings;
        }
    }

    /// <summary>
    /// Interface for the metadata related to an eye tracking system.
    /// </summary>
    public interface IEyeTrackingSystemMetadata : IEyeTrackerPluginMetadata
    {
        /// <summary>
        /// Number of videos that we should select when playing or postprocessing a video from this system.
        /// </summary>
        public VideoEyeConfiguration VideoConfiguration { get; }

    }

    /// <summary>
    /// Posible configuration for videos.
    /// </summary>
    public enum VideoEyeConfiguration
    {
        OneVideo,
        TwoVideos,
    }

    /// <summary>
    /// Class for the atribute to add a description to the exported eye tracking systems.
    /// </summary>
    [MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginDescriptionEyeTrackingSystemAttribute : ExportAttribute, IEyeTrackingSystemMetadata
    {
        /// <summary>
        /// Name of the eye tracker system. Used to select it in the settings menu.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Number of videos that we should select when playing or postprocessing a video from this system.
        /// </summary>
        public VideoEyeConfiguration VideoConfiguration { get; private set; }

        /// <summary>
        /// The type of the settings that this pluggin will work with.
        /// </summary>
        public Type SettingsType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="settingsType"></param>
        /// <param name="videoConfiguration"></param>
        public PluginDescriptionEyeTrackingSystemAttribute(string name, Type settingsType, VideoEyeConfiguration videoConfiguration = VideoEyeConfiguration.TwoVideos)
            : base(typeof(IEyeTrackerPluginMetadata))
        {
            Name = name;
            VideoConfiguration = videoConfiguration;
            SettingsType = settingsType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public PluginDescriptionEyeTrackingSystemAttribute(string name)
            : this(name, typeof(EyeTrackingSystemSettings), VideoEyeConfiguration.TwoVideos)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;
    }

    /// <summary>
    /// Exception for plugin manager
    /// </summary>
    [Serializable]
    public class PluginManagerException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public PluginManagerException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public PluginManagerException(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public PluginManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected PluginManagerException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
#pragma warning disable CA1812 // Class never instantiated. Because it is used in reflection

    }

    /// <summary>
    /// Class that will appear in a property grid as a drop down list with all the classes that
    /// implemenent a given interface. Useful for plugins. So all the plugins available will show up
    /// and the user can select. http://stackoverflow.com/questions/14593364/propertygrid-control-and-drop-down-lists
    /// </summary>
    /// <typeparam name="T">Interface that the list elements must implement.</typeparam>
    internal class PluginListTypeConverter<T> : TypeConverter
        where T : class
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var classesAvailable = typeof(T) switch
            {
                Type _ when typeof(T) == typeof(EyeTrackingSystemBase) => EyeTrackerPluginManager.EyeTrackingsyStemFactory?.ClassesAvaiable,
                Type _ when typeof(T) == typeof(CalibrationPipelineBase) => EyeTrackerPluginManager.CalibrationPipelineFactory?.ClassesAvaiable,
                Type _ when typeof(T) == typeof(EyeTrackingPipelineBase) => EyeTrackerPluginManager.EyeTrackingPipelineFactory?.ClassesAvaiable,
                _ => null,
            } ?? throw new InvalidOperationException("Wrong type");

            return new StandardValuesCollection(classesAvailable.Select(x => x.Name).OrderBy(x => x).ToList());
        }
    }
}