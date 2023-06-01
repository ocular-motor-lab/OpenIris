//-----------------------------------------------------------------------
// <copyright file="IrisData.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
    using System;
    using System.Drawing;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the geometric properties of the measured iris.
    /// </summary>
    [Serializable]
    public struct IrisData : IEquatable<IrisData>
    {
        /// <summary>
        /// Gets or sets the center of the circle.
        /// </summary>
        public PointF Center { get; set; }

        /// <summary>
        /// Gets or sets the radius of the circle.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Initializes a new instance of the CircleFSerializable class.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        public IrisData(PointF center, float radius)
            : this()
        {
            this.Center = center;
            this.Radius = radius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)(Radius*Center.X*Center.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IrisData other)
        {
            return Center == other.Center && Radius == other.Radius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is IrisData && Equals((IrisData)obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(IrisData left, IrisData right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(IrisData left, IrisData right)
        {
            return !(left == right);
        }
    }
}
