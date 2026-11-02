//-----------------------------------------------------------------------
// <copyright file="SettingsSubset.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris.UI
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Some of the settings of an object, for a PropertyGrid: for example only the shared settings,
    /// or all but them. Changes go to the object itself.
    /// </summary>
    internal sealed class SettingsSubset : CustomTypeDescriptor
    {
        private readonly object settings;
        private readonly Func<PropertyDescriptor, bool> include;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="settings">Object with the settings.</param>
        /// <param name="include">Which of its properties to show.</param>
        public SettingsSubset(object settings, Func<PropertyDescriptor, bool> include)
            : base(TypeDescriptor.GetProvider(settings).GetTypeDescriptor(settings))
        {
            this.settings = settings;
            this.include = include;
        }

        /// <summary>
        /// Gets the object with the settings.
        /// </summary>
        public object Settings => settings;

        /// <summary>
        /// Whether a property is a shared setting (the same in every profile).
        /// </summary>
        public static bool IsShared(PropertyDescriptor property) => property.Attributes[typeof(SharedSettingAttribute)] != null;

        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) =>
            new PropertyDescriptorCollection(base.GetProperties(attributes).Cast<PropertyDescriptor>().Where(include).ToArray());

        public override object GetPropertyOwner(PropertyDescriptor? pd) => settings;
    }
}
