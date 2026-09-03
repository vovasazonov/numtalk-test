using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Input.Scripts
{
    public static class InputInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<PointerSystem>();
            builder.RegisterSystemIntoArchApp<JoystickSystem>();
        }
    }
}