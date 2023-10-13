using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable once CheckNamespace

namespace NJsonSchema
{
    /// <summary>
    /// Performance helpers avoiding struct enumerator building and generally faster accessing.
    /// </summary>
    internal static class ObservableCollectionExtensions
    {
        public static int Count<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < collection.Count; ++i)
            {
                if (predicate(collection[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public static T ElementAt<T>(this ObservableCollection<T> collection, int index)
        {
            return collection[index];
        }

        public static T First<T>(this ObservableCollection<T> collection)
        {
            if (collection.Count > 0)
            {
                return collection[0];
            }

            ThrowNoMatchingElement();
            return default!;
        }

        public static T First<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
        {
            for (var i = 0; i < collection.Count; ++i)
            {
                var arg = collection[i];
                if (predicate(arg))
                {
                    return arg;
                }
            }

            ThrowNoMatchingElement();
            return default!;
        }

        public static T? FirstOrDefault<T>(this ObservableCollection<T> collection, Func<T, bool> predicate) where T : class
        {
            for (var i = 0; i < collection.Count; ++i)
            {
                var arg = collection[i];
                if (predicate(arg))
                {
                    return arg;
                }
            }

            return null;
        }

        public static T? FirstOrDefault<T>(this ObservableCollection<T> collection) where T : class
        {
            if (collection.Count > 0)
            {
                return collection[0];
            }
            return null;
        }

        public static bool Any<T>(this ObservableCollection<T> collection)
        {
            return collection.Count > 0;
        }

        public static bool Any<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
        {
            for (var i = 0; i < collection.Count; ++i)
            {
                if (predicate(collection[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNoMatchingElement()
        {
            throw new InvalidOperationException("Collection contains no matching element");
        }
    }
}