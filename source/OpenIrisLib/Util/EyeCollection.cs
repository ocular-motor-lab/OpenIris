//-----------------------------------------------------------------------
// <copyright file="EyeCollection.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Collection of objects related to the left eye, the right eye or both. Once it gets initialized to have one eye or both it cannot be changed.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the collection.</typeparam>
    [Serializable]
    public class EyeCollection<T> : IEnumerable<T>
    {
        /// <summary>
        /// List of elements.
        /// </summary>
        private T[] items;

        /// <summary>
        /// Initializes a new instance of the EyeCollection class. Necessary for serialization.
        /// </summary>
        public EyeCollection() { items = Array.Empty<T>(); }

        /// <summary>
        /// Initializes a new instance of the EyeCollection class
        /// </summary>
        /// <param name="objects">The objects</param>
        public EyeCollection(params T[] objects)
        {
            if (objects is null) throw new ArgumentNullException(nameof(objects));
            if (objects.Count() < 1 || objects.Count() > 2) throw new InvalidOperationException("Only one or two items;");

            items = objects;
        }

        /// <summary>
        /// Initializes a new instance of the EyeCollection class
        /// </summary>
        /// <param name="objects">The objects</param>
        public EyeCollection(IEnumerable<T> objects) : this(objects.ToArray()) { }

        /// <summary>
        /// Enables to index the collection with the Eye enumaration.
        /// </summary>
        /// <param name="whichEye">Left eye, right eye or both.</param>
        /// <returns>The corresponding object.</returns>
        public T this[Eye whichEye]
        {
            get => items[GetAndCheckIndex(whichEye)];
            set => items[GetAndCheckIndex(whichEye)] = value;
        }

        /// <summary>
        /// Enables to index the collection.
        /// </summary>
        /// <param name="idx">Index number of the object.</param>
        /// <returns>The corresponding object.</returns>
        public T this[int idx] { get => items[idx]; }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count { get => items.Length; }

        /// <summary>
        /// Has to be implemented for the interface also.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in items) yield return item;
        }

        /// <summary>
        /// Necessary for serialization.
        /// </summary>
        /// <param name="obj"></param>
        public void Add(object obj)
        {
            items = items.Append((T)obj).ToArray();
        }

        /// <summary>
        /// Gets a string displaying the list.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var s = new System.Text.StringBuilder();
            foreach (var item in items)
            {
                if (!(s.Length > 0)) s.Append(";");
                s.Append((item != null) ? item.ToString() : "null");
            }

            return s.ToString();
        }

        /// <summary>
        /// Converts an EyeCollection into an array.
        /// </summary>
        /// <param name="collection"></param>
        public static implicit operator T[](EyeCollection<T> collection)
        {
            return collection?.items ?? Array.Empty<T>();
        }

        /// <summary>
        /// Converts an array into an EyeCollection.
        /// </summary>
        /// <param name="collection"></param>
        public static implicit operator EyeCollection<T>(T[] collection)
        {
            return new EyeCollection<T>(collection);
        }

        /// <summary>
        /// Executes an action for each element of the collection
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(Action<T> action) { foreach (var item in items) action(item); }

        /// <summary>
        /// Gets the index correspnding with the eye.
        /// </summary>
        /// <param name="whichEye"></param>
        /// <returns></returns>
        private int GetAndCheckIndex(Eye whichEye)
        {
            return (items.Length, whichEye) switch
            {
                (1, Eye.Both) => 0,
                (2, Eye.Left) => 0,
                (2, Eye.Right) => 1,
                (_, _) => throw new Exception("Wrong size or eye."),
            };
        }
    }
}
