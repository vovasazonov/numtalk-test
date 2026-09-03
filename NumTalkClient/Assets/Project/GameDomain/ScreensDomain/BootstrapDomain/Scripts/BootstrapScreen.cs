using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;
using Project.CoreDomain.Screen;
using Project.CoreDomain.View;
using Project.GameDomain.Features.Bootstrap;
using Project.GameDomain.ScreensDomain.BootstrapDomain.Scripts.Splash.View;
using UnityEngine;

namespace Project.GameDomain.ScreensDomain.BootstrapDomain.Scripts
{
    public class BootstrapScreen : Screen<BootstrapScreen>
    {
        private readonly IViewService _viewService;
        private readonly BootstrapScreenContent _content;
        private readonly BootstrapCommand _bootstrapCommand;
        private IContentKeeper<ISplashView> _view;

        protected override string ScreenId => "BootstrapScreen";

        public override bool IsDisposeOnSwitch => true;

        public BootstrapScreen(
            IViewService viewService,
            BootstrapScreenContent content,
            BootstrapCommand bootstrapCommand)
        {
            _viewService = viewService;
            _content = content;
            _bootstrapCommand = bootstrapCommand;
        }

        public override UniTask ShowAsync()
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask HideAsync()
        {
            await _view.Value.Hide().ContinueWith(_view.Dispose);
        }

        protected override async UniTask InitializeScreenAsync()
        {
            var bootstrapTask = _bootstrapCommand.ExecuteAsync();

            _view = await _viewService.CreateAsync<ISplashView>(_content.Splash.AssetGUID);
            await bootstrapTask;
            
            Application.targetFrameRate = 120;
        }

        protected override UniTask DisposeScreenAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
