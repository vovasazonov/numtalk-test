using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public static class PhysicsInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<GravitySystem>();
            builder.RegisterSystemIntoArchApp<FallSystem>();
        }
    }
}
