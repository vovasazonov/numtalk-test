using System;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Features.Ui.Scripts
{
    public class MenuUiPresenter : IInitializable, IDisposable
    {
        private const string ArenaScreenId = "ArenaScreen";

        private readonly IMenuUiView _view;
        private readonly IScreensService _screensService;

        private bool _isSwitchingToArena;

        public MenuUiPresenter(IMenuUiView view, IScreensService screensService)
        {
            _view = view;
            _screensService = screensService;
        }

        public void Initialize()
        {
            _view.PlayClicked += OnPlayClicked;
        }

        public void Dispose()
        {
            _view.PlayClicked -= OnPlayClicked;
        }

        private void OnPlayClicked()
        {
            if (_isSwitchingToArena)
            {
                return;
            }

            _isSwitchingToArena = true;
            SwitchToArenaAsync().Forget();
        }

        private async UniTask SwitchToArenaAsync()
        {
            await _screensService.SwitchAsync(ArenaScreenId);
        }
    }
}
