// --------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// --------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Prototype
{
    /// <summary>
    /// An effiient resizable array abstraction.
    /// </summary>
    /// <remarks>
    /// The SizableArray class represents a resizable "counted" array. These arrays are used
    /// as a building block to create efficient collections. Sizable arrays
    /// have three primary attributes:
    /// 
    ///      Count: The total number of used slots in the array, 
    ///             starting from slot 0.
    ///
    ///      Capacity: The total amount of data that can be stored
    ///                within the current dynamic size of the array.
    ///
    ///      Length: The amount of physical heap space dedicated to the
    ///              array, specified in count of items.
    ///
    /// The following invariant always holds:
    ///
    ///      0 <= Count <= Capacity <= Length
    ///
    /// The three attributes enable sizable arrays to be supported 
    /// efficiently by the runtime. In particular:
    ///
    /// * During a GC operation, the collector doesn't visit unused
    ///   portions of a sizable array. The GC restricts itself to 
    ///   visiting only Count entries in the array, thus reducing the
    ///   amount of work done by the GC.
    ///
    /// * Whenever the GC relocates a sizable array, it only
    ///   makes room for and copies Count entries of the array.
    ///   Thus if a sizable array is shrunk, only the requisite amount
    ///   of data is copied.
    ///
    /// * By leveraging the invariant, redundant bounds checking is
    ///   eliminated. There's no need to check the count and capacity,
    ///   only bounds checking against the count is needed.
    ///
    /// * When reducing the count or capacity of a sizable array,
    ///   there is no need to clear the freed elements to default(T)
    ///   in order to avoid holding onto stale GC references since the 
    ///   GC knows not to consider elements beyond Count.
    ///
    /// * When expanding a sizable array, the runtime avoids wasting time
    ///   redundantly setting the new array to 0 by directly copying the old
    ///   array's state into the new chunk of heap space being allocated.
    ///
    /// Of course, right now the runtime doesn't support this type explicitly and
    /// thus these potential optimizations are not realized, except for the elimination
    /// of redundant bounds checking. Perhaps one day the runtime can address the other
    /// opportunities for optimization that this type provides.
    ///
    /// There are various issues around what different GCs can and can't do in order to optimize this
    /// type, so the pretty world described above may never be realized fully given the constraints that
    /// the collectors operate under. We'll see how far we can get once we try cramming support for this
    /// in there.
    ///
    /// The current implementation is pretty straightforward. Capacity and Length are one and the same,
    /// and there's no integration with the GC, so no magic trimming, etc. We do get the benefits of 
    /// eliding bounds checks.
    /// </remarks>
    public readonly struct SizableArray<T>
    {
        private readonly T[] _data;
        private readonly int _count;

        private SizableArray(T[] data, int count)
        {
            _data = data;
            _count = count;
        }

        public SizableArray(int capacity, int count)
        {
            if (capacity > 0)
            {
                _data = new T[capacity];
            }
            else
            {
                _data = Array.Empty<T>();
            }

            _count = count;
        }

        public static bool IncreaseCount(ref SizableArray<T> array)
        {
            if (array.Count >= array.Capacity)
            {
                // can't fit in current capacity
                return false;
            }

            array = new SizableArray<T>(array._data, array.Count + 1);
            return true;
        }

        public static bool IncreaseCount(ref SizableArray<T> array, int amount)
        {
            if (amount > array.Capacity - array.Count)
            {
                // can't fit in current capacity
                return false;
            }

            array = new SizableArray<T>(array._data, array.Count + amount);
            return true;
        }

        public static void DecreaseCount(ref SizableArray<T> array)
        {
            int newCount = array.Count - 1;
            array._data[newCount] = default;   // eliminate any stale GC references
            array = new SizableArray<T>(array._data, newCount);
        }

        public static void DecreaseCount(ref SizableArray<T> array, int amount)
        {
            int newCount = array.Count - amount;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(array._data, newCount, amount);
            }

            array = new SizableArray<T>(array._data, newCount);
        }

        public static void ResetCount(ref SizableArray<T> array)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(array._data, 0, array.Count);
            }

            array = new SizableArray<T>(array._data, 0);
        }

        public static void Resize(ref SizableArray<T> array, int capacity, int count)
        {
            var copyCount = array.Count;
            if (copyCount > count)
            {
                copyCount = count;
            }

            T[] newData = Array.Empty<T>();
            if (capacity > 0)
            {
                newData = new T[capacity];
            }

            Array.Copy(array._data, newData, copyCount);

            array = new SizableArray<T>(newData, count);
        }

        public T this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public int Count => _count;
        public int Capacity => _data.Length;
        public void Clear() => Array.Clear(_data, 0, _count);
    }
}
