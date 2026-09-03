using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;
using Project.CoreDomain.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.CoreDomain.View
{
    public class ViewService : IViewService
    {
        private readonly IContentService _contentService;
        private readonly IObjectResolver _resolver;
        private readonly Transform _parent;

        public ViewService(IContentService contentService, IObjectResolver resolver, LifetimeScope scope)
        {
            _contentService = contentService;
            _resolver = resolver;
            _parent = scope.transform;
        }

        public async UniTask<IContentKeeper<T>> CreateAsync<T>(string assetId)
        {
            var prefabKeeper = await _contentService.LoadAsync<GameObject>(assetId);
            var prefab = prefabKeeper.Value;
            var gameObject = _resolver.InstantiateAsync(prefab, _parent);
            var component = gameObject.GetComponent<T>();

            return new ContentDisposableView<T>(component, gameObject, prefabKeeper);
        }
    }
}