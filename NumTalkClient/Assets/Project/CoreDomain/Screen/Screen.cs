using Cysharp.Threading.Tasks;
using Project.CoreDomain.Scripts.Logger;
using UnityEngine;

namespace Project.CoreDomain.Screen
{
    public abstract class Screen<TSpecificScreen> : IScreen where TSpecificScreen : Screen<TSpecificScreen>
    {
        protected abstract string ScreenId { get; }

        public abstract bool IsDisposeOnSwitch { get; }

        public abstract UniTask ShowAsync();

        public abstract UniTask HideAsync();

        public async UniTask InitializeAsync()
        {
            ProjectLogger.Log($"Start initialize {ScreenId} screen");

            await InitializeScreenAsync();
            
            ProjectLogger.Log($"Finish initialize {ScreenId} screen");
        }

        protected abstract UniTask InitializeScreenAsync();

        protected abstract UniTask DisposeScreenAsync();

        public async UniTask DisposeAsync()
        {
            ProjectLogger.Log($"Start dispose {ScreenId} screen");

            await DisposeScreenAsync();
            UnityEngine.Object.Destroy(GameObject.Find(ScreenId + "Context"));

            ProjectLogger.Log($"Finish dispose {ScreenId} screen");
        }
    }
}