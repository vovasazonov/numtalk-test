using Arch.Unity.Conversion;
using Project.GameDomain.Features.Course.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Pickup.Scripts
{
    public sealed class CoinBaker : MonoBehaviour, IComponentConverter
    {
        [Tooltip("Stable id used by the checkpoint snapshot. Must be unique within the course.")]
        [SerializeField] private int _id;
        [SerializeField, Min(1)] private int _value = 1;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Pickup");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PickupComponent { Id = _id, Value = _value });
            converter.AddComponent(new InitialStateComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
            });
        }
    }
}
