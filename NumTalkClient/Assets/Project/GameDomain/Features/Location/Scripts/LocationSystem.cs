using Arch.Core;
using Arch.Unity.Toolkit;
using Project.CoreDomain.Scripts.Logger;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Project.GameDomain.Features.Universe.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Location.Scripts
{
    public sealed class LocationSystem : UnitySystemBase
    {
        private readonly ILocationService _locationService;
        private readonly LocationTilesetDatabase _locationTilesetDatabase;

        private readonly QueryDescription _playerBodies =
            new QueryDescription().WithAll<PlayerTagComponent, PositionComponent>();

        private readonly QueryDescription _locationTiles =
            new QueryDescription().WithAll<LocationComponent, PositionComponent>();

        private readonly ForEach<PositionComponent> _trackPlayerPosition;
        private readonly ForEach<LocationComponent, PositionComponent> _recycleTile;

        private float _playerX;

        public LocationSystem(
            World world,
            ILocationService locationService,
            LocationTilesetDatabase locationTilesetDatabase) : base(world)
        {
            _locationService = locationService;
            _locationTilesetDatabase = locationTilesetDatabase;
            _trackPlayerPosition = TrackPlayerPosition;
            _recycleTile = RecycleTile;
        }

        public override void Initialize()
        {
            CreateLocationTiles();
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _playerBodies, _trackPlayerPosition);
            World.Query(in _locationTiles, _recycleTile);
        }

        private void CreateLocationTiles()
        {
            LocationType type = _locationService.Current;
            if (!_locationTilesetDatabase.TryGetEntry(type, out var locationTileset))
            {
                ProjectLogger.LogError(LogNoFoundLocationType(type));
                return;
            }
            
            float width = UniverseConsts.CalculateUnitsBasePixels(locationTileset.PixelWidth);
            
            for (int index = 0; index < LocationConsts.Count; index++)
            {
                int centeredIndex = index - LocationConsts.Count / 2;
                float tileX = centeredIndex * width;
                World.Create(
                    new ViewComponent(),
                    new LocationComponent
                    {
                        Location = type,
                        MovableHeight = UniverseConsts.CalculateUnitsBasePixels(locationTileset.RunnablePixelHeight),
                    },
                    new PositionComponent { Position = new float3(tileX, 0f, 0f) },
                    new PhysicsComponent { Gravity = _locationService.Gravity });
            }
        }

        private void TrackPlayerPosition(ref PositionComponent position)
        {
            _playerX = position.Position.x;
        }

        private void RecycleTile(ref LocationComponent location, ref PositionComponent position)
        {
            if (!_locationTilesetDatabase.TryGetEntry(location.Location, out var locationTileset))
            {
                ProjectLogger.LogError(LogNoFoundLocationType(location.Location));
                return;
            }
            
            float width = UniverseConsts.CalculateUnitsBasePixels(locationTileset.PixelWidth);
            
            bool isFullyBehindPlayer = _playerX - position.Position.x > width;
            if (isFullyBehindPlayer)
            {
                position.Position.x += width * LocationConsts.Count;
            }
        }

        private static string LogNoFoundLocationType(LocationType locationType)
        {
            return $"[{nameof(LocationSystem)}] No location tileset found for type {locationType}";
        }
    }
}