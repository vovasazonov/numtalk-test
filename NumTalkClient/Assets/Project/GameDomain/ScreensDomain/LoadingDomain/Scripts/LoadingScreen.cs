using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;

namespace Project.GameDomain.ScreensDomain.LoadingDomain.Scripts
{
    public class LoadingScreen : Screen<LoadingScreen>
    {
        protected override string ScreenId => "LoadingScreen";

        public override bool IsDisposeOnSwitch => false;

        public override UniTask ShowAsync()
        {
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync()
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InitializeScreenAsync()
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask DisposeScreenAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}