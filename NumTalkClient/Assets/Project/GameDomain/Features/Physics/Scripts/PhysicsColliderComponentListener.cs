using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class PhysicsColliderComponentListener : ComponentListener<PhysicsColliderComponent>
    {
        [SerializeField] private BoxCollider _box;
        [SerializeField] private SphereCollider _sphere;
        [SerializeField] private CapsuleCollider _capsule;

        [SerializeField] private PhysicsMaterial _defaultMaterial;

        public override void UpdateView(in PhysicsColliderComponent component)
        {
            gameObject.layer = transform.parent.gameObject.layer;

            _box.enabled = component.Shape == ColliderShape.Box;
            _sphere.enabled = component.Shape == ColliderShape.Sphere;
            _capsule.enabled = component.Shape == ColliderShape.Capsule;

            switch (component.Shape)
            {
                case ColliderShape.Sphere:
                    _sphere.radius = component.Size.x * 0.5f;
                    _sphere.isTrigger = component.IsTrigger;
                    _sphere.sharedMaterial = _defaultMaterial;
                    break;
                case ColliderShape.Capsule:
                    _capsule.radius = component.Size.x * 0.5f;
                    _capsule.height = component.Size.y;
                    _capsule.isTrigger = component.IsTrigger;
                    _capsule.sharedMaterial = _defaultMaterial;
                    break;
                default:
                    _box.size = component.Size;
                    _box.isTrigger = component.IsTrigger;
                    _box.sharedMaterial = _defaultMaterial;
                    break;
            }
        }
    }
}
