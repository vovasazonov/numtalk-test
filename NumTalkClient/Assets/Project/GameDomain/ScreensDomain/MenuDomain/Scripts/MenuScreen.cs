using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Scripts
{
    public class MenuScreen : Screen<MenuScreen>
    {
        private const string ArenaScreenId = "ArenaScreen";

        private readonly IScreensService _screensService;
        private GameObject _canvas;
        private Button _playButton;
        private bool _isSwitchingToArena;

        protected override string ScreenId => "MenuScreen";

        public override bool IsDisposeOnSwitch => true;

        public MenuScreen(IScreensService screensService)
        {
            _screensService = screensService;
        }

        public override UniTask ShowAsync()
        {
            _canvas.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync()
        {
            _canvas.SetActive(false);
            return UniTask.CompletedTask;
        }

        protected override UniTask InitializeScreenAsync()
        {
            _canvas = CreateMenuCanvas();
            return UniTask.CompletedTask;
        }

        protected override UniTask DisposeScreenAsync()
        {
            Object.Destroy(_canvas);
            return UniTask.CompletedTask;
        }

        private GameObject CreateMenuCanvas()
        {
            var canvasObject = new GameObject(
                "MenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            CreatePlayButton(canvasObject.transform);
            return canvasObject;
        }

        private void CreatePlayButton(Transform parent)
        {
            var buttonObject = new GameObject(
                "PlayButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(260, 80);

            var image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            _playButton = buttonObject.GetComponent<Button>();
            _playButton.targetGraphic = image;
            _playButton.onClick.AddListener(OnPlayClicked);

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "PLAY";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 32;
            label.color = Color.black;
        }

        private void OnPlayClicked()
        {
            if (_isSwitchingToArena)
            {
                return;
            }

            _isSwitchingToArena = true;
            _playButton.interactable = false;
            SwitchToArenaAsync().Forget();
        }

        private async UniTask SwitchToArenaAsync()
        {
            await _screensService.SwitchAsync(ArenaScreenId);
        }
    }
}
