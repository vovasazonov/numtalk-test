using Arch.Unity.Conversion;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerBaker : MonoBehaviour, IComponentConverter
    {
        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new ViewComponent());
            converter.AddComponent(new PlayerTagComponent());
            converter.AddComponent(new PickUpCollectorComponent());
        }
    }
}
