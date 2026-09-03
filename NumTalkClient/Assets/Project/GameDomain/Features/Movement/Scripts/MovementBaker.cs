using Arch.Unity.Conversion;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Movement.Scripts
{
    public sealed class MovementBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField] private Vector3 _velocity = new(0.5f, 0f, 0f);

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new MovementComponent
            {
                Velocity = new float3(_velocity.x, _velocity.y, _velocity.z),
            });
        }
    }
}
