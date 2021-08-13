﻿//-----------------------------------------------------------------------
// <copyright file="IrisData.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
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
        /// Conversion from IrisData to CircleF.
        /// </summary>
        /// <param name="irisData">Iris Data.</param>
        /// <returns>New CircleF.</returns>
        public static implicit operator CircleF(IrisData irisData)
        {
            return irisData.ToCircleF();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CircleF ToCircleF()
        {
            return new CircleF(Center, Radius);
        }

        /// <summary>
        /// Conversion from CircleF to IrisData.
        /// </summary>
        /// <param name="circle">The circle data.</param>
        /// <returns>The new Iris data.</returns>
        public static implicit operator IrisData(CircleF circle)
        {
            return new IrisData(circle.Center, circle.Radius);
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
