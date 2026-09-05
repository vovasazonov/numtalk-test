using Arch.Core;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Scripts
{
    public class ArenaScreen : Screen<ArenaScreen>
    {
        private readonly ComponentListenerRegistry _componentListenerRegistry;
        private readonly ArenaSceneLoader _arenaSceneLoader;
        private readonly World _world;
        private CanvasGroup _fadeOverlay;
        private int _fadeVersion;
        private const float FadeDuration = 0.6f;

        protected override string ScreenId => "ArenaScreen";

        public override bool IsDisposeOnSwitch => false;

        public ArenaScreen(
            ComponentListenerRegistry componentListenerRegistry,
            ArenaSceneLoader arenaSceneLoader,
            World world)
        {
            _componentListenerRegistry = componentListenerRegistry;
            _arenaSceneLoader = arenaSceneLoader;
            _world = world;
        }

        public override UniTask ShowAsync()
        {
            _fadeVersion++;
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.alpha = 1f;
            FadeInAsync(_fadeVersion).Forget();
            return UniTask.CompletedTask;
        }

        private async UniTask FadeInAsync(int version)
        {
            await UniTask.Delay(1000, ignoreTimeScale: true);
            if (_fadeOverlay == null || version != _fadeVersion)
            {
                return;
            }

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                await UniTask.Yield();
                if (_fadeOverlay == null || version != _fadeVersion)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / FadeDuration);
            }

            _fadeOverlay.alpha = 0f;
            _fadeOverlay.gameObject.SetActive(false);
        }

        public override UniTask HideAsync()
        {
            _fadeVersion++;
            _fadeOverlay.gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        protected override async UniTask InitializeScreenAsync()
        {
            // Cover the scene before additive loading can render its first frame.
            var overlay = new GameObject("ArenaFade", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(Image));
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // Below the splash (100), above the arena HUD.
            overlay.GetComponent<Image>().color = Color.black;
            overlay.GetComponent<Image>().raycastTarget = false;
            _fadeOverlay = overlay.GetComponent<CanvasGroup>();
            _fadeOverlay.blocksRaycasts = false;
            _fadeOverlay.interactable = false;

            await _componentListenerRegistry.InitializeAsync();
            await _arenaSceneLoader.LoadAndBakeAsync(_world);
        }

        protected override UniTask DisposeScreenAsync()
        {
            _fadeVersion++;
            if (_fadeOverlay != null)
            {
                Object.Destroy(_fadeOverlay.gameObject);
            }

            _componentListenerRegistry.Dispose();
            return UniTask.CompletedTask;
        }
    }
}
