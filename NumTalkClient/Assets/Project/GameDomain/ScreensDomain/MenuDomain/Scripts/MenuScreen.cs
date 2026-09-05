using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Scripts
{
    public class MenuScreen : Screen<MenuScreen>
    {
        protected override string ScreenId => "MenuScreen";

        public override bool IsDisposeOnSwitch => true;

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
