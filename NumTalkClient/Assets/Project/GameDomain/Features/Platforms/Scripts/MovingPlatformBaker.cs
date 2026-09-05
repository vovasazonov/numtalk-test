using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Adds an authored two-point route to a platform. The route is drawn in the Scene view.</summary>
    [RequireComponent(typeof(PlatformBaker))]
    public sealed class MovingPlatformBaker : MonoBehaviour, IComponentConverter
    {
        [Header("Route (local offset, metres)")]
        [SerializeField] private Vector3 _endOffset = new(0f, 0f, 6f);

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _speed = 2.5f;
        [SerializeField, Min(0f)] private float _waitTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _startProgress;

        public Vector3 EndPosition => transform.position + transform.rotation * _endOffset;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PlatformMotionComponent
            {
                StartPosition = transform.position,
                EndPosition = EndPosition,
                Speed = _speed,
                WaitTime = _waitTime,
                Progress = _startProgress,
                IsForward = true,
            });
        }

        private void OnDrawGizmos()
        {
            Vector3 start = transform.position;
            Vector3 end = EndPosition;

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.25f);
            Gizmos.DrawWireSphere(end, 0.25f);

            Gizmos.matrix = Matrix4x4.TRS(end, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
