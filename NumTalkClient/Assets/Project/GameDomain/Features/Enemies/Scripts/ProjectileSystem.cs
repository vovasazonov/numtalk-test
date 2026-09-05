using Arch.Core;
using Project.GameDomain.Features.Course.Scripts;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Sweeps every live projectile over its complete travel segment each fixed step. Because the whole segment is
    /// cast rather than the projectile being teleported and tested, no speed can tunnel through geometry, and the
    /// cast's explicit mask - never the Enemy layer - is what excludes the firing enemy and other projectiles.
    /// </summary>
    public sealed class ProjectileSystem : UnitySystemBase
    {
        private readonly ProjectilePool _pool;
        private readonly PlatformerTuningConfig _tuning;

        public ProjectileSystem(World world, ProjectilePool pool, PlatformerTuningConfig tuning) : base(world)
        {
            _pool = pool;
            _tuning = tuning;
        }

        public override void Update(in SystemState state)
        {
            float dt = state.DeltaTime;
            if (dt <= 0f) return;

            // Backwards, because a projectile that hits or expires is returned to the pool during the walk.
            for (int index = _pool.Live.Count - 1; index >= 0; index--)
            {
                Entity entity = _pool.Live[index];
                if (!World.IsAlive(entity))
                {
                    _pool.Return(entity);
                    continue;
                }

                Step(entity, dt);
            }
        }

        private void Step(Entity entity, float dt)
        {
            ref var projectile = ref World.Get<ProjectileComponent>(entity);
            ref var pose = ref World.Get<EntityTransformComponent>(entity);

            float3 step = projectile.Velocity * dt;
            float distance = math.length(step);
            if (distance <= 0f)
            {
                _pool.Return(entity);
                return;
            }

            float3 direction = step / distance;
            if (UnityEngine.Physics.SphereCast(pose.Position, projectile.Radius, direction, out RaycastHit hit,
                    distance, _tuning.ProjectileHitMask, QueryTriggerInteraction.Ignore))
            {
                pose.Position += direction * hit.distance;
                Resolve(hit, direction);
                _pool.Return(entity);
                return;
            }

            pose.Position += step;
            projectile.RemainingLifeTime -= dt;
            if (projectile.RemainingLifeTime <= 0f) _pool.Return(entity);
        }

        /// <summary>A hit on the player becomes an external impulse, decayed by the motor apart from intent.</summary>
        private void Resolve(RaycastHit hit, float3 direction)
        {
            var view = hit.collider.GetComponentInParent<EntityView>();
            if (view == null) return;

            Entity target = view.Entity;
            if (!World.IsAlive(target) || !World.Has<ExternalVelocityComponent>(target)) return;

            if (World.TryGet(target, out RunStateComponent run) && run.IsComplete) return;
            if (World.TryGet(target, out HealthComponent health) && health.IsProtected) return;

            float3 push = math.normalizesafe(new float3(direction.x, 0f, direction.z));
            // Replaced rather than accumulated, so a burst of shots cannot stack into an unrecoverable launch.
            World.Get<ExternalVelocityComponent>(target).Velocity = push * _tuning.KnockbackSpeed;
        }
    }
}
