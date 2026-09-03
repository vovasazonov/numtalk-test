using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Creature.Scripts
{
    public sealed class CreatureStateSystem : UnitySystemBase
    {
        private const float GroundZ = 0f;
        private const float GroundedEpsilon = 0.001f;
        private const float MovingThresholdSqr = 0.0001f;
        private const float SideThreshold = 0.01f;

        private readonly QueryDescription _creatures =
            new QueryDescription().WithAll<CreatureComponent, PositionComponent>();

        private readonly ForEachWithEntity<CreatureComponent, PositionComponent> _updateState;

        public CreatureStateSystem(World world) : base(world)
        {
            _updateState = UpdateState;
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _creatures, _updateState);
        }

        private void UpdateState(Entity entity, ref CreatureComponent creature, ref PositionComponent position)
        {
            creature.HeightAboveGround = ResolveHeightAboveGround(entity, position.Position.z);

            float3 velocity = float3.zero;
            if (World.Has<MovementComponent>(entity))
            {
                velocity = World.Get<MovementComponent>(entity).Velocity;
            }

            bool isMoving = math.lengthsq(velocity) > MovingThresholdSqr;

            bool isAirborne = creature.HeightAboveGround > GroundedEpsilon && !IsHovering(entity);

            creature.State = isAirborne
                ? CreatureState.Jump
                : isMoving ? CreatureState.Walk : CreatureState.Idle;

            if (isMoving)
            {
                CreatureSide side = ResolveSide(velocity);
                if (side != CreatureSide.None)
                {
                    creature.Side = side;
                }
            }
        }

        private bool IsHovering(Entity entity)
        {
            if (!World.Has<RigidbodyComponent>(entity))
            {
                return false;
            }

            return !World.Get<RigidbodyComponent>(entity).IsGravityEnabled && !World.Has<FallingComponent>(entity);
        }

        private float ResolveHeightAboveGround(Entity entity, float z)
        {
            if (!World.Has<ColliderComponent>(entity))
            {
                return 0f;
            }

            float restZ = GroundZ + World.Get<ColliderComponent>(entity).Size.y * 0.5f;
            return math.max(0f, z - restZ);
        }

        private static CreatureSide ResolveSide(float3 velocity)
        {
            CreatureSide side = CreatureSide.None;

            if (velocity.x > SideThreshold)
            {
                side |= CreatureSide.Right;
            }
            else if (velocity.x < -SideThreshold)
            {
                side |= CreatureSide.Left;
            }

            if (velocity.y > SideThreshold)
            {
                side |= CreatureSide.Up;
            }
            else if (velocity.y < -SideThreshold)
            {
                side |= CreatureSide.Down;
            }

            return side;
        }
    }
}
