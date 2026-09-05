using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Marks a platform as slick. Composes with motion and crumble on the same instance.</summary>
    [RequireComponent(typeof(PlatformBaker))]
    public sealed class IceSurfaceBaker : MonoBehaviour, IComponentConverter
    {
        [Tooltip("Scales the rider's intrinsic deceleration. 1 is normal ground, 0 keeps all momentum.")]
        [SerializeField, Range(0f, 1f)] private float _decelerationScale = 0.1f;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new IceSurfaceComponent { DecelerationScale = _decelerationScale });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 1.02f);
        }
    }
}
