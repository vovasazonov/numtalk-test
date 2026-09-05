using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>
    /// Resolves whatever the player is standing on into the rider's platform velocity channel and surface slip.
    /// Every surface behaviour composes here: motion supplies the velocity, ice supplies the slip, and a surface
    /// that has stopped being standable supplies neither. A fourth behaviour reads its own component in this one
    /// place rather than forking the motor.
    /// </summary>
    public sealed class PlatformRiderSystem : UnitySystemBase
    {
        private readonly QueryDescription _riders = new QueryDescription()
            .WithAll<PlayerTagComponent, GroundStateComponent, PlatformRiderComponent>();

        private readonly ForEach _resolve;

        public PlatformRiderSystem(World world) : base(world)
        {
            _resolve = Resolve;
        }

        public override void Update(in SystemState state) => World.Query(in _riders, _resolve);

        private void Resolve(Entity entity)
        {
            ref var ground = ref World.Get<GroundStateComponent>(entity);
            ref var rider = ref World.Get<PlatformRiderComponent>(entity);

            rider.Platform = default;
            rider.SurfaceVelocity = float3.zero;
            rider.SurfaceSlip = 0f;

            if (!ground.IsGrounded || !World.IsAlive(ground.GroundEntity)) return;
            if (!World.TryGet(ground.GroundEntity, out PlatformSurfaceComponent surface) || !surface.IsStandable) return;

            rider.Platform = ground.GroundEntity;
            rider.SurfaceVelocity = surface.SurfaceVelocity;

            if (World.TryGet(ground.GroundEntity, out IceSurfaceComponent ice))
            {
                rider.SurfaceSlip = 1f - ice.DecelerationScale;
            }
        }
    }
}
