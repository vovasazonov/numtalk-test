using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class RigidbodyBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField] private bool _isGravityEnabled = true;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new RigidbodyComponent
            {
                IsGravityEnabled = _isGravityEnabled,
            });
        }
    }
}
