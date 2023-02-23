//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettings.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.IO;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    //
    // TODO: consider using ApplicateSettingsBase. I tried but I could not get it to serialized properly.
    //

    /// <summary>
    /// Class containing all the settings of the eye tracker. It can be linked to a property grid to
    /// offer a nice gui.
    /// </summary>
    [Serializable]
    public sealed class EyeTrackerSettings : EyeTrackerSettingsXml
    {
        private EyeTrackerSettingsDictionary<EyeTrackingSystemSettings> allEyeTrackerSystemsSettings;
        private EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings> allEyeTrackingPipelinesSettings;
        private EyeTrackerSettingsDictionary<CalibrationSettings> allCalibrationImplementations;

        /// <summary>
        /// Initializes a new instance of the EyeTrackerSettings class.
        /// </summary>
        public EyeTrackerSettings()
        {
            allEyeTrackerSystemsSettings = new EyeTrackerSettingsDictionary<EyeTrackingSystemSettings>();
            allEyeTrackingPipelinesSettings = new EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings>();
            allCalibrationImplementations = new EyeTrackerSettingsDictionary<CalibrationSettings>();
            EyeTrackingPipeline = eyeTrackingPipeline;
            EyeTrackerSystem = eyeTrackerSystem;
            CalibrationMethod = calibrationMethod;

            LastVideoEyeTrackerSystem = "";
            LastVideoFolder = "";
        }

        /// <summary>
        /// This collection holds the settings for all the eye tracking systems and will be serialized
        /// into the XML file.
        /// </summary>
        [Browsable(false)]
        public EyeTrackerSettingsDictionary<EyeTrackingSystemSettings> AllEyeTrackerSystemSettings 
        {
            get => allEyeTrackerSystemsSettings;
            set
            {
                if (value is null) return;

                foreach (var v in value.Values)
                {
                    // make sure the property changes propagate
                    v.PropertyChanged += (o, e) => OnPropertyChanged(o, nameof(EyeTrackingSystemSettings));
                }

                allEyeTrackerSystemsSettings = value;
            }
        }

        /// <summary>
        /// This collection holds the settings for all the eye tracking pipelines and will be serialized
        /// into the XML file.
        /// </summary>
        [Browsable(false)]
        public EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings> AllTrackingPipelinesSettings
        {
            get => allEyeTrackingPipelinesSettings;
            set
            {
                if (value is null) return;

                foreach (var v in value.Values)
                {
                    // make sure the property changes propagate
                    v.PropertyChanged += (o, e) => OnPropertyChanged(o, nameof(EyeTrackingPipelineSettings));
                }

                allEyeTrackingPipelinesSettings = value;
            }
        }

        /// <summary>
        /// This collection holds the settings for all the calibrations and will be serialized
        /// into the XML file.
        /// </summary>
        [Browsable(false)]
        public EyeTrackerSettingsDictionary<CalibrationSettings> AllCalibrationImplementations
        {
            get => allCalibrationImplementations;
            set
            {
                if (value is null) return;

                foreach (var v in value.Values)
                {
                    // make sure the property changes propagate
                    v.PropertyChanged += (o, e) => OnPropertyChanged(o, nameof(CalibrationSettings));
                }

                allCalibrationImplementations = value;
            }
        }

        /// <summary>
        /// This property will return the settings for the current eye tracking system. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [NeedsRestarting(true)]
        public EyeTrackingSystemSettings EyeTrackingSystemSettings { get { return AllEyeTrackerSystemSettings[eyeTrackerSystem]; } }

        /// <summary>
        /// This property will return the settings for the current pipeline. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public EyeTrackingPipelineSettings TrackingpipelineSettings { get { return AllTrackingPipelinesSettings[eyeTrackingPipeline]; } }

        /// <summary>
        /// This property will return the settings for the current calibration. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public CalibrationSettings CalibrationSettings { get { return AllCalibrationImplementations[calibrationMethod]; } }


        #region A) Choose an eye tracking system"

        [Category("A) Choose an eye tracking system"), Description("EyeTracker system. What type of device or device configuration you want to use.")]
        [NeedsRestarting]
        [TypeConverter(typeof(PluginListTypeConverter<EyeTrackingSystem>))]
        public string EyeTrackerSystem
        {
            get { return eyeTrackerSystem; }
            set
            {
                // If the dictionary does not contain the settings for the current eye tracking
                // system, get the defaults and add them.
                if (!AllEyeTrackerSystemSettings.ContainsKey(value))
                {
                    var eyeTrackerSystemSettings = (EyeTrackingSystemSettings?)EyeTrackerPluginManager.EyeTrackingsyStemFactory?.GetDefaultSettings(value)
                        ?? throw new InvalidOperationException("Bad EyeTracking System");

                    AllEyeTrackerSystemSettings.Add(value, eyeTrackerSystemSettings);

                    AllEyeTrackerSystemSettings[value].PropertyChanged += (o, e) => OnPropertyChanged(o, nameof(EyeTrackingSystemSettings));
                }

                if (value != eyeTrackerSystem)
                {
                    eyeTrackerSystem = value;

                    OnPropertyChanged(this, nameof(EyeTrackerSystem));
                }

                // TODO: very ugly line to just make sure the settings are updated properly
                // Need to set the mm per pixel for the tracking settings
                // Updated on 2/10/23. A little less ugly but still not great. 
                // at least fixed the problem of updating the mmperpix in pipeline when 
                // it gets updated on the system settings. 
                foreach (var t in AllTrackingPipelinesSettings.Values)
                {
                    t.GetMmPerPix = ()=> EyeTrackingSystemSettings.MmPerPix;
                }
            }
        }

        private string eyeTrackerSystem = "Simulation";

        #endregion A) Choose an eye tracking system"

        #region B) Choose a tracking pipeline

        [Category("B) Choose a tracking pipeline"), Description("Tracking pipeline. What pipeline for tracking position, torsion etc you want to use?")]
        [TypeConverter(typeof(PluginListTypeConverter<IEyeTrackingPipeline>))]
        public string EyeTrackingPipeline
        {
            get { TrackingpipelineSettings.EyeTrackingPipelineName = eyeTrackingPipeline; return eyeTrackingPipeline; }
            set
            {
                // If the dictionary does not contain the settings for the current eye tracking
                // system, get the defaults and add them.
                if (!AllTrackingPipelinesSettings.ContainsKey(value))
                {
                    var trackingSettings = (EyeTrackingPipelineSettings?)EyeTrackerPluginManager.EyeTrackingPipelineFactory?.GetDefaultSettings(value)
                        ?? throw new InvalidOperationException("Bad EyeTrackingPipelineFactory");
                    trackingSettings.EyeTrackingPipelineName = value;

                    AllTrackingPipelinesSettings.Add(value, trackingSettings);

                    AllTrackingPipelinesSettings[value].PropertyChanged += (o, e) =>
                    {
                        OnPropertyChanged(o, nameof(TrackingpipelineSettings));
                    };
                }

                if (value != eyeTrackingPipeline)
                {
                    eyeTrackingPipeline = value;


                    OnPropertyChanged(this, nameof(EyeTrackingPipeline));
                }
            }
        }
        private string eyeTrackingPipeline = "JOM";

        #endregion B) Choose a tracking pipeline

        #region C) Choose a calibration method

        [Category("C) Choose a calibration method"), Description("Calibration method")]
        [TypeConverter(typeof(PluginListTypeConverter<CalibrationSession>))]
        public string CalibrationMethod
        {
            get { return calibrationMethod; }
            set
            {
                // If the dictionary does not contain the settings for the current eye tracking
                // system, get the defaults and add them.
                if (!AllCalibrationImplementations.ContainsKey(value))
                {
                    var calibrationSettings = (CalibrationSettings?)EyeTrackerPluginManager.CalibrationFactory?.GetDefaultSettings(value)
                        ?? throw new InvalidOperationException("Bad CalibrationFactory");

                    AllCalibrationImplementations.Add(value, calibrationSettings);

                    AllCalibrationImplementations[value].PropertyChanged += (o, e) =>
                    {
                        OnPropertyChanged(o, nameof(CalibrationSettings));
                    };
                }

                if (value != calibrationMethod)
                {
                    calibrationMethod = value;
                    OnPropertyChanged(this, nameof(CalibrationMethod));
                }
            }
        }
        private string calibrationMethod = "Auto"; // Default value

        #endregion C) Choose a calibration method

        #region D) General settings

        [Category("D) General settings"), Description("Value indicating where debuging images should be shown")]
        public bool Debug
        {
            get { return debug; }
            set
            {
                if (value != debug)
                {
                    debug = value;
                    OnPropertyChanged(this, nameof(Debug));
                }
            }
        }
        private bool debug = false; // Default value

        [Category("D) General settings"), Description("Gets or sets the tracking mode")]
        [NeedsRestarting]
        public Eye TrackingMode
        {
            get { return trackingMode; }
            set
            {
                if (value != trackingMode)
                {
                    trackingMode = value;
                    OnPropertyChanged(this, nameof(TrackingMode));
                }
            }
        }
        private Eye trackingMode = Eye.Both; // Default value


        [Category("D) General settings"), Description("Gets the size of the buffer.")]
        [NeedsRestarting]
        public int BufferSize
        {
            get { return bufferSize; }
            set
            {
                if (value != bufferSize)
                {
                    bufferSize = value;
                    OnPropertyChanged(this, nameof(BufferSize));
                }
            }
        }
        private int bufferSize = 100; // Default value

        #endregion D) General settings

        #region E) Recording settings

        [Category("E) Recording settings"), Description("Name of the data files")]
        public string SessionName
        {
            get
            {
                return sessionName;
            }

            set
            {
                if (value != sessionName)
                {
                    sessionName = value;
                    OnPropertyChanged(this, nameof(SessionName));
                }
            }
        }

        private string sessionName = "SESSION_NAME"; // Default value

        [Category("E) Recording settings"), Description("Folder where data is saved")]
        public string DataFolder
        {
            get
            {
                return dataFolder;
            }

            set
            {
                if (value != dataFolder)
                {
                    dataFolder = value;
                    OnPropertyChanged(this, nameof(DataFolder));
                }
            }
        }

        private string dataFolder = @"C:\secure\Data\Raw\Torsion\DataTest"; // Default value

        [Browsable(false)]
        [Category("E) Recording settings"), Description("Last file saved.")]
        public string LastRecordedFile
        {
            get
            {
                return lastRecordedFile;
            }

            set
            {
                if (value != lastRecordedFile)
                {
                    lastRecordedFile = value;
                    OnPropertyChanged(this, nameof(LastRecordedFile));
                }
            }
        }

        private string lastRecordedFile = string.Empty; // Default value

        [Category("E) Recording settings"), Description("Indicates if video should be recorded.")]
        public bool RecordVideo
        {
            get
            {
                return recordVideo;
            }

            set
            {
                if (value != recordVideo)
                {
                    recordVideo = value;
                    OnPropertyChanged(this, nameof(RecordVideo));
                }
            }
        }

        private bool recordVideo = true; // Default value

        [Category("E) Recording settings"), Description("Proportion of frames that should be recorded (1 every x).")]
        public int DecimateVideoRatio
        {
            get
            {
                return decimateVideoRatio;
            }

            set
            {
                if (value != decimateVideoRatio)
                {
                    decimateVideoRatio = value;
                    OnPropertyChanged(this, nameof(DecimateVideoRatio));
                }
            }
        }

        private int decimateVideoRatio = 1; // Default value

        #endregion E) Recording settings

        #region F) Display settings

        [Category("F) Display settings"), Description("Number of seconds in trace plots.")]
        public double TraceSpan
        {
            get
            {
                return traceSpan;
            }

            set
            {
                if (value != traceSpan)
                {
                    traceSpan = value;
                    OnPropertyChanged(this, nameof(TraceSpan));
                }
            }
        }
        private double traceSpan = 5; // Default value

        #endregion F) Display settings

        #region G) Network settings

        [Category("F) Network "), Description("TCP port where the host is listening")]
        [NeedsRestarting]
        public int ServiceListeningPort
        {
            get
            {
                return serviceListeningPort;
            }

            set
            {
                if (value != serviceListeningPort)
                {
                    serviceListeningPort = value;
                    OnPropertyChanged(this, nameof(ServiceListeningPort));
                }
            }
        }

        private int serviceListeningPort = 9000; // Default value

        #endregion  Network settings

        #region Settings just for storage

        /// <summary>
        /// Last system selected to play or process a video.
        /// </summary>
        [Browsable(false)]
        public string LastVideoEyeTrackerSystem { get; set; }

        /// <summary>
        /// Folder of the last video selected to play or process.
        /// </summary>
        [Browsable(false)]
        public string LastVideoFolder { get; set; }

        #endregion

        /// <summary>
        /// Tries to load the settings from disk. If not possible loads the default settings
        /// </summary>
        public static EyeTrackerSettings Load()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "EyeTrackerSettings.xml");
            return (Load(typeof(EyeTrackerSettings), filePath) as EyeTrackerSettings)
                ?? new EyeTrackerSettings { SettingsPath = filePath };
        }
    }

    /// <summary>
    /// Attribute used to indicate if a given settings requires the eye tracker to restart to take effect.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NeedsRestartingAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether the changed property requires restarting to take effect.
        /// </summary>
        public bool Value { get; }

        /// <summary>
        /// Initializes a new instance of the NeedsRestarting class.
        /// </summary>
        /// <param name="needsRestarting">
        /// Value indicating whether the changed property requires restarting to take effect.
        /// </param>
        public NeedsRestartingAttribute(bool needsRestarting = true)
        {
            Value = needsRestarting;
        }
    }

    [Serializable]
    public class EyeTrackerSettingsXml : EyeTrackerSettingsBase
    {
        /// <summary>
        /// Path of the file.
        /// </summary>
        protected string SettingsPath { get; set; } = "";

        /// <summary>
        /// Tries to load the settings from disk. If not possible loads the default settings
        /// </summary>
        public static EyeTrackerSettingsXml? Load(Type type, string settingsFileName)
        {
            try
            {
                var reader = new XmlSerializer(type, EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());

                using var file = new StreamReader(settingsFileName);
                using var xmlreader = XmlReader.Create(file);

                var settings = (EyeTrackerSettingsXml)reader.Deserialize(xmlreader);
                settings.SettingsPath = settingsFileName;
                return settings;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.WriteLine("Error loading settings going to defaults. " + ex);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Trace.WriteLine("Error loading settings going to defaults. " + ex);
            }

            return null;
        }


        /// <summary>
        /// Saves the current settings to disk on the same path it was loaded.
        /// </summary>
        public void Save()
        {
            Save(SettingsPath);
        }

        /// <summary>
        /// Save the settings in the desired location.
        /// </summary>
        /// <param name="location">Path for saving the settings.</param>
        public void Save(string location)
        {
            try
            {
                var writer = new XmlSerializer(typeof(EyeTrackerSettings), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());
                using var file = new StreamWriter(location);
                writer.Serialize(file, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error saving settings file. " + ex);
            }
        }
    }


    [Serializable]
    public class EyeTrackerSettingsBase : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChange event
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">Event parameters</param>
        public void OnPropertyChanged(object o, string name)
        {
            // Save thesettings everytime something changes
            PropertyChanged?.Invoke(o, new PropertyChangedEventArgs(name));
        }
    }
}