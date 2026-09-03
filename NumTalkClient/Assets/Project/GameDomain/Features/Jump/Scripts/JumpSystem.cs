using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Position.Scripts;

namespace Project.GameDomain.Features.Jump.Scripts
{
    public sealed class JumpSystem : UnitySystemBase
    {
        private const float GroundZ = 0f;
        private const float GroundedEpsilon = 0.001f;

        private readonly QueryDescription _jumpRequests = new QueryDescription()
            .WithAll<JumpRequestComponent, PositionComponent, MovementComponent, ColliderComponent, RigidbodyComponent>();

        private readonly ForEachWithEntity<PositionComponent, MovementComponent, ColliderComponent, RigidbodyComponent> _jump;
        private readonly List<Entity> _requesters = new();
        private readonly List<Entity> _jumpers = new();

        public JumpSystem(World world) : base(world)
        {
            _jump = Jump;
        }

        public override void Update(in SystemState state)
        {
            _requesters.Clear();
            _jumpers.Clear();
            World.Query(in _jumpRequests, _jump);

            for (int index = 0; index < _requesters.Count; index++)
            {
                World.Remove<JumpRequestComponent>(_requesters[index]);
            }

            for (int index = 0; index < _jumpers.Count; index++)
            {
                World.Add(_jumpers[index], new FallingComponent());
            }
        }

        private void Jump(
            Entity entity,
            ref PositionComponent position,
            ref MovementComponent movement,
            ref ColliderComponent collider,
            ref RigidbodyComponent rigidbody)
        {
            _requesters.Add(entity);

            if (!CanJump(entity, position.Position.z, collider.Size.y))
            {
                return;
            }

            movement.Velocity.z = World.Get<JumpRequestComponent>(entity).Force;
            rigidbody.IsGravityEnabled = false;
            _jumpers.Add(entity);
        }

        private bool CanJump(Entity entity, float z, float colliderHeight)
        {
            float restZ = GroundZ + colliderHeight * 0.5f;
            bool isGrounded = z <= restZ + GroundedEpsilon;
            return isGrounded && !World.Has<FallingComponent>(entity);
        }
    }
}
