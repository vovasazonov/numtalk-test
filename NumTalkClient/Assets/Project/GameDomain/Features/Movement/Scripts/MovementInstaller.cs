using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Movement.Scripts
{
    public static class MovementInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<MovementSystem>();
        }
    }
}