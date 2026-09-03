using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Pickup.Scripts
{
    public sealed class PickUpCollisionSystem : UnitySystemBase
    {
        private readonly QueryDescription _collectors =
            new QueryDescription().WithAll<PickUpCollectorComponent, PositionComponent, ColliderComponent>();
        private readonly QueryDescription _items =
            new QueryDescription().WithAll<PickUpAbleComponent, PositionComponent, ColliderComponent>();

        private readonly ForEachWithEntity<PositionComponent, ColliderComponent> _collectCollector;
        private readonly ForEachWithEntity<PositionComponent, ColliderComponent> _collectItem;

        private readonly List<Body> _collectorBodies = new();
        private readonly List<Body> _itemBodies = new();
        private readonly List<PickUp> _pickUps = new();

        public PickUpCollisionSystem(World world) : base(world)
        {
            _collectCollector = CollectCollector;
            _collectItem = CollectItem;
        }

        public override void Update(in SystemState state)
        {
            _collectorBodies.Clear();
            World.Query(in _collectors, _collectCollector);

            if (_collectorBodies.Count == 0)
            {
                return;
            }

            _itemBodies.Clear();
            World.Query(in _items, _collectItem);

            _pickUps.Clear();
            for (int itemIndex = 0; itemIndex < _itemBodies.Count; itemIndex++)
            {
                Body item = _itemBodies[itemIndex];
                for (int collectorIndex = 0; collectorIndex < _collectorBodies.Count; collectorIndex++)
                {
                    Body collector = _collectorBodies[collectorIndex];
                    if (IsOverlapping(collector, item))
                    {
                        _pickUps.Add(new PickUp { Collector = collector.Entity, Item = item.Entity });
                        break;
                    }
                }
            }

            for (int index = 0; index < _pickUps.Count; index++)
            {
                PickUp pickUp = _pickUps[index];
                if (!World.IsAlive(pickUp.Item) || World.Has<PickUpEventComponent>(pickUp.Item))
                {
                    continue;
                }

                World.Add(pickUp.Item, new PickUpEventComponent
                {
                    CollectorEntity = pickUp.Collector,
                });
                World.Remove<PickUpAbleComponent>(pickUp.Item);
            }
        }

        private void CollectCollector(Entity entity, ref PositionComponent position, ref ColliderComponent collider)
        {
            _collectorBodies.Add(new Body
            {
                Entity = entity,
                Center = position.Position,
                HalfSize = collider.Size * 0.5f,
            });
        }

        private void CollectItem(Entity entity, ref PositionComponent position, ref ColliderComponent collider)
        {
            _itemBodies.Add(new Body
            {
                Entity = entity,
                Center = position.Position,
                HalfSize = collider.Size * 0.5f,
            });
        }

        private static bool IsOverlapping(Body a, Body b)
        {
            float3 delta = math.abs(a.Center - b.Center);
            float3 reach = a.HalfSize + b.HalfSize;
            return delta.x <= reach.x && delta.y <= reach.y && delta.z <= reach.z;
        }

        private struct Body
        {
            public Entity Entity;
            public float3 Center;
            public float3 HalfSize;
        }

        private struct PickUp
        {
            public Entity Collector;
            public Entity Item;
        }
    }
}
