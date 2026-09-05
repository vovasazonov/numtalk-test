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

        private readonly Dictionary<Type, int> _rootComponentUsers = new();
        private readonly HashSet<Type> _ownedRootComponents = new();

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
                    ReleaseRootComponents(keeper.Value);
                    registry.Release(componentType, keeper);
                }
            }
        }

        public void ReleaseAllListeners(ComponentListenerRegistry registry)
        {
            foreach (KeyValuePair<Type, IContentKeeper<ComponentListener>> pair in _activeListeners)
            {
                ReleaseRootComponents(pair.Value.Value);
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

            AcquireRootComponents(listener);
            listener.transform.SetParent(transform, false);
            listener.gameObject.SetActive(true);
            _activeListeners[componentType] = keeper;
            listener.Sync(_world, entity);
        }

        private void AcquireRootComponents(ComponentListener listener)
        {
            IReadOnlyList<Type> required = listener.RequiredRootComponents;
            for (int index = 0; index < required.Count; index++)
            {
                Type type = required[index];
                _rootComponentUsers.TryGetValue(type, out int users);
                _rootComponentUsers[type] = users + 1;

                bool isFirstUser = users == 0;
                if (!isFirstUser || GetComponent(type) != null)
                {
                    continue;
                }

                gameObject.AddComponent(type);
                _ownedRootComponents.Add(type);
            }
        }

        private void ReleaseRootComponents(ComponentListener listener)
        {
            listener.Release();
            IReadOnlyList<Type> required = listener.RequiredRootComponents;
            for (int index = 0; index < required.Count; index++)
            {
                Type type = required[index];
                if (!_rootComponentUsers.TryGetValue(type, out int users))
                {
                    continue;
                }

                users--;
                if (users > 0)
                {
                    _rootComponentUsers[type] = users;
                    continue;
                }

                _rootComponentUsers.Remove(type);

                bool isOwned = _ownedRootComponents.Remove(type);
                if (!isOwned)
                {
                    continue;
                }

                UnityEngine.Component component = GetComponent(type);
                if (component != null)
                {
                    // Immediate, so a view reused in the same frame does not see a component that is about to die.
                    DestroyImmediate(component);
                }
            }
        }
    }
}
