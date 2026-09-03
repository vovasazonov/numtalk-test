using Arch.Unity;
using VContainer;

namespace Project.GameDomain.Features.Pickup.Scripts
{
    public static class PickupInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<PickUpEventResetSystem>();
            builder.RegisterSystemIntoArchApp<PickUpCollisionSystem>();
        }
    }
}
