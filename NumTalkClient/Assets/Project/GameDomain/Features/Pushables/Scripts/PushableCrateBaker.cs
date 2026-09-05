using Arch.Unity.Conversion;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Pushables.Scripts
{
    /// <summary>
    /// Crate authoring. Mass and collision detection come from <c>PhysicsBodyBaker</c> on the same object; the crate
    /// also carries the shared platform surface so the player can ride and jump off it.
    /// </summary>
    public sealed class PushableCrateBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField, Min(0f)] private float _pushAcceleration = 18f;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Pushable");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PushableComponent { PushAcceleration = _pushAcceleration });
            converter.AddComponent(new PlatformSurfaceComponent { IsStandable = true });
            converter.AddComponent(new InitialStateComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
            });
        }
    }
}
