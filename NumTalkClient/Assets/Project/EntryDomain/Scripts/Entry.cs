using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;
using UnityEngine;
using VContainer;

namespace Project.EntryDomain.Scripts
{
    public class Entry : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private string _splashScreenId;
        [SerializeField] private string _loadingScreenId;
        [SerializeField] private string _menuScreenId;
        
        [Space]
        [SerializeField] private GameObject _tempBeforeSplash;

        private IScreenInitializable _screenInitializable;
        private IScreensService _screensService;

        [Inject]
        private void Construct(
            IScreenInitializable screenInitializable,
            IScreensService screensService
        )
        {
            _screenInitializable = screenInitializable;
            _screensService = screensService;
        }

        private void Start()
        {
            InitializeScreens().Forget();
        }

        private async UniTask InitializeScreens()
        {
            _screenInitializable.SetSplashScreen(_splashScreenId);
            _screenInitializable.SetLoadingScreen(_loadingScreenId);
            await _screensService.SwitchAsync(_splashScreenId);
            Destroy(_tempBeforeSplash);
            await _screensService.SwitchAsync(_menuScreenId);
        }
    }
}
