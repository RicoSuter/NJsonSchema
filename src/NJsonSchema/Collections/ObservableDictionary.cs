//-----------------------------------------------------------------------
// <copyright file="ObservableDictionary.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://mytoolkit.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NJsonSchema.Collections
{
    /// <summary>An implementation of an observable dictionary. </summary>
    /// <typeparam name="TKey">The type of the key. </typeparam>
    /// <typeparam name="TValue">The type of the value. </typeparam>
    internal sealed class ObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>, INotifyCollectionChanged,
        INotifyPropertyChanged, IDictionary, 
        IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private Dictionary<TKey, TValue?> _dictionary;

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        public ObservableDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue?>();
        }

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="dictionary">The dictionary to initialize this dictionary. </param>
        public ObservableDictionary(IDictionary<TKey, TValue?> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue?>(dictionary);
        }

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="comparer">The comparer. </param>
        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue?>(comparer);
        }

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="capacity">The capacity. </param>
        public ObservableDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue?>(capacity);
        }

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="dictionary">The dictionary to initialize this dictionary. </param>
        /// <param name="comparer">The comparer. </param>
        public ObservableDictionary(IDictionary<TKey, TValue?> dictionary, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue?>(dictionary, comparer);
        }

        /// <summary>Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="capacity">The capacity. </param>
        /// <param name="comparer">The comparer. </param>
        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue?>(capacity, comparer);
        }

        /// <summary>Adds multiple key-value pairs the the dictionary. </summary>
        /// <param name="items">The key-value pairs. </param>
        public void AddRange(IDictionary<TKey, TValue?> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count > 0)
            {
                if (_dictionary.Count > 0)
                {
                    if (items.Keys.Any(k => _dictionary.ContainsKey(k)))
                    {
                        throw new ArgumentException("An item with the same key has already been added.");
                    }

                    foreach (var item in items)
                    {
                        _dictionary.Add(item.Key, item.Value);
                    }
                }
                else
                {
                    _dictionary = new Dictionary<TKey, TValue?>(items);
                }

                OnCollectionChanged(NotifyCollectionChangedAction.Add, items.ToArray());
            }
        }

        /// <summary>Inserts a key-value pair into the dictionary. </summary>
        /// <param name="key">The key. </param>
        /// <param name="value">The value. </param>
        /// <param name="add">If true and key already exists then an exception is thrown. </param>
        private void Insert(TKey key, TValue? value, bool add)
        {
            TValue? item;
            if (_dictionary.TryGetValue(key, out item))
            {
                if (add)
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }

                if (Equals(item, value))
                {
                    return;
                }

                _dictionary[key] = value;
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue?>(key, value),
                    new KeyValuePair<TKey, TValue?>(key, item));
            }
            else
            {
                _dictionary[key] = value;
                OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue?>(key, value));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            var copy = PropertyChanged;
            if (copy != null)
            {
                copy(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            var copy = CollectionChanged;
            if (copy != null)
            {
                copy(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue?> changedItem)
        {
            OnPropertyChanged();
            var copy = CollectionChanged;
            if (copy != null)
            {
                copy(this, new NotifyCollectionChangedEventArgs(action, changedItem, 0));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue?> newItem,
            KeyValuePair<TKey, TValue?> oldItem)
        {
            OnPropertyChanged();
            var copy = CollectionChanged;
            if (copy != null)
            {
                copy(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, 0));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            OnPropertyChanged();
            var copy = CollectionChanged;
            if (copy != null)
            {
                copy(this, new NotifyCollectionChangedEventArgs(action, newItems, 0));
            }
        }

        private void OnPropertyChanged()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }

        #region IDictionary<TKey,TValue> interface

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        ICollection IDictionary.Values => ((IDictionary) _dictionary).Values;

        ICollection IDictionary.Keys => ((IDictionary) _dictionary).Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            TValue? value;
            _dictionary.TryGetValue(key, out value);

            var removed = _dictionary.Remove(key);
            if (removed)
            {
                OnCollectionChanged();
            }

            //OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value));
            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value!);
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        public ICollection<TValue> Values => _dictionary.Values!;

        public TValue this[TKey key]
        {
            get => _dictionary[key]!;
            set => Insert(key, value, false);
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> interface

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        void IDictionary.Add(object key, object? value)
        {
            Insert((TKey) key, (TValue?)value, true);
        }

        public void Clear()
        {
            if (_dictionary.Count > 0)
            {
                _dictionary.Clear();
                OnCollectionChanged();
            }
        }

        public void Initialize(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            var pairs = keyValuePairs.ToList();
            foreach (var pair in pairs)
            {
                _dictionary[pair.Key] = pair.Value;
            }

            foreach (var key in _dictionary.Keys.Where(k => !pairs.Any(p => Equals(p.Key, k))).ToArray())
            {
                _dictionary.Remove(key);
            }

            OnCollectionChanged();
        }

        public void Initialize(IEnumerable keyValuePairs)
        {
            Initialize(keyValuePairs.Cast<KeyValuePair<TKey, TValue>>());
        }

        public bool Contains(object key)
        {
            return ContainsKey((TKey) key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary) _dictionary).GetEnumerator();
        }

        public void Remove(object key)
        {
            Remove((TKey) key);
        }

        public bool IsFixedSize => false;

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary!.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary) _dictionary).CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary) _dictionary).CopyTo(array, index);
        }

        public int Count => _dictionary.Count;

        public bool IsSynchronized { get; private set; }
        public object SyncRoot { get; } = new object();

        public bool IsReadOnly => ((IDictionary) _dictionary).IsReadOnly;

        object? IDictionary.this[object key]
        {
            get => this[(TKey) key];
            set => this[(TKey) key] = (TValue?)value!;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> interface

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        public Dictionary<TKey, TValue?>.Enumerator GetEnumerator() => _dictionary.GetEnumerator();

        #endregion

        #region IEnumerable interface

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _dictionary).GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged interface

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        #endregion

        #region INotifyPropertyChanged interface

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion
    }
}