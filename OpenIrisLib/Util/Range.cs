//-----------------------------------------------------------------------
// <copyright file="Range.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Structure to represent a range of values from a minimum to a maximum.
    /// </summary>
    [TypeConverter(typeof(StructConverter<Range>))]
    public struct Range
    {
        /// <summary>
        /// Initializes a new instance of the Range structure.
        /// </summary>
        /// <param name="begin">Begining of the range.</param>
        /// <param name="end">End of the range.</param>
        public Range(long begin, long end)
            : this()
        {
            Begin = begin;
            End = end;
        }

        /// <summary>
        /// Gets or sets the begining of the range.
        /// </summary>
        public long Begin { get; }

        /// <summary>
        /// Gets or sets the end of the range.
        /// </summary>
        public long End { get; }

        /// <summary>
        /// Gets a value indicating wether the range is empty.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty { get => Begin == End && End == 0; }

        /// <summary>
        /// Check if the range contains a certain value.
        /// </summary>
        /// <param name="value">Number to check.</param>
        /// <returns>Value indicating if the value is within the range.</returns>
        public bool Contains(long value)
        {
            if (IsEmpty) return false;

            return value >= Begin && value <= End;
        }

        /// <summary>
        /// Check if the range contains a certain value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool DoesNotContain(long value)
        {
            return !this.Contains(value);
        }

        /// <summary>
        /// Gets the string for the range.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + Begin + "-" + End + "]";
        }
    }


    /// <summary>
    /// Structure to represent a range of values from a minimum to a maximum.
    /// </summary>
    [TypeConverter(typeof(StructConverter<RangeDouble>))]
    public struct RangeDouble
    {
        /// <summary>
        /// Initializes a new instance of the Range structure.
        /// </summary>
        /// <param name="begin">Begining of the range.</param>
        /// <param name="end">End of the range.</param>
        public RangeDouble(double begin, double end)
            : this()
        {
            Begin = begin;
            End = end;
        }

        /// <summary>
        /// Gets or sets the begining of the range.
        /// </summary>
        public double Begin { get; }

        /// <summary>
        /// Gets or sets the end of the range.
        /// </summary>
        public double End { get; }

        /// <summary>
        /// Gets a value indicating wether the range is empty.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty { get { return Begin == End && End == 0; } }

        /// <summary>
        /// Check if the range contains a certain value.
        /// </summary>
        /// <param name="value">Number to check.</param>
        /// <returns>Value indicating if the value is within the range.</returns>
        public bool Contains(double value)
        {
            if (IsEmpty) return false;

            return value >= Begin && value <= End;
        }

        /// <summary>
        /// Check if the range contains a certain value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool DoesNotContain(double value)
        {
            return !this.Contains(value);
        }

        /// <summary>
        /// Gets the string for the range.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + Begin + "-" + End + "]";
        }
    }
}
