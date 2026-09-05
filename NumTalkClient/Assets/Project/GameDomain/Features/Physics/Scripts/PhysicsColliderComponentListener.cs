using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class PhysicsColliderComponentListener : ComponentListener<PhysicsColliderComponent>
    {
        [SerializeField] private BoxCollider _box;
        [SerializeField] private SphereCollider _sphere;
        [SerializeField] private CapsuleCollider _capsule;

        // One material instance per listener, created once and re-tuned in place, so the authored friction survives
        // the bake without the component holding an asset reference and without allocating per frame.
        private PhysicsMaterial _material;

        public override void UpdateView(in PhysicsColliderComponent component)
        {
            gameObject.layer = transform.parent.gameObject.layer;

            if (_material == null)
            {
                _material = new PhysicsMaterial("BakedSurface") { hideFlags = HideFlags.HideAndDontSave };
            }

            _material.dynamicFriction = component.DynamicFriction;
            _material.staticFriction = component.StaticFriction;
            _material.bounciness = component.Bounciness;
            _material.frictionCombine = (PhysicsMaterialCombine)component.FrictionCombine;
            _material.bounceCombine = (PhysicsMaterialCombine)component.BounceCombine;

            _box.enabled = component.Shape == ColliderShape.Box;
            _sphere.enabled = component.Shape == ColliderShape.Sphere;
            _capsule.enabled = component.Shape == ColliderShape.Capsule;

            switch (component.Shape)
            {
                case ColliderShape.Sphere:
                    _sphere.radius = component.Size.x * 0.5f;
                    _sphere.isTrigger = component.IsTrigger;
                    _sphere.sharedMaterial = _material;
                    break;
                case ColliderShape.Capsule:
                    _capsule.radius = component.Size.x * 0.5f;
                    _capsule.height = component.Size.y;
                    _capsule.isTrigger = component.IsTrigger;
                    _capsule.sharedMaterial = _material;
                    break;
                default:
                    _box.size = component.Size;
                    _box.isTrigger = component.IsTrigger;
                    _box.sharedMaterial = _material;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
        }
    }
}
