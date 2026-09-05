using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>Authored shooter. The fire line and range are drawn in the Scene view.</summary>
    [RequireComponent(typeof(EnemyBaker))]
    public sealed class ShooterBaker : MonoBehaviour, IComponentConverter
    {
        [Header("Fire line (local direction)")]
        [SerializeField] private Vector3 _fireDirection = Vector3.back;
        [SerializeField, Min(0f)] private float _range = 18f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _fireInterval = 2.2f;
        [SerializeField, Min(0f)] private float _windUpTime = 0.5f;
        [SerializeField, Min(0f)] private float _projectileSpeed = 14f;

        public Vector3 FireDirection => (transform.rotation * _fireDirection).normalized;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new ShooterComponent
            {
                FireDirection = FireDirection,
                Range = _range,
                FireInterval = _fireInterval,
                WindUpTime = _windUpTime,
                ProjectileSpeed = _projectileSpeed,
            });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawRay(transform.position, FireDirection * _range);
            Gizmos.DrawWireSphere(transform.position + FireDirection * _range, 0.4f);
        }
    }
}
