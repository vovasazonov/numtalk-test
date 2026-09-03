using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Creature.Scripts
{
    public static class CreatureInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<CreatureStateSystem>();
        }
    }
}
