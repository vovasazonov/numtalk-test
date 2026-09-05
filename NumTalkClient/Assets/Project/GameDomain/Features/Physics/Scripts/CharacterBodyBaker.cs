using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>Adds a swept character mover. The runtime CharacterController is created on the entity root on demand.</summary>
    public sealed class CharacterBodyBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField, Min(0f)] private float _height = 2f;
        [SerializeField, Min(0f)] private float _radius = 0.4f;
        [SerializeField] private Vector3 _center = new(0f, 1f, 0f);
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 50f;
        [SerializeField, Min(0f)] private float _stepOffset = 0.35f;
        [SerializeField, Min(0.0001f)] private float _skinWidth = 0.04f;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new CharacterBodyComponent
            {
                Height = _height,
                Radius = _radius,
                Center = _center,
                SlopeLimit = _slopeLimit,
                StepOffset = _stepOffset,
                SkinWidth = _skinWidth,
            });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + _center, _radius);
        }
    }
}
