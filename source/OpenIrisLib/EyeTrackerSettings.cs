//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettings.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    using System;
    using System.IO;
    using System.ComponentModel;
    using System.Xml;
    using System.Xml.Serialization;


    /// <summary>
    /// Class containing all the settings of the eye tracker. It can be linked to a property grid to
    /// offer a nice gui.
    /// </summary>
    [Serializable]
    public sealed class EyeTrackerSettings : EyeTrackerSettingsBase
    {
        #region A) Choose an eye tracking system plugin"

        [Category("A) Choose an eye tracking system"), Description("EyeTracker system. What type of device or device configuration you want to use.")]
        [NeedsRestarting]
        [TypeConverter(typeof(PluginListTypeConverter<EyeTrackingSystemBase>))]
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

                    AllEyeTrackerSystemSettings[value].PropertyChanged += (o, e) => OnPropertyChanged(o, e);
                    AllEyeTrackerSystemSettings[value].PropertyChangingNeedsRestart += (o, e) => OnPropertyChangingNeedsRestart(o, e.PropertyName);
                    AllEyeTrackerSystemSettings[value].MmPerPixChanged += (o, e) =>
                    {
                        foreach (var t in AllTrackingPipelinesSettings.Values)
                        {
                            t.MmPerPix = EyeTrackingSystemSettings.MmPerPix;
                        }
                    };
                }

                if (value != eyeTrackerSystem)
                {
                    eyeTrackerSystem = value;

                    OnPropertyChanged(this, nameof(EyeTrackerSystem));
                }

                // TODO: very ugly line to just make sure the settings are updated properly
                // Need to set the mm per pixel for the tracking settings
                foreach (var t in AllTrackingPipelinesSettings.Values)
                {
                    t.MmPerPix = EyeTrackingSystemSettings.MmPerPix;
                }
            }
        }

        private string eyeTrackerSystem = "Simulation";

        /// <summary>
        /// This property will return the settings for the current eye tracking system. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [NeedsRestarting(true)]
        public EyeTrackingSystemSettings EyeTrackingSystemSettings => AllEyeTrackerSystemSettings[eyeTrackerSystem];


        #endregion A) Choose an eye tracking system"

        #region B) Choose a tracking pipeline plugin

        [Category("B) Choose a tracking pipeline"), Description("Tracking pipeline. What pipeline for tracking position, torsion etc you want to use?")]
        [TypeConverter(typeof(PluginListTypeConverter<EyeTrackingPipelineBase>))]
        public string EyeTrackingPipeline
        {
            get { return eyeTrackingPipeline; }
            set
            {
                // If the dictionary does not contain the settings for the current eye tracking
                // system, get the defaults and add them.
                if (!AllTrackingPipelinesSettings.ContainsKey(value))
                {
                    var trackingSettings = (EyeTrackingPipelineSettings?)EyeTrackerPluginManager.EyeTrackingPipelineFactory?.GetDefaultSettings(value)
                        ?? throw new InvalidOperationException("Bad EyeTrackingPipelineFactory");

                    AllTrackingPipelinesSettings.Add(value, trackingSettings);

                    AllTrackingPipelinesSettings[value].PropertyChanged += (o, e) => OnPropertyChanged(o, e.PropertyName);
                    AllTrackingPipelinesSettings[value].PropertyChangingNeedsRestart += (o, e) => OnPropertyChangingNeedsRestart(o, e.PropertyName);
                }

                if (value != eyeTrackingPipeline)
                {
                    eyeTrackingPipeline = value;

                    OnPropertyChanged(this, nameof(EyeTrackingPipeline));
                }
            }
        }
        private string eyeTrackingPipeline = "JOM";

        /// <summary>
        /// This property will return the settings for the current pipeline. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public EyeTrackingPipelineSettings TrackingPipelineSettings => AllTrackingPipelinesSettings[eyeTrackingPipeline];

        #endregion B) Choose a tracking pipeline

        #region C) Choose a calibration method plugin


        [Category("C) Choose a calibration method"), Description("Calibration method")]
        [TypeConverter(typeof(PluginListTypeConverter<CalibrationPipelineBase>))]
        public string CalibrationMethod
        {
            get { return calibrationMethod; }
            set
            {
                // If the dictionary does not contain the settings for the current eye tracking
                // system, get the defaults and add them.
                if (!AllCalibrationImplementations.ContainsKey(value))
                {
                    var calibrationSettings = (CalibrationSettings?)EyeTrackerPluginManager.CalibrationPipelineFactory?.GetDefaultSettings(value)
                        ?? throw new InvalidOperationException("Bad CalibrationFactory");

                    AllCalibrationImplementations.Add(value, calibrationSettings);

                    AllCalibrationImplementations[value].PropertyChanged += (o, e) => OnPropertyChanged(o, e.PropertyName);
                    AllCalibrationImplementations[value].PropertyChangingNeedsRestart += (o, e) => OnPropertyChangingNeedsRestart(o, e.PropertyName);
                }

                if (value != calibrationMethod)
                {
                    calibrationMethod = value;
                    OnPropertyChanged(this, nameof(CalibrationMethod));
                }
            }
        }
        private string calibrationMethod = "Auto"; // Default value


        /// <summary>
        /// This property will return the settings for the current calibration. It will show in the UI but it
        /// will not be serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public CalibrationSettings CalibrationSettings => AllCalibrationImplementations[calibrationMethod];

        #endregion C) Choose a calibration method

        #region D) General settings

        [Category("D) General settings"), Description("Maximum number of processing threads.")]
        [NeedsRestarting(true)]
        public int MaxNumberOfProcessingThreads { get => maxNumberOfProcessingThreads; set => SetProperty(ref maxNumberOfProcessingThreads, value, nameof(MaxNumberOfProcessingThreads)); }
        private int maxNumberOfProcessingThreads = 1; // Default value

        [Category("D) General settings"), Description("Value indicating where debuging images should be shown")]
        public bool Debug { get => debug; set => SetProperty(ref debug, value, nameof(Debug)); }
        private bool debug = false; // Default value


        [Category("D) General settings"), Description("Gets the size of the buffer.")]
        [NeedsRestarting]
        public int BufferSize { get => bufferSize; set => SetProperty(ref bufferSize, value, nameof(BufferSize)); }
        private int bufferSize = 100; // Default value

        #endregion D) General settings

        #region E) Recording settings

        [Category("E) Recording settings"), Description("Name of the data files")]
        public string SessionName { get => sessionName; set => SetProperty(ref sessionName, value, nameof(SessionName)); }
        private string sessionName = "SESSION_NAME"; // Default value

        [Category("E) Recording settings"), Description("Folder where data is saved")]
        public string DataFolder { get => dataFolder; set => SetProperty(ref dataFolder, value, nameof(DataFolder)); }
        private string dataFolder = @"C:\secure\Data\Raw\Torsion\DataTest"; // Default value

        [Browsable(false)]
        [Category("E) Recording settings"), Description("Last file saved.")]
        public string LastRecordedFile { get => lastRecordedFile; set => SetProperty(ref lastRecordedFile, value, nameof(LastRecordedFile)); }
        private string lastRecordedFile = string.Empty; // Default value

        [Category("E) Recording settings"), Description("Indicates if video should be recorded.")]
        public bool RecordVideo { get => recordVideo; set => SetProperty(ref recordVideo, value, nameof(RecordVideo)); }
        private bool recordVideo = true; // Default value

        [Category("E) Recording settings"), Description("Proportion of frames that should be recorded (1 every x).")]
        public int DecimateVideoRatio { get => decimateVideoRatio; set => SetProperty(ref decimateVideoRatio, value, nameof(DecimateVideoRatio)); }
        private int decimateVideoRatio = 1; // Default value

        #endregion E) Recording settings

        #region F) Display settings

        [Category("F) Display settings"), Description("Number of seconds in trace plots.")]
        public double TraceSpan { get => traceSpan; set => SetProperty(ref traceSpan, value, nameof(TraceSpan)); }
        private double traceSpan = 5; // Default value

        #endregion F) Display settings

        #region G) Network settings

        [Category("F) Network "), Description("TCP port where the host is listening")]
        [NeedsRestarting]
        public int ServiceListeningPort { get => serviceListeningPort; set => SetProperty(ref serviceListeningPort, value, nameof(ServiceListeningPort)); }
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

        #region Setting loading and saving, including plugin settings

        /// <summary>
        /// Path of the file.
        /// </summary>
        private string settingsPath = "";

        public EyeTrackerSettings()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EyeTrackerSettings class.
        /// </summary>
        public EyeTrackerSettings(bool safeMode = false)
        {
            allEyeTrackerSystemsSettings = new EyeTrackerSettingsDictionary<EyeTrackingSystemSettings>();
            allEyeTrackingPipelinesSettings = new EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings>();
            allCalibrationImplementations = new EyeTrackerSettingsDictionary<CalibrationSettings>();

            if (safeMode is false)
            {
                EyeTrackingPipeline = eyeTrackingPipeline;
                EyeTrackerSystem = eyeTrackerSystem;
                CalibrationMethod = calibrationMethod;
            }

            LastVideoEyeTrackerSystem = string.Empty;
            LastVideoFolder = string.Empty;
        }


        /// <summary>
        /// Tries to load the settings from disk. If not possible loads the default settings
        /// </summary>
        public static EyeTrackerSettings Load()
        {
            var settingsFileName = Path.Combine(Directory.GetCurrentDirectory(), "EyeTrackerSettings.xml");

            try
            {
                var reader = new XmlSerializer(typeof(EyeTrackerSettings), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());

                using var file = new StreamReader(settingsFileName);
                using var xmlreader = XmlReader.Create(file);

                var settings = (EyeTrackerSettings)reader.Deserialize(xmlreader);
                settings.settingsPath = settingsFileName;
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

            return new EyeTrackerSettings { settingsPath = settingsFileName };
        }

        /// <summary>
        /// Saves the current settings to disk on the same path it was loaded.
        /// </summary>
        public void Save()
        {
            Save(settingsPath);
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
                SetPropertyArray(ref allEyeTrackerSystemsSettings, value, nameof(AllEyeTrackerSystemSettings));

                // TODO: very ugly line to just make sure the settings are updated properly
                // Need to set the mm per pixel for the tracking settings
                foreach (var set in value)
                {
                    set.Value.MmPerPixChanged += (o, e) =>
                    {
                        if (EyeTrackerSystem == set.Key)
                        {
                            foreach (var t in AllTrackingPipelinesSettings.Values)
                            {
                                t.MmPerPix = EyeTrackingSystemSettings.MmPerPix;
                            }
                        }
                    };
                }
            }
        }
        private EyeTrackerSettingsDictionary<EyeTrackingSystemSettings> allEyeTrackerSystemsSettings;

        /// <summary>
        /// This collection holds the settings for all the eye tracking pipelines and will be serialized
        /// into the XML file.
        /// </summary>
        [Browsable(false)]
        public EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings> AllTrackingPipelinesSettings
        {
            get => allEyeTrackingPipelinesSettings;
            set => SetPropertyArray(ref allEyeTrackingPipelinesSettings, value, nameof(AllTrackingPipelinesSettings));
        }

        private EyeTrackerSettingsDictionary<EyeTrackingPipelineSettings> allEyeTrackingPipelinesSettings;

        /// <summary>
        /// This collection holds the settings for all the calibrations and will be serialized
        /// into the XML file.
        /// </summary>
        [Browsable(false)]
        public EyeTrackerSettingsDictionary<CalibrationSettings> AllCalibrationImplementations { get => allCalibrationImplementations; set => SetPropertyArray(ref allCalibrationImplementations, value, nameof(AllCalibrationImplementations)); }
        private EyeTrackerSettingsDictionary<CalibrationSettings> allCalibrationImplementations;

        #endregion
    }
}