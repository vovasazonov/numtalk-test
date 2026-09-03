using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Position.Scripts;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class FallSystem : UnitySystemBase
    {
        private const float GroundZ = 0f;
        private const float GroundedEpsilon = 0.001f;

        private readonly QueryDescription _physicsSource = new QueryDescription().WithAll<PhysicsComponent>();
        private readonly QueryDescription _fallingBodies = new QueryDescription()
            .WithAll<FallingComponent, PositionComponent, MovementComponent, ColliderComponent, RigidbodyComponent>();

        private readonly ForEach<PhysicsComponent> _readGravity;
        private readonly ForEachWithEntity<PositionComponent, MovementComponent, ColliderComponent, RigidbodyComponent> _fall;
        private readonly List<Entity> _landed = new();

        private float _gravity;
        private float _deltaTime;

        public FallSystem(World world) : base(world)
        {
            _readGravity = ReadGravity;
            _fall = Fall;
        }

        public override void Update(in SystemState state)
        {
            _gravity = 0f;
            _deltaTime = state.DeltaTime;
            _landed.Clear();
            World.Query(in _physicsSource, _readGravity);
            World.Query(in _fallingBodies, _fall);

            for (int index = 0; index < _landed.Count; index++)
            {
                World.Remove<FallingComponent>(_landed[index]);
            }
        }

        private void ReadGravity(ref PhysicsComponent physics)
        {
            _gravity = physics.Gravity;
        }

        private void Fall(
            Entity entity,
            ref PositionComponent position,
            ref MovementComponent movement,
            ref ColliderComponent collider,
            ref RigidbodyComponent rigidbody)
        {
            float restZ = GroundZ + collider.Size.y * 0.5f;

            if (position.Position.z <= restZ + GroundedEpsilon)
            {
                movement.Velocity.z = 0f;
                rigidbody.IsGravityEnabled = true;
                _landed.Add(entity);
                return;
            }

            movement.Velocity.z -= _gravity * _deltaTime;

            if (_deltaTime > 0f && position.Position.z + movement.Velocity.z * _deltaTime <= restZ)
            {
                movement.Velocity.z = (restZ - position.Position.z) / _deltaTime;
            }
        }
    }
}
