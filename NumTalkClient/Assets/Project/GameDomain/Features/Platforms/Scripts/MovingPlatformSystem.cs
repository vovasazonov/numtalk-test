using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>
    /// Advances every authored route in fixed time and publishes the resulting surface velocity. Riders are never
    /// parented: they read <see cref="PlatformSurfaceComponent.SurfaceVelocity"/> as their own velocity channel.
    /// </summary>
    public sealed class MovingPlatformSystem : UnitySystemBase
    {
        private readonly QueryDescription _platforms = new QueryDescription()
            .WithAll<PlatformMotionComponent, PlatformSurfaceComponent, EntityTransformComponent>();

        private readonly ForEach _advance;
        private float _dt;

        public MovingPlatformSystem(World world) : base(world)
        {
            _advance = Advance;
        }

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (_dt > 0f) World.Query(in _platforms, _advance);
        }

        private void Advance(Entity entity)
        {
            ref var motion = ref World.Get<PlatformMotionComponent>(entity);
            ref var surface = ref World.Get<PlatformSurfaceComponent>(entity);
            ref var pose = ref World.Get<EntityTransformComponent>(entity);

            // A platform that has given way is owned by the crumble system, so motion yields rather than fighting it.
            if (!surface.IsStandable)
            {
                surface.SurfaceVelocity = float3.zero;
                return;
            }

            float length = math.distance(motion.StartPosition, motion.EndPosition);
            float previous = motion.Progress;

            if (motion.WaitTimer > 0f)
            {
                motion.WaitTimer = math.max(0f, motion.WaitTimer - _dt);
            }
            else if (length > 0f)
            {
                float step = motion.Speed * _dt / length;
                motion.Progress = math.clamp(motion.Progress + (motion.IsForward ? step : -step), 0f, 1f);

                if (motion.Progress >= 1f || motion.Progress <= 0f)
                {
                    motion.IsForward = !motion.IsForward;
                    motion.WaitTimer = motion.WaitTime;
                }
            }

            // Velocity comes from the progress delta rather than the pose, so a route that starts part-way along
            // cannot report a one-tick teleport as surface velocity.
            float3 target = math.lerp(motion.StartPosition, motion.EndPosition, motion.Progress);
            surface.SurfaceVelocity = (target - math.lerp(motion.StartPosition, motion.EndPosition, previous)) / _dt;
            pose.Position = target;
        }
    }
}
