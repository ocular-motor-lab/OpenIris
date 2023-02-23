//-----------------------------------------------------------------------
// <copyright file="EyePhysicalModel.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
    using System;
    using System.ComponentModel;
    using System.Collections;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the geometric properties of the measured iris.
    /// </summary>
    [Serializable, TypeConverter(typeof(StructConverter<EyePhysicalModel>))]
    public struct EyePhysicalModel: IEquatable<EyePhysicalModel>
    {
        /// <summary>
        /// Gets or sets the center of the eye globe.
        /// </summary>
        [TypeConverter(typeof(StructConverter<PointF>))]
        public PointF Center { get;  set; }
        
        /// <summary>
        /// Gets or sets the radius of the eye globe.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Gets or sets the ratio between the horizontal and the vertical radii 
        /// of the globe.
        /// </summary>
        public float HorizontalVerticalRatio { get; set; }

        /// <summary>
        /// Initializes a new instance of the CircleFSerializable structure.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="horizontalVerticalRatio">Ratio between the horizontal and the vertical radii (H/V).</param>
        public EyePhysicalModel(PointF center, float radius, float horizontalVerticalRatio = 1)
            : this()
        {
            this.Center = center;
            this.Radius = radius;
            this.HorizontalVerticalRatio = horizontalVerticalRatio;
        }

        /// <summary>
        /// Checks if the eye model is empty (radius is zero).
        /// </summary>
        public bool IsEmpty { get { return this.Radius == 0; } }

        /// <summary>
        /// Gets the circle corresponding with the eye globe.
        /// </summary>
        public CircleF CircleF { get { return new CircleF(this.Center, this.Radius); } }

        /// <summary>
        /// Compares two eye physical models.
        /// </summary>
        /// <param name="obj">Object to be compared with.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is EyePhysicalModel))
            {
                return false;
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Compares two eye physical models.
        /// </summary>
        /// <param name="obj">Object to be compared with.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(EyePhysicalModel obj)
        {
            return obj.Center == this.Center && obj.Radius == this.Radius;
        }

        /// <summary>
        /// Compares two eye physical models.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(EyePhysicalModel x, EyePhysicalModel y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Compares two eye physical models.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(EyePhysicalModel x, EyePhysicalModel y)
        {
            return !(x == y);
        }
        
        /// <summary>
        /// Gets a string summarizing the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Center: (" + this.Center.X + ", " + this.Center.Y + ") - Radius : " + this.Radius;
        }

        /// <summary>
        /// Gets the hashcode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Center.GetHashCode();
        }
    }

    /// <summary>
    /// http://stackoverflow.com/questions/15746897/modifying-structure-property-in-a-propertygrid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StructConverter<T> : ExpandableObjectConverter where T : struct
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="propertyValues"></param>
        /// <returns></returns>
        public override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
        {
            if (propertyValues is null)
                throw new ArgumentNullException("propertyValues");

            T ret = default;
            object boxed = ret;
            foreach (DictionaryEntry entry in propertyValues)
            {
                PropertyInfo pi = ret.GetType().GetProperty(entry.Key.ToString());
                if ((pi != null) && (pi.CanWrite))
                {
                    pi.SetValue(boxed, Convert.ChangeType(entry.Value, pi.PropertyType), null);
                }
            }
            return (T)boxed;
        }
    }
}
