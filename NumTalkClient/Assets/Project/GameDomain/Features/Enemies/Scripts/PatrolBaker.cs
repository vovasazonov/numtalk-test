using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>Authored patrol route. The route is drawn in the Scene view so ledges can be judged at a glance.</summary>
    [RequireComponent(typeof(EnemyBaker))]
    public sealed class PatrolBaker : MonoBehaviour, IComponentConverter
    {
        [Header("Route (local offset, metres)")]
        [SerializeField] private Vector3 _endOffset = new(4f, 0f, 0f);

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _speed = 2f;
        [SerializeField, Min(0f)] private float _waitTime = 0.35f;

        public Vector3 EndPosition => transform.position + transform.rotation * _endOffset;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PatrolComponent
            {
                StartPosition = transform.position,
                EndPosition = EndPosition,
                Speed = _speed,
                WaitTime = _waitTime,
                IsForward = true,
            });
        }

        private void OnDrawGizmos()
        {
            Vector3 start = transform.position;
            Vector3 end = EndPosition;

            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.9f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(end, 0.3f);
        }
    }
}
