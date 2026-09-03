using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Position.Scripts;

namespace Project.GameDomain.Features.Movement.Scripts
{
    public sealed class MovementSystem : UnitySystemBase
    {
        private readonly QueryDescription _movingEntities = new QueryDescription().WithAll<PositionComponent, MovementComponent>();
        private readonly ForEach<PositionComponent, MovementComponent> _move;

        private float _deltaTime;

        public MovementSystem(World world) : base(world)
        {
            _move = Move;
        }

        public override void Update(in SystemState state)
        {
            _deltaTime = state.DeltaTime;
            World.Query(in _movingEntities, _move);
        }

        private void Move(ref PositionComponent position, ref MovementComponent movement)
        {
            position.Position += movement.Velocity * _deltaTime;
        }
    }
}
