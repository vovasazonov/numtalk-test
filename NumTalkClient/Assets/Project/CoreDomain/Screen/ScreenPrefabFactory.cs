using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.CoreDomain.Screen
{
    public class ScreenPrefabFactory : IScreenFactory
    {
        private readonly GameObject _prefab;
        private readonly LifetimeScope _parentScope;

        public string Id { get; }

        public ScreenPrefabFactory(string id, GameObject prefab, LifetimeScope parentScope)
        {
            Id = id;
            _prefab = prefab;
            _parentScope = parentScope;
        }

        public IScreen Create()
        {
            LifetimeScope childScope;

            using (LifetimeScope.EnqueueParent(_parentScope))
            {
                var instance = Object.Instantiate(_prefab);
                instance.name = Id + "Context";
                childScope = instance.GetComponent<LifetimeScope>();
            }

            return childScope.Container.Resolve<IScreen>();
        }
    }
}
