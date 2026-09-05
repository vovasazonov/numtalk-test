using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Checkpoints.Scripts
{
    public sealed class CheckpointBaker : MonoBehaviour, IComponentConverter
    {
        [Tooltip("Course order. The run always resumes from the highest activated id.")]
        [SerializeField, Min(1)] private int _id = 1;

        [Tooltip("Where the player reappears, relative to this checkpoint.")]
        [SerializeField] private Vector3 _respawnOffset = new(0f, 1.2f, 0f);

        public Vector3 RespawnPosition => transform.position + _respawnOffset;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Pickup");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new CheckpointComponent
            {
                Id = _id,
                RespawnPosition = RespawnPosition,
            });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(RespawnPosition, 0.5f);
            Gizmos.DrawLine(transform.position, RespawnPosition);
        }
    }
}
