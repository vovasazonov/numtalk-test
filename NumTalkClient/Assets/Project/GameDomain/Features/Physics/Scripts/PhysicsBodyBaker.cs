using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>Adds a dynamic body. The runtime Rigidbody is created on the entity root on demand.</summary>
    public sealed class PhysicsBodyBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField, Min(0.001f)] private float _mass = 6f;
        [SerializeField] private bool _freezeRotation = true;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PhysicsBodyComponent
            {
                Mass = _mass,
                FreezeRotation = _freezeRotation,
            });
        }
    }
}
