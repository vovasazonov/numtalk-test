using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Player.Scripts
{
    public static class PlayerInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<PlayerMoveSystem>();
            builder.RegisterSystemIntoArchApp<PlayerJumpSystem>();
            builder.RegisterSystemIntoArchApp<CameraFollowPlayerSystem>();
        }
    }
}
