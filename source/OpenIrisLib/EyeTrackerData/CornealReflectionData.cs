//-----------------------------------------------------------------------
// <copyright file="CornealReflectionData.cs">
//     Copyright (c) 2018 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------                            
namespace OpenIris
{
    using System;
    using System.Drawing;
    using Emgu.CV.Structure;

    /// <summary>
    /// Data structure containing the geometric properties of the measured corneal reflection.
    /// </summary>
    [Serializable]
    public struct CornealReflectionData : IEquatable<CornealReflectionData>
    {
        /// <summary>
        /// The center of the box.
        /// </summary>
        public PointF Center { get; set; }

        /// <summary>
        /// The size of the box.
        /// </summary>
        public SizeF Size { get; set; }

        /// <summary>
        /// The angle between the horizontal axis and the first side (i.e. width) in degrees.
        /// </summary>
        /// <remarks>Positive value means counter-clock wise rotation.</remarks>
        public float Angle { get; set; }

        /// <summary>
        /// Gets a value indicating if the corneal reflection information is empty.
        /// </summary>
        /// <returns>True if the pupil is empty.</returns>
        public bool IsEmpty { get { return this.Size.IsEmpty; } }

        /// <summary>
        /// Create a CornealReflectionData structure with the specific parameters.
        /// </summary>
        /// <param name="center">The center of the box.</param>
        /// <param name="size">The size of the box.</param>
        /// <param name="angle">The angle of the box in degrees. Possitive value means counter-clock wise rotation.</param>
        public CornealReflectionData(PointF center, SizeF size, float angle)
        {
            Center = center;
            Size = size;
            Angle = angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(CornealReflectionData other)
        {
            return Center == other.Center && Size == other.Size && Angle == other.Angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is CornealReflectionData && Equals((CornealReflectionData)obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)(Angle*Size.Width*Size.Height);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(CornealReflectionData left, CornealReflectionData right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(CornealReflectionData left, CornealReflectionData right)
        {
            return !(left == right);
        }
    }
}
