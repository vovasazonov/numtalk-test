using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using VContainer;

namespace Project.GameDomain.Features.Location.Scripts
{
    public class LocationComponentListener : ComponentListener<LocationComponent>
    {
        private LocationTilesetDatabase _database;
        private LocationType _location = LocationType.None;
        private GameObject _tileset;

        [Inject]
        private void Inject(LocationTilesetDatabase database)
        {
            _database = database;
        }

        public override void UpdateView(in LocationComponent component)
        {
            if (component.Location == _location)
            {
                return;
            }

            _location = component.Location;
            SpawnTileset(_location);
        }

        private void SpawnTileset(LocationType location)
        {
            ClearTileset();

            if (_database.TryGetEntry(location, out LocationTilesetDatabase.Entry entry))
            {
                _tileset = Instantiate(entry.Tileset, transform);
            }
        }

        private void ClearTileset()
        {
            if (_tileset != null)
            {
                Destroy(_tileset);
                _tileset = null;
            }
        }

        private void OnDisable()
        {
            ClearTileset();
            _location = LocationType.None;
        }
    }
}