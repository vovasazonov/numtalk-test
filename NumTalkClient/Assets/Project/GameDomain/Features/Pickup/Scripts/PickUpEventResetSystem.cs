using Arch.Core;
using Arch.Unity.Toolkit;

namespace Project.GameDomain.Features.Pickup.Scripts
{
    public sealed class PickUpEventResetSystem : UnitySystemBase
    {
        private readonly QueryDescription _pickUpEvents = new QueryDescription().WithAll<PickUpEventComponent>();

        public PickUpEventResetSystem(World world) : base(world)
        {
        }

        public override void BeforeUpdate(in SystemState state)
        {
            World.Remove<PickUpEventComponent>(in _pickUpEvents);
        }
    }
}
