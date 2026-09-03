using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Project.GameDomain.Features.Reaper.Scripts;
using Project.GameDomain.Features.Universe.Scripts;

namespace Project.GameDomain.Features.ReapBehindPlayer.Scripts
{
    public sealed class ReapBehindPlayerSystem : UnitySystemBase
    {
        private const int BehindDistancePixels = 400;

        private readonly QueryDescription _players =
            new QueryDescription().WithAll<PlayerTagComponent, PositionComponent>();
        private readonly QueryDescription _reapable =
            new QueryDescription().WithAll<ReapBehindPlayerComponent, PositionComponent>().WithNone<ReaperComponent>();

        private readonly ForEach<PositionComponent> _readPlayer;
        private readonly ForEachWithEntity<PositionComponent> _collectBehind;
        private readonly List<Entity> _behind = new();
        private readonly float _behindDistance = UniverseConsts.CalculateUnitsBasePixels(BehindDistancePixels);

        private float _playerX;
        private bool _hasPlayer;

        public ReapBehindPlayerSystem(World world) : base(world)
        {
            _readPlayer = ReadPlayer;
            _collectBehind = CollectBehind;
        }

        public override void Update(in SystemState state)
        {
            _hasPlayer = false;
            World.Query(in _players, _readPlayer);

            if (!_hasPlayer)
            {
                return;
            }

            _behind.Clear();
            World.Query(in _reapable, _collectBehind);

            for (int index = 0; index < _behind.Count; index++)
            {
                World.Add(_behind[index], new ReaperComponent());
            }
        }

        private void ReadPlayer(ref PositionComponent position)
        {
            _playerX = position.Position.x;
            _hasPlayer = true;
        }

        private void CollectBehind(Entity entity, ref PositionComponent position)
        {
            if (position.Position.x < _playerX - _behindDistance)
            {
                _behind.Add(entity);
            }
        }
    }
}
