using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.GameDomain.Features.Location.Scripts
{
    [CreateAssetMenu(
        fileName = "LocationTilesetDatabase",
        menuName = "IdleStory/Features/Location/Location Tileset Database")]
    public class LocationTilesetDatabase : ScriptableObject
    {
        [SerializeField] private List<Entry> _entries;

        public bool TryGetEntry(LocationType location, out Entry outEntry)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Location == location)
                {
                    outEntry = entry;
                    return true;
                }
            }

            outEntry = null;
            return false;
        }

        [Serializable]
        public class Entry
        {
            [field: SerializeField] public LocationType Location { get; private set; }
            [field: SerializeField] public GameObject Tileset { get; private set; }
            [field: SerializeField] public int PixelWidth { get; private set; }
            [field: SerializeField] public int PixelHeight { get; private set; }
            [field: SerializeField] public int RunnablePixelHeight { get; private set; }
        }
    }
}