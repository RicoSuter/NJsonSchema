using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NJsonSchema;

internal static class CollectionExtensions
{
    public static int IndexOf<T>(this ICollection<T> collection, T item)
    {
        if (collection is List<T> l)
        {
            return l.IndexOf(item);
        }

        if (collection is Collection<T> c)
        {
            return c.IndexOf(item);
        }

        int index = 0;
        foreach (var element in collection)
        {
            if (EqualityComparer<T>.Default.Equals(element, item))
            {
                return index;
            }
            index++;
        }
        return -1; // Item not found
    }
}