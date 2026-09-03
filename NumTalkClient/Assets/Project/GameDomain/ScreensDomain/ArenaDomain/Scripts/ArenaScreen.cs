using Arch.Core;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Screen;
using Project.GameDomain.Features.EcsArchitecture.Scripts;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Scripts
{
    public class ArenaScreen : Screen<ArenaScreen>
    {
        private readonly ComponentListenerRegistry _componentListenerRegistry;
        private readonly ArenaSceneLoader _arenaSceneLoader;
        private readonly World _world;

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
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync()
        {
            return UniTask.CompletedTask;
        }

        protected override async UniTask InitializeScreenAsync()
        {
            await _componentListenerRegistry.InitializeAsync();
            await _arenaSceneLoader.LoadAndBakeAsync(_world);
        }

        protected override UniTask DisposeScreenAsync()
        {
            _componentListenerRegistry.Dispose();
            return UniTask.CompletedTask;
        }
    }
}
