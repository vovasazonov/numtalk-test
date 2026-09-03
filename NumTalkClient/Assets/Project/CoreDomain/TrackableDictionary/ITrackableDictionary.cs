using System;
using System.Collections.Generic;

namespace Project.CoreDomain.TrackableDictionary
{
    public interface ITrackableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        event Action<TKey, TValue> Adding;
        event Action<TKey, TValue> Removing;
    }
}