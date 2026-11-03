//-----------------------------------------------------------------------
// <copyright file="EyeCollectionConverter.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Shows the items of an EyeCollection in a property grid, named by their eye (Both, or Left and
    /// Right), so each one can be edited.
    /// </summary>
    /// <typeparam name="T">Type of the items.</typeparam>
    public class EyeCollectionConverter<T> : ExpandableObjectConverter
    {
        /// <inheritdoc/>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value is not EyeCollection<T> collection) return PropertyDescriptorCollection.Empty;

            var eyes = collection.Count == 1 ? new[] { Eye.Both } : new[] { Eye.Left, Eye.Right };
            return new PropertyDescriptorCollection(eyes.Select(eye => (PropertyDescriptor)new ItemDescriptor(eye)).ToArray());
        }

        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is EyeCollection<T> collection)
            {
                return collection.Count == 1 ? "One camera" : "Two cameras";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        private sealed class ItemDescriptor : PropertyDescriptor
        {
            private readonly Eye eye;

            public ItemDescriptor(Eye eye)
                : base(eye.ToString(), null)
            {
                this.eye = eye;
            }

            public override Type ComponentType => typeof(EyeCollection<T>);

            public override bool IsReadOnly => false;

            public override Type PropertyType => typeof(T);

            public override bool CanResetValue(object component) => false;

            public override object? GetValue(object component) => ((EyeCollection<T>)component)[eye];

            public override void ResetValue(object component) { }

            public override void SetValue(object component, object value) => ((EyeCollection<T>)component)[eye] = (T)value;

            public override bool ShouldSerializeValue(object component) => false;
        }
    }
}
