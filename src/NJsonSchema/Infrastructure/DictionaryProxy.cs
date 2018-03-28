#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.Infrastructure
{
    internal class DictionaryProxy<TKey, TInterface, TImplementation> : IDictionary<TKey, TInterface>
        where TImplementation : TInterface
    {
        private readonly IDictionary<TKey, TImplementation> _dictionary;

        public DictionaryProxy(IDictionary<TKey, TImplementation> dictionary)
        {
            _dictionary = dictionary;
        }

        public IEnumerator<KeyValuePair<TKey, TInterface>> GetEnumerator()
        {
            return _dictionary.Select(t => new KeyValuePair<TKey, TInterface>(t.Key, t.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TInterface> item)
        {
            _dictionary.Add(item.Key, (TImplementation)item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TInterface> item)
        {
            return _dictionary.Contains(new KeyValuePair<TKey, TImplementation>(item.Key, (TImplementation)item.Value));
        }

        public void CopyTo(KeyValuePair<TKey, TInterface>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array.Select(t => new KeyValuePair<TKey, TImplementation>(t.Key, (TImplementation)t.Value)).ToArray(), arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TInterface> item)
        {
            return _dictionary.Remove(new KeyValuePair<TKey, TImplementation>(item.Key, (TImplementation)item.Value));
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => _dictionary.IsReadOnly;

        public void Add(TKey key, TInterface value)
        {
            _dictionary.Add(key, (TImplementation)value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TInterface value)
        {
            if (_dictionary.TryGetValue(key, out var x))
            {
                value = x;
                return true;
            }

            value = default(TInterface);
            return false;
        }

        public TInterface this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = (TImplementation)value;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TInterface> Values => _dictionary.Values.OfType<TInterface>().ToList();
    }
}