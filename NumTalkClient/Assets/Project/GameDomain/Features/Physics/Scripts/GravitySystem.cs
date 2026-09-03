using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Position.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class GravitySystem : UnitySystemBase
    {
        private const float GroundZ = 0f;

        private readonly QueryDescription _physicsSource =
            new QueryDescription().WithAll<PhysicsComponent>();
        private readonly QueryDescription _fallingBodies =
            new QueryDescription().WithAll<PositionComponent, RigidbodyComponent, ColliderComponent>();

        private readonly ForEach<PhysicsComponent> _readGravity;
        private readonly ForEach<PositionComponent, RigidbodyComponent, ColliderComponent> _applyGravity;

        private float _gravity;
        private float _deltaTime;

        public GravitySystem(World world) : base(world)
        {
            _readGravity = ReadGravity;
            _applyGravity = ApplyGravity;
        }

        public override void Update(in SystemState state)
        {
            _deltaTime = state.DeltaTime;
            World.Query(in _physicsSource, _readGravity);
            World.Query(in _fallingBodies, _applyGravity);
        }

        private void ReadGravity(ref PhysicsComponent physics)
        {
            _gravity = physics.Gravity;
        }

        private void ApplyGravity(ref PositionComponent position, ref RigidbodyComponent rigidbody, ref ColliderComponent collider)
        {
            if (!rigidbody.IsGravityEnabled)
            {
                return;
            }

            float halfHeight = collider.Size.y * 0.5f;
            float restZ = GroundZ + halfHeight;
            position.Position.z = math.max(position.Position.z - _gravity * _deltaTime, restZ);
        }
    }
}
