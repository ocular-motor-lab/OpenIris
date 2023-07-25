//-----------------------------------------------------------------------
// <copyright file="EyeTrackerSettings.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Forms;


    //
    // TODO: consider using ApplicateSettingsBase. I tried but I could not get it to serialized properly.
    //


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
    public class EyeTrackerSettingsBase : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChangingNeedsRestart;

        /// <summary>
        /// https://www.danrigby.com/2012/01/08/inotifypropertychanged-the-anders-hejlsberg-way/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="name"></param>
        protected void SetProperty<T>(ref T field, T value, string name)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                var needsRestartingAttributes = this.GetType().GetProperty(name)?.
                    GetCustomAttributes(typeof(NeedsRestartingAttribute), false) as NeedsRestartingAttribute[];

                bool needsRestarting = needsRestartingAttributes?.Select(x => x.Value).SingleOrDefault() ?? false;

                if (needsRestarting)
                {
                    PropertyChangingNeedsRestart?.Invoke(this, new PropertyChangedEventArgs(name));
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// https://www.danrigby.com/2012/01/08/inotifypropertychanged-the-anders-hejlsberg-way/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="name"></param>
        protected void SetPropertyArray<T>(ref EyeTrackerSettingsDictionary<T> field, EyeTrackerSettingsDictionary<T> value, string name)
            where T : EyeTrackerSettingsBase
        {
            if (field != value)
            {
                var needsRestartingAttributes = this.GetType().GetProperty(name)?.
                    GetCustomAttributes(typeof(NeedsRestartingAttribute), false) as NeedsRestartingAttribute[];

                bool needsRestarting = needsRestartingAttributes?.Select(x => x.Value).SingleOrDefault() ?? false;

                if (needsRestarting)
                {
                    PropertyChangingNeedsRestart?.Invoke(this, new PropertyChangedEventArgs(name));
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

                foreach (var v in value.Values)
                {
                    // make sure the property changes propagate
                    v.PropertyChanged += (o, e) => OnPropertyChanged(o, e.PropertyName);
                    v.PropertyChangingNeedsRestart += (o, e) => OnPropertyChangingNeedsRestart(o, e.PropertyName);
                }
            }
        }

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

        /// <summary>
        /// Raises the PropertyChange event
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">Event parameters</param>
        public void OnPropertyChangingNeedsRestart(object o, string name)
        {
            // Save thesettings everytime something changes
            PropertyChangingNeedsRestart?.Invoke(o, new PropertyChangedEventArgs(name));
        }
    }
}