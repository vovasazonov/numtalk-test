using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;
using Project.CoreDomain.Lifecycle;
using Project.CoreDomain.View;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public sealed class ComponentListenerRegistry : ITaskAsyncInitializable, IDisposable
    {
        private readonly IViewService _viewService;

        private readonly Dictionary<Type, Entry> _entries = new();
        private readonly List<ComponentListener> _probes = new();
        private readonly List<IContentKeeper<ComponentListener>> _keepers = new();

        private Transform _poolRoot;

        public ComponentListenerRegistry(IViewService viewService)
        {
            _viewService = viewService;
        }

        public IReadOnlyList<ComponentListener> Probes => _probes;

        public async UniTask InitializeAsync()
        {
            List<UniTask> tasks = new();

            foreach (Type type in typeof(ComponentListener).Assembly.GetTypes())
            {
                bool isListener = !type.IsAbstract && typeof(ComponentListener).IsAssignableFrom(type);
                if (!isListener)
                {
                    continue;
                }

                Entry entry = new Entry($"{type.Name}.prefab");
                tasks.Add(InitializeEntryAsync(entry));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask InitializeEntryAsync(Entry entry)
        {
            IContentKeeper<ComponentListener> keeper = await CreateAsync(entry);
            ComponentListener probe = keeper.Value;

            Park(probe);
            entry.Pool.Push(keeper);
            _entries[probe.ComponentType] = entry;
            _probes.Add(probe);
        }

        public async UniTask<IContentKeeper<ComponentListener>> AcquireAsync(Type componentType)
        {
            Entry entry = _entries[componentType];
            if (entry.Pool.Count > 0)
            {
                return entry.Pool.Pop();
            }

            return await CreateAsync(entry);
        }

        public void Release(Type componentType, IContentKeeper<ComponentListener> keeper)
        {
            Park(keeper.Value);
            _entries[componentType].Pool.Push(keeper);
        }

        public void Dispose()
        {
            foreach (IContentKeeper<ComponentListener> keeper in _keepers)
            {
                keeper.Dispose();
            }

            _keepers.Clear();
            _entries.Clear();
            _probes.Clear();

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }

        private async UniTask<IContentKeeper<ComponentListener>> CreateAsync(Entry entry)
        {
            IContentKeeper<ComponentListener> keeper = await _viewService.CreateAsync<ComponentListener>(entry.Address);
            _keepers.Add(keeper);
            return keeper;
        }

        private void Park(ComponentListener listener)
        {
            listener.gameObject.SetActive(false);
            listener.transform.SetParent(PoolRoot, false);
        }

        private Transform PoolRoot
        {
            get
            {
                if (_poolRoot == null)
                {
                    GameObject root = new GameObject("ComponentListenerPool");
                    root.SetActive(false);
                    _poolRoot = root.transform;
                }

                return _poolRoot;
            }
        }

        private sealed class Entry
        {
            public Entry(string address)
            {
                Address = address;
            }

            public string Address { get; }

            public Stack<IContentKeeper<ComponentListener>> Pool { get; } = new();
        }
    }
}
