using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Makes a platform give way shortly after it is stood on. Composes with motion and ice.</summary>
    [RequireComponent(typeof(PlatformBaker))]
    public sealed class CrumblePlatformBaker : MonoBehaviour, IComponentConverter
    {
        [Header("Timings (seconds)")]
        [Tooltip("Visible warning before the platform commits to falling.")]
        [SerializeField, Min(0f)] private float _telegraphTime = 0.35f;
        [SerializeField, Min(0f)] private float _fallDelay = 0.55f;
        [SerializeField, Min(0f)] private float _respawnTime = 3f;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new CrumbleStateComponent
            {
                Phase = CrumblePhase.Stable,
                TelegraphTime = _telegraphTime,
                FallDelay = _fallDelay,
                RespawnTime = _respawnTime,
            });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 1.04f);
        }
    }
}
