using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.GameInput.Scripts
{
    public static class GameInputInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<GameInputSystem>();
        }
    }
}
