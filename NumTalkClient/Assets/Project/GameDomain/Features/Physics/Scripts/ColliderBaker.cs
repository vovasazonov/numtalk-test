using Arch.Unity.Conversion;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class ColliderBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField] private Vector3 _size = new(0.25f, 0.25f, 0.25f);

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new ColliderComponent
            {
                Size = new float3(_size.x, _size.y, _size.z),
            });
        }
    }
}
