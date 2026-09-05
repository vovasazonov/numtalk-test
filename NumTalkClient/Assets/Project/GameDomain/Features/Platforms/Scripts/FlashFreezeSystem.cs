using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    public sealed class FlashFreezeSystem : UnitySystemBase
    {
        private readonly QueryDescription _players = new QueryDescription().WithAll<PlayerTagComponent, EntityTransformComponent>();
        private readonly QueryDescription _surfaces = new QueryDescription().WithAll<FlashFreezeComponent>();
        private readonly ForEach _readPlayer, _step;
        private float _playerZ, _dt;

        public FlashFreezeSystem(World world) : base(world)
        {
            _readPlayer = e => _playerZ = World.Get<EntityTransformComponent>(e).Position.z;
            _step = e => World.Get<FlashFreezeComponent>(e).Step(_playerZ, _dt);
        }

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            _playerZ = float.NegativeInfinity;
            World.Query(in _players, _readPlayer);
            World.Query(in _surfaces, _step);
        }
    }
}
