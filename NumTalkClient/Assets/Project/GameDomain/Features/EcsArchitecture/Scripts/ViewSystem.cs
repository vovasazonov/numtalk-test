using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;
using Project.CoreDomain.View;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public sealed class ViewSystem : UnitySystemBase
    {
        private const string EntityViewAddress = "Entity.prefab";

        private readonly IViewService _viewService;
        private readonly ComponentListenerRegistry _registry;
        private readonly QueryDescription _viewEntities = new QueryDescription().WithAll<ViewComponent>();
        private readonly ForEach _spawnMissingView;

        private readonly Dictionary<Entity, IContentKeeper<EntityView>> _activeViews = new();
        private readonly Stack<IContentKeeper<EntityView>> _pool = new();
        private readonly HashSet<Entity> _pendingViews = new();
        private readonly List<Entity> _viewsToRelease = new();

        public ViewSystem(World world, IViewService viewService, ComponentListenerRegistry registry) : base(world)
        {
            _viewService = viewService;
            _registry = registry;
            _spawnMissingView = SpawnMissingView;
        }

        public override void Update(in SystemState state)
        {
            ReleaseEmptyViews();
            SpawnMissingViews();
            SyncListeners();
        }

        private void SyncListeners()
        {
            foreach (IContentKeeper<EntityView> keeper in _activeViews.Values)
            {
                keeper.Value.SyncListeners(_registry);
            }
        }

        private void ReleaseEmptyViews()
        {
            _viewsToRelease.Clear();
            foreach (KeyValuePair<Entity, IContentKeeper<EntityView>> pair in _activeViews)
            {
                Entity entity = pair.Key;
                if (!World.IsAlive(entity) || !World.Has<ViewComponent>(entity))
                {
                    _viewsToRelease.Add(entity);
                }
            }

            foreach (Entity entity in _viewsToRelease)
            {
                ReturnToPool(entity);
            }
        }

        private void SpawnMissingViews()
        {
            World.Query(in _viewEntities, _spawnMissingView);
        }

        private void SpawnMissingView(Entity entity)
        {
            bool needsView = !_activeViews.ContainsKey(entity)
                             && !_pendingViews.Contains(entity);
            if (needsView)
            {
                _pendingViews.Add(entity);
                SpawnViewAsync(entity).Forget();
            }
        }

        private async UniTaskVoid SpawnViewAsync(Entity entity)
        {
            IContentKeeper<EntityView> keeper = await AcquireViewAsync();

            if (!World.IsAlive(entity) || !World.Has<ViewComponent>(entity))
            {
                _pendingViews.Remove(entity);
                PushToPool(keeper);
                return;
            }

            EntityView view = keeper.Value;
            view.gameObject.SetActive(true);
            view.Initialize(World, entity);

            _activeViews[entity] = keeper;
            _pendingViews.Remove(entity);
        }

        private async UniTask<IContentKeeper<EntityView>> AcquireViewAsync()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            return await _viewService.CreateAsync<EntityView>(EntityViewAddress);
        }

        private void ReturnToPool(Entity entity)
        {
            IContentKeeper<EntityView> keeper = _activeViews[entity];
            _activeViews.Remove(entity);
            PushToPool(keeper);
        }

        private void PushToPool(IContentKeeper<EntityView> keeper)
        {
            keeper.Value.ReleaseAllListeners(_registry);
            keeper.Value.gameObject.SetActive(false);
            _pool.Push(keeper);
        }

        public override void Dispose()
        {
            foreach (IContentKeeper<EntityView> keeper in _activeViews.Values)
            {
                keeper.Dispose();
            }

            foreach (IContentKeeper<EntityView> keeper in _pool)
            {
                keeper.Dispose();
            }

            _activeViews.Clear();
            _pool.Clear();
            _pendingViews.Clear();

            base.Dispose();
        }
    }
}
