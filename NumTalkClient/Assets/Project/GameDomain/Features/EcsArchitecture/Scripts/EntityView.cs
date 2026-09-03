using System;
using System.Collections.Generic;
using Arch.Core;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public class EntityView : MonoBehaviour
    {
        public Entity Entity { get; private set; }
        private World _world;

        private readonly Dictionary<Type, IContentKeeper<ComponentListener>> _activeListeners = new();
        private readonly HashSet<Type> _pendingListeners = new();

        public void Initialize(World world, Entity entity)
        {
            _world = world;
            Entity = entity;
        }

        public void SyncListeners(ComponentListenerRegistry registry)
        {
            IReadOnlyList<ComponentListener> probes = registry.Probes;
            for (int index = 0; index < probes.Count; index++)
            {
                ComponentListener probe = probes[index];
                Type componentType = probe.ComponentType;
                bool hasComponent = probe.Matches(_world, Entity);
                bool isActive = _activeListeners.TryGetValue(componentType, out IContentKeeper<ComponentListener> keeper);

                if (hasComponent && !isActive && !_pendingListeners.Contains(componentType))
                {
                    AttachListenerAsync(registry, componentType).Forget();
                }
                else if (hasComponent && isActive)
                {
                    keeper.Value.Sync(_world, Entity);
                }
                else if (!hasComponent && isActive)
                {
                    _activeListeners.Remove(componentType);
                    registry.Release(componentType, keeper);
                }
            }
        }

        public void ReleaseAllListeners(ComponentListenerRegistry registry)
        {
            foreach (KeyValuePair<Type, IContentKeeper<ComponentListener>> pair in _activeListeners)
            {
                registry.Release(pair.Key, pair.Value);
            }

            _activeListeners.Clear();
        }

        private async UniTaskVoid AttachListenerAsync(ComponentListenerRegistry registry, Type componentType)
        {
            Entity entity = Entity;
            _pendingListeners.Add(componentType);
            IContentKeeper<ComponentListener> keeper = await registry.AcquireAsync(componentType);
            _pendingListeners.Remove(componentType);

            ComponentListener listener = keeper.Value;
            if (Entity != entity || !_world.IsAlive(entity) || !listener.Matches(_world, entity))
            {
                registry.Release(componentType, keeper);
                return;
            }

            listener.transform.SetParent(transform, false);
            listener.gameObject.SetActive(true);
            _activeListeners[componentType] = keeper;
            listener.Sync(_world, entity);
        }
    }
}
