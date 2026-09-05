using Arch.Unity.Conversion;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>Captures the authored collider so the runtime view can rebuild an identical volume from ECS data.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PhysicsColliderBaker : MonoBehaviour, IComponentConverter
    {
        public void Convert(IEntityConverter converter)
        {
            Collider collider = GetComponent<Collider>();
            float3 scale = transform.lossyScale;

            switch (collider)
            {
                case SphereCollider sphere:
                    converter.AddComponent(new PhysicsColliderComponent
                    {
                        Shape = ColliderShape.Sphere,
                        Size = sphere.radius * 2f * scale,
                        IsTrigger = sphere.isTrigger,
                    });
                    break;
                case CapsuleCollider capsule:
                    converter.AddComponent(new PhysicsColliderComponent
                    {
                        Shape = ColliderShape.Capsule,
                        Size = new float3(capsule.radius * 2f * scale.x, capsule.height * scale.y, capsule.radius * 2f * scale.z),
                        IsTrigger = capsule.isTrigger,
                    });
                    break;
                case BoxCollider box:
                    converter.AddComponent(new PhysicsColliderComponent
                    {
                        Shape = ColliderShape.Box,
                        Size = (float3)box.size * scale,
                        IsTrigger = box.isTrigger,
                    });
                    break;
                default:
                    Debug.LogWarning($"'{name}' has an unsupported collider '{collider.GetType().Name}'. Not baked.", this);
                    break;
            }
        }
    }
}
