//-----------------------------------------------------------------------
// <copyright file="EyeTrackingPluginLoader.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
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
                var pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

                // An aggregate catalog that combines multiple catalogs
                // Adds all the parts found in all assemblies in the same directory as the executing program
                // and in the plugin directory
                using var catalog = new AggregateCatalog();
                using var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                using var pluginCatalog = new DirectoryCatalog(pluginFolder);

                catalog.Catalogs.Add(assemblyCatalog);
                if (!safeMode)
                {
                    catalog.Catalogs.Add(pluginCatalog);
                }

                EyeTrackingsyStemFactory = new EyeTrackerPluginLoader<IEyeTrackingSystem, IEyeTrackingSystemMetadata>(catalog);
                CalibrationPipelineFactory = new EyeTrackerPluginLoader<ICalibrationPipeline, IEyeTrackerPluginMetadata>(catalog);
                EyeTrackingPipelineFactory = new EyeTrackerPluginLoader<IEyeTrackingPipeline, IEyeTrackerPluginMetadata>(catalog);

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

        /// <summary>
        /// Extra types that should be given tot he xml serializer to read the settings file.
        /// </summary>
        public static List<Type>? ExtraSettingsTypesForXML { get; private set; }

        /// <summary>
        /// Factory for eye tracking systems.
        /// </summary>
        public static EyeTrackerPluginLoader<IEyeTrackingSystem, IEyeTrackingSystemMetadata>? EyeTrackingsyStemFactory { get; private set; }

        /// <summary>
        /// Factory for eye tracking calibration methods.
        /// </summary>
        public static EyeTrackerPluginLoader<ICalibrationPipeline, IEyeTrackerPluginMetadata>? CalibrationPipelineFactory { get; private set; }

        /// <summary>
        /// Factory for eye tracking pipelines.
        /// </summary>
        public static EyeTrackerPluginLoader<IEyeTrackingPipeline, IEyeTrackerPluginMetadata>? EyeTrackingPipelineFactory { get; private set; }

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
        where TPlugin : class, IPlugin
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
                    var newPluginObject = ClassFactories.First(x => x.Metadata.Name == name)?.CreateExport().Value ??
                        throw new PluginManagerException($"Plugin '{name}' not found.");
                    newPluginObject.Name = name;
                    return newPluginObject;
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
    }


    /// <summary>
    /// Interface of all plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        string Name { get; set; }
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
            this.Name = name;
            this.SettingsType = typeOfSettings;

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
        /// <summary>
        /// No videos.
        /// </summary>
        None,

        /// <summary>
        /// One video for both eyes.
        /// </summary>
        SingleVideoTwoEyes,

        /// <summary>
        /// One video for only one eye.
        /// </summary>
        SingleVideoOneEye,

        /// <summary>
        /// Two videos, one for each eye.
        /// </summary>
        TwoVideosTwoEyes,
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
        public PluginDescriptionEyeTrackingSystemAttribute(string name, Type settingsType, VideoEyeConfiguration videoConfiguration = VideoEyeConfiguration.TwoVideosTwoEyes)
            : base(typeof(IEyeTrackerPluginMetadata))
        {
            this.Name = name;
            this.VideoConfiguration = videoConfiguration;
            this.SettingsType = settingsType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public PluginDescriptionEyeTrackingSystemAttribute(string name)
            : this(name, typeof(EyeTrackingSystemSettings), VideoEyeConfiguration.TwoVideosTwoEyes)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
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
            string[]? classesAvailable = null;

            if (typeof(T) == typeof(EyeTrackingSystemBase))
            {
                classesAvailable = EyeTrackerPluginManager.EyeTrackingsyStemFactory?.ClassesAvaiable.Select(x => x.Name).ToArray();
            }

            if (typeof(T) == typeof(CalibrationSession))
            {
                classesAvailable = EyeTrackerPluginManager.CalibrationPipelineFactory?.ClassesAvaiable.Select(x => x.Name).ToArray();
            }

            if (typeof(T) == typeof(IEyeTrackingPipeline))
            {
                classesAvailable = EyeTrackerPluginManager.EyeTrackingPipelineFactory?.ClassesAvaiable.Select(x => x.Name).ToArray();
            }

            if (classesAvailable is null) throw new InvalidOperationException("Wrong type");

            Array.Sort(classesAvailable);

            return new StandardValuesCollection(classesAvailable);
        }
    }
}