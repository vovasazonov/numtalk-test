using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Jump.Scripts
{
    public static class JumpInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<JumpSystem>();
        }
    }
}
