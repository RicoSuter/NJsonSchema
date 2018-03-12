#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.Infrastructure
{
    public class CollectionProxy<TInterface, TImplementation> : ICollection<TInterface>
        where TImplementation : TInterface
    {
        private readonly ICollection<TImplementation> _collection;

        public CollectionProxy(ICollection<TImplementation> collection)
        {
            _collection = collection;
        }

        public IEnumerator<TInterface> GetEnumerator()
        {
            return _collection.OfType<TInterface>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TInterface item)
        {
            _collection.Add((TImplementation)item);
        }

        public void Clear()
        {
            _collection.Clear();
        }

        public bool Contains(TInterface item)
        {
            return _collection.Contains((TImplementation)item);
        }

        public void CopyTo(TInterface[] array, int arrayIndex)
        {
            _collection.CopyTo(array.OfType<TImplementation>().ToArray(), arrayIndex);
        }

        public bool Remove(TInterface item)
        {
            return _collection.Remove((TImplementation)item);
        }

        public int Count => _collection.Count;

        public bool IsReadOnly => _collection.IsReadOnly;
    }
}