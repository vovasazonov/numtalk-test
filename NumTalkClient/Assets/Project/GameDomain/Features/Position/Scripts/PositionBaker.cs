using Arch.Unity.Conversion;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Position.Scripts
{
    public sealed class PositionBaker : MonoBehaviour, IComponentConverter
    {
        public void Convert(IEntityConverter converter)
        {
            Vector3 position = transform.position;
            converter.AddComponent(new PositionComponent
            {
                Position = new float3(position.x, position.y, position.z),
            });
        }
    }
}
