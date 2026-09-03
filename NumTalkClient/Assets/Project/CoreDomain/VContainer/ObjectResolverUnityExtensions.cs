using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.CoreDomain.VContainer
{
    public static class ObjectResolverUnityExtensions
    {
        /// <summary>
        /// Use this method until VContainer add own implementation.
        /// https://github.com/hadashiA/VContainer/issues/694
        /// </summary>
        public static GameObject InstantiateAsync(this IObjectResolver resolver, GameObject prefab, Transform parent, bool worldPositionStays = false)
        {
            var wasActive = prefab.activeSelf;
            using (new global::VContainer.Unity.ObjectResolverUnityExtensions.PrefabDirtyScope(prefab))
            {
                prefab.SetActive(false);

                GameObject instance = null;
                try
                {
                    instance = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
                    SetName(instance, prefab);
                    resolver.InjectGameObject(instance);
                }
                finally
                {
                    prefab.SetActive(wasActive);
                    instance?.SetActive(wasActive);
                }
                return instance;
            }
        }
        
        static void SetName(UnityEngine.Object instance, UnityEngine.Object prefab)
        {
            if (VContainerSettings.Instance != null && VContainerSettings.Instance.RemoveClonePostfix)
                instance.name = prefab.name;
        }
    }
}