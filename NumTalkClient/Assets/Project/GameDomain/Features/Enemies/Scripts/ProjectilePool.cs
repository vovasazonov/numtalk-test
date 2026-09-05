using System.Collections.Generic;
using Arch.Core;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Reuses projectile entities instead of creating one per shot. A dormant projectile keeps its entity but
    /// loses its <see cref="ViewComponent"/>, which returns the view GameObject to the ViewSystem's own pool.
    /// </summary>
    public sealed class ProjectilePool
    {
        private static readonly float4 ProjectileTint = new(1f, 0.35f, 0.2f, 1f);

        private readonly World _world;
        private readonly Stack<Entity> _dormant = new();
        private readonly List<Entity> _live = new();
        private readonly int _layer;

        public ProjectilePool(World world)
        {
            _world = world;
            _layer = LayerMask.NameToLayer("EnemyProjectile");
        }

        /// <summary>Live projectiles, so a respawn can return every transient to the pool.</summary>
        public IReadOnlyList<Entity> Live => _live;

        public Entity Rent(float3 position, float3 velocity, float radius, float lifeTime)
        {
            var projectile = new ProjectileComponent
            {
                Velocity = velocity,
                Radius = radius,
                RemainingLifeTime = lifeTime,
            };
            var pose = new EntityTransformComponent
            {
                Position = position,
                Rotation = quaternion.identity,
                Layer = _layer,
            };

            if (TryTakeDormant(out Entity entity))
            {
                _world.Get<ProjectileComponent>(entity) = projectile;
                _world.Get<EntityTransformComponent>(entity) = pose;
                _world.Add(entity, new ViewComponent());
            }
            else
            {
                entity = _world.Create(projectile, pose, new ViewComponent(), new ShapeComponent
                {
                    Shape = PrimitiveShape.Sphere,
                    Size = new float3(radius * 2f),
                    Tint = ProjectileTint,
                });
            }

            _live.Add(entity);
            return entity;
        }

        public void Return(Entity entity)
        {
            _live.Remove(entity);
            if (!_world.IsAlive(entity)) return;

            if (_world.Has<ViewComponent>(entity)) _world.Remove<ViewComponent>(entity);
            _dormant.Push(entity);
        }

        public void ReturnAll()
        {
            for (int index = _live.Count - 1; index >= 0; index--) Return(_live[index]);
        }

        private bool TryTakeDormant(out Entity entity)
        {
            while (_dormant.Count > 0)
            {
                entity = _dormant.Pop();
                if (_world.IsAlive(entity)) return true;
            }

            entity = default;
            return false;
        }
    }
}
