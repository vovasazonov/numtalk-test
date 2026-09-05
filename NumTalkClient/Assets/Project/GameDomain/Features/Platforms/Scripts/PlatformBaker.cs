using Arch.Unity.Conversion;
using Project.GameDomain.Features.Course.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>
    /// Shared platform authoring. Add this to any standable surface, then add the optional behaviour bakers
    /// (<see cref="MovingPlatformBaker"/>, <see cref="IceSurfaceBaker"/>, <see cref="CrumblePlatformBaker"/>)
    /// to the same object in any combination.
    /// </summary>
    public sealed class PlatformBaker : MonoBehaviour, IComponentConverter
    {
        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Platform");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PlatformSurfaceComponent { IsStandable = true });
            converter.AddComponent(new InitialStateComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
            });
        }
    }
}
