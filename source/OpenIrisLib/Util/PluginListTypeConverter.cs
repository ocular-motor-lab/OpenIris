//-----------------------------------------------------------------------
// <copyright file="PluginListTypeConverter.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

#pragma warning disable CA1812 // Class never instantiated. Because it is used in reflection

    using System;
    using System.Linq;
    using System.ComponentModel;

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
