using System;
using Arch.Core;
using Cysharp.Threading.Tasks;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Scripts
{
    public sealed class ArenaSceneLoader
    {
        private const string ArenaScenePath =
            "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scenes/ArenaScene.unity";

        private Scene _arenaScene;

        public async UniTask LoadAndBakeAsync(World world)
        {
            if (_arenaScene.isLoaded)
            {
                return;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(ArenaScenePath, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                throw new InvalidOperationException($"Unable to load arena scene at '{ArenaScenePath}'.");
            }

            await loadOperation.ToUniTask();
            _arenaScene = SceneManager.GetSceneByPath(ArenaScenePath);

            if (!_arenaScene.IsValid())
            {
                throw new InvalidOperationException($"Arena scene at '{ArenaScenePath}' was not loaded.");
            }

            BakeSceneObjects(world);
        }

        private void BakeSceneObjects(World world)
        {
            foreach (GameObject root in _arenaScene.GetRootGameObjects())
            {
                BakerComponent[] bakers = root.GetComponentsInChildren<BakerComponent>(true);
                foreach (BakerComponent baker in bakers)
                {
                    baker.Bake(world);
                }
            }
        }
    }
}
