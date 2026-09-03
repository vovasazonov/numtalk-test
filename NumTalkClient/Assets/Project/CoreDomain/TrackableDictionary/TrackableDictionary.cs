using System;
using System.Collections.Generic;
using System.Linq;

namespace Project.CoreDomain.TrackableDictionary
{
    public class TrackableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ITrackableDictionary<TKey, TValue>, IReadOnlyTrackableDictionary<TKey, TValue>
    {
        public event Action<TKey, TValue> Adding;
        public event Action<TKey, TValue> Added;
        public event Action<TKey, TValue> Removing;
        public event Action<TKey, TValue> Removed;

        public TrackableDictionary()
        {
        }

        public TrackableDictionary(int capacity) : base(capacity)
        {
        }

        public new void Clear()
        {
            while (Keys.Count != 0)
            {
                TKey key = Keys.First();
                Remove(key);
            }
        }

        public new void Add(TKey key, TValue value)
        {
            CallAdding(key, value);
            base.Add(key, value);
            CallAdded(key, value);
        }

        public new bool Remove(TKey key)
        {
            bool containsValue = TryGetValue(key, out var value);

            if (containsValue)
            {
                CallRemoving(key, value);
                base.Remove(key);
                CallRemoved(key, value);
            }

            return containsValue;
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                if (ContainsKey(key))
                {
                    Remove(key);
                }

                Add(key, value);
            }
        }

        protected virtual void CallAdding(TKey key, TValue value)
        {
            Adding?.Invoke(key, value);
        }

        protected virtual void CallRemoving(TKey key, TValue value)
        {
            Removing?.Invoke(key, value);
        }

        protected virtual void CallAdded(TKey key, TValue value)
        {
            Added?.Invoke(key, value);
        }

        protected virtual void CallRemoved(TKey key, TValue value)
        {
            Removed?.Invoke(key, value);
        }
    }
}