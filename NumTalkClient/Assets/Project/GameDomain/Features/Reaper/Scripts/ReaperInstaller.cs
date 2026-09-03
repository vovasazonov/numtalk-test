using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Reaper.Scripts
{
    public static class ReaperInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<ReaperSystem>();
        }
    }
}
