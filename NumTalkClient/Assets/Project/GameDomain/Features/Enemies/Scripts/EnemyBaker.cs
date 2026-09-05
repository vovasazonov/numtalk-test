using Arch.Unity.Conversion;
using Project.GameDomain.Features.Course.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Shared enemy authoring. Add <see cref="PatrolBaker"/> or <see cref="ShooterBaker"/> to the same object for
    /// behaviour; both compose on one enemy if that is what the course needs.
    /// </summary>
    public sealed class EnemyBaker : MonoBehaviour, IComponentConverter
    {
        [Tooltip("A stompable enemy dies to a top-down hit and hurts the player from the side or below.")]
        [SerializeField] private bool _isStompable = true;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new EnemyComponent());
            converter.AddComponent(new InitialStateComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
            });

            if (_isStompable)
            {
                converter.AddComponent(new StompTargetComponent());
            }
        }
    }
}
