using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.ReapBehindPlayer.Scripts
{
    public static class ReapBehindPlayerInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<ReapBehindPlayerSystem>();
        }
    }
}