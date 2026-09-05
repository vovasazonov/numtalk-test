using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Pushables.Scripts
{
    /// <summary>
    /// Reads each simulating body's pose back into ECS, because physics is authoritative while a dynamic body is
    /// awake. Publishing its velocity as <see cref="PlatformSurfaceComponent.SurfaceVelocity"/> is what makes the
    /// crate a ride surface: it then reaches the player through the same rider channel as a moving platform, and
    /// jumping off it inherits velocity by the same rule.
    /// </summary>
    public sealed class PushableBodySystem : UnitySystemBase
    {
        private readonly RigidBodyService _bodies;

        private readonly QueryDescription _dynamic = new QueryDescription()
            .WithAll<PhysicsBodyComponent, EntityTransformComponent>();

        private readonly ForEach _readBack;

        public PushableBodySystem(World world, RigidBodyService bodies) : base(world)
        {
            _bodies = bodies;
            _readBack = ReadBack;
        }

        public override void Update(in SystemState state) => World.Query(in _dynamic, _readBack);

        private void ReadBack(Entity entity)
        {
            if (!_bodies.IsReady(entity)) return;

            _bodies.Read(entity, out float3 position, out quaternion rotation, out float3 velocity);

            ref var pose = ref World.Get<EntityTransformComponent>(entity);
            pose.Position = position;
            pose.Rotation = rotation;

            if (World.Has<PlatformSurfaceComponent>(entity))
            {
                World.Get<PlatformSurfaceComponent>(entity).SurfaceVelocity = velocity;
            }
        }
    }
}
