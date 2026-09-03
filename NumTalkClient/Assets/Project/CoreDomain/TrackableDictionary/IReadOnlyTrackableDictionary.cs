using System;
using System.Collections.Generic;

namespace Project.CoreDomain.TrackableDictionary
{
    public interface IReadOnlyTrackableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        event Action<TKey, TValue> Adding;
        event Action<TKey, TValue> Added;
        event Action<TKey, TValue> Removing;
        event Action<TKey, TValue> Removed;
    }
}